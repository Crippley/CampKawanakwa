using System.Collections.Generic;
using Unity.MLAgents;
using Unity.MLAgents.Actuators;
using Unity.MLAgents.Sensors;
using UnityEngine;
using Core;
using Unity.MLAgents.Policies;
using System;

namespace Entities
{
    public class Player : Agent, IDetectionTriggerHandler
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
        [SerializeField] private float maxXVelocity = 12f;
        [SerializeField] private float minXVelocity = -12f;

        [SerializeField] private float maxYVelocity = 12f;
        [SerializeField] private float minYVelocity = -12f;

        [Header("Camper velocity min/max")]
        [SerializeField] private float camperMaxXVelocity = 14f;
        [SerializeField] private float camperMinXVelocity = -14f;

        [SerializeField] private float camperMaxYVelocity = 14f;
        [SerializeField] private float camperMinYVelocity = -14f;

        [Header("Currently active rewards")]
        [SerializeField] private float killCamperReward;
        [SerializeField] private float killCamperWithItemReward;
        [SerializeField] private float timeReward;

        [Header("Currently inactive rewards")]
        [SerializeField] private float findCamperReward;
        [SerializeField] private float loseCamperReward;
        [SerializeField] private float seeingCamperDistanceBasedReward;

        [SerializeField] private BehaviorType behaviorType;

        public BehaviorType BehaviorType => behaviorType;

        private Dictionary<Camper, List<Collider2D>> visibleCampers = new Dictionary<Camper, List<Collider2D>>();

        private Vector3 movementVector;
        private Quaternion turningRotation;

        [NonSerialized] public float killedCamperCount;
        private int lastEpisodeCount = -1;
        #endregion

        #region Camper detection
        private void OnCollisionEnter2D(Collision2D other) 
        {
            Camper collidingCamper = other.gameObject?.GetComponent<Camper>();

            if (collidingCamper)
            {
                float reward;
                if (collidingCamper.HeldItem != null)
                    reward = killCamperWithItemReward;
                else
                    reward = killCamperReward;

                visibleCampers.Remove(collidingCamper);
                collidingCamper.GetKilled();

                AddReward(reward);
                AgentManager.Instance.currentKillRewards += killCamperReward;

                killedCamperCount++;

                if (AgentManager.Instance.campers.Count == killedCamperCount)
                    AgentManager.InvokeEpisodeEnd();
            }
        }
        #endregion

        #region Agent
        public override void OnEpisodeBegin()
        {
            if (lastEpisodeCount == AgentManager.Instance.currentEpisodeCount)
                return;

            Debug.Log("Killer's episode started");
            
            transform.position = AgentManager.Instance.GetRandomKillerSpawnPosition();
            visibleCampers = new Dictionary<Camper, List<Collider2D>>();
            killedCamperCount = 0;
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

                sensor.AddObservation((camperVelocity.x - camperMinXVelocity) / (camperMaxXVelocity - camperMinXVelocity));
                sensor.AddObservation((camperVelocity.y - camperMinYVelocity) / (camperMaxYVelocity - camperMinYVelocity));

                //float reward = seeingCamperDistanceBasedReward * (maxSeeingDistance - Mathf.Clamp(Vector3.Distance(transform.position, value.Key.transform.position), 0, maxSeeingDistance - 1));
                //AddReward(reward);
                //AgentManager.Instance.currentMaintainCamperVisionRewards += reward;
            }
        }

        /*public override void Heuristic(in ActionBuffers actionsOut)
        {
            ActionSegment<float> continuousActions = actionsOut.ContinuousActions;

            continuousActions[0] = Input.GetAxis("Horizontal");
            continuousActions[1] = Input.GetAxis("Vertical");

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

            if(detectedCamper == null)
                return;

            if (!visibleCampers.ContainsKey(detectedCamper))
                visibleCampers.Add(detectedCamper, new List<Collider2D>());

            if (visibleCampers[detectedCamper].Contains(detectingCollider))
                return;

            visibleCampers[detectedCamper].Add(detectingCollider);

            // NOTE: Reason why we double-up on the reward is because we want the killer to turn towards the camper if he detects him via proximity or to catch up to him if he detects with via vision
            //AddReward(findCamperReward);
            //AgentManager.Instance.currentFindCamperRewards += findCamperReward;
        }

        public void OnLeaveDetectionZone(GameObject detectedObject, Collider2D detectingCollider)
        {
            Camper detectedCamper = detectedObject.GetComponent<Camper>();

            if(detectedCamper == null)
                return;

            List<Collider2D> colliders;

            if (!visibleCampers.TryGetValue(detectedCamper, out colliders))
                return;
                
            if (colliders.Count > 1)
                visibleCampers[detectedCamper].Remove(detectingCollider);
            else
                visibleCampers.Remove(detectedCamper);

            // NOTE: Reason why we double-up on the punishment is because we want the killer to turn try to catch up to the camper AND keep them in their vision
            /*if (detectedCamper.gameObject.activeInHierarchy)
            {
                AddReward(loseCamperReward);
                AgentManager.Instance.currentLoseCamperRewards += loseCamperReward;
            }*/
        }
        #endregion
    }
}
