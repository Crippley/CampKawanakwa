using System.Collections.Generic;
using Unity.MLAgents;
using Unity.MLAgents.Actuators;
using Unity.MLAgents.Sensors;
using UnityEngine;
using Core;
using Unity.MLAgents.Policies;

namespace Entities
{
    public class Player : Agent, IDetectionTriggerHandler
    {
        #region Vars
        [SerializeField] private float movementSpeed;
        [SerializeField] private float rotationSpeed;

        [SerializeField] private float killCamperReward;
        [SerializeField] private float findCamperReward;
        [SerializeField] private float loseCamperReward;
        [SerializeField] private float timeReward;

        [SerializeField] private BehaviorType behaviorType;

        public BehaviorType BehaviorType => behaviorType;

        private Dictionary<Camper, List<Collider2D>> visibleCampers = new Dictionary<Camper, List<Collider2D>>();
        private Vector3 startingPosition;

        private Vector3 movementVector;
        private Quaternion turningRotation;

        private float killedCamperCount;
        #endregion

        #region Initialization
        private void Start() 
        {
            startingPosition = transform.position;
        }
        #endregion

        #region Camper detection
        private void OnCollisionEnter2D(Collision2D other) 
        {
            Camper collidingCamper = other.gameObject?.GetComponent<Camper>();

            if (collidingCamper)
            {
                visibleCampers.Remove(collidingCamper);
                collidingCamper.GetKilled();
                AddReward(killCamperReward);
                killedCamperCount++;

                if (AgentManager.Instance.campers.Count == killedCamperCount)
                    AgentManager.InvokeEpisodeEnd();
            }
        }
        #endregion

        #region Agent
        public override void OnEpisodeBegin()
        {
            Debug.Log("Killer's episode started again");
            transform.position = startingPosition;
            visibleCampers = new Dictionary<Camper, List<Collider2D>>();
            killCamperReward = 0;
        }

        public override void CollectObservations(VectorSensor sensor)
        {
            sensor.AddObservation(transform.position);

            foreach (KeyValuePair<Camper, List<Collider2D>> value in visibleCampers)
                sensor.AddObservation(value.Key.transform.position);
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

            continuousActions[2] = -angle;
        }*/

        public override void OnActionReceived(ActionBuffers actions)
        {
            float moveX = actions.ContinuousActions[0];
            float moveY = actions.ContinuousActions[1];
            
            movementVector = new Vector3(moveX, moveY, 0f);
            movementVector = Vector3.ClampMagnitude(movementVector, 1f);

            float rotateZ = actions.ContinuousActions[2];

            turningRotation = Quaternion.identity;
            turningRotation *= Quaternion.Euler(0f, 0f, rotateZ);
            
            AddReward(timeReward);
        }

        private void FixedUpdate() 
        {
            transform.position += movementVector * movementSpeed * Time.fixedDeltaTime;
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
            AddReward(findCamperReward);
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
            AddReward(loseCamperReward);
        }
        #endregion
    }
}
