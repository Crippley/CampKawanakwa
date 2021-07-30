using System.Collections.Generic;
using Core;
using Items;
using Unity.MLAgents;
using Unity.MLAgents.Actuators;
using Unity.MLAgents.Sensors;
using UnityEngine;
using Zones;

namespace Entities
{
    public class Camper : Agent, IDetectionTriggerHandler
    {
        #region Vars
        public Rigidbody2D rb;

        [SerializeField] private float movementSpeed;
        [SerializeField] private float rotationSpeed;
        [SerializeField] private float maxSeeingDistance;

        [Header("Position min/max")]
        [SerializeField] private float maxXPosition = 11f;
        [SerializeField] private float minXPosition = -11f;

        [SerializeField] private float maxYPosition = 18f;
        [SerializeField] private float minYPosition = -18f;

        [Header("Velocity min/max")]
        [SerializeField] private float maxXVelocity = 14f;
        [SerializeField] private float minXVelocity = -14f;

        [SerializeField] private float maxYVelocity = 14f;
        [SerializeField] private float minYVelocity = -14f;

        [Header("Killer velocity min/max")]
        [SerializeField] private float killerMaxXVelocity = 12f;
        [SerializeField] private float killerMinXVelocity = -12f;

        [SerializeField] private float killerMaxYVelocity = 12f;
        [SerializeField] private float killerMinYVelocity = -12f;

        [Header("Currently active rewards")]
        [SerializeField] private float seeingCamperDistanceBasedReward;
        [SerializeField] private float seeingObjectiveDistanceBasedReward;
        [SerializeField] private float objectivePickupReward;
        [SerializeField] private float objectiveDropOffReward;
        [SerializeField] private float seeingDropOffZoneDistanceBasedReward;
        [SerializeField] private float seeingKillerDistanceBasedReward;
        [SerializeField] private float deathReward;
        [SerializeField] private float timeReward;

        [Header("Currently inactive rewards")]
        [SerializeField] private float objectiveFoundReward;
        [SerializeField] private float objectiveLostReward;
        [SerializeField] private float dropOffZoneFoundReward;
        [SerializeField] private float dropOffZoneLostReward;
        [SerializeField] private float seeingKillerReward;
        [SerializeField] private float loosingKillerReward;

        [SerializeField] private LayerMask noHeldItemIgnoreLayerMask;
        [SerializeField] private LayerMask heldItemIgnoreLayerMask;

        [SerializeField] private LayerMask heldItemLayerMaskAfterPickup;
        [SerializeField] private LayerMask heldItemLayerMaskAfterDropoff;

        [SerializeField] private DetectionZone visionZone;
        [SerializeField] private DetectionZone closeProximityZone;

        private Dictionary<Camper, List<Collider2D>> visibleCampers = new Dictionary<Camper, List<Collider2D>>();
        private Dictionary<Player, List<Collider2D>> visibleKillers = new Dictionary<Player, List<Collider2D>>();

        private Vector3 movementVector;
        private Quaternion turningRotation;
        private int lastEpisodeCount = -1;
        #endregion

        #region Item holding
        private Objective heldItem;

        public Objective HeldItem => heldItem;

        public void AddItem(Objective item)
        {
            heldItem = item;
            heldItem.gameObject.layer = (int) Mathf.Log(heldItemLayerMaskAfterPickup, 2);

            AgentManager.Instance.camperAgentGroup.AddGroupReward(objectivePickupReward);
            AgentManager.Instance.currentObjectivePickedUpRewards += objectivePickupReward;

            visionZone.SetIgnoreLayerMask(heldItemIgnoreLayerMask);
            visionZone.gameObject.SetActive(false);
            visionZone.gameObject.SetActive(true);

            closeProximityZone.SetIgnoreLayerMask(heldItemIgnoreLayerMask);
            closeProximityZone.gameObject.SetActive(false);
            closeProximityZone.gameObject.SetActive(true);

            if (AgentManager.Instance.knownObjectives.ContainsKey(item))
                AgentManager.Instance.knownObjectives.Remove(item);
        }

        public void RemoveItem(bool success)
        {
            visionZone.SetIgnoreLayerMask(noHeldItemIgnoreLayerMask);
            visionZone.gameObject.SetActive(false);
            visionZone.gameObject.SetActive(true);

            closeProximityZone.SetIgnoreLayerMask(noHeldItemIgnoreLayerMask);
            closeProximityZone.gameObject.SetActive(false);
            closeProximityZone.gameObject.SetActive(true);

            if (heldItem)
                heldItem.transform.parent = heldItem.initialParent;
            else
                return;

            if (success)
            {
                AgentManager.Instance.camperAgentGroup.AddGroupReward(objectiveDropOffReward);
                AgentManager.Instance.currentObjectiveDroppedOffRewards += objectiveDropOffReward;
            }
            else
            {
                heldItem.IsActive = true;
            }

            heldItem.gameObject.layer = (int) Mathf.Log(heldItemLayerMaskAfterDropoff, 2);
            heldItem = null;
        }

