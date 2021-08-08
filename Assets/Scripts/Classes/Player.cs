using Unity.MLAgents;
using Unity.MLAgents.Actuators;
using UnityEngine;
using Core;
using Unity.MLAgents.Policies;

namespace Entities
{
    public class Player : Agent
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
        [SerializeField] private float leapSpeed;
        [SerializeField] private int abilityStepCooldown;

        [Header("Rewars values")]
        [SerializeField] private float killCamperReward;
        [SerializeField] private float hitTargetWithAbility;
        [SerializeField] private float missedTargetWithAbility;
		
        private Vector3 movementVector;
        private Vector3 turningRotation;

        private int lastAbilityUseStep;

        private int lastEpisodeCount = -1;
        private float killedCamperCount;
        public float KilledCamperCount => killedCamperCount;
        #endregion

        #region Camper detection
        private void OnCollisionEnter(Collision other) 
        {
            Camper collidingCamper = other.gameObject?.GetComponent<Camper>();

            if (collidingCamper)
            {
                collidingCamper.GetKilled();

                AddReward(killCamperReward);
                agentManager.currentKillRewards += killCamperReward;

                killedCamperCount++;

                if (agentManager.campers.Length == killedCamperCount)
                    agentManager.InvokeEpisodeEnd();
            }
        }
        #endregion

        #region Agent
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

            Debug.Log("Mosquito's episode started");
            
            transform.position = agentManager.GetRandomMosquitoSpawnPosition();
            killedCamperCount = 0;
            lastEpisodeCount = agentManager.CurrentEpisodeCount;
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
                bool hasHit = Physics.Raycast(transform.position, transform.forward, out RaycastHit hit, abilityRange, ~abilityMask);

                if (hasHit && hit.collider.GetComponent<Camper>() != null)
                    reward = hitTargetWithAbility;
                else
                    reward = missedTargetWithAbility;

                AddReward(reward);

                rb.AddForce(transform.forward * leapSpeed, ForceMode.VelocityChange);
                lastAbilityUseStep = Academy.Instance.StepCount + abilityStepCooldown;
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
