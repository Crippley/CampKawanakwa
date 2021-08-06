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

        [SerializeField] private Camera agentCamera;

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
            killedCamperCount = 0;
            lastEpisodeCount = AgentManager.Instance.currentEpisodeCount;
        }

        public override void Heuristic(in ActionBuffers actionsOut)
        {
            ActionSegment<int> continuousActions = actionsOut.DiscreteActions;

            if (Input.GetAxis("Horizontal") < 0)
            {
                continuousActions[0] = 2;
            }
            else if (Input.GetAxis("Horizontal") > 0)
            {
                continuousActions[0] = 1;
            }
            else
            {
                continuousActions[0] = 0;
            }

            if (Input.GetAxis("Vertical") < 0)
            {
                continuousActions[1] = 2;
            }
            else if (Input.GetAxis("Vertical") > 0)
            {
                continuousActions[1] = 1;
            }
            else
            {
                continuousActions[1] = 0;
            }
        }

        public override void OnActionReceived(ActionBuffers actions)
        {
            float moveX;
            float moveY;

            if (actions.DiscreteActions[0] == 0)
            {
                moveX = 0;
            }
            else if (actions.DiscreteActions[0] == 1)
            {
                moveX = 1f;
            }
            else
            {
                moveX = -1f;
            }

            if (actions.DiscreteActions[1] == 0)
            {
                moveY = 0;
            }
            else if (actions.DiscreteActions[1] == 1)
            {
                moveY = 1f;
            }
            else
            {
                moveY = -1f;
            }
            
            movementVector = new Vector3(moveX, moveY, 0f);
            movementVector = movementVector.normalized;
        }

        private void FixedUpdate() 
        {
            rb.AddForce(movementVector * movementSpeed, ForceMode2D.Impulse);
            transform.rotation = Quaternion.Slerp(transform.rotation, turningRotation, rotationSpeed * Time.fixedDeltaTime);
        }
        #endregion

        #region Agent detection

        public void OnEnterDetectionZone(GameObject detectedObject, Collider2D detectingCollider)
        {
            if (detectedObject.GetComponent<Camper>())
            {
                agentCamera.cullingMask |= (1 << detectedObject.layer);
            }
        }

        public void OnLeaveDetectionZone(GameObject detectedObject, Collider2D detectingCollider)
        {
            if (detectedObject.GetComponent<Camper>())
            {
                agentCamera.cullingMask &= ~(1 << detectedObject.layer);
            }
        }

        #endregion
    }
}
