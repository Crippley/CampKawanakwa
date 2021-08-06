using Core;
using Entities;
using UnityEngine;

namespace Items
{
    public class Objective : MonoBehaviour
    {
        public Transform initialParent;
        public SpriteRenderer spriteRenderer;

        public LayerMask initialLayer;
        public Color initialColor;
        public Color heldColor;
        public Color droppedOffColor;

        public bool IsActive {get; set;}
        public bool IsCompleted {get; set;}

        #region Initialization
        private void Start() 
        {
            Reset();
        }

        public void Reset()
        {
            transform.parent = initialParent;
            transform.position = AgentManager.Instance.GetRandomObjectiveSpawnPosition();
            IsActive = true;
            IsCompleted = false;
            spriteRenderer.color = initialColor;
            gameObject.layer = initialLayer;
        }
        #endregion

        private void OnTriggerEnter2D(Collider2D other) 
        {
            if (!IsActive)
                return;

            Camper triggeringCamper = other?.GetComponent<Camper>();

            if (triggeringCamper && triggeringCamper.HeldItem == null)
            {
                triggeringCamper.AddItem(this);
                transform.SetParent(triggeringCamper.transform, true);
                IsActive = false;
                spriteRenderer.color = heldColor;
                gameObject.layer = other.gameObject.layer;
            }
        }
    }
}