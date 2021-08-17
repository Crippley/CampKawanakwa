using System.Collections;
using System.Collections.Generic;
using Core;
using Unity.MLAgents;
using UnityEngine;

namespace Environment.Training
{
    public class NegativeRewardOnCollisionDistributer : MonoBehaviour
    {
        [SerializeField] AgentManager agentManager;
        [SerializeField] private float negativeCollisionReward = -0.01f;
        [SerializeField] private float negativeCollisionRewardInterval = 0.1f;

        private Dictionary<Agent, Coroutine> collidingAgentsToCoroutines = new Dictionary<Agent, Coroutine>();

        private void OnCollisionEnter(Collision other) 
        {
            Agent agent = other.gameObject.GetComponent<Agent>();

            if (agent && !collidingAgentsToCoroutines.ContainsKey(agent))
            {
                Coroutine collisionCoroutine = StartCoroutine(CollisionCoroutine(agent));

                collidingAgentsToCoroutines.Add(agent, collisionCoroutine);
            }
        }

        private void OnCollisionExit(Collision other) 
        {
            Agent agent = other.gameObject.GetComponent<Agent>();

            if (agent && collidingAgentsToCoroutines.ContainsKey(agent))
            {
                Coroutine collisionCoroutine;

                if (collidingAgentsToCoroutines.TryGetValue(agent, out collisionCoroutine))
                {
                    StopCoroutine(collisionCoroutine);
                    collidingAgentsToCoroutines.Remove(agent);
                }
            }
        }

        IEnumerator CollisionCoroutine(Agent agent)
        {
            while (true)
            {
                agent.AddReward(negativeCollisionReward);
                agentManager.currentCollisionRewards += negativeCollisionReward;

                yield return new WaitForSeconds(negativeCollisionReward);
            }
        }

        public void Reset()
        {
            foreach(KeyValuePair<Agent, Coroutine> value in collidingAgentsToCoroutines)
                StopCoroutine(value.Value);

            collidingAgentsToCoroutines.Clear();
        }
    }
}
