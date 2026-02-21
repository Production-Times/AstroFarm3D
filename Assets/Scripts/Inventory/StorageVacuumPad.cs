using UnityEngine;
using UnityEngine.Events;
using System.Collections.Generic;

namespace Inventory
{
    public class StorageVacuumPad : MonoBehaviour
    {
        [Header("Pad Settings")]
        public string padName = "Storage Pad";
        public Color padColor = Color.blue;
        
        [Header("Vacuum Settings")]
        public float vacuumRadius = 3f;
        public float vacuumSpeed = 8f;
        public float collectDistance = 0.5f;
        public LayerMask itemLayer = -1;
        
        [Header("Capacity")]
        public int maxCapacity = 20;
        
        [Header("Item Filter")]
        public List<ItemData> acceptedItemTypes = new List<ItemData>();
        public bool acceptAllItems = false;
        
        [Header("Stack Settings")]
        public StackSettings stackSettings = new StackSettings();
        
        [Header("Visual Feedback")]
        public GameObject vacuumParticlePrefab;
        public GameObject collectParticlePrefab;
        public Vector3 particleOffset = Vector3.zero;
        public Renderer padRenderer;
        public string colorPropertyName = "_BaseColor";
        
        [Header("Events")]
        public UnityEvent<InventoryItem> onItemCaptured;
        public UnityEvent<InventoryItem> onItemStored;
        public UnityEvent onStorageFull;
        
        private List<InventoryItem> storedItems = new List<InventoryItem>();
        private HashSet<InventoryItem> attractingItems = new HashSet<InventoryItem>();
        private Collider[] nearbyColliders = new Collider[20];
        private bool isActive = true;
        private Material padMaterial;
        
        private void Awake()
        {
            if (padRenderer != null)
            {
                padMaterial = padRenderer.material;
                SetPadColor(padColor);
            }
        }
        
        private void Update()
        {
            if (isActive && !IsFull())
            {
                VacuumNearbyItems();
            }
        }
        
        private void VacuumNearbyItems()
        {
            int count = Physics.OverlapSphereNonAlloc(transform.position, vacuumRadius, nearbyColliders, itemLayer);
            
            HashSet<InventoryItem> currentFrameItems = new HashSet<InventoryItem>();
            
            for (int i = 0; i < count; i++)
            {
                if (nearbyColliders[i] == null) continue;
                
                InventoryItem item = nearbyColliders[i].GetComponent<InventoryItem>();
                if (item == null)
                {
                    item = nearbyColliders[i].GetComponentInParent<InventoryItem>();
                }
                
                if (item != null)
                {
                    currentFrameItems.Add(item);
                    
                    if (CanVacuumItem(item))
                    {
                        float distance = Vector3.Distance(transform.position, item.transform.position);
                        
                        if (distance <= collectDistance)
                        {
                            StoreItem(item);
                            attractingItems.Remove(item);
                        }
                        else
                        {
                            AttractItem(item);
                            attractingItems.Add(item);
                        }
                    }
                }
            }
            
            attractingItems.RemoveWhere(item => item == null || !currentFrameItems.Contains(item));
        }
        
        private bool CanVacuumItem(InventoryItem item)
        {
            if (item == null || item.itemData == null)
                return false;
            
            if (item.isBeingCarried || item.isPlaced)
                return false;
            
            if (storedItems.Contains(item))
                return false;
            
            if (IsFull())
                return false;
            
            ItemState state = item.GetCurrentState();
            if (state != ItemState.Free && state != ItemState.TractorUnloaded)
                return false;
            
            if (acceptAllItems)
                return true;
            
            return acceptedItemTypes.Contains(item.itemData);
        }
        
