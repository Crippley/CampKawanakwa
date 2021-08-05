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
    public class Camper : Agent
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

            AgentManager.Instance.camperAgentGroup.AddGroupReward(objectivePickupReward);
            AddReward(objectivePickupReward);
            AgentManager.Instance.currentObjectivePickedUpRewards += objectivePickupReward;
        }

        public void RemoveItem(bool success)
        {
            if (heldItem)
            {
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

                heldItem = null;
            }
        }

        public void GetKilled()
        {
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
            lastEpisodeCount = AgentManager.Instance.currentEpisodeCount;
        }

        public override void CollectObservations(VectorSensor sensor)
        {
            sensor.AddObservation(heldItem == null ? -1f : 1f);
        }

        /*public override void Heuristic(in ActionBuffers actionsOut)
        {
            ActionSegment<int> continuousActions = actionsOut.DiscreteActions;

            if (Input.GetAxis("Horizontal") < 0)
            {
                continuousActions[0] = 3;
            }
            else if (Input.GetAxis("Horizontal") > 0)
            {
                continuousActions[0] = 2;
            }
            else
            {
                continuousActions[0] = 1;
            }

            if (Input.GetAxis("Vertical") < 0)
            {
                continuousActions[1] = 3;
            }
            else if (Input.GetAxis("Vertical") > 0)
            {
                continuousActions[1] = 2;
            }
            else
            {
                continuousActions[1] = 1;
            }
        }*/

        public override void OnActionReceived(ActionBuffers actions)
        {
            float moveX;
            float moveY;

            if (actions.DiscreteActions[0] <= 1)
            {
                moveX = 0;
            }
            else if (actions.DiscreteActions[0] == 2)
            {
                moveX = 1f;
            }
            else
            {
                moveX = -1f;
            }

            if (actions.DiscreteActions[1] <= 1)
            {
                moveY = 0;
            }
            else if (actions.DiscreteActions[1] == 2)
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
    }
}