        public void GetKilled()
        {
            AddReward(deathReward);
            AgentManager.Instance.currentDeathRewards += deathReward;

            if (heldItem)
                RemoveItem(false);

            Debug.Log("Camper " + name + "'s episode ended");
            gameObject.SetActive(false);
        }
        #endregion

        #region Agent code
        public override void OnEpisodeBegin()
        {
            if (lastEpisodeCount == AgentManager.Instance.currentEpisodeCount)
                return;
            
            Debug.Log("Camper " + name + "'s episode started");

            visibleKillers.Clear();
            visibleCampers.Clear();
            transform.position = AgentManager.Instance.GetRandomCamperSpawnPosition();
            heldItem = null;
            lastEpisodeCount = AgentManager.Instance.currentEpisodeCount;
        }

        public override void CollectObservations(VectorSensor sensor)
        {
            sensor.AddObservation((Mathf.Clamp(transform.position.x, minXPosition, maxXPosition) - minXPosition) / (maxXPosition - minXPosition));
            sensor.AddObservation((Mathf.Clamp(transform.position.y, minYPosition, maxYPosition) - minYPosition) / (maxYPosition - minYPosition));

            sensor.AddObservation((rb.velocity.x - minXVelocity) / (maxXVelocity - minXVelocity));
            sensor.AddObservation((rb.velocity.y - minYVelocity) / (maxYVelocity - minYVelocity));

            foreach (KeyValuePair<Camper, List<Collider2D>> value in visibleCampers)
            {
                sensor.AddObservation(Vector3.SignedAngle(transform.forward, value.Key.transform.position - transform.position, Vector3.forward) / 180f);

                Vector3 camperVelocity = value.Key.rb.velocity;

                sensor.AddObservation((camperVelocity.x - minXVelocity) / (maxXVelocity - minXVelocity));
                sensor.AddObservation((camperVelocity.y - minYVelocity) / (maxYVelocity - minYVelocity));

                float reward = seeingCamperDistanceBasedReward * (maxSeeingDistance - Mathf.Clamp(Vector3.Distance(transform.position, value.Key.transform.position), 0, maxSeeingDistance - 1));
                AddReward(reward);
            }

            foreach (KeyValuePair<Objective, Vector2> value in AgentManager.Instance.knownObjectives)
            {
                sensor.AddObservation(Vector3.SignedAngle(transform.forward, value.Key.transform.position - transform.position, Vector3.forward) / 180f);

                if (Vector2.Distance(transform.position, value.Value) < maxSeeingDistance)
                {
                    float reward = seeingObjectiveDistanceBasedReward * (maxSeeingDistance - Mathf.Clamp(Vector3.Distance(transform.position, value.Key.transform.position), 0, maxSeeingDistance - 1));
                    AddReward(reward);
                    AgentManager.Instance.currentObjectiveMaintainedVisionRewards += reward;
                }
            }

            foreach (KeyValuePair<DropOffZone, Vector2> value in AgentManager.Instance.knownZones)
            {
                sensor.AddObservation(Vector3.SignedAngle(transform.forward, value.Key.transform.position - transform.position, Vector3.forward) / 180f);

                if (Vector2.Distance(transform.position, value.Value) < maxSeeingDistance)
                {
                    float reward = seeingDropOffZoneDistanceBasedReward * (maxSeeingDistance - Mathf.Clamp(Vector3.Distance(transform.position, value.Key.transform.position), 0, maxSeeingDistance - 1));
                    AddReward(reward);
                    AgentManager.Instance.currentDropOffZoneMaintainedVisionRewards += reward;
                }
            }

            foreach (KeyValuePair<Player, Vector2> value in AgentManager.Instance.visibleKillers)
            {
                sensor.AddObservation(Vector3.SignedAngle(transform.forward, value.Key.transform.position - transform.position, Vector3.forward) / 180f);

                Vector3 killerVelocity = value.Key.rb.velocity;

                sensor.AddObservation((killerVelocity.x - killerMinXVelocity) / (killerMaxXVelocity - killerMinXVelocity));
                sensor.AddObservation((killerVelocity.y - killerMinYVelocity) / (killerMaxYVelocity - killerMinYVelocity));

                if (Vector2.Distance(transform.position, value.Value) < maxSeeingDistance)
                {
                    float reward = seeingKillerDistanceBasedReward * (maxSeeingDistance - Mathf.Clamp(Vector3.Distance(transform.position, value.Key.transform.position), 0, maxSeeingDistance - 1));
                    AddReward(reward);
                    AgentManager.Instance.currentKillerMaintainedVisionRewards += reward;
                }
            }

            sensor.AddObservation(heldItem != null);
        }

        /*public override void Heuristic(in ActionBuffers actionsOut)
        {
            ActionSegment<float> continuousActions = actionsOut.ContinuousActions;

            continuousActions[0] = Input.GetAxisRaw("Horizontal");
            continuousActions[1] = Input.GetAxisRaw("Vertical");
            
            Vector3 mousePosition = Input.mousePosition;
            Vector2 onScreenPosition = Camera.main.WorldToScreenPoint(transform.position);
            Vector2 offset = new Vector2(mousePosition.x - onScreenPosition.x, mousePosition.y - onScreenPosition.y);
            float angle = Mathf.Atan2(offset.x, offset.y) * Mathf.Rad2Deg;

            continuousActions[2] = -angle / 180f;
        }*/

        public override void OnActionReceived(ActionBuffers actions)
        {
            float moveX = actions.ContinuousActions[0];
            float moveY = actions.ContinuousActions[1];
            
            movementVector = new Vector3(moveX, moveY, 0f);
            movementVector = movementVector.normalized;

            float rotateZ = actions.ContinuousActions[2] * 180f;

            turningRotation = Quaternion.identity;
            turningRotation *= Quaternion.Euler(0f, 0f, rotateZ);

            AddReward(timeReward);
        }

        private void FixedUpdate() 
        {
            rb.AddRelativeForce(movementVector * movementSpeed, ForceMode2D.Impulse);
            transform.rotation = Quaternion.Slerp(transform.rotation, turningRotation, rotationSpeed * Time.fixedDeltaTime);
        }
        #endregion

        #region IDetectionTriggerHandler methods
        public void OnEnterDetectionZone(GameObject detectedObject, Collider2D detectingCollider)
        {
            Camper detectedCamper = detectedObject.GetComponent<Camper>();

            if (detectedCamper != null)
            {
                if (detectedCamper == this)
                    return;

                if (!visibleCampers.ContainsKey(detectedCamper))
                    visibleCampers.Add(detectedCamper, new List<Collider2D>());

                if (visibleCampers[detectedCamper].Contains(detectingCollider))
                    return;

                visibleCampers[detectedCamper].Add(detectingCollider);
            }
            else
            {
                Objective detectedObjective = detectedObject.GetComponent<Objective>();

                if (detectedObjective != null)
                {
                    if (detectedObjective.IsCompleted || !detectedObjective.IsActive)
                        return;

                    if (!AgentManager.Instance.knownObjectives.ContainsKey(detectedObjective))
                        AgentManager.Instance.knownObjectives.Add(detectedObjective, detectedObjective.transform.position);
                    else
                        AgentManager.Instance.knownObjectives[detectedObjective] = detectedObjective.transform.position;
                }
                else
                {
                    DropOffZone detectedDropOffZone = detectedObject.GetComponent<DropOffZone>();

                    if (detectedDropOffZone != null)
                    {
                        if (!AgentManager.Instance.knownZones.ContainsKey(detectedDropOffZone))
                            AgentManager.Instance.knownZones.Add(detectedDropOffZone, detectedDropOffZone.transform.position);
                        else
                            AgentManager.Instance.knownZones[detectedDropOffZone] = detectedDropOffZone.transform.position;
                    }
                    else
                    {
                        Player detectedKiller = detectedObject.GetComponent<Player>();

                        if (detectedKiller == null)
                            return;

                        if (!visibleKillers.ContainsKey(detectedKiller))
                            visibleKillers.Add(detectedKiller, new List<Collider2D>());
                        else if (visibleKillers[detectedKiller].Contains(detectingCollider))
                            return;

                        visibleKillers[detectedKiller].Add(detectingCollider);

                        if (!AgentManager.Instance.visibleKillers.ContainsKey(detectedKiller))
                            AgentManager.Instance.visibleKillers.Add(detectedKiller, detectedKiller.transform.position);
                        else
                            AgentManager.Instance.visibleKillers[detectedKiller] = detectedKiller.transform.position;
                    }
                }
            }
        }

        public void OnLeaveDetectionZone(GameObject detectedObject, Collider2D detectingCollider)
        {
            Camper detectedCamper = detectedObject.GetComponent<Camper>();

            if (detectedCamper != null)
            {
                if (detectedCamper == this)
                    return;

                List<Collider2D> colliders;

                if (!visibleCampers.TryGetValue(detectedCamper, out colliders))
                    return;
                
                if (colliders.Count > 1)
                    visibleCampers[detectedCamper].Remove(detectingCollider);
                else
                    visibleCampers.Remove(detectedCamper);
            }
            else
            {
                Player detectedKiller = detectedObject.GetComponent<Player>();

                if (detectedKiller == null)
                    return;

                List<Collider2D> colliders;

                if (!visibleKillers.TryGetValue(detectedKiller, out colliders))
                    return;
                    
                if (colliders.Count > 1)
                {
                    visibleKillers[detectedKiller].Remove(detectingCollider);
                }
                else
                {
                    visibleKillers.Remove(detectedKiller);
                    List<Camper> allCampers = AgentManager.Instance.campers;

                    bool keepGlobalKillerReference = false;
                    for (int i = 0; i < allCampers.Count; i++)
                    {
                        if (allCampers[i].CanSeeKiller(detectedKiller))
                            keepGlobalKillerReference = true;
                    }

                    if (!keepGlobalKillerReference)
                        AgentManager.Instance.visibleKillers.Remove(detectedKiller);
                }
            }   
        }
        #endregion

        public bool CanSeeKiller(Player killer)
        {
            return visibleKillers.ContainsKey(killer);
        }
    }
}
