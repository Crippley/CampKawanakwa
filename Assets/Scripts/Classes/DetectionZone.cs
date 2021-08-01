using System.Collections.Generic;
using UnityEngine;

namespace Entities
{
    public class DetectionZone : MonoBehaviour
    {
        [SerializeField] private GameObject detectionTriggerHandlerHolder;
        [SerializeField] private Collider2D detectionCollider;
        [SerializeField] private LayerMask detectionLayerMask;
        
        [SerializeField] private bool useRaycastingToMaintainDetection;
        [SerializeField] private LayerMask ignoreLayerMask;
        [SerializeField] private Color debugRaycastColor;

        private IDetectionTriggerHandler detectionTriggerHandler;

        private RaycastHit2D[] raycastResults = new RaycastHit2D[5];
        private Dictionary<Collider2D, bool> detectedColliders = new Dictionary<Collider2D, bool>();

        private void Awake() 
        {
            detectionTriggerHandler = detectionTriggerHandlerHolder.GetComponent<IDetectionTriggerHandler>();
        }

        private void OnTriggerEnter2D(Collider2D other) 
        {
            if (detectionTriggerHandler == null)
            {
                Debug.LogError("Detection couldn't be processed, no detectionTriggerHandler assigned to the detectionTriggerHandlerHolder");
                return;
            }

            GameObject detectedObject = other.gameObject;

            if (detectedObject && detectedObject != detectionTriggerHandlerHolder)
            {
                if (detectionLayerMask == (detectionLayerMask | (1 << detectedObject.layer)) && ignoreLayerMask != (ignoreLayerMask | (1 << detectedObject.layer)))
                {
                    if (!detectedColliders.ContainsKey(other))
                    {
                        detectedColliders.Add(other, !useRaycastingToMaintainDetection);

                        if (!useRaycastingToMaintainDetection)
                            detectionTriggerHandler.OnEnterDetectionZone(other.gameObject, detectionCollider);
                    }
                }
            }
        }

        private void OnTriggerExit2D(Collider2D other) 
        {
            if (detectionTriggerHandler == null)
            {
                Debug.LogError("Detection couldn't be processed, no detectiontriggerhandler assigned to the detectionTriggerHandlerHolder");
                return;
            }

            GameObject detectedObject = other.gameObject;

            if (detectedObject && detectedObject != detectionTriggerHandlerHolder)
            {
                if (detectionLayerMask == (detectionLayerMask | (1 << detectedObject.layer)))
                {
                    if (detectedColliders.ContainsKey(other))
                    {
                        detectedColliders.Remove(other);
                        detectionTriggerHandler.OnLeaveDetectionZone(other.gameObject, detectionCollider);
                    }
                }
            }
        }

        //TODO: Consider moving this code from an update loop to a coroutine that fires on a set timer
        private void Update() 
        {
            if (useRaycastingToMaintainDetection)
            {
                Dictionary<Collider2D, bool> collidersToModify = new Dictionary<Collider2D, bool>();
                foreach(KeyValuePair<Collider2D, bool> pair in detectedColliders)
                {
                    int length = Physics2D.RaycastNonAlloc(transform.position, pair.Key.transform.position - transform.position, raycastResults,  Vector2.Distance(transform.position, pair.Key.transform.position), ~ignoreLayerMask);

                    float closestDistance = float.PositiveInfinity;
                    RaycastHit2D? closestHit = null;

                    for(int i = 0; i < length; i++)
                    {
                        RaycastHit2D hit = raycastResults[i];

                        if (hit.collider != null && hit.collider.gameObject != detectionTriggerHandlerHolder && hit.distance < closestDistance)
                        {
                            closestDistance = hit.distance;
                            closestHit = hit;
                        }
                    }

                    bool visible = closestHit?.collider == pair.Key;
                    
                    if (visible && !pair.Value)
                        detectionTriggerHandler.OnEnterDetectionZone(pair.Key.gameObject, detectionCollider);
                    else if (!visible && pair.Value)
                        detectionTriggerHandler.OnLeaveDetectionZone(pair.Key.gameObject, detectionCollider);

                    if (visible != pair.Value)
                        collidersToModify.Add(pair.Key, !pair.Value);
                }

                foreach(KeyValuePair<Collider2D, bool> pair in collidersToModify)
                    detectedColliders[pair.Key] = pair.Value;
            }
        }

        public void SetIgnoreLayerMask(LayerMask mask)
        {
            ignoreLayerMask = mask;

            Dictionary<Collider2D, bool> collidersToModify = new Dictionary<Collider2D, bool>();

            foreach(KeyValuePair<Collider2D, bool> pair in detectedColliders)
            {
                if (ignoreLayerMask == (ignoreLayerMask | (1 << pair.Key.gameObject.layer)))
                {
                    collidersToModify.Add(pair.Key, pair.Value);
                }
            }

            foreach(KeyValuePair<Collider2D, bool> pair in collidersToModify)
            {
                detectedColliders.Remove(pair.Key);
                detectionTriggerHandler.OnLeaveDetectionZone(pair.Key.gameObject, detectionCollider);
            }
        }
    }
}