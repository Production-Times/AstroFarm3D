using UnityEngine;

namespace Inventory
{
    public class DeliveryTruckStarter : MonoBehaviour
    {
        [Header("References")]
        public DeliveryTruck deliveryTruck;
        
        [Header("Auto Start Settings")]
        public bool autoStartOnAwake = false;
        public float autoStartDelay = 2f;
        
        [Header("Manual Trigger")]
        public KeyCode triggerKey = KeyCode.Space;
        public bool enableKeyboardTrigger = true;
        
        private void Start()
        {
            if (deliveryTruck == null)
            {
                deliveryTruck = FindFirstObjectByType<DeliveryTruck>();
            }
            
            if (autoStartOnAwake && deliveryTruck != null)
            {
                Invoke(nameof(StartDelivery), autoStartDelay);
            }
        }
        
        private void Update()
        {
            if (enableKeyboardTrigger && Input.GetKeyDown(triggerKey))
            {
                StartDelivery();
            }
        }
        
        public void StartDelivery()
        {
            if (deliveryTruck != null)
            {
                deliveryTruck.StartDeliverySequence();
                Debug.Log("[DeliveryTruckStarter] Delivery sequence started!");
            }
            else
            {
                Debug.LogWarning("[DeliveryTruckStarter] No DeliveryTruck reference assigned!");
            }
        }
    }
}
