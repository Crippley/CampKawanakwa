using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Entities
{
    public class SpawnZone : MonoBehaviour
    {
        [SerializeField] private float spawnRadius;
        [SerializeField] private LayerMask spawnZoneLayerMask;

        public Vector3 GetRandomPoint()
        {
            Vector3 randomPosition;
            bool positionTaken = true;

            do
            {
                randomPosition = Random.insideUnitCircle * spawnRadius;
                positionTaken = Physics2D.OverlapPoint(randomPosition, spawnZoneLayerMask);
            } while(positionTaken);

            return transform.position + randomPosition;
        }

        private void OnDrawGizmosSelected() 
        {
            Gizmos.DrawSphere(transform.position, spawnRadius);
        }
    }
}