        private void AttractItem(InventoryItem item)
        {
            Collider col = item.GetComponent<Collider>();
            if (col != null && !col.isTrigger)
            {
                col.isTrigger = true;
            }
            
            Rigidbody rb = item.GetComponent<Rigidbody>();
            if (rb != null)
            {
                rb.isKinematic = true;
                rb.useGravity = false;
                rb.linearVelocity = Vector3.zero;
            }
            
            item.transform.position = Vector3.MoveTowards(
                item.transform.position,
                transform.position,
                vacuumSpeed * Time.deltaTime
            );
            
            if (!attractingItems.Contains(item))
            {
                onItemCaptured?.Invoke(item);
            }
        }
        
        private void StoreItem(InventoryItem item)
        {
            if (IsFull())
                return;
            
            storedItems.Add(item);
            
            item.transform.SetParent(transform);
            Vector3 localPos = stackSettings.GetStackPosition(storedItems.Count - 1);
            item.transform.localPosition = localPos;
            item.transform.localRotation = Quaternion.identity;
            
            Rigidbody rb = item.GetComponent<Rigidbody>();
            if (rb != null)
            {
                rb.isKinematic = true;
                rb.useGravity = false;
                rb.linearVelocity = Vector3.zero;
                rb.angularVelocity = Vector3.zero;
            }
            
            Collider col = item.GetComponent<Collider>();
            if (col != null)
            {
                col.enabled = false;
            }
            
            item.OnPlaced();
            
            if (collectParticlePrefab != null)
            {
                Instantiate(collectParticlePrefab, item.transform.position + particleOffset, Quaternion.identity);
            }
            
            onItemStored?.Invoke(item);
            
            if (IsFull())
            {
                onStorageFull?.Invoke();
            }
        }
        
        public InventoryItem RemoveItem()
        {
            if (storedItems.Count == 0)
                return null;
            
            InventoryItem item = storedItems[0];
            storedItems.RemoveAt(0);
            
            item.transform.SetParent(null);
            item.OnDropped();
            
            Collider col = item.GetComponent<Collider>();
            if (col != null)
            {
                col.enabled = true;
                col.isTrigger = false;
            }
            
            Rigidbody rb = item.GetComponent<Rigidbody>();
            if (rb != null)
            {
                rb.isKinematic = false;
                rb.useGravity = true;
            }
            
            return item;
        }
        
        public List<InventoryItem> RemoveAllItems()
        {
            List<InventoryItem> items = new List<InventoryItem>(storedItems);
            
            foreach (var item in items)
            {
                if (item != null)
                {
                    item.transform.SetParent(null);
                    item.OnDropped();
                    
                    Collider col = item.GetComponent<Collider>();
                    if (col != null)
                    {
                        col.enabled = true;
                        col.isTrigger = false;
                    }
                    
                    Rigidbody rb = item.GetComponent<Rigidbody>();
                    if (rb != null)
                    {
                        rb.isKinematic = false;
                        rb.useGravity = true;
                    }
                }
            }
            
            storedItems.Clear();
            return items;
        }
        
        public int GetStoredCount()
        {
            return storedItems.Count;
        }
        
        public bool IsFull()
        {
            return storedItems.Count >= maxCapacity;
        }
        
        public List<InventoryItem> GetStoredItems()
        {
            return new List<InventoryItem>(storedItems);
        }
        
        public void SetActive(bool active)
        {
            isActive = active;
        }
        
        private void SetPadColor(Color color)
        {
            if (padMaterial != null)
            {
                padMaterial.SetColor(colorPropertyName, color);
            }
        }
        
        private void OnDrawGizmosSelected()
        {
            Gizmos.color = new Color(padColor.r, padColor.g, padColor.b, 0.3f);
            Gizmos.DrawWireSphere(transform.position, vacuumRadius);
            
            Gizmos.color = padColor;
            Gizmos.DrawWireSphere(transform.position, collectDistance);
            
            if (stackSettings != null)
            {
                for (int i = 0; i < maxCapacity; i++)
                {
                    Vector3 stackPos = transform.TransformPoint(stackSettings.GetStackPosition(i));
                    Gizmos.color = new Color(padColor.r, padColor.g, padColor.b, 0.5f);
                    Gizmos.DrawWireCube(stackPos, Vector3.one * 0.3f);
                }
            }
        }
    }
}
