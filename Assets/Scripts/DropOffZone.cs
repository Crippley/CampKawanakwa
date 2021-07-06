using System.Collections.Generic;
using Entities;
using Items;
using UnityEngine;

namespace Zones
{
    public class DropOffZone : MonoBehaviour
    {
        private List<Objective> droppedOffObjectives = new List<Objective>();

        private void OnTriggerEnter(Collider other) 
        {
            Camper triggeringCamper = other?.GetComponent<Camper>();

            if (triggeringCamper)
            {
                droppedOffObjectives.Add(triggeringCamper.HeldItem);
                triggeringCamper.HeldItem.transform.SetParent(transform, true);
                triggeringCamper.RemoveItem(true);
            }
        }
    }
}