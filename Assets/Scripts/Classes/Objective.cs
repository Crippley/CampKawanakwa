using Entities;
using UnityEngine;

namespace Items
{
    public class Objective : MonoBehaviour
    {
        public bool IsActive {get; set;}
        public bool IsCompleted {get; set;}

        private Vector3 startingPosition;

        #region Initialization
        private void Start() 
        {
            startingPosition = transform.position;
        }

        public void Reset()
        {
            transform.position = startingPosition;
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