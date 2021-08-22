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
        [SerializeField] private AgentManager agentManager;
        [SerializeField] private Camera agentCamera;

        [Header("Movement related values")]
        [SerializeField] private Rigidbody rb;
        [SerializeField] private float movementSpeed;
        [SerializeField] private float rotationSpeed;

        [Header("Rewars values")]
        [SerializeField] private float killCamperReward;
        [SerializeField] private float timeReward;

        private Vector3 movementVector;
        private Vector3 turningRotation;

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

            if (Input.GetAxis("Horizontal") < 0)
                discreteActions[0] = 2;
            else if (Input.GetAxis("Horizontal") > 0)
                discreteActions[0] = 1;
            else
                discreteActions[0] = 0;

            if (Input.GetAxis("Vertical") < 0)
                discreteActions[1] = 2;
            else if (Input.GetAxis("Vertical") > 0)
                discreteActions[1] = 1;
            else
                discreteActions[1] = 0;

            if (Input.GetAxis("Mouse X") < 0)
                discreteActions[2] = 2;
            else if (Input.GetAxis("Mouse X") > 0)
                discreteActions[2] = 1;
            else
                discreteActions[2] = 0;
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
