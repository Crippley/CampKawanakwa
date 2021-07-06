using System.Collections;
using UnityEngine;
using UnityEditor;
using Entities;
using Core;

namespace CustomEditorScripts
{
    #if UNITY_EDITOR

    [CustomEditor(typeof(Player))]
    public class AgentManagerEditor : Editor
    {
        public override void OnInspectorGUI()
        {
            DrawDefaultInspector();

            AgentManager agentManager = (AgentManager)target;
            
            if (GUILayout.Button("Fetch all campers"))
                agentManager.FindAllCampers();

            if (GUILayout.Button("Fetch killer"))
                agentManager.FindKiller();
        }
    }

    #else

    public class AgentManagerEditor : MonoBehaviour { }

    #endif
}
