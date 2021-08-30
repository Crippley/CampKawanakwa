using Core;
using Entities;
using UnityEngine;

namespace Items
{
    public class Objective : MonoBehaviour
    {
        public Transform initialParent;
        [SerializeField] private MeshRenderer meshRenderer;
        [SerializeField] private AgentManager agentManager;

        public bool IsCompleted { get; set; }

        #region Initialization
        public void Reset()
        {
            meshRenderer.enabled = true;
            transform.parent = initialParent;
            transform.position = agentManager.GetRandomObjectiveSpawnPosition();
            IsCompleted = false;
        }
        #endregion

        private void OnTriggerEnter(Collider other) 
        {
            Camper triggeringCamper = other?.GetComponent<Camper>();

            if (triggeringCamper == null)
                return;

            if (triggeringCamper.HeldObjective == null)
            {
                triggeringCamper.PickUpObjective(this);
                meshRenderer.enabled = false;
            }
            else
            {
                triggeringCamper.AddReward(triggeringCamper.forbiddenInteractionReward);
            }
        }
    }
}