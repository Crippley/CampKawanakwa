using System.Collections;
using System.Collections.Generic;
using Entities;
using UnityEngine;
using Items;
using System;
using Unity.MLAgents;
using Zones;
using Environment.Training;
using UnityEngine.Serialization;

#if UNITY_EDITOR
using System.Linq;
#endif

namespace Core
{
    public class AgentManager : MonoBehaviour
    {
        #region Vars
        public float camperTimeReward;
        public float mosquitoTimeReward;
        public float winReward;
        public float lossReward;
        public float timeoutReward;
        public int maxStepCountPerEpisode;
        public int maxEpisodes;

        [FormerlySerializedAs("killer")]
        public Player mosquito;
        public DropOffZone dropOffZone;

        public Camper[] campers;
        public Objective[] objectives;
        public GameObject[] environmentElements;

        public SpawnZone[] camperSpawnZones;
        [FormerlySerializedAs("killerSpawnZones")]
        public SpawnZone[] mosquitoSpawnZones;
        public SpawnZone[] objectiveSpawnZones;
        public SpawnZone[] dropOffZoneSpawnZones;
        public SpawnZone[] environmentElementsSpawnZones;


        public bool loopInfinitiely;

        [Header("Read only training values")]
        [Header("Win counters")]
        public int mosquitoWins;
        public int camperWins;

        [Header("Current episode rewards")]
        [Header("Misc rewards")]
        public float currentCollisionRewards;

        [Header("Mosquito rewards")]
        public float currentKillRewards;

        [Header("Camper rewards")]
        public float currentObjectivePickedUpRewards;
        public float currentObjectiveDroppedOffRewards;

        public float currentDeathRewards;

        [Header("Total rewards")]
        [Header("Misc rewards")]
        public float totalTimeouts;
        public float totalCollisionRewards;

        [Header("Mosquito rewards")]
        public float totalKillRewards;

        public float totalMosquitoWins;
        public float totalMosquitoLosses;

        [Header("Camper rewards")]
        public float totalObjectivePickedUpRewards;
        public float totalObjectiveDroppedOffRewards;

        public float totalDeathRewards;

        public float totalCamperWins;
        public float totalCamperLosses;

        public bool IsResetConditionMet { get; set; }

        private int currentEpisodeCount = 0;
        public int CurrentEpisodeCount => currentEpisodeCount;

        private SimpleMultiAgentGroup camperAgentGroup;
        public SimpleMultiAgentGroup CamperAgentGroup => camperAgentGroup;

        private StatsRecorder statsRecorder;
        public StatsRecorder StatsRecorder => statsRecorder;

        private int currentMaxStepCountPerEpisode = 0;
        public int CurrentMaxStepCountPerEpisode => currentMaxStepCountPerEpisode;
        #endregion
        
        #region Editor code
        #if UNITY_EDITOR
        public void FindMosquito()
        {
            mosquito = FindObjectOfType<Player>();
        }

        public void FindAllCampers()
        {
            campers = FindObjectsOfType<Camper>();
        }

        public void FindAllObjectives()
        {
            objectives = FindObjectsOfType<Objective>();
        }
        #endif
        #endregion

        #region Init
        private void Awake()
        {
            currentMaxStepCountPerEpisode = maxStepCountPerEpisode;

            camperAgentGroup = new SimpleMultiAgentGroup();
            statsRecorder = Academy.Instance.StatsRecorder;

            InvokeEpisodeBegin();
        }

        /// <summary>
        /// Resets all tracked rewards values of the episode after adding them up to the total values and adding them to the tensorboard file.
        /// </summary>
        private void ResetEpisodeValues()
        {
            #region Misc rewards
            statsRecorder.Add("TimeOutRewards", totalTimeouts, StatAggregationMethod.MostRecent);

            totalCollisionRewards += currentCollisionRewards;
            statsRecorder.Add("CollisionRewards", totalCollisionRewards, StatAggregationMethod.MostRecent);
            currentCollisionRewards = 0f;
            #endregion

            #region Mosquito rewards
            totalKillRewards += currentKillRewards;
            statsRecorder.Add("Agent/Mosquito/KillRewards", totalKillRewards, StatAggregationMethod.MostRecent);
            currentKillRewards = 0f;

            statsRecorder.Add("Agent/Mosquito/VictoryRewards", totalMosquitoWins, StatAggregationMethod.MostRecent);

            statsRecorder.Add("Agent/Mosquito/DefeatRewards", totalMosquitoLosses, StatAggregationMethod.MostRecent);
            #endregion

            #region Camper rewards
            totalObjectivePickedUpRewards += currentObjectivePickedUpRewards;
            statsRecorder.Add("Agent/Camper/ObjectivePickUpRewards", totalObjectivePickedUpRewards, StatAggregationMethod.MostRecent);
            currentObjectivePickedUpRewards = 0f;

            totalObjectiveDroppedOffRewards += currentObjectiveDroppedOffRewards;
            statsRecorder.Add("Agent/Camper/ObjectiveDropOffRewards", totalObjectiveDroppedOffRewards, StatAggregationMethod.MostRecent);
            currentObjectiveDroppedOffRewards = 0f;

            totalDeathRewards += currentDeathRewards;
            statsRecorder.Add("Agent/Camper/DeathRewards", totalDeathRewards, StatAggregationMethod.MostRecent);
            currentDeathRewards = 0f;

            statsRecorder.Add("Agent/Camper/VictoryRewards", totalCamperWins, StatAggregationMethod.MostRecent);

            statsRecorder.Add("Agent/Camper/DefeatRewards", totalCamperLosses, StatAggregationMethod.MostRecent);
            #endregion

            currentMaxStepCountPerEpisode += maxStepCountPerEpisode;
        }

        private void ResetSpawnZones()
        {
            for (int i = 0; i < camperSpawnZones.Length; i++)
                camperSpawnZones[i].Reset();

            for (int i = 0; i < mosquitoSpawnZones.Length; i++)
                mosquitoSpawnZones[i].Reset();

            for (int i = 0; i < objectiveSpawnZones.Length; i++)
                objectiveSpawnZones[i].Reset();

            for (int i = 0; i < dropOffZoneSpawnZones.Length; i++)
                dropOffZoneSpawnZones[i].Reset();

            for (int i = 0; i < environmentElementsSpawnZones.Length; i++)
                environmentElementsSpawnZones[i].Reset();
        }
        #endregion

        #region Agent
        /// <summary>
        /// Ends the current episode if the environment's max step count has been reached.
        /// </summary>
        private void FixedUpdate() 
        {
            if (camperAgentGroup.GetRegisteredAgents().Count > 0)
                camperAgentGroup.AddGroupReward(camperTimeReward);

            if (mosquito.gameObject.activeInHierarchy)
                mosquito.AddReward(mosquitoTimeReward);

            if (Academy.Instance.StepCount > currentMaxStepCountPerEpisode)
            {
                IsResetConditionMet = true;
                InvokeEpisodeEnd();
            }
        }

        /// <summary>
        /// Provides a random camper spawn position in a random camper spawn zone.
        /// </summary>
        public Vector3 GetRandomCamperSpawnPosition()
        {
            SpawnZone[] unusedZones = Array.FindAll(camperSpawnZones, x => !x.Used);
            int randomIndex = UnityEngine.Random.Range(0, unusedZones.Length);

            return unusedZones[randomIndex].GetRandomPosition();
        }

        /// <summary>
        /// Provides a random mosquito spawn position in a random mosquito spawn zone.
        /// </summary>
        public Vector3 GetRandomMosquitoSpawnPosition()
        {
            SpawnZone[] unusedZones = Array.FindAll(mosquitoSpawnZones, x => !x.Used);
            int randomIndex = UnityEngine.Random.Range(0, unusedZones.Length);

            return unusedZones[randomIndex].GetRandomPosition();
        }

        /// <summary>
        /// Provides a random objective spawn position in a random objective spawn zone.
        /// </summary>
        public Vector3 GetRandomObjectiveSpawnPosition()
        {
            SpawnZone[] unusedZones = Array.FindAll(objectiveSpawnZones, x => !x.Used);
            int randomIndex = UnityEngine.Random.Range(0, unusedZones.Length);

            return unusedZones[randomIndex].GetRandomPosition();
        }

        /// <summary>
        /// Provides a random drop off zone spawn position in a random drop off zone spawn zone.
        /// </summary>
        public Vector3 GetRandomDropOffZoneSpawnPosition()
        {
            SpawnZone[] unusedZones = Array.FindAll(dropOffZoneSpawnZones, x => !x.Used);
            int randomIndex = UnityEngine.Random.Range(0, unusedZones.Length);

            return unusedZones[randomIndex].GetRandomPosition();
        }

        /// <summary>
        /// Provides a random environment element spawn position in a random evironment element spawn zone.
        /// </summary>
        public Vector3 GetRandomEnvironmentSpawnPosition()
        {
            SpawnZone[] unusedZones = Array.FindAll(environmentElementsSpawnZones, x => !x.Used);
            int randomIndex = UnityEngine.Random.Range(0, unusedZones.Length);

            return unusedZones[randomIndex].GetRandomPosition();
        }

        /// <summary>
        /// Invoked when an episode is supposed to begin. Either prepares the environment for a new episode or ends training.
        /// </summary>
        public void InvokeEpisodeBegin()
        {
            Debug.Log("Episode started");
            ResetEpisodeValues();

            if (loopInfinitiely || currentMaxStepCountPerEpisode / maxStepCountPerEpisode < maxEpisodes)
            {
                currentEpisodeCount++;

                ResetSpawnZones();

                for (int i = 0; i < environmentElements.Length; i++)
                {
                    environmentElements[i].transform.position = GetRandomEnvironmentSpawnPosition();
                    environmentElements[i].GetComponentInChildren<NegativeRewardOnCollisionDistributer>()?.Reset();
                }

                dropOffZone.Reset();

                for(int i = 0; i < objectives.Length; i++)
                    objectives[i].Reset();

                mosquito.gameObject.SetActive(true);

                for(int i = 0; i < campers.Length; i++)
                {
                    campers[i].gameObject.SetActive(true);
                    camperAgentGroup.RegisterAgent(campers[i]);
                }

                Debug.Log("Environment reset");
            }
            else
            {
                Debug.Log("Training has ended");
                Application.Quit();
            }
        }

        /// <summary>
        /// Invoked when an episode is supposed to end for whatever reason. The reason is concluded and the episode is terminated if any reason is found, after giving the proper rewards to all agents.
        /// </summary>
        public void InvokeEpisodeEnd()
        {
            bool campersWin = Array.Find(objectives, x => !x.IsCompleted) == null;
            bool mosquitoWins = Array.Find(campers, x => x.gameObject.activeInHierarchy) == null;

            float mosquitoReward = 0f;
            float camperReward = 0f;

            if (mosquitoWins || campersWin || IsResetConditionMet)
            {
                if (IsResetConditionMet)
                {
                    Debug.Log("Episode timed out - no one wins.");
                    mosquitoReward = timeoutReward;
                    camperReward = timeoutReward;
                }
                if (mosquitoWins)
                {
                    Debug.Log("Mosquito wins!");
                    mosquitoReward = winReward;
                    camperReward = lossReward;
                }
                else if (campersWin)
                {
                    Debug.Log("Campers win!");
                    mosquitoReward = lossReward;
                    camperReward = winReward;
                }

                if (IsResetConditionMet)
                {
                    totalTimeouts++;
                }
                else
                {
                    if (camperReward > 0f)
                    {
                        totalCamperWins++;
                        totalMosquitoLosses++;
                    }
                    else
                    {
                        totalCamperLosses++;
                        totalMosquitoWins++;
                    }
                }

                camperAgentGroup.AddGroupReward(camperReward);
                mosquito.AddReward(mosquitoReward);

                if (IsResetConditionMet)
                    camperAgentGroup.GroupEpisodeInterrupted();
                else
                    camperAgentGroup.EndGroupEpisode();

                for(int i = 0; i < campers.Length; i++)
                {
                    if (campers[i].isActiveAndEnabled)
                    {
                        Debug.Log("Camper " + campers[i].name + "'s episode ended");
                        campers[i].DropHeldObjective(false);
                        campers[i].gameObject.SetActive(false);
                    }
                }

                Debug.Log("Mosquito's episode ended");
                mosquito.gameObject.SetActive(false);

                Debug.Log("Episode ended");
                InvokeEpisodeBegin();

                IsResetConditionMet = false;
            }
        }
        #endregion
    }
}
