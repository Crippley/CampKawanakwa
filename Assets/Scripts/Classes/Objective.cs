using Core;
using Entities;
using UnityEngine;

namespace Items
{
    public class Objective : MonoBehaviour
    {
        public Transform initialParent;

        public bool IsCompleted { get; set; }

        #region Initialization
        public void Reset()
        {
            transform.parent = initialParent;
            transform.position = AgentManager.Instance.GetRandomObjectiveSpawnPosition();
            IsCompleted = false;
        }
        #endregion

        private void OnTriggerEnter(Collider other) 
        {
            Camper triggeringCamper = other?.GetComponent<Camper>();

            if (triggeringCamper == null || triggeringCamper.HeldObjective != null)
                return;

            triggeringCamper.PickUpObjective(this);
        }
    }
}