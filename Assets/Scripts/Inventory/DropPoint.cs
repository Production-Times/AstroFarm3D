using UnityEngine;
using UnityEngine.Events;
using System.Collections.Generic;

namespace Inventory
{
    public class DropPoint : MonoBehaviour
    {
        [Header("Point Settings")]
        public Transform dropPointTransform;
        
        [Header("Capacity")]
        public bool hasCapacity = true;
        public int maxCapacity = 10;
        
        [Header("Eligible Items")]
        public List<ItemData> eligibleItems = new List<ItemData>();
        public bool acceptAllItems = false;
        
        [Header("Stack Visualization")]
        public StackSettings stackSettings = new StackSettings();
        
        [Header("Particle Effects")]
        public GameObject placementParticlePrefab;
        public Vector3 particleOffset = Vector3.zero;
        
        [Header("Conveyor Belt Processing")]
        public bool sendToConveyorBelt = false;
        public ConveyorBelt targetConveyorBelt;
        
        [Header("Custom Events")]
        public UnityEvent<InventoryItem> onItemPlaced;
        public UnityEvent<InventoryItem> onItemRemoved;
        public UnityEvent onCapacityReached;
        
        protected List<InventoryItem> storedItems = new List<InventoryItem>();
        
        protected virtual void Awake()
        {
            if (dropPointTransform == null)
            {
                dropPointTransform = transform;
            }
        }
        
        public virtual bool CanAcceptItem(InventoryItem item)
        {
            if (item == null || item.itemData == null)
                return false;
            
            if (hasCapacity && storedItems.Count >= maxCapacity)
                return false;
            
            if (acceptAllItems)
                return true;
            
            return eligibleItems.Contains(item.itemData);
        }
        
        public virtual bool TryPlaceItem(InventoryItem item)
        {
            if (!CanAcceptItem(item))
                return false;
            
            storedItems.Add(item);
            
            item.transform.SetParent(dropPointTransform);
            Vector3 localPos = stackSettings.GetStackPosition(storedItems.Count - 1);
            item.transform.localPosition = localPos;
            item.transform.localRotation = Quaternion.identity;
            
            item.OnPlaced();
            
            SpawnPlacementParticle(item.transform.position);
            
            onItemPlaced?.Invoke(item);
            
            if (hasCapacity && storedItems.Count >= maxCapacity)
            {
                onCapacityReached?.Invoke();
            }
            
            if (sendToConveyorBelt && targetConveyorBelt != null)
            {
                SendItemToConveyor(item);
            }
            
            return true;
        }
        
        public virtual InventoryItem RemoveItem(int index)
        {
            if (index < 0 || index >= storedItems.Count)
                return null;
            
            InventoryItem item = storedItems[index];
            storedItems.RemoveAt(index);
            
            item.transform.SetParent(null);
            item.OnDropped();
            
            onItemRemoved?.Invoke(item);
            
            RefreshStackPositions();
            
            return item;
        }
        
        public virtual InventoryItem RemoveLastItem()
        {
            if (storedItems.Count == 0)
                return null;
            
            return RemoveItem(storedItems.Count - 1);
        }
        
        public virtual void ClearAllItems()
        {
            while (storedItems.Count > 0)
            {
                InventoryItem item = RemoveLastItem();
                if (item != null)
                {
                    Destroy(item.gameObject);
                }
            }
        }
        
        protected virtual void SendItemToConveyor(InventoryItem item)
        {
            if (item == null || targetConveyorBelt == null)
                return;
            
            storedItems.Remove(item);
            
            targetConveyorBelt.AddItemToBelt(item);
            
            RefreshStackPositions();
        }
        
        protected virtual void RefreshStackPositions()
        {
            for (int i = 0; i < storedItems.Count; i++)
            {
                Vector3 localPos = stackSettings.GetStackPosition(i);
                storedItems[i].transform.localPosition = localPos;
            }
        }
        
        protected virtual void SpawnPlacementParticle(Vector3 worldPosition)
        {
            if (placementParticlePrefab != null)
            {
                Vector3 spawnPos = worldPosition + particleOffset;
                Instantiate(placementParticlePrefab, spawnPos, Quaternion.identity);
            }
        }
        
        public int GetCurrentCount()
        {
            return storedItems.Count;
        }
        
        public bool IsFull()
        {
            return hasCapacity && storedItems.Count >= maxCapacity;
        }
        
        public List<InventoryItem> GetStoredItems()
        {
            return new List<InventoryItem>(storedItems);
        }
        
        private void OnDrawGizmosSelected()
        {
            Transform point = dropPointTransform != null ? dropPointTransform : transform;
            
            Gizmos.color = IsFull() ? Color.red : Color.green;
            Gizmos.DrawWireSphere(point.position, 0.3f);
            
            Gizmos.matrix = point.localToWorldMatrix;
            Gizmos.color = new Color(0, 1, 0, 0.3f);
            
            int visualizeCount = hasCapacity ? maxCapacity : 10;
            for (int i = 0; i < visualizeCount; i++)
            {
                Vector3 localPos = stackSettings.GetStackPosition(i);
                Gizmos.DrawWireCube(localPos, stackSettings.GetGizmoSize());
            }
            
            Gizmos.matrix = Matrix4x4.identity;
        }
    }
}
