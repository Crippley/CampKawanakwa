using Core;
using Items;
using Unity.MLAgents;
using Unity.MLAgents.Actuators;
using Unity.MLAgents.Sensors;
using UnityEngine;

namespace Entities
{
    public class Camper : Agent
    {
        #region Vars
        [Header("Movement related values")]
        [SerializeField] private Rigidbody rb;
        [SerializeField] private float movementSpeed;
        [SerializeField] private float rotationSpeed;

        [Header("Held objective position")]
        [SerializeField] private Vector3 addedHeldObjectivePosition;

        [Header("Reward values")]
        [SerializeField] private float objectivePickupReward;
        [SerializeField] private float objectiveDropOffReward;
        [SerializeField] private float timeReward;
        [SerializeField] private float deathReward;

        [Header("Misc values")]
        [SerializeField] private float deathToTimeRatioMultiplier;

        private Vector3 movementVector;
        private Vector3 turningRotation;
        private int lastEpisodeCount = -1;
        #endregion

        #region Objective holding
        private Objective heldObjective;
        public Objective HeldObjective => heldObjective;

        public void PickUpObjective(Objective objective)
        {
            heldObjective = objective;
            heldObjective.transform.SetParent(transform);
            heldObjective.transform.position = transform.position + addedHeldObjectivePosition;

            AgentManager.Instance.CamperAgentGroup.AddGroupReward(objectivePickupReward);
            AddReward(objectivePickupReward);
            AgentManager.Instance.currentObjectivePickedUpRewards += objectivePickupReward;
        }

        public void DropHeldObjective(bool success)
        {
            if (heldObjective == null)
                return;

            if (success)
            {
                AgentManager.Instance.CamperAgentGroup.AddGroupReward(objectiveDropOffReward);
                AddReward(objectiveDropOffReward);
                AgentManager.Instance.currentObjectiveDroppedOffRewards += objectiveDropOffReward;
            }
            else
            {
                heldObjective.transform.parent = heldObjective.initialParent;
                heldObjective.transform.position = transform.position;
                heldObjective.transform.rotation = Quaternion.identity;
            }

            heldObjective = null;
        }

        public void GetKilled()
        {
            AddReward(deathReward * (deathToTimeRatioMultiplier - Academy.Instance.StepCount / AgentManager.Instance.maxStepCountPerEpisode * deathToTimeRatioMultiplier));
            AgentManager.Instance.currentDeathRewards += deathReward;
            DropHeldObjective(false);

            Debug.Log("Camper " + name + "'s episode ended");
            gameObject.SetActive(false);
        }
        #endregion

        #region Agent code
        public override void OnEpisodeBegin()
        {
            if (lastEpisodeCount == AgentManager.Instance.CurrentEpisodeCount)
                return;
            
            Debug.Log("Camper " + name + "'s episode started");

            transform.position = AgentManager.Instance.GetRandomCamperSpawnPosition();
            DropHeldObjective(false);
            lastEpisodeCount = AgentManager.Instance.CurrentEpisodeCount;
        }

        public override void CollectObservations(VectorSensor sensor)
        {
            sensor.AddObservation(heldObjective == null ? -1f : 1f);
        }

        public override void Heuristic(in ActionBuffers actionsOut)
        {
            ActionSegment<int> discreteActions = actionsOut.DiscreteActions;

            if (Input.GetAxis("Horizontal") < 0)
            {
                discreteActions[0] = 2;
            }
            else if (Input.GetAxis("Horizontal") > 0)
            {
                discreteActions[0] = 1;
            }
            else
            {
                discreteActions[0] = 0;
            }

            if (Input.GetAxis("Vertical") < 0)
            {
                discreteActions[1] = 2;
            }
            else if (Input.GetAxis("Vertical") > 0)
            {
                discreteActions[1] = 1;
            }
            else
            {
                discreteActions[1] = 0;
            }

            // TODO: Replace with mouse movement
            if (Input.GetKey(KeyCode.Q))
            {
                discreteActions[2] = 2;
            }
            else if (Input.GetKey(KeyCode.E))
            {
                discreteActions[2] = 1;
            }
            else
            {
                discreteActions[2] = 0;
            }
        }

        public override void OnActionReceived(ActionBuffers actions)
        {
            Vector3 moveX;
            Vector3 moveZ;

            if (actions.DiscreteActions[0] == 0)
            {
                moveX = Vector3.zero;
            }
            else if (actions.DiscreteActions[0] == 1)
            {
                moveX = transform.right;
            }
            else
            {
                moveX = -transform.right;
            }

            if (actions.DiscreteActions[1] == 0)
            {
                moveZ = Vector3.zero;
            }
            else if (actions.DiscreteActions[1] == 1)
            {
                moveZ = transform.forward;
            }
            else
            {
                moveZ = -transform.forward;
            }
            
            movementVector = moveX + moveZ;
            movementVector = movementVector.normalized;

            if (actions.DiscreteActions[2] == 0)
            {
                turningRotation = Vector3.zero;
            }
            else if (actions.DiscreteActions[2] == 1)
            {
                turningRotation = transform.up;
            }
            else
            {
                turningRotation = -transform.up;
            }

            AddReward(timeReward);
        }

        private void FixedUpdate() 
        {
            rb.AddForce(movementVector * movementSpeed, ForceMode.VelocityChange);
            transform.Rotate(turningRotation, rotationSpeed * Time.fixedDeltaTime);
        }
        #endregion
    }
}
