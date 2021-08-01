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
                    AgentManager.Instance.InvokeEpisodeEnd();
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
            visibleCampers.Clear();
            killedCamperCount = 0;
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

                    sensor.AddObservation(2f * (camperVelocity.x - camperMinXVelocity) / (camperMaxXVelocity - camperMinXVelocity) - 1f);
                    sensor.AddObservation(2f * (camperVelocity.y - camperMinYVelocity) / (camperMaxYVelocity - camperMinYVelocity) - 1f);

                    sensor.AddObservation(value.Key.HeldItem == null ? -1f : 1f);
                }
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

            if(detectedCamper == null)
                return;

            if (!visibleCampers.ContainsKey(detectedCamper))
                visibleCampers.Add(detectedCamper, new List<Collider2D>());
            
            if (!visibleCampers[detectedCamper].Contains(detectingCollider))
                visibleCampers[detectedCamper].Add(detectingCollider);
        }

        public void OnLeaveDetectionZone(GameObject detectedObject, Collider2D detectingCollider)
        {
            Camper detectedCamper = detectedObject.GetComponent<Camper>();

            if(detectedCamper == null)
                return;

            List<Collider2D> colliders;
            if (!visibleCampers.TryGetValue(detectedCamper, out colliders))
                return;

            if (visibleCampers[detectedCamper].Contains(detectingCollider))
                visibleCampers[detectedCamper].Remove(detectingCollider);
        }
        #endregion
    }
}
