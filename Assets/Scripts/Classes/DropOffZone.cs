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

        public void Reset()
        {
            droppedOffObjectives = new List<Objective>();
        }

        private void OnTriggerEnter2D(Collider2D other)
        {
            Camper triggeringCamper = other?.GetComponent<Camper>();

            if (triggeringCamper?.HeldItem)
            {
                Objective droppedOffObjective = triggeringCamper.HeldItem;
                triggeringCamper.RemoveItem(true);

                droppedOffObjectives.Add(droppedOffObjective);
                droppedOffObjective.transform.SetParent(transform, true);
                droppedOffObjective.IsCompleted = true;

                if (droppedOffObjectives.Count >= AgentManager.Instance.objectives.Count)
                    AgentManager.InvokeEpisodeEnd();
            }
        }
    }
}