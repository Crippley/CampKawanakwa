using System.Collections;
using System.Collections.Generic;
using Unity.MLAgents;
using UnityEngine;

namespace Environment.Training
{
    public class NegativeRewardOnCollisionDistributer : MonoBehaviour
    {
        [SerializeField] private float negativeCollisionReward = 1f;
        [SerializeField] private float negativeCollisionRewardRepetitionInterval = 1f;

        private Dictionary<Agent, Coroutine> collidingAgentsToCoroutines = new Dictionary<Agent, Coroutine>();

        private void OnCollisionEnter2D(Collision2D other) 
        {
            Agent agent = other.gameObject.GetComponent<Agent>();

            if (agent)
            {
                Coroutine collisionCoroutine = StartCoroutine(CollisionCoroutine(agent));
                collidingAgentsToCoroutines.Add(agent, collisionCoroutine);
            }
        }

        private void OnCollisionExit2D(Collision2D other) 
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
            agent.AddReward(negativeCollisionReward);
            yield return new WaitForSeconds(negativeCollisionRewardRepetitionInterval);
        }
    }
}
