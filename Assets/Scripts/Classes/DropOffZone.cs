using System.Collections.Generic;
using Core;
using Entities;
using Items;
using UnityEngine;

namespace Zones
{
    public class DropOffZone : MonoBehaviour
    {
        [SerializeField] private AgentManager agentManager;
        [SerializeField] private Vector3 addedDroppedOffObjectivePosition;

        private int currentDroppedOffObjectiveCount;
        private List<Objective> droppedOffObjectives = new List<Objective>();

        public void Reset()
        {
            droppedOffObjectives = new List<Objective>();
            transform.position = agentManager.GetRandomDropOffZoneSpawnPosition();
            currentDroppedOffObjectiveCount = 0;
        }

        private void OnTriggerEnter(Collider other)
        {
            Camper triggeringCamper = other?.GetComponent<Camper>();

            if (triggeringCamper?.HeldObjective == null)
                return;

            Objective droppedOffObjective = triggeringCamper.HeldObjective;
            droppedOffObjective.IsCompleted = true;
            triggeringCamper.DropHeldObjective(true);

            droppedOffObjectives.Add(droppedOffObjective);
            currentDroppedOffObjectiveCount++;

            droppedOffObjective.transform.SetParent(transform);
            droppedOffObjective.transform.localPosition = addedDroppedOffObjectivePosition * currentDroppedOffObjectiveCount;
            droppedOffObjective.transform.rotation = Quaternion.identity;

            if (currentDroppedOffObjectiveCount >= agentManager.objectives.Length)
                agentManager.InvokeEpisodeEnd();
        }
    }
}