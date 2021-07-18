using System.Collections.Generic;
using Core;
using Entities;
using Items;
using UnityEngine;

namespace Zones
{
    public class DropOffZone : MonoBehaviour
    {
        private List<Objective> droppedOffObjectives = new List<Objective>();

        private void OnTriggerEnter2D(Collider2D other)
        {
            Camper triggeringCamper = other?.GetComponent<Camper>();

            if (triggeringCamper?.HeldItem)
            {
                droppedOffObjectives.Add(triggeringCamper.HeldItem);
                triggeringCamper.HeldItem.transform.SetParent(transform, true);
                triggeringCamper.HeldItem.IsCompleted = true;
                triggeringCamper.RemoveItem(true);

                if (droppedOffObjectives.Count >= AgentManager.Instance.objectives.Count)
                    AgentManager.InvokeEpisodeEnd();
            }
        }
    }
}