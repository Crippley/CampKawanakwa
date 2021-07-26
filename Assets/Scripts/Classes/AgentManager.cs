using System.Collections;
using System.Collections.Generic;
using Entities;
using UnityEngine;
using Items;
using System;
using Unity.MLAgents;
using Zones;

#if UNITY_EDITOR
using System.Linq;
#endif

namespace Core
{
    public class AgentManager : MonoBehaviour
    {
        #region Vars
        public static AgentManager Instance { get; private set; }

        public float winReward;
        public float lossReward;
        public int maxStepCountPerEpisode;
        public int maxEpisodes;

        public Player killer;
        public DropOffZone dropOffZone;
        public List<Camper> campers = new List<Camper>();
        public List<Objective> objectives = new List<Objective>();
        public List<GameObject> environmentPieces = new List<GameObject>();
        public List<SpawnZone> camperSpawnZones = new List<SpawnZone>();
        public List<SpawnZone> killerSpawnZones = new List<SpawnZone>();
        public List<SpawnZone> objectiveSpawnZones = new List<SpawnZone>();
        public List<SpawnZone> dropOffZoneSpawnZones = new List<SpawnZone>();
        public List<SpawnZone> environmentSpawnZones = new List<SpawnZone>();

        [NonSerialized] public bool IsResetConditionMet = false; // TODO: Replace when a reset condition has been found (camper/killer getting stuck, items becoming inaccessible, anything that breaks the game)

        [NonSerialized] public int currentEpisodeCount = 0;

        [NonSerialized] public SimpleMultiAgentGroup camperAgentGroup;

        public bool continueLooping;

        [Header("Read only training values")]
        [Header("Win counters")]
        public int killerWins;
        public int camperWins;

        [Header("Current episode rewards")]
        [Header("Misc rewards")]
        public float currentCollisionRewards;

        [Header("Killer rewards")]
        public float currentKillerWinRewards;
        public float currentKillerLossRewards;

        public float currentKillRewards;

        public float currentFindCamperRewards;
        public float currentLoseCamperRewards;
        public float currentMaintainCamperVisionRewards;

        [Header("Camper rewards")]
        public float currentCamperWinRewards;
        public float currentCamperLossRewards;

        public float currentObjectiveFoundRewards;
        public float currentObjectiveLostRewards;
        public float currentObjectivePickedUpRewards;
        public float currentObjectiveDroppedOffRewards;
        public float currentObjectiveMaintainedVisionRewards;

        public float currentDropOffZoneFoundRewards;
        public float currentDropOffZoneLostRewards;
        public float currentDropOffZoneMaintainedVisionRewards;

        public float currentKillerFoundRewards;
        public float currentKillerLostRewards;
        public float currentKillerMaintainedVisionRewards;

        public float currentDeathRewards;

        [Header("Total rewards")]
        [Header("Misc rewards")]
        public float totalCollisionRewards;

        [Header("Killer rewards")]
        public float totalKillerWinRewards;
        public float totalKillerLossRewards;

        public float totalKillRewards;

        public float totalFindCamperRewards;
        public float totalLoseCamperRewards;
        public float totalMaintainCamperVisionRewards;

        [Header("Camper rewards")]
        public float totalCamperWinRewards;
        public float totalCamperLossRewards;

        public float totalObjectiveFoundRewards;
        public float totalObjectiveLostRewards;
        public float totalObjectivePickedUpRewards;
        public float totalObjectiveDroppedOffRewards;
        public float totalObjectiveMaintainedVisionRewards;

        public float totalDropOffZoneFoundRewards;
        public float totalDropOffZoneLostRewards;
        public float totalDropOffZoneMaintainedVisionRewards;

        public float totalKillerFoundRewards;
        public float totalKillerLostRewards;
        public float totalKillerMaintainedVisionRewards;

        public float totalDeathRewards;

        private int currentMaxStepCountPerEpisode = 0;
        #endregion
        
        #region Editor code
        #if UNITY_EDITOR
        public void FindKiller()
        {
            killer = FindObjectOfType<Player>();
        }

        public void FindAllCampers()
        {
            campers = FindObjectsOfType<Camper>().ToList();
        }

        public void FindAllObjectives()
        {
            objectives = FindObjectsOfType<Objective>().ToList();
        }
        #endif
        #endregion

        #region Init
        private void Awake()
        {
            currentMaxStepCountPerEpisode = maxStepCountPerEpisode;

            if (Instance != null)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;
            camperAgentGroup = new SimpleMultiAgentGroup();

            InvokeEpisodeBegin();
        }

        private void ResetEpisodeValues()
        {
            totalCollisionRewards += currentCollisionRewards;
            currentCollisionRewards = 0f;
            
            totalKillerWinRewards += currentKillerWinRewards;
            currentKillerWinRewards = 0f;
            totalKillerLossRewards += currentKillerLossRewards;
            currentKillerLossRewards = 0f;

            totalKillRewards += currentKillRewards;
            currentKillRewards = 0f;

            totalFindCamperRewards += currentFindCamperRewards;
            currentFindCamperRewards = 0f;
            totalLoseCamperRewards += currentLoseCamperRewards;
            currentLoseCamperRewards = 0f;
            totalMaintainCamperVisionRewards += currentMaintainCamperVisionRewards;
            currentMaintainCamperVisionRewards = 0f;

            totalCamperWinRewards += currentCamperWinRewards;
            currentCamperWinRewards = 0f;
            totalCamperLossRewards += currentCamperLossRewards;
            currentCamperLossRewards = 0f;

            totalObjectiveFoundRewards += currentObjectiveFoundRewards;
            currentObjectiveFoundRewards = 0f;
            totalObjectiveLostRewards += currentObjectiveLostRewards;
            currentObjectiveLostRewards = 0f;
            totalObjectivePickedUpRewards += currentObjectivePickedUpRewards;
            currentObjectivePickedUpRewards = 0f;
            totalObjectiveDroppedOffRewards += currentObjectiveDroppedOffRewards;
            currentObjectiveDroppedOffRewards = 0f;
            totalObjectiveMaintainedVisionRewards += currentObjectiveMaintainedVisionRewards;
            currentObjectiveMaintainedVisionRewards = 0f;

            totalDropOffZoneFoundRewards += currentDropOffZoneFoundRewards;
            currentDropOffZoneFoundRewards = 0f;
            totalDropOffZoneLostRewards += currentDropOffZoneLostRewards;
            currentDropOffZoneLostRewards = 0f;
            totalDropOffZoneMaintainedVisionRewards += currentDropOffZoneMaintainedVisionRewards;
            currentDropOffZoneMaintainedVisionRewards = 0f;

            totalKillerFoundRewards += currentKillerFoundRewards;
            currentKillerFoundRewards = 0f;
            totalKillerLostRewards += currentKillerLostRewards;
            currentKillerLostRewards = 0f;
            totalKillerMaintainedVisionRewards += currentKillerMaintainedVisionRewards;
            currentKillerMaintainedVisionRewards = 0f;

            totalDeathRewards += currentDeathRewards;
            currentDeathRewards = 0f;
        }
        #endregion

        #region Agent
        private void FixedUpdate() 
        {
            if (Academy.Instance.StepCount > currentMaxStepCountPerEpisode)
            {
                currentMaxStepCountPerEpisode += maxStepCountPerEpisode;
                IsResetConditionMet = true;
                InvokeEpisodeEnd();
            }
        }

        public Vector3 GetRandomCamperSpawnPosition()
        {
            int randomSpawnZone = UnityEngine.Random.Range(0, camperSpawnZones.Count);
            return camperSpawnZones[randomSpawnZone].GetRandomPoint();
        }

        public Vector3 GetRandomKillerSpawnPosition()
        {
            int randomSpawnZone = UnityEngine.Random.Range(0, killerSpawnZones.Count);
            return killerSpawnZones[randomSpawnZone].GetRandomPoint();
        }

        public Vector3 GetRandomObjectiveSpawnPosition()
        {
            int randomSpawnZone = UnityEngine.Random.Range(0, objectiveSpawnZones.Count);
            return objectiveSpawnZones[randomSpawnZone].GetRandomPoint();
        }

        public Vector3 GetRandomDropOffZoneSpawnPosition()
        {
            int randomSpawnZone = UnityEngine.Random.Range(0, dropOffZoneSpawnZones.Count);
            return dropOffZoneSpawnZones[randomSpawnZone].GetRandomPoint();
        }

        public Vector3 GetRandomEnvironmentSpawnPosition()
        {
            int randomSpawnZone = UnityEngine.Random.Range(0, environmentSpawnZones.Count);
            return environmentSpawnZones[randomSpawnZone].GetRandomPoint();
        }

        public static void InvokeEpisodeBegin()
        {
            Debug.Log("Episode started");
            Instance.ResetEpisodeValues();

            if (Instance.continueLooping || Instance.currentMaxStepCountPerEpisode / Instance.maxStepCountPerEpisode < Instance.maxEpisodes)
            {
                Instance.currentEpisodeCount++;

                for (int i = 0; i < Instance.environmentPieces.Count; i++)
                {
                    Instance.environmentPieces[i].transform.position = Instance.GetRandomEnvironmentSpawnPosition();
                }

                Instance.dropOffZone.Reset();

                for(int i = 0; i < Instance.objectives.Count; i++)
                {
                    Instance.objectives[i].Reset();
                }

                Instance.killer.gameObject.SetActive(true);

                for(int i = 0; i < Instance.campers.Count; i++)
                {
                    Instance.campers[i].gameObject.SetActive(true);
                    Instance.camperAgentGroup.RegisterAgent(Instance.campers[i]);
                    Instance.campers[i].RemoveItem(false);
                }

                Debug.Log("Environment reset");
            }
            else
            {
                Debug.Log("Training has ended");
                Application.Quit();
            }
        }

        public static void InvokeEpisodeEnd()
        {
            bool campersWin = Instance.objectives.Find(x => !x.IsCompleted) == null;
            bool killerWins = Instance.campers.Find(x => x.gameObject.activeInHierarchy) == null;

            float killerReward = 0f;
            float camperReward = 0f;

            if (killerWins || campersWin || Instance.IsResetConditionMet)
            {
                if (Instance.IsResetConditionMet)
                {
                    Instance.camperAgentGroup.GroupEpisodeInterrupted();

                    for(int i = 0; i < Instance.campers.Count; i++)
                    {
                        if (Instance.campers[i].isActiveAndEnabled)
                        {
                            Debug.Log("Camper " + Instance.campers[i].name + "'s episode ended");
                            Instance.campers[i].RemoveItem(false);
                            Instance.campers[i].gameObject.SetActive(false);
                        }
                    }

                    Debug.Log("Killer's episode ended");
                    Instance.killer.gameObject.SetActive(false);

                    Debug.Log("Episode ended");
                    InvokeEpisodeBegin();

                    return;
                }

                if (killerWins)
                {
                    Debug.Log("Killer wins!");
                    killerReward = Instance.winReward;
                    camperReward = Instance.lossReward;
                }
                else if (campersWin)
                {
                    Debug.Log("Campers win!");
                    killerReward = Instance.lossReward;
                    camperReward = Instance.winReward;
                }

                Instance.camperAgentGroup.AddGroupReward(camperReward);

                if (camperReward > 0f)
                    Instance.currentCamperWinRewards += camperReward;
                else
                    Instance.currentCamperLossRewards += camperReward;

                Instance.camperAgentGroup.EndGroupEpisode();

                for(int i = 0; i < Instance.campers.Count; i++)
                {
                    if (Instance.campers[i].isActiveAndEnabled)
                    {
                        Debug.Log("Camper " + Instance.campers[i].name + "'s episode ended");
                        Instance.campers[i].RemoveItem(false);
                        Instance.campers[i].gameObject.SetActive(false);
                    }
                }

                Debug.Log("Killer's episode ended");
                Instance.killer.AddReward(killerReward);

                if (killerReward > 0f)
                    Instance.currentKillerWinRewards += killerReward;
                else
                    Instance.currentKillerLossRewards += killerReward;

                Instance.killer.gameObject.SetActive(false);

                Debug.Log("Episode ended");
                InvokeEpisodeBegin();
            }
        }
        #endregion
    }
}
