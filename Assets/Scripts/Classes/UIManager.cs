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
                // TODO: ADD DIFFERENT CAMERA POSITION AND CURSOR BEHAVIOUR FOR EVERYTHING BUT HEURISTIC ONLY TO SIMULATE SCENE VIEW
                if (player.BehaviorType == BehaviorType.HeuristicOnly || player.BehaviorType == BehaviorType.Default)
                {
                    Cursor.lockState = CursorLockMode.Confined;
                }
                else
                {
                    
                }
            }
        }
    }
}