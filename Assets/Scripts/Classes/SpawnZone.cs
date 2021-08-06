using UnityEngine;

namespace Entities
{
    public class SpawnZone : MonoBehaviour
    {
        [SerializeField] private float spawnRadius;
        [SerializeField] private Color sphereColor;

        private bool used;
        public bool Used => used;

        public void Reset()
        {
            used = false;
        }

        public Vector3 GetRandomPosition()
        {
            used = true;
            
            Vector3 randomPosition = Random.insideUnitSphere * spawnRadius;
            randomPosition.Set(randomPosition.x, 0.5f, randomPosition.z);

            return transform.position + randomPosition;
        }

        private void OnDrawGizmosSelected() 
        {
            Gizmos.color = sphereColor;
            Gizmos.DrawSphere(transform.position, spawnRadius);
        }
    }
}
