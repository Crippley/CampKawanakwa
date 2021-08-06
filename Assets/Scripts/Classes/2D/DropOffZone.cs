using System.Collections.Generic;
using Core;
using Entities;
using Items;
using UnityEngine;

namespace Zones
{
    public class DropOffZone : MonoBehaviour
    {
        [SerializeField] private Vector3[] droppedOffObjectivePositions;

        private int currentObjectiveDropOffPositionIndex;
        private List<Objective> droppedOffObjectives = new List<Objective>();

        private void Start() 
        {
            transform.position = AgentManager.Instance.GetRandomDropOffZoneSpawnPosition();
        }

        public void Reset()
        {
            droppedOffObjectives = new List<Objective>();
            transform.position = AgentManager.Instance.GetRandomDropOffZoneSpawnPosition();
            currentObjectiveDropOffPositionIndex = 0;
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
                droppedOffObjective.spriteRenderer.color = droppedOffObjective.droppedOffColor;

                droppedOffObjective.transform.rotation = Quaternion.identity;
                droppedOffObjective.transform.localPosition = droppedOffObjectivePositions[currentObjectiveDropOffPositionIndex];
                currentObjectiveDropOffPositionIndex++;

                if (droppedOffObjectives.Count >= AgentManager.Instance.objectives.Count)
                    AgentManager.Instance.InvokeEpisodeEnd();
            }
        }
    }
}