using Core;
using Items;
using Unity.MLAgents;
using Unity.MLAgents.Actuators;
using Unity.MLAgents.Policies;
using Unity.MLAgents.Sensors;
using UnityEngine;

namespace Entities
{
    public class Camper : Agent
    {
        #region Vars
        [Header("General values")]
        [SerializeField] private AgentManager agentManager;
        [SerializeField] private Camera agentCamera;

        [Header("Movement related values")]
        [SerializeField] private Rigidbody rb;
        [SerializeField] private float movementSpeed;
        [SerializeField] private float rotationSpeed;

        [Header("Ability values")]
        [SerializeField] private LayerMask abilityMask;
        [SerializeField] private float abilityRange;
        [SerializeField] private float knockbackSpeed;
        [SerializeField] private int abilityStepCooldown;

        [Header("Held objective position")]
        [SerializeField] private Vector3 addedHeldObjectivePosition;

        [Header("Reward values")]
        [SerializeField] private float objectivePickupReward;
        [SerializeField] private float objectiveDropOffReward;
        [SerializeField] private float hitTargetWithAbility;
        [SerializeField] private float missedTargetWithAbility;
        [SerializeField] private float deathReward;

        private Vector3 movementVector;
        private Vector3 turningRotation;

        private int lastAbilityUseStep;

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

            agentManager.CamperAgentGroup.AddGroupReward(objectivePickupReward);
            //AddReward(objectivePickupReward);
            agentManager.currentObjectivePickedUpRewards += objectivePickupReward;
        }

        public void DropHeldObjective(bool success)
        {
            if (heldObjective == null)
                return;

            if (success)
            {
                agentManager.CamperAgentGroup.AddGroupReward(objectiveDropOffReward);
                //AddReward(objectiveDropOffReward);
                agentManager.currentObjectiveDroppedOffRewards += objectiveDropOffReward;
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
            AddReward(deathReward);
            agentManager.currentDeathRewards += deathReward;
            DropHeldObjective(false);

            Debug.Log("Camper " + name + "'s episode ended");
            gameObject.SetActive(false);
        }
        #endregion

        #region Agent code
        private void Start() 
        {
            if (GetComponent<BehaviorParameters>()?.BehaviorType == BehaviorType.HeuristicOnly)
            {
                Cursor.lockState = CursorLockMode.Locked;
                Cursor.visible = false;
            }
        }
        
        public override void OnEpisodeBegin()
        {
            if (lastEpisodeCount == agentManager.CurrentEpisodeCount)
                return;
            
            Debug.Log("Camper " + name + "'s episode started");

            transform.position = agentManager.GetRandomCamperSpawnPosition();
            DropHeldObjective(false);
            lastEpisodeCount = agentManager.CurrentEpisodeCount;
        }

        public override void CollectObservations(VectorSensor sensor)
        {
            sensor.AddObservation(heldObjective == null);
        }

        /*public override void Heuristic(in ActionBuffers actionsOut)
        {
            ActionSegment<int> discreteActions = actionsOut.DiscreteActions;

            // Left-right movement
            if (Input.GetAxis("Horizontal") < 0)
                discreteActions[0] = 2;
            else if (Input.GetAxis("Horizontal") > 0)
                discreteActions[0] = 1;
            else
                discreteActions[0] = 0;

            // Forward-backward movement
            if (Input.GetAxis("Vertical") < 0)
                discreteActions[1] = 2;
            else if (Input.GetAxis("Vertical") > 0)
                discreteActions[1] = 1;
            else
                discreteActions[1] = 0;

            // Left-right rotation
            if (Input.GetAxis("Mouse X") < 0)
                discreteActions[2] = 2;
            else if (Input.GetAxis("Mouse X") > 0)
                discreteActions[2] = 1;
            else
                discreteActions[2] = 0;

            // Ability usage
            if (Input.GetMouseButtonDown(0) || Input.GetMouseButton(0))
                discreteActions[3] = 1;
            else
                discreteActions[3] = 0;
        }*/

        public override void OnActionReceived(ActionBuffers actions)
        {
            Vector3 moveX;
            Vector3 moveZ;

            if (actions.DiscreteActions[0] == 0)
                moveX = Vector3.zero;
            else if (actions.DiscreteActions[0] == 1)
                moveX = transform.right;
            else
                moveX = -transform.right;

            if (actions.DiscreteActions[1] == 0)
                moveZ = Vector3.zero;
            else if (actions.DiscreteActions[1] == 1)
                moveZ = transform.forward;
            else
                moveZ = -transform.forward;
            
            movementVector = moveX + moveZ;
            movementVector = movementVector.normalized;

            if (actions.DiscreteActions[2] == 0)
                turningRotation = Vector3.zero;
            else if (actions.DiscreteActions[2] == 1)
                turningRotation = transform.up;
            else
                turningRotation = -transform.up;

            if (actions.DiscreteActions[3] == 1 && Academy.Instance.StepCount > lastAbilityUseStep)
            {
                float reward;
                bool hasHit = Physics.Raycast(transform.position, transform.forward, out RaycastHit hit, abilityRange, abilityMask);

                if (hasHit && hit.collider.GetComponent<Player>() != null)
                {
                    reward = hitTargetWithAbility;

                    hit.rigidbody.AddForce((hit.transform.position - transform.position).normalized * knockbackSpeed, ForceMode.VelocityChange);
                    lastAbilityUseStep = Academy.Instance.StepCount + abilityStepCooldown;
                }
                else
                {
                    reward = missedTargetWithAbility;
                }

                AddReward(reward);
            }
        }

        private void FixedUpdate() 
        {
            rb.AddForce(movementVector * movementSpeed, ForceMode.VelocityChange);
            transform.Rotate(turningRotation, rotationSpeed * Time.fixedDeltaTime);
        }
        #endregion
    }
}
