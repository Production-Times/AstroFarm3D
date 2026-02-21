using UnityEngine;
using UnityEngine.Events;
using System.Collections;
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
        public float conveyorSendDelay = 1.5f;
        
        [Header("Custom Events")]
        public UnityEvent<InventoryItem> onItemPlaced;
        public UnityEvent<InventoryItem> onItemRemoved;
        public UnityEvent onCapacityReached;
        
        protected List<InventoryItem> storedItems = new List<InventoryItem>();
        private Queue<InventoryItem> conveyorQueue = new Queue<InventoryItem>();
        private bool isSendingToConveyor = false;
        
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
                QueueItemForConveyor(item);
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
        
        protected virtual void QueueItemForConveyor(InventoryItem item)
        {
            if (item == null || targetConveyorBelt == null)
                return;
            
            conveyorQueue.Enqueue(item);
            
            if (!isSendingToConveyor)
            {
                StartCoroutine(ProcessConveyorQueue());
            }
        }
        
        protected virtual IEnumerator ProcessConveyorQueue()
        {
            isSendingToConveyor = true;
            
            while (conveyorQueue.Count > 0)
            {
                InventoryItem item = conveyorQueue.Peek();
                
                if (item == null)
                {
                    conveyorQueue.Dequeue();
                    continue;
                }
                
                if (!storedItems.Contains(item))
                {
                    conveyorQueue.Dequeue();
                    continue;
                }
                
                if (targetConveyorBelt != null && targetConveyorBelt.CanAcceptItem())
                {
                    conveyorQueue.Dequeue();
                    SendItemToConveyor(item);
                    
                    yield return new WaitForSeconds(conveyorSendDelay);
                }
                else
                {
                    yield return new WaitForSeconds(0.1f);
                }
            }
            
            isSendingToConveyor = false;
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
            for (int i = storedItems.Count - 1; i >= 0; i--)
            {
                if (storedItems[i] == null)
                {
                    storedItems.RemoveAt(i);
                    continue;
                }
                
                Vector3 localPos = stackSettings.GetStackPosition(i);
                storedItems[i].transform.localPosition = localPos;
            }
        }
        
        public virtual void SendAllItemsToConveyor()
        {
            if (!sendToConveyorBelt || targetConveyorBelt == null)
                return;
            
            List<InventoryItem> itemsToQueue = new List<InventoryItem>(storedItems);
            
            foreach (var item in itemsToQueue)
            {
                if (item != null && !conveyorQueue.Contains(item))
                {
                    conveyorQueue.Enqueue(item);
                }
            }
            
            if (!isSendingToConveyor && conveyorQueue.Count > 0)
            {
                StartCoroutine(ProcessConveyorQueue());
            }
        }
        
        public virtual void StopSendingToConveyor()
        {
            conveyorQueue.Clear();
            StopAllCoroutines();
            isSendingToConveyor = false;
        }
        
        public virtual int GetConveyorQueueCount()
        {
            return conveyorQueue.Count;
        }
        
        public virtual bool IsSendingToConveyor()
        {
            return isSendingToConveyor;
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
