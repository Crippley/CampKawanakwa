using Entities;
using UnityEngine;

namespace Items
{
    public class Objective : MonoBehaviour
    {
        public bool IsActive {get; set;}
        public bool IsCompleted {get; set;}

        private void OnTriggerEnter2D(Collider2D other) 
        {
            if (!IsActive)
                return;

            Camper triggeringCamper = other?.GetComponent<Camper>();

            if (triggeringCamper)
            {
                triggeringCamper.AddItem(this);
                transform.SetParent(triggeringCamper.transform, true);
                IsActive = false;
            }
        }
    }
}