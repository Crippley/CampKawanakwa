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
        [SerializeField] private float objectivePickupReward;
        [SerializeField] private float objectiveDropOffReward;
        [SerializeField] private float deathReward;

        [Header("Detection zone replacement layer masks")]
        [SerializeField] private LayerMask noHeldItemIgnoreLayerMask;
        [SerializeField] private LayerMask heldItemIgnoreLayerMask;

        [SerializeField] private LayerMask heldItemLayerMaskAfterPickup;
        [SerializeField] private LayerMask heldItemLayerMaskAfterDropoff;

        [SerializeField] private DetectionZone visionZone;
        [SerializeField] private DetectionZone closeProximityZone;

        private Dictionary<Camper, List<Collider2D>> visibleCampers = new Dictionary<Camper, List<Collider2D>>();
        private Dictionary<Objective, List<Collider2D>> visibleObjectives = new Dictionary<Objective, List<Collider2D>>();
        private Dictionary<DropOffZone, List<Collider2D>> visibleDropOffZones = new Dictionary<DropOffZone, List<Collider2D>>();
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
            AddReward(objectivePickupReward);
            AgentManager.Instance.currentObjectivePickedUpRewards += objectivePickupReward;

            visionZone.SetIgnoreLayerMask(heldItemIgnoreLayerMask);
            visionZone.gameObject.SetActive(false);
            visionZone.gameObject.SetActive(true);

            closeProximityZone.SetIgnoreLayerMask(heldItemIgnoreLayerMask);
            closeProximityZone.gameObject.SetActive(false);
            closeProximityZone.gameObject.SetActive(true);

            visibleObjectives.Remove(item);
        }

        public void RemoveItem(bool success)
        {
            if (heldItem)
            {
                visionZone.SetIgnoreLayerMask(noHeldItemIgnoreLayerMask);
                visionZone.gameObject.SetActive(false);
                visionZone.gameObject.SetActive(true);

                closeProximityZone.SetIgnoreLayerMask(noHeldItemIgnoreLayerMask);
                closeProximityZone.gameObject.SetActive(false);
                closeProximityZone.gameObject.SetActive(true);
                
                visibleDropOffZones.Clear();

                heldItem.transform.parent = heldItem.initialParent;

                if (success)
                {
                    AgentManager.Instance.camperAgentGroup.AddGroupReward(objectiveDropOffReward);
                    AddReward(objectiveDropOffReward);
                    AgentManager.Instance.currentObjectiveDroppedOffRewards += objectiveDropOffReward;
                }
                else
                {
                    heldItem.IsActive = true;
                    heldItem.transform.position = transform.position;
                    heldItem.transform.rotation = Quaternion.identity;
                }

                heldItem.gameObject.layer = (int) Mathf.Log(heldItemLayerMaskAfterDropoff, 2);
                heldItem = null;
            }
        }

        public void GetKilled()
        {
            AgentManager.Instance.camperAgentGroup.AddGroupReward(deathReward);
            AddReward(deathReward);
            AgentManager.Instance.currentDeathRewards += deathReward;
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

            transform.position = AgentManager.Instance.GetRandomCamperSpawnPosition();

            RemoveItem(false);
            visibleCampers.Clear();
            visibleObjectives.Clear();
            visibleDropOffZones.Clear();
            visibleKillers.Clear();

            lastEpisodeCount = AgentManager.Instance.currentEpisodeCount;
        }

        public override void CollectObservations(VectorSensor sensor)
        {
            sensor.AddObservation(2f * (Mathf.Clamp(transform.position.x, minXPosition, maxXPosition) - minXPosition) / (maxXPosition - minXPosition) - 1f);
            sensor.AddObservation(2f * (Mathf.Clamp(transform.position.y, minYPosition, maxYPosition) - minYPosition) / (maxYPosition - minYPosition) - 1f);

            sensor.AddObservation(2f * (rb.velocity.x - minXVelocity) / (maxXVelocity - minXVelocity) - 1f);
            sensor.AddObservation(2f * (rb.velocity.y - minYVelocity) / (maxYVelocity - minYVelocity) - 1f);

            foreach (KeyValuePair<Camper, List<Collider2D>> value in visibleCampers)
            {
                if (value.Value.Count > 0)
                {
                    sensor.AddObservation(Vector2.Dot(transform.up, (value.Key.transform.position - transform.position).normalized));
                    sensor.AddObservation(Vector2.Dot(transform.right, (value.Key.transform.position - transform.position).normalized));

                    Vector3 camperVelocity = value.Key.rb.velocity;

                    sensor.AddObservation(2f * (camperVelocity.x - minXVelocity) / (maxXVelocity - minXVelocity) - 1f);
                    sensor.AddObservation(2f * (camperVelocity.y - minYVelocity) / (maxYVelocity - minYVelocity) - 1f);
                }
            }

            foreach (KeyValuePair<Objective, List<Collider2D>> value in visibleObjectives)
            {
                if (value.Value.Count > 0)
                {
                    sensor.AddObservation(Vector2.Dot(transform.up, (value.Key.transform.position - transform.position).normalized));
                    sensor.AddObservation(Vector2.Dot(transform.right, (value.Key.transform.position - transform.position).normalized));
                }
            }

            foreach (KeyValuePair<DropOffZone, List<Collider2D>> value in visibleDropOffZones)
            {
                if (value.Value.Count > 0)
                {
                    sensor.AddObservation(Vector2.Dot(transform.up, (value.Key.transform.position - transform.position).normalized));
                    sensor.AddObservation(Vector2.Dot(transform.right, (value.Key.transform.position - transform.position).normalized));
                }
            }

            foreach (KeyValuePair<Player, List<Collider2D>> value in visibleKillers)
            {
                if (value.Value.Count > 0)
                {
                    sensor.AddObservation(Vector2.Dot(transform.up, (value.Key.transform.position - transform.position).normalized));
                    sensor.AddObservation(Vector2.Dot(transform.right, (value.Key.transform.position - transform.position).normalized));

                    Vector3 killerVelocity = value.Key.rb.velocity;

                    sensor.AddObservation(2f * (killerVelocity.x - killerMinXVelocity) / (killerMaxXVelocity - killerMinXVelocity) - 1f);
                    sensor.AddObservation(2f * (killerVelocity.y - killerMinYVelocity) / (killerMaxYVelocity - killerMinYVelocity) - 1f);
                }
            }

            sensor.AddObservation(heldItem == null ? -1f : 1f);
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
        }

        private void FixedUpdate() 
        {
            rb.AddForce(movementVector * movementSpeed, ForceMode2D.Impulse);
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
                
                if (!visibleCampers[detectedCamper].Contains(detectingCollider))
                    visibleCampers[detectedCamper].Add(detectingCollider);
            }
            else
            {
                Objective detectedObjective = detectedObject.GetComponent<Objective>();

                if (detectedObjective != null)
                {
                    if (detectedObjective.IsCompleted || !detectedObjective.IsActive)
                        return;

                    if (!visibleObjectives.ContainsKey(detectedObjective))
                        visibleObjectives.Add(detectedObjective, new List<Collider2D>());
                    
                    if (!visibleObjectives[detectedObjective].Contains(detectingCollider))
                        visibleObjectives[detectedObjective].Add(detectingCollider);
                }
                else
                {
                    DropOffZone detectedDropOffZone = detectedObject.GetComponent<DropOffZone>();

                    if (detectedDropOffZone != null)
                    {
                        if (!visibleDropOffZones.ContainsKey(detectedDropOffZone))
                            visibleDropOffZones.Add(detectedDropOffZone, new List<Collider2D>());
                        
                        if (!visibleDropOffZones[detectedDropOffZone].Contains(detectingCollider))
                            visibleDropOffZones[detectedDropOffZone].Add(detectingCollider);
                    }
                    else
                    {
                        Player detectedKiller = detectedObject.GetComponent<Player>();

                        if (detectedKiller == null)
                            return;

                        if (!visibleKillers.ContainsKey(detectedKiller))
                            visibleKillers.Add(detectedKiller, new List<Collider2D>());
                        
                        if (!visibleKillers[detectedKiller].Contains(detectingCollider))
                            visibleKillers[detectedKiller].Add(detectingCollider);
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
                
                if (visibleCampers[detectedCamper].Contains(detectingCollider))
                    visibleCampers[detectedCamper].Remove(detectingCollider);
            }
            else
            {
                Objective detectedObjective = detectedObject.GetComponent<Objective>();

                if (detectedObjective != null)
                {
                    List<Collider2D> colliders;

                    if (!visibleObjectives.TryGetValue(detectedObjective, out colliders))
                        return;
                
                    if (visibleObjectives[detectedObjective].Contains(detectingCollider))
                        visibleObjectives[detectedObjective].Remove(detectingCollider);
                }
                else
                {
                    DropOffZone detectedDropOffZone = detectedObject.GetComponent<DropOffZone>();

                    if (detectedDropOffZone != null)
                    {
                        List<Collider2D> colliders;

                        if (!visibleDropOffZones.TryGetValue(detectedDropOffZone, out colliders))
                            return;

                        if (visibleDropOffZones[detectedDropOffZone].Contains(detectingCollider))
                            visibleDropOffZones[detectedDropOffZone].Remove(detectingCollider);
                    }
                    else
                    {
                        Player detectedKiller = detectedObject.GetComponent<Player>();

                        if (detectedKiller == null)
                            return;

                        List<Collider2D> colliders;

                        if (!visibleKillers.TryGetValue(detectedKiller, out colliders))
                            return;

                        if (visibleKillers[detectedKiller].Contains(detectingCollider))
                            visibleKillers[detectedKiller].Remove(detectingCollider);
                    }
                }
            }   
        }
        #endregion
    }
}
