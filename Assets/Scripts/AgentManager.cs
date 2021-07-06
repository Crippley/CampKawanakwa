using System.Collections;
using System.Collections.Generic;
using Entities;
using UnityEngine;

#if UNITY_EDITOR
using System.Linq;
#endif

namespace Core
{
    public class AgentManager : MonoBehaviour
    {
        #region Vars
        public static AgentManager Instance { get; private set; }

        public static float winReward;
        public static float lossReward;

        public Player killer;
        public List<Camper> campers = new List<Camper>();
        #endregion
        
        #region Editor code
        #if UNITY_EDITOR
        public void FindAllCampers()
        {
            campers = FindObjectsOfType<Camper>().ToList();
        }

        public void FindKiller()
        {
            killer = FindObjectOfType<Player>();
        }
        #endif
        #endregion

        #region Init
        private void Awake()
        {
            if (Instance != null)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;
        }
        #endregion

        #region Agent
        public static void InvokeEpisodeBegin()
        {

        }

        public static void InvokeEpisodeEnd()
        {
            if (Instance.campers.Count == 0)
            {
                // TODO: AWARD REWARD TO PLAYER, AWARD PUNISHMENTS TO CAMPERS, END EPISODE
            }
            else
            {
                // TODO: GO THROUGH ALL OBJECTIVES AND SEE IF THEY ARE COMPLETED AND IF THEY ARE, AWARD REWARD TO CAMPERS, AWARD PUNISHMENT TO PLAYER, END EPISODE
            }
        }
        #endregion
    }
}
