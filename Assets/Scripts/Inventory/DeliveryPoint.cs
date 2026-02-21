using UnityEngine;
using UnityEngine.Events;
using System.Collections;

namespace Inventory
{
    public class DeliveryPoint : DropPoint
    {
        [Header("Delivery Settings")]
        public bool useTimedDelivery = true;
        public float deliveryTime = 5f;
        
        [Header("Coin Rewards")]
        public UnityEvent<int> onCoinsGranted;
        public UnityEvent onDeliveryComplete;
        
        private bool isProcessingDelivery = false;
        private float deliveryTimer = 0f;
        
        private void Update()
        {
            if (isProcessingDelivery && useTimedDelivery)
            {
                deliveryTimer += Time.deltaTime;
                
                if (deliveryTimer >= deliveryTime)
                {
                    ProcessDelivery();
                }
            }
        }
        
        public void StartDelivery()
        {
            if (storedItems.Count == 0 || isProcessingDelivery)
                return;
            
            foreach (var item in storedItems)
            {
                if (item != null)
                {
                    item.OnDeliveryPoint();
                }
            }
            
            if (useTimedDelivery)
            {
                isProcessingDelivery = true;
                deliveryTimer = 0f;
            }
            else
            {
                ProcessDelivery();
            }
        }
        
        public void UnloadDelivery()
        {
            ProcessDelivery();
        }
        
        private void ProcessDelivery()
        {
            int totalValue = CalculateTotalValue();
            
            foreach (var item in storedItems)
            {
                if (item != null)
                {
                    item.OnSelling();
                }
            }
            
            ClearAllItems();
            
            onCoinsGranted?.Invoke(totalValue);
            onDeliveryComplete?.Invoke();
            
            isProcessingDelivery = false;
            deliveryTimer = 0f;
        }
        
        private int CalculateTotalValue()
        {
            int total = 0;
            
            foreach (var item in storedItems)
            {
                if (item != null && item.itemData != null && item.itemData.hasValue)
                {
                    total += item.itemData.value;
                }
            }
            
            return total;
        }
        
        public float GetDeliveryProgress()
        {
            if (!useTimedDelivery || !isProcessingDelivery)
                return 0f;
            
            return Mathf.Clamp01(deliveryTimer / deliveryTime);
        }
        
        public bool IsProcessing()
        {
            return isProcessingDelivery;
        }
    }
}
