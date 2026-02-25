using UnityEngine;
using UnityEngine.Events;
using System.Collections.Generic;

namespace Inventory
{
    public class CashConverter : MonoBehaviour
    {
        [Header("Conversion Settings")]
        [Tooltip("Automatically convert items to cash when they enter this container")]
        [SerializeField] private bool autoConvert = true;
        
        [Tooltip("Delay before converting items to cash (in seconds)")]
        [SerializeField] private float conversionDelay = 0.5f;
        
        [Tooltip("Destroy items after converting to cash")]
        [SerializeField] private bool destroyItemsAfterConversion = true;
        
        [Header("Events")]
        public UnityEvent<int> onItemsConverted;
        public UnityEvent<int> onCashEarned;
        
        private List<InventoryItem> pendingItems = new List<InventoryItem>();
        
        private void OnTriggerEnter(Collider other)
        {
            if (!autoConvert)
                return;
            
            InventoryItem item = other.GetComponent<InventoryItem>();
            if (item == null)
            {
                item = other.GetComponentInParent<InventoryItem>();
            }
            
            if (item != null && !pendingItems.Contains(item))
            {
                pendingItems.Add(item);
                Invoke(nameof(ProcessPendingItems), conversionDelay);
            }
        }
        
        public void ConvertAllContainedItems()
        {
            InventoryItem[] items = GetComponentsInChildren<InventoryItem>();
            
            if (items.Length == 0)
            {
                Debug.Log("CashConverter: No items to convert.");
                return;
            }
            
            int totalValue = 0;
            List<GameObject> itemsToDestroy = new List<GameObject>();
            
            foreach (InventoryItem item in items)
            {
                if (item.itemData != null && item.itemData.hasValue)
                {
                    totalValue += item.itemData.value;
                    itemsToDestroy.Add(item.gameObject);
                }
            }
            
            if (totalValue > 0)
            {
                CashManager.Instance.AddCash(totalValue);
                onCashEarned?.Invoke(totalValue);
                onItemsConverted?.Invoke(itemsToDestroy.Count);
                
                Debug.Log($"CashConverter: Converted {itemsToDestroy.Count} items worth ${totalValue}");
                
                if (destroyItemsAfterConversion)
                {
                    foreach (GameObject obj in itemsToDestroy)
                    {
                        Destroy(obj);
                    }
                }
            }
        }
        
        public int CalculateContainedValue()
        {
            return CashManager.Instance.CalculateContainedObjectsValue(transform);
        }
        
        private void ProcessPendingItems()
        {
            if (pendingItems.Count == 0)
                return;
            
            int totalValue = 0;
            List<GameObject> itemsToDestroy = new List<GameObject>();
            
            foreach (InventoryItem item in pendingItems)
            {
                if (item != null && item.itemData != null && item.itemData.hasValue)
                {
                    totalValue += item.itemData.value;
                    itemsToDestroy.Add(item.gameObject);
                }
            }
            
            if (totalValue > 0)
            {
                CashManager.Instance.AddCash(totalValue);
                onCashEarned?.Invoke(totalValue);
                onItemsConverted?.Invoke(itemsToDestroy.Count);
                
                Debug.Log($"CashConverter: Auto-converted {itemsToDestroy.Count} items worth ${totalValue}");
                
                if (destroyItemsAfterConversion)
                {
                    foreach (GameObject obj in itemsToDestroy)
                    {
                        Destroy(obj);
                    }
                }
            }
            
            pendingItems.Clear();
        }
    }
}
