using Unity.MLAgents.Policies;
using Core;
using UnityEngine;
using Entities;

namespace UI
{
    public class UIManager : MonoBehaviour
    {
        private void Start() 
        {
            Player player = AgentManager.Instance?.killer;

            if (player)
            {
                if (player.BehaviorType == BehaviorType.HeuristicOnly || player.BehaviorType == BehaviorType.Default)
                {
                    Cursor.lockState = CursorLockMode.Confined;
                }
            }
        }
    }
}