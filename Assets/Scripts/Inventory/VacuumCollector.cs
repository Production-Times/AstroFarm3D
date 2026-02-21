using UnityEngine;
using UnityEngine.Events;
using System.Collections;
using System.Collections.Generic;

namespace Inventory
{
    public class VacuumCollector : MonoBehaviour
    {
        [Header("Vacuum Settings")]
        public Transform vacuumNozzle;
        public Transform storagePoint;
        public float vacuumRange = 5f;
        public float vacuumForce = 10f;
        public float collectDistance = 0.5f;
        
        [Header("Capacity")]
        public int maxCapacity = 20;
        
        [Header("Collection Filter")]
        public List<ItemData> acceptedItems = new List<ItemData>();
        public bool acceptAllItems = true;
        public LayerMask itemLayer;
        
        [Header("Visual Effects")]
        public GameObject collectParticlePrefab;
        public LineRenderer vacuumBeam;
        public Vector3 particleOffset = Vector3.zero;
        
        [Header("Events")]
        public UnityEvent<InventoryItem> onItemCaptured;
        public UnityEvent<InventoryItem> onItemCollected;
        public UnityEvent onCapacityFull;
        
        private List<InventoryItem> collectedItems = new List<InventoryItem>();
        private List<InventoryItem> itemsBeingVacuumed = new List<InventoryItem>();
        private bool isVacuumActive = false;
        
        private void Awake()
        {
            if (vacuumNozzle == null)
                vacuumNozzle = transform;
            if (storagePoint == null)
                storagePoint = transform;
        }
        
        private void Update()
        {
            if (isVacuumActive)
            {
                VacuumNearbyItems();
            }
            
            UpdateVacuumBeam();
        }
        
        public void StartVacuum()
        {
            isVacuumActive = true;
        }
        
        public void StopVacuum()
        {
            isVacuumActive = false;
            ReleaseAllVacuumedItems();
        }
        
        public bool IsFull()
        {
            return collectedItems.Count >= maxCapacity;
        }
        
        private void VacuumNearbyItems()
        {
            if (IsFull())
            {
                StopVacuum();
                onCapacityFull?.Invoke();
                return;
            }
            
            Collider[] nearbyColliders = Physics.OverlapSphere(vacuumNozzle.position, vacuumRange, itemLayer);
            
            foreach (Collider col in nearbyColliders)
            {
                InventoryItem item = col.GetComponent<InventoryItem>();
                if (item == null)
                {
                    item = col.GetComponentInParent<InventoryItem>();
                }
                
                if (item != null && CanVacuumItem(item))
                {
                    VacuumItem(item);
                }
            }
        }
        
        private bool CanVacuumItem(InventoryItem item)
        {
            if (item == null || item.itemData == null)
                return false;
            
            if (item.isBeingCarried || item.isPlaced)
                return false;
            
            if (collectedItems.Contains(item) || itemsBeingVacuumed.Contains(item))
                return false;
            
            if (IsFull())
                return false;
            
            if (acceptAllItems)
                return true;
            
            return acceptedItems.Contains(item.itemData);
        }
        
        private void VacuumItem(InventoryItem item)
        {
            if (!itemsBeingVacuumed.Contains(item))
            {
                itemsBeingVacuumed.Add(item);
                item.OnVacuumCaptured();
                onItemCaptured?.Invoke(item);
            }
            
            Vector3 directionToNozzle = vacuumNozzle.position - item.transform.position;
            float distance = directionToNozzle.magnitude;
            
            if (distance <= collectDistance)
            {
                CollectItem(item);
            }
            else
            {
                item.transform.position = Vector3.MoveTowards(
                    item.transform.position,
                    vacuumNozzle.position,
                    vacuumForce * Time.deltaTime
                );
            }
        }
        
        private void CollectItem(InventoryItem item)
        {
            itemsBeingVacuumed.Remove(item);
            collectedItems.Add(item);
            
            item.transform.SetParent(storagePoint);
            item.transform.localPosition = Vector3.zero;
            item.gameObject.SetActive(false);
            
            if (collectParticlePrefab != null)
            {
                Instantiate(collectParticlePrefab, vacuumNozzle.position + particleOffset, Quaternion.identity);
            }
            
            onItemCollected?.Invoke(item);
        }
        
        private void ReleaseAllVacuumedItems()
        {
            foreach (InventoryItem item in itemsBeingVacuumed)
            {
                if (item != null)
                {
                    item.OnDropped();
                }
            }
            itemsBeingVacuumed.Clear();
        }
        
        public InventoryItem UnloadItem()
        {
            if (collectedItems.Count == 0)
                return null;
            
            InventoryItem item = collectedItems[0];
            collectedItems.RemoveAt(0);
            
            item.transform.SetParent(null);
            item.gameObject.SetActive(true);
            item.OnDropped();
            
            return item;
        }
        
        public List<InventoryItem> UnloadAllItems()
        {
            List<InventoryItem> unloadedItems = new List<InventoryItem>(collectedItems);
            
            foreach (InventoryItem item in unloadedItems)
            {
                if (item != null)
                {
                    item.transform.SetParent(null);
                    item.gameObject.SetActive(true);
                    item.OnDropped();
                }
            }
            
            collectedItems.Clear();
            return unloadedItems;
        }
        
        public int GetCollectedCount()
        {
            return collectedItems.Count;
        }
        
        private void UpdateVacuumBeam()
        {
            if (vacuumBeam != null)
            {
                vacuumBeam.enabled = isVacuumActive && itemsBeingVacuumed.Count > 0;
                
                if (vacuumBeam.enabled && itemsBeingVacuumed.Count > 0)
                {
                    vacuumBeam.positionCount = itemsBeingVacuumed.Count * 2;
                    
                    for (int i = 0; i < itemsBeingVacuumed.Count; i++)
                    {
                        if (itemsBeingVacuumed[i] != null)
                        {
                            vacuumBeam.SetPosition(i * 2, vacuumNozzle.position);
                            vacuumBeam.SetPosition(i * 2 + 1, itemsBeingVacuumed[i].transform.position);
                        }
                    }
                }
            }
        }
        
        private void OnDrawGizmosSelected()
        {
            Transform nozzle = vacuumNozzle != null ? vacuumNozzle : transform;
            
            Gizmos.color = new Color(0, 1, 1, 0.3f);
            Gizmos.DrawWireSphere(nozzle.position, vacuumRange);
            
            Gizmos.color = Color.cyan;
            Gizmos.DrawWireSphere(nozzle.position, collectDistance);
            
            if (storagePoint != null)
            {
                Gizmos.color = Color.green;
                Gizmos.DrawWireCube(storagePoint.position, Vector3.one * 0.5f);
            }
        }
    }
}
