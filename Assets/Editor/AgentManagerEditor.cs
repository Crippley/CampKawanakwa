using System.Collections;
using UnityEngine;
using UnityEditor;
using Entities;
using Core;

namespace CustomEditorScripts
{
    #if UNITY_EDITOR

    [CustomEditor(typeof(AgentManager))]
    public class AgentManagerEditor : Editor
    {
        public override void OnInspectorGUI()
        {
            DrawDefaultInspector();

            /*AgentManager agentManager = target as AgentManager;

            if (agentManager)
            {
                if (GUILayout.Button("Fetch mosquito"))
                    agentManager.FindMosquito();

                if (GUILayout.Button("Fetch all campers"))
                    agentManager.FindAllCampers();

                if (GUILayout.Button("Fetch all objectives"))
                    agentManager.FindAllObjectives();
            }*/
        }
    }

    #else

    public class AgentManagerEditor : MonoBehaviour { }

    #endif
}
