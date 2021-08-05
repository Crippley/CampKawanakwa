using System.Collections;
using System.Collections.Generic;
using Entities;
using UnityEngine;
using Items;
using System;
using Unity.MLAgents;
using Zones;
using Environment.Training;

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
        public float timeoutReward;
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

        [NonSerialized] public StatsRecorder statsRecorder;

        public bool continueLooping;

        [Header("Read only training values")]
        [Header("Win counters")]
        public int killerWins;
        public int camperWins;

        [Header("Current episode rewards")]
        [Header("Misc rewards")]
        public float currentCollisionRewards;

        [Header("Killer rewards")]
        public float currentKillRewards;

        [Header("Camper rewards")]
        public float currentObjectivePickedUpRewards;
        public float currentObjectiveDroppedOffRewards;

        public float currentDeathRewards;

        [Header("Total rewards")]
        [Header("Misc rewards")]
        public float totalTimeouts;
        public float totalCollisionRewards;

        [Header("Killer rewards")]
        public float totalKillRewards;

        public float totalKillerWins;
        public float totalKillerLosses;

        [Header("Camper rewards")]
        public float totalObjectivePickedUpRewards;
        public float totalObjectiveDroppedOffRewards;

        public float totalDeathRewards;

        public float totalCamperWins;
        public float totalCamperLosses;

        private int currentMaxStepCountPerEpisode = 0;

        private Dictionary<Camper, Objective> assignedObjectives = new Dictionary<Camper, Objective>();
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

            #region Killer rewards
            totalKillRewards += currentKillRewards;
            statsRecorder.Add("Agent/Killer/KillRewards", totalKillRewards, StatAggregationMethod.MostRecent);
            currentKillRewards = 0f;

            statsRecorder.Add("Agent/Killer/VictoryRewards", totalKillerWins, StatAggregationMethod.MostRecent);

            statsRecorder.Add("Agent/Killer/DefeatRewards", totalKillerLosses, StatAggregationMethod.MostRecent);
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
            assignedObjectives.Clear();
        }
        #endregion

        #region Agent
        /// <summary>
        /// Ends the current episode if the environment's max step count has been reached.
        /// </summary>
        private void FixedUpdate() 
        {
            if (Academy.Instance.StepCount > currentMaxStepCountPerEpisode)
            {
                IsResetConditionMet = true;
                InvokeEpisodeEnd();
            }
        }

        // TODO: ADD UNCHOSEN SPAWN POINT PREFERENCE WHEN CHOOSING SPAWN ZONES
        /// <summary>
        /// Provides a random camper spawn position in a random camper spawn zone.
        /// </summary>
        public Vector3 GetRandomCamperSpawnPosition()
        {
            int randomSpawnZone = UnityEngine.Random.Range(0, camperSpawnZones.Count);
            return camperSpawnZones[randomSpawnZone].GetRandomPoint();
        }

        // TODO: ADD UNCHOSEN SPAWN POINT PREFERENCE WHEN CHOOSING SPAWN ZONES
        /// <summary>
        /// Provides a random killer spawn position in a random killer spawn zone.
        /// </summary>
        public Vector3 GetRandomKillerSpawnPosition()
        {
            int randomSpawnZone = UnityEngine.Random.Range(0, killerSpawnZones.Count);
            return killerSpawnZones[randomSpawnZone].GetRandomPoint();
        }

        // TODO: ADD UNCHOSEN SPAWN POINT PREFERENCE WHEN CHOOSING SPAWN ZONES
        /// <summary>
        /// Provides a random objective spawn position in a random objective spawn zone.
        /// </summary>
        public Vector3 GetRandomObjectiveSpawnPosition()
        {
            int randomSpawnZone = UnityEngine.Random.Range(0, objectiveSpawnZones.Count);
            return objectiveSpawnZones[randomSpawnZone].GetRandomPoint();
        }

        // TODO: ADD UNCHOSEN SPAWN POINT PREFERENCE WHEN CHOOSING SPAWN ZONES
        /// <summary>
        /// Provides a random drop off zone spawn position in a random drop off zone spawn zone.
        /// </summary>
        public Vector3 GetRandomDropOffZoneSpawnPosition()
        {
            int randomSpawnZone = UnityEngine.Random.Range(0, dropOffZoneSpawnZones.Count);
            return dropOffZoneSpawnZones[randomSpawnZone].GetRandomPoint();
        }

        // TODO: ADD UNCHOSEN SPAWN POINT PREFERENCE WHEN CHOOSING SPAWN ZONES
        /// <summary>
        /// Provides a random environment element spawn position in a random evironment element spawn zone.
        /// </summary>
        public Vector3 GetRandomEnvironmentSpawnPosition()
        {
            int randomSpawnZone = UnityEngine.Random.Range(0, environmentSpawnZones.Count);
            return environmentSpawnZones[randomSpawnZone].GetRandomPoint();
        }

        public Vector3? AssignCamperToObjective(Camper camper)
        {
            if (assignedObjectives.Count >= objectives.Count)   
                return null;
            
            Objective freeObjective = null;

            for (int i = 0; i < objectives.Count; i++)
            {
                if (objectives[i].IsActive && !objectives[i].IsCompleted && !assignedObjectives.ContainsValue(objectives[i]))
                {
                    freeObjective = objectives[i];
                    break;
                }
            }

            if (freeObjective = null)
                return null;

            assignedObjectives.Add(camper, freeObjective);

            return freeObjective.transform.position;
        }

        public void ObjectiveDropped(Camper droppingCamper, Objective droppedobjective)
        {
            assignedObjectives.Remove(droppingCamper);

            if (!droppedobjective.IsActive && droppedobjective.IsCompleted)
                return;

            for (int i = 0; i < campers.Count; i++)
            {
                if (!campers[i].isDead && campers[i].currentGoal == null)
                {
                    campers[i].currentGoal = droppedobjective.transform.position;
                    return;
                }
            }
        } 

        /// <summary>
        /// Invoked when an episode is supposed to begin. Either prepares the environment for a new episode or ends training.
        /// </summary>
        public void InvokeEpisodeBegin()
        {
            Debug.Log("Episode started");
            ResetEpisodeValues();

            if (continueLooping || currentMaxStepCountPerEpisode / maxStepCountPerEpisode < maxEpisodes)
            {
                currentEpisodeCount++;

                for (int i = 0; i < environmentPieces.Count; i++)
                {
                    environmentPieces[i].transform.position = GetRandomEnvironmentSpawnPosition();
                    environmentPieces[i].GetComponentInChildren<NegativeRewardOnCollisionDistributer>()?.Reset();
                }

                dropOffZone.Reset();

                for(int i = 0; i < objectives.Count; i++)
                    objectives[i].Reset();

                killer.gameObject.SetActive(true);

                for(int i = 0; i < campers.Count; i++)
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
            bool campersWin = objectives.Find(x => !x.IsCompleted) == null;
            bool killerWins = campers.Find(x => x.gameObject.activeInHierarchy) == null;

            float killerReward = 0f;
            float camperReward = 0f;

            if (killerWins || campersWin || IsResetConditionMet)
            {
                if (IsResetConditionMet)
                {
                    Debug.Log("Episode timedout.");
                    killerReward = timeoutReward;
                    camperReward = timeoutReward;
                }
                if (killerWins)
                {
                    Debug.Log("Killer wins!");
                    killerReward = winReward;
                    camperReward = lossReward;
                }
                else if (campersWin)
                {
                    Debug.Log("Campers win!");
                    killerReward = lossReward;
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
                        totalKillerLosses++;
                    }
                    else
                    {
                        totalCamperLosses++;
                        totalKillerWins++;
                    }
                }

                camperAgentGroup.AddGroupReward(camperReward);
                killer.AddReward(killerReward);

                if (IsResetConditionMet)
                    camperAgentGroup.EndGroupEpisode();
                else
                    camperAgentGroup.GroupEpisodeInterrupted();

                for(int i = 0; i < campers.Count; i++)
                {
                    if (campers[i].isActiveAndEnabled)
                    {
                        Debug.Log("Camper " + campers[i].name + "'s episode ended");
                        campers[i].RemoveItem(false);
                        campers[i].gameObject.SetActive(false);
                    }
                }

                Debug.Log("Killer's episode ended");
                killer.gameObject.SetActive(false);

                Debug.Log("Episode ended");
                InvokeEpisodeBegin();
            }
        }
        #endregion
    }
}
