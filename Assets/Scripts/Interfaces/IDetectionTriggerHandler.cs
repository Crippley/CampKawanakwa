using UnityEngine;

namespace Entities
{
    public interface IDetectionTriggerHandler
    {
        void OnEnterDetectionZone(GameObject detectedObject, Collider2D detectingCollider);
        void OnLeaveDetectionZone(GameObject detectedObject, Collider2D detectingCollider);
    }
}
