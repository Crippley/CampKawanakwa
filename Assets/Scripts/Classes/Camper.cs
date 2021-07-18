using System.Collections.Generic;
using Core;
using Items;
using Unity.MLAgents;
using Unity.MLAgents.Actuators;
using Unity.MLAgents.Sensors;
using UnityEngine;

namespace Entities
{
    public class Camper : Agent, IDetectionTriggerHandler
    {
        #region Vars
        [SerializeField] private float movementSpeed;
        [SerializeField] private float rotationSpeed;

        [SerializeField] private float objectiveFoundReward;
        [SerializeField] private float objectiveLostReward;
        [SerializeField] private float objectivePickupReward;
        [SerializeField] private float objectiveDropOffReward;
        [SerializeField] private float seeingKillerReward;
        [SerializeField] private float loosingKillerReward;
        [SerializeField] private float deathReward;
        [SerializeField] private float timeReward;

        [SerializeField] private LayerMask noHeldItemIgnoreLayerMask;
        [SerializeField] private LayerMask heldItemIgnoreLayerMask;
        [SerializeField] private DetectionZone visionZone;

        private Dictionary<Camper, List<Collider2D>> visibleCampers = new Dictionary<Camper, List<Collider2D>>();
        private Dictionary<Objective, List<Collider2D>> visibleObjectives = new Dictionary<Objective, List<Collider2D>>();
        private Player visibleKiller;
        private Player visibleDropOffZone;
        private Vector3 startingPosition;

        private Vector3 movementVector;
        private Quaternion turningRotation;
        #endregion

        #region Initialization
        private void Start() 
        {
            startingPosition = transform.position;
        }
        #endregion

        #region Item holding
        private Objective heldItem;

        public Objective HeldItem => heldItem;

        public void AddItem(Objective item)
        {
            heldItem = item;
            AddReward(objectivePickupReward);
            visionZone.SetIgnoreLayerMask(heldItemIgnoreLayerMask);
        }

        public void RemoveItem(bool success)
        {
            if (success)
                AddReward(objectiveDropOffReward);
            else
                heldItem.IsActive = true;

            heldItem = null;
            visionZone.SetIgnoreLayerMask(noHeldItemIgnoreLayerMask);
        }

        public void GetKilled()
        {
            AddReward(deathReward);

            if (heldItem)
            {
                heldItem.transform.parent = null;
                RemoveItem(false);
            }

            Debug.Log("Camper " + name + "'s episode ended");
            EndEpisode();
            gameObject.SetActive(false);
        }
        #endregion

        #region Agent code
        public override void OnEpisodeBegin()
        {
            Debug.Log("Camper " + name + "'s episode started");
            transform.position = AgentManager.Instance.GetRandomCamperSpawnPosition();
            heldItem = null;
        }

        public override void CollectObservations(VectorSensor sensor)
        {
            sensor.AddObservation(transform.position);

            foreach (KeyValuePair<Camper, List<Collider2D>> value in visibleCampers)
                sensor.AddObservation(value.Key.transform.position);

            foreach (KeyValuePair<Objective, List<Collider2D>> value in visibleObjectives)
                sensor.AddObservation(value.Key.transform.position);

            if (visibleKiller != null)
                sensor.AddObservation(visibleKiller.transform.position);

            if (visibleDropOffZone != null)
                sensor.AddObservation(visibleDropOffZone.transform.position);
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

            continuousActions[2] = -angle;
        }*/

        public override void OnActionReceived(ActionBuffers actions)
        {
            float moveX = actions.ContinuousActions[0];
            float moveY = actions.ContinuousActions[1];
            
            movementVector = new Vector3(moveX, moveY, 0f);
            movementVector = Vector3.ClampMagnitude(movementVector, 1f);

            float rotateZ = actions.ContinuousActions[2];

            turningRotation = Quaternion.identity;
            turningRotation *= Quaternion.Euler(0f, 0f, rotateZ);

            AddReward(timeReward);
        }

        private void FixedUpdate() 
        {
            transform.position += movementVector * movementSpeed * Time.fixedDeltaTime;
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

                    if (!visibleObjectives.ContainsKey(detectedObjective))
                        visibleObjectives.Add(detectedObjective, new List<Collider2D>());
                    else if (visibleObjectives[detectedObjective].Contains(detectingCollider))
                        return;

                    visibleObjectives[detectedObjective].Add(detectingCollider);

                    // NOTE: Reason why we double-up on the reward is because we want the campers to turn towards the objective after finding it
                    AddReward(objectiveFoundReward);
                }
                else
                {
                    Player detectedKiller = detectedObject.GetComponent<Player>();

                    if (detectedKiller == null || visibleKiller != null)
                        return;

                    visibleKiller = detectedKiller;

                    // NOTE: Reason why we double-up on the reward is because we want the campers not let the killer approach them
                    AddReward(seeingKillerReward);
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
                Objective detectedObjective = detectedObject.GetComponent<Objective>();

                if (detectedObjective != null)
                {
                    List<Collider2D> colliders;

                    if (!visibleObjectives.TryGetValue(detectedObjective, out colliders))
                        return;
                
                    if (colliders.Count > 1)
                        visibleObjectives[detectedObjective].Remove(detectingCollider);
                    else
                        visibleObjectives.Remove(detectedObjective);

                    // NOTE: Reason why we double-up on the reward is because we want the campers to look at and pick up the objectives
                    AddReward(objectiveLostReward);
                }
                else
                {
                    Player detectedKiller = detectedObject.GetComponent<Player>();

                    if (detectedKiller == null || visibleKiller == null)
                        return;

                    visibleKiller = null;

                    // NOTE: Reason why we double-up on the reward is because we want the campers to run away from the killer
                    AddReward(loosingKillerReward);
                }
            }   
        }
        #endregion
    }
}
