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

        [Header("Currently active rewards")]
        [SerializeField] private float objectivePickupReward;
        [SerializeField] private float objectiveDropOffReward;
        [SerializeField] private float deathReward;
        [SerializeField] private float deathTeamReward;
        [SerializeField] private float timeReward;

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
            AgentManager.Instance.currentObjectivePickedUpRewards += objectivePickupReward;
        }

        public void RemoveItem(bool success)
        {
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

            heldItem = null;
        }

        public void GetKilled()
        {
            AddReward(deathReward);
            AgentManager.Instance.camperAgentGroup.AddGroupReward(deathTeamReward);
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
    }
}
