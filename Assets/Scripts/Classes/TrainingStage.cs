using Entities;
using Items;
using UnityEngine;
using Zones;

namespace Core
{
    public class TrainingStage : MonoBehaviour
    {
        public Player mosquito;
        public DropOffZone dropOffZone;

        public Camper[] campers;
        public Objective[] objectives;
        public GameObject[] environmentElements;

        public SpawnZone[] camperSpawnZones;
        public SpawnZone[] mosquitoSpawnZones;
        public SpawnZone[] objectiveSpawnZones;
        public SpawnZone[] dropOffZoneSpawnZones;
        public SpawnZone[] environmentElementsSpawnZones;
    }
}
