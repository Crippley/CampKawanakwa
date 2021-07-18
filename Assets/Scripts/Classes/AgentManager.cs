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
        public int maxStepCountPerEpisode = 30000;
        public int maxEpisodes;

        public Player killer;
        public DropOffZone dropOffZone;
        public List<Camper> campers = new List<Camper>();
        public List<Objective> objectives = new List<Objective>();
        public List<SpawnZone> camperSpawnZones = new List<SpawnZone>();
        public List<SpawnZone> objectiveSpawnZones = new List<SpawnZone>();

        [NonSerialized] public bool IsKillerConditionMet = Instance && !Instance.IsCamperConditionMet && Instance.campers?.Count == 0;
        [NonSerialized] public bool IsCamperConditionMet = Instance && Instance.objectives.Find(x => !x.IsCompleted) == null;
        [NonSerialized] public bool IsResetConditionMet = false; // TODO: Replace when a reset condition has been found (camper/killer getting stuck, items becoming inaccessible, anything that breaks the game)

        public bool continueLooping;

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

            try
            {
                Academy.Instance.OnEnvironmentReset += InvokeEpisodeBegin;
            }
            catch(Exception e)
            {
                Debug.LogError(e.Message);
            }
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

        public Vector3 GetRandomObjectiveSpawnPosition()
        {
            int randomSpawnZone = UnityEngine.Random.Range(0, objectiveSpawnZones.Count);
            return objectiveSpawnZones[randomSpawnZone].GetRandomPoint();
        }

        public static void InvokeEpisodeBegin()
        {
            Debug.Log("Episode started");

            if (Instance.continueLooping && Instance.currentMaxStepCountPerEpisode / Instance.maxStepCountPerEpisode < Instance.maxEpisodes)
            {
                Instance.dropOffZone.Reset();

                for(int i = 0; i < Instance.campers.Count; i++)
                {
                    Instance.campers[i].gameObject.SetActive(true);
                }

                for(int i = 0; i < Instance.objectives.Count; i++)
                {
                    Instance.objectives[i].Reset();
                }

                Debug.Log("Objective position's reset");
            }
            else
            {
                Application.Quit();
            }
        }

        public static void InvokeEpisodeEnd()
        {
            bool killerWins = Instance.IsKillerConditionMet;
            bool campersWin = Instance.IsCamperConditionMet;

            float killerReward = 0f;

            if (killerWins || campersWin || Instance.IsResetConditionMet)
            {
                if (Instance.IsResetConditionMet)
                {
                    for(int i = 0; i < Instance.campers.Count; i++)
                    {
                        if (Instance.campers[i].isActiveAndEnabled)
                        {
                            Debug.Log("Camper " + Instance.campers[i].name + "'s episode ended");
                            Instance.campers[i].EndEpisode();
                        }
                    }

                    Debug.Log("Killer's episode ended");
                    Instance.killer.EndEpisode();

                    Debug.Log("Episode ended");
                    return;
                }

                if (killerWins)
                {
                    killerReward = Instance.winReward;
                }
                else if (campersWin)
                {
                    killerReward = Instance.lossReward;
                    float camperReward = Instance.winReward;

                    for(int i = 0; i < Instance.campers.Count; i++)
                    {
                        if (Instance.campers[i].isActiveAndEnabled)
                        {
                            Debug.Log("Camper " + Instance.campers[i].name + "'s episode ended");
                            Instance.campers[i].AddReward(camperReward);
                            Instance.campers[i].EndEpisode();
                        }
                    }
                }

                Debug.Log("Killer's episode ended");
                Instance.killer.AddReward(killerReward);
                Instance.killer.EndEpisode();

                Debug.Log("Episode ended");
            }
        }
        #endregion
    }
}
