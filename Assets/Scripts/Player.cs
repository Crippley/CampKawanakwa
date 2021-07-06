using System.Collections.Generic;
using Unity.MLAgents;
using Unity.MLAgents.Actuators;
using Unity.MLAgents.Sensors;
using UnityEngine;
using Core;

#if UNITY_EDITOR
using System.Linq;
#endif

namespace Entities
{
    public class Player : Agent
    {
        #region Vars
        [SerializeField] private float movementSpeed;
        [SerializeField] private float rotationSpeed;

        [SerializeField] private float killCamperReward;
        [SerializeField] private float findCamperReward;
        [SerializeField] private float loseCamperReward;

        private List<Camper> visibleCampers = new List<Camper>();
        #endregion

        #region Camper detection
        private void OnCollisionEnter2D(Collision2D other) 
        {
            Camper collidingCamper = other.gameObject?.GetComponent<Camper>();

            if (collidingCamper)
            {
                AgentManager.Instance.campers.Remove(collidingCamper);
                visibleCampers.Remove(collidingCamper);
                collidingCamper.GetKilled();
                AddReward(killCamperReward);

                if (AgentManager.Instance.campers.Count == 0)
                    AgentManager.InvokeEpisodeEnd();
            }
        }

        private void OnTriggerEnter2D(Collider2D other) 
        {
            Camper detectedCamper = other.gameObject?.GetComponent<Camper>();

            if (detectedCamper)
            {
                visibleCampers.Add(detectedCamper);
                AddReward(findCamperReward);
            }
        }

        private void OnTriggerExit2D(Collider2D other) 
        {
            Camper detectedCamper = other.gameObject?.GetComponent<Camper>();

            if (detectedCamper && visibleCampers.Contains(detectedCamper))
            {
                visibleCampers.Add(detectedCamper);
                AddReward(loseCamperReward);
            }
        }
        #endregion

        #region Agent
        public override void CollectObservations(VectorSensor sensor)
        {
            sensor.AddObservation(transform.position);

            for(int i = 0; i < visibleCampers.Count; i++)
                sensor.AddObservation(visibleCampers[i].transform.position);
        }

        public override void OnActionReceived(ActionBuffers actions)
        {
            float moveX = actions.ContinuousActions[0];
            float moveY = actions.ContinuousActions[1];
            float rotateZ = actions.ContinuousActions[2];

            transform.position += new Vector3(moveX, moveY, 0) * Time.deltaTime * movementSpeed;
            transform.eulerAngles += new Vector3(0, 0, rotateZ) * Time.deltaTime * rotationSpeed;
        }
        #endregion
    }
}
