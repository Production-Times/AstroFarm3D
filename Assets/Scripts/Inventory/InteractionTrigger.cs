using UnityEngine;
using UnityEngine.Events;

namespace Inventory
{
    [RequireComponent(typeof(Collider))]
    public class InteractionTrigger : MonoBehaviour
    {
        [Header("Trigger Settings")]
        public string targetTag = "Player";
        public bool requiresInput = false;
        public KeyCode interactionKey = KeyCode.E;
        
        [Header("Events")]
        public UnityEvent onTriggerEntered;
        public UnityEvent onTriggerStayed;
        public UnityEvent onTriggerExited;
        public UnityEvent onInteraction;
        
        private bool isPlayerInRange = false;
        
        private void Awake()
        {
            Collider col = GetComponent<Collider>();
            if (!col.isTrigger)
            {
                col.isTrigger = true;
            }
        }
        
        private void Update()
        {
            if (requiresInput && isPlayerInRange && Input.GetKeyDown(interactionKey))
            {
                onInteraction?.Invoke();
            }
        }
        
        private void OnTriggerEnter(Collider other)
        {
            if (string.IsNullOrEmpty(targetTag) || other.CompareTag(targetTag))
            {
                isPlayerInRange = true;
                onTriggerEntered?.Invoke();
            }
        }
        
        private void OnTriggerStay(Collider other)
        {
            if (string.IsNullOrEmpty(targetTag) || other.CompareTag(targetTag))
            {
                onTriggerStayed?.Invoke();
            }
        }
        
        private void OnTriggerExit(Collider other)
        {
            if (string.IsNullOrEmpty(targetTag) || other.CompareTag(targetTag))
            {
                isPlayerInRange = false;
                onTriggerExited?.Invoke();
            }
        }
    }
}
