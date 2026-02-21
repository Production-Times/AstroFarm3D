using UnityEngine;
using UnityEngine.Events;
using System.Collections.Generic;

namespace Inventory
{
    public class ConveyorBelt : MonoBehaviour
    {
        [Header("Conveyor Settings")]
        public Transform startPoint;
        public Transform endPoint;
        public float conveyorSpeed = 2f;
        
        [Header("Item Spacing")]
        [Tooltip("Minimum progress difference between items (0.0 to 1.0). 0.15 = 15% of belt length")]
        public float minimumItemSpacing = 0.2f;
        public bool enforceSpacing = true;
        
        [Header("Processing")]
        public bool processesItems = true;
        public GameObject processingParticlePrefab;
        public Vector3 particleOffset = Vector3.zero;
        
        [Header("Events")]
        public UnityEvent<InventoryItem> onItemEntered;
        public UnityEvent<InventoryItem> onItemProcessed;
        public UnityEvent<InventoryItem> onItemExited;
        
        private List<ConveyorItemData> itemsOnBelt = new List<ConveyorItemData>();
        
        private class ConveyorItemData
        {
            public InventoryItem item;
            public float progress;
            public bool hasBeenProcessed;
        }
        
        private void Awake()
        {
            if (startPoint == null || endPoint == null)
            {
                Debug.LogWarning($"ConveyorBelt on {gameObject.name} is missing start or end point!");
            }
        }
        
        private void Update()
        {
            UpdateConveyorItems();
        }
        
        private void UpdateConveyorItems()
        {
            if (startPoint == null || endPoint == null)
                return;
            
            for (int i = itemsOnBelt.Count - 1; i >= 0; i--)
            {
                ConveyorItemData data = itemsOnBelt[i];
                
                data.progress += conveyorSpeed * Time.deltaTime / Vector3.Distance(startPoint.position, endPoint.position);
                
                if (data.progress >= 0.5f && !data.hasBeenProcessed && processesItems)
                {
                    ProcessItem(data);
                }
                
                if (data.progress >= 1f)
                {
                    RemoveItemFromBelt(i);
                }
                else
                {
                    Vector3 currentPos = Vector3.Lerp(startPoint.position, endPoint.position, data.progress);
                    data.item.transform.position = currentPos;
                }
            }
        }
        
        public void AddItemToBelt(InventoryItem item)
        {
            if (item == null)
            {
                Debug.LogWarning("Attempted to add null item to conveyor belt");
                return;
            }
            
            if (enforceSpacing && !CanAcceptItem())
            {
                return;
            }
            
            ConveyorItemData data = new ConveyorItemData
            {
                item = item,
                progress = 0f,
                hasBeenProcessed = false
            };
            
            itemsOnBelt.Add(data);
            
            item.transform.SetParent(transform);
            item.transform.position = startPoint.position;
            item.OnConveyorBeltProcessing();
            
            onItemEntered?.Invoke(item);
        }
        
        public bool CanAcceptItem()
        {
            if (!enforceSpacing)
                return true;
            
            if (itemsOnBelt.Count == 0)
                return true;
            
            ConveyorItemData lastItem = itemsOnBelt[itemsOnBelt.Count - 1];
            
            return lastItem.progress >= minimumItemSpacing;
        }
        
        private void ProcessItem(ConveyorItemData data)
        {
            data.hasBeenProcessed = true;
            
            if (data.item.itemData != null && data.item.itemData.canBeProcessed && data.item.itemData.processedResult != null)
            {
                ItemData newItemData = data.item.itemData.processedResult;
                
                if (processingParticlePrefab != null)
                {
                    Vector3 spawnPos = data.item.transform.position + particleOffset;
                    Instantiate(processingParticlePrefab, spawnPos, Quaternion.identity);
                }
                
                if (newItemData.prefab != null)
                {
                    Vector3 itemPos = data.item.transform.position;
                    Quaternion itemRot = data.item.transform.rotation;
                    
                    Destroy(data.item.gameObject);
                    
                    GameObject newItemObj = Instantiate(newItemData.prefab, itemPos, itemRot);
                    InventoryItem newItem = newItemObj.GetComponent<InventoryItem>();
                    
                    if (newItem == null)
                    {
                        newItem = newItemObj.AddComponent<InventoryItem>();
                    }
                    
                    newItem.itemData = newItemData;
                    data.item = newItem;
                    data.item.transform.SetParent(transform);
                    data.item.OnConveyorBeltProcessing();
                }
                else
                {
                    data.item.itemData = newItemData;
                }
                
                onItemProcessed?.Invoke(data.item);
            }
        }
        
        private void RemoveItemFromBelt(int index)
        {
            ConveyorItemData data = itemsOnBelt[index];
            itemsOnBelt.RemoveAt(index);
            
            data.item.transform.SetParent(null);
            data.item.transform.position = endPoint.position;
            data.item.OnDropped();
            
            onItemExited?.Invoke(data.item);
        }
        
        private void OnTriggerEnter(Collider other)
        {
            InventoryItem item = other.GetComponent<InventoryItem>();
            if (item == null)
            {
                item = other.GetComponentInParent<InventoryItem>();
            }
            
            if (item != null && !item.isBeingCarried && !item.isPlaced)
            {
                bool alreadyOnBelt = false;
                foreach (var data in itemsOnBelt)
                {
                    if (data.item == item)
                    {
                        alreadyOnBelt = true;
                        break;
                    }
                }
                
                if (!alreadyOnBelt)
                {
                    AddItemToBelt(item);
                }
            }
        }
        
        private void OnDrawGizmosSelected()
        {
            if (startPoint != null && endPoint != null)
            {
                Gizmos.color = Color.blue;
                Gizmos.DrawLine(startPoint.position, endPoint.position);
                
                Gizmos.color = Color.green;
                Gizmos.DrawWireSphere(startPoint.position, 0.3f);
                
                Gizmos.color = Color.red;
                Gizmos.DrawWireSphere(endPoint.position, 0.3f);
                
                Gizmos.color = Color.yellow;
                Vector3 midPoint = Vector3.Lerp(startPoint.position, endPoint.position, 0.5f);
                Gizmos.DrawWireSphere(midPoint, 0.2f);
                
                if (enforceSpacing && minimumItemSpacing > 0)
                {
                    Gizmos.color = Color.cyan;
                    Vector3 spacingPoint = Vector3.Lerp(startPoint.position, endPoint.position, minimumItemSpacing);
                    Gizmos.DrawWireSphere(spacingPoint, 0.15f);
                    Gizmos.DrawLine(startPoint.position, spacingPoint);
                }
            }
        }
        
        public int GetItemCount()
        {
            return itemsOnBelt.Count;
        }
    }
}
