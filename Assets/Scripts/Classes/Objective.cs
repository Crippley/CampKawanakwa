using Core;
using Entities;
using UnityEngine;

namespace Items
{
    public class Objective : MonoBehaviour
    {
        public Transform initialParent;

        public bool IsActive {get; set;}
        public bool IsCompleted {get; set;}

        #region Initialization
        private void Start() 
        {
            IsActive = true;
            transform.position = AgentManager.Instance.GetRandomObjectiveSpawnPosition();
        }

        public void Reset()
        {
            transform.parent = initialParent;
            transform.position = AgentManager.Instance.GetRandomObjectiveSpawnPosition();
            IsActive = true;
            IsCompleted = false;
        }
        #endregion

        private void OnTriggerEnter2D(Collider2D other) 
        {
            if (!IsActive)
                return;

            Camper triggeringCamper = other?.GetComponent<Camper>();

            if (triggeringCamper && triggeringCamper.HeldItem == null)
            {
                triggeringCamper.AddItem(this);
                transform.SetParent(triggeringCamper.transform, true);
                IsActive = false;
            }
        }
    }
}