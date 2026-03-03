using UnityEngine;
using UnityEngine.Events;
using System.Collections.Generic;

namespace Inventory
{
    public class PlayerBackpack : MonoBehaviour
    {
        [Header("Backpack Settings")]
        public Transform backpackSlot;
        public int baseMaxCapacity = 10;
        [HideInInspector]
        public int maxCapacity = 10;
        public float pickupRange = 3f;
        public float attractionSpeed = 8f;
        public float collectionDistance = 0.5f;
        public LayerMask itemLayerMask = -1;
        
        [Header("Stack Visualization")]
        public Vector3 itemSpacing = new Vector3(0.3f, 0.3f, 0);
        
        [Header("Drop Settings")]
        public float dropForce = 2f;
        public Vector3 dropOffset = new Vector3(0, 0, 1f);
        
        [Header("Drop Point Detection")]
        public float dropPointRange = 2f;
        public bool autoDropAtDropPoints = true;
        public bool excludeVehicleDropPoints = true;
        
        [Header("Debug")]
        public bool debugPickupChecks = true;
        
        [Header("Events")]
        public UnityEvent<int> onInventoryChanged;
        public UnityEvent onInventoryFull;
        
        private List<InventoryItem> stack = new List<InventoryItem>();
        private Collider[] hitColliders = new Collider[20];
        private DropPoint nearestDropPoint;
        private HashSet<InventoryItem> attractedItems = new HashSet<InventoryItem>();
        private StorageTransferPad nearbyTransferPad;
        private float transferPadCheckTimer = 0f;
        private const float TRANSFER_PAD_CHECK_INTERVAL = 0.2f;
        
        private void Awake()
        {
            if (backpackSlot == null)
            {
                GameObject slot = new GameObject("BackpackSlot");
                slot.transform.SetParent(transform);
                slot.transform.localPosition = new Vector3(0, 1.5f, -0.5f);
                backpackSlot = slot.transform;
            }
            
            ApplyUpgrade();
        }
        
        public void ApplyUpgrade()
        {
            if (UpgradeManager.Instance != null && UpgradeManager.Instance.upgradeDatabase != null)
            {
                int upgradedCapacity = Mathf.RoundToInt(UpgradeManager.Instance.GetUpgradeValue(UpgradeType.PlayerBackpackCapacity));
                if (upgradedCapacity > 0)
                {
                    maxCapacity = upgradedCapacity;
                }
                else
                {
                    maxCapacity = baseMaxCapacity;
                }
            }
            else
            {
                maxCapacity = baseMaxCapacity;
            }
        }
        
        private void Update()
        {
            CheckForNearbyTransferPad();
            
            // Vacuum pickup - DISABLED when on transfer pad
            if (stack.Count < maxCapacity && nearbyTransferPad == null)
            {
                PerformVacuum();
            }
            
            // Auto drop at drop points
            if (autoDropAtDropPoints && stack.Count > 0)
            {
                DetectAndDropAtDropPoints();
            }
        }
        
        private void CheckForNearbyTransferPad()
        {
            transferPadCheckTimer += Time.deltaTime;
            
            if (transferPadCheckTimer >= TRANSFER_PAD_CHECK_INTERVAL)
            {
                transferPadCheckTimer = 0f;
                
                StorageTransferPad[] transferPads = FindObjectsByType<StorageTransferPad>(FindObjectsSortMode.None);
                nearbyTransferPad = null;
                
                foreach (var pad in transferPads)
                {
                    if (pad != null)
                    {
                        float distance = Vector3.Distance(transform.position, pad.transform.position);
                        if (distance < pickupRange + 1f)
                        {
                            nearbyTransferPad = pad;
                            break;
                        }
                    }
                }
            }
        }
        
        private void PerformVacuum()
        {
            int numColliders = Physics.OverlapSphereNonAlloc(transform.position, pickupRange, hitColliders, itemLayerMask);
            
            HashSet<InventoryItem> currentFrameItems = new HashSet<InventoryItem>();
            
            for (int i = 0; i < numColliders; i++)
            {
                var col = hitColliders[i];
                if (col == null) continue;

                InventoryItem item = col.GetComponent<InventoryItem>();
                if (item == null)
                {
                    item = col.GetComponentInParent<InventoryItem>();
                }

                if (item != null)
                {
                    currentFrameItems.Add(item);
                    
                    if (CanPickupItem(item))
                    {
                        float distToBackpack = Vector3.Distance(backpackSlot.position, item.transform.position);

                        if (distToBackpack <= collectionDistance)
                        {
                            AddToStack(item);
                            attractedItems.Remove(item);
                        }
                        else
                        {
                            AttractItem(item, backpackSlot.position);
                            attractedItems.Add(item);
                        }
                    }
                    else
                    {
                        if (attractedItems.Contains(item))
                        {
                            ReleaseItem(item);
                            attractedItems.Remove(item);
                        }
                    }
                }
                else
                {
                    // Try Collectible from Harvesting system
                    Harvesting.Collectible collectible = col.GetComponent<Harvesting.Collectible>();
                    if (collectible == null)
                    {
                        collectible = col.GetComponentInParent<Harvesting.Collectible>();
                    }

                    if (collectible != null && !collectible.isCollected)
                    {
                        float distToBackpack = Vector3.Distance(backpackSlot.position, collectible.transform.position);

                        if (distToBackpack <= collectionDistance)
                        {
                            // Convert to InventoryItem and add to stack
                            InventoryItem convertedItem = collectible.GetComponent<InventoryItem>();
                            if (convertedItem == null)
                            {
                                convertedItem = collectible.gameObject.AddComponent<InventoryItem>();
                            }
                            
                            collectible.OnCollected();
                            AddToStack(convertedItem);
                        }
                        else
                        {
                            // Make collider a trigger during attraction
                            Collider collectibleCol = collectible.GetComponent<Collider>();
                            if (collectibleCol != null && !collectibleCol.isTrigger)
                            {
                                collectibleCol.isTrigger = true;
                            }
                            
                            collectible.AttractTo(backpackSlot.position, attractionSpeed);
                        }
                    }
                }
            }
        }
        
        private void AttractItem(InventoryItem item, Vector3 targetPosition)
        {
            if (!CanPickupItem(item))
                return;
            
            // Make collider a trigger to avoid physics collisions with player
            Collider col = item.GetComponent<Collider>();
            if (col != null && !col.isTrigger)
            {
                col.isTrigger = true;
            }
            
            // Disable physics during attraction
            Rigidbody rb = item.GetComponent<Rigidbody>();
            if (rb != null)
            {
                rb.isKinematic = true;
                rb.useGravity = false;
                rb.linearVelocity = Vector3.zero;
            }
            
            // Move item towards backpack
            item.transform.position = Vector3.MoveTowards(item.transform.position, targetPosition, attractionSpeed * Time.deltaTime);
        }
        
        private void ReleaseItem(InventoryItem item)
        {
            if (item == null)
                return;
            
            ItemState currentState = item.GetCurrentState();
            item.SetState(currentState);
            
            if (debugPickupChecks)
            {
                Debug.Log($"[PlayerBackpack] Released item {item.name} back to state {currentState}");
            }
        }
        
        private bool CanPickupItem(InventoryItem item)
        {
            if (item == null)
                return false;
            
            if (!item.CanBePickedUp())
            {
                if (debugPickupChecks)
                    Debug.Log($"[PlayerBackpack] Cannot pickup {item.name}: Drop cooldown active");
                return false;
            }
            
            if (item.isBeingCarried)
            {
                if (debugPickupChecks)
                    Debug.Log($"[PlayerBackpack] Cannot pickup {item.name}: isBeingCarried = true");
                return false;
            }
            
            if (item.isPlaced)
            {
                if (debugPickupChecks)
                    Debug.Log($"[PlayerBackpack] Cannot pickup {item.name}: isPlaced = true, State = {item.GetCurrentState()}");
                return false;
            }
            
            if (item.transform.parent != null)
            {
                DropPoint dropPoint = item.transform.parent.GetComponent<DropPoint>();
                if (dropPoint != null)
                {
                    if (debugPickupChecks)
                        Debug.Log($"[PlayerBackpack] Cannot pickup {item.name}: parented to DropPoint ({dropPoint.name})");
                    return false;
                }
                
                ConveyorBelt conveyorBelt = item.transform.parent.GetComponent<ConveyorBelt>();
                if (conveyorBelt != null)
                {
                    if (debugPickupChecks)
                        Debug.Log($"[PlayerBackpack] Cannot pickup {item.name}: parented to ConveyorBelt ({conveyorBelt.name})");
                    return false;
                }
            }
            
            ItemState currentState = item.GetCurrentState();
            if (currentState != ItemState.Free && currentState != ItemState.TractorUnloaded)
            {
                if (debugPickupChecks)
                    Debug.Log($"[PlayerBackpack] Cannot pickup {item.name}: Invalid state = {currentState}");
                return false;
            }
            
            return true;
        }
        
        private void AddToStack(InventoryItem item)
        {
            if (stack.Count >= maxCapacity)
            {
                onInventoryFull?.Invoke();
                return;
            }

            stack.Add(item);
            
            // Disable physics completely when in backpack
            Rigidbody rb = item.GetComponent<Rigidbody>();
            if (rb != null)
            {
                rb.isKinematic = true;
                rb.useGravity = false;
            }
            
            // Disable collider to prevent physics interactions
            Collider col = item.GetComponent<Collider>();
            if (col != null)
            {
                col.enabled = false;
            }
            
            item.transform.SetParent(backpackSlot);
            
            Vector3 targetPos = CalculateStackPosition(stack.Count - 1);
            item.transform.localPosition = targetPos;
            item.transform.localRotation = Quaternion.identity;
            
            item.OnPickedUp();
            
            onInventoryChanged?.Invoke(stack.Count);
        }
        
        private Vector3 CalculateStackPosition(int index)
        {
            return new Vector3(
                0,
                index * itemSpacing.y,
                index * itemSpacing.z
            );
        }
        
        public void DropAllItems()
        {
            if (stack.Count == 0)
                return;
            
            foreach (var item in stack)
            {
                if (item != null)
                {
                    item.transform.SetParent(null);
                    item.transform.position = transform.position + transform.TransformDirection(dropOffset);
                    item.OnDropped();
                    
                    // Re-enable physics and restore collider to solid
                    Rigidbody rb = item.GetComponent<Rigidbody>();
                    if (rb != null)
                    {
                        rb.isKinematic = false;
                        rb.useGravity = true;
                        rb.AddForce(transform.forward * dropForce, ForceMode.Impulse);
                    }
                    
                    // Restore collider to solid (not trigger)
                    Collider col = item.GetComponent<Collider>();
                    if (col != null)
                    {
                        col.enabled = true;
                        col.isTrigger = false;
                    }
                }
            }
            
            stack.Clear();
            onInventoryChanged?.Invoke(0);
        }
        
        public int GetItemCount()
        {
            return stack.Count;
        }
        
        public bool IsFull()
        {
            return stack.Count >= maxCapacity;
        }
        
        private void DetectAndDropAtDropPoints()
        {
            // Find all DropPoints in range
            DropPoint[] dropPoints = FindObjectsByType<DropPoint>(FindObjectsSortMode.None);
            DropPoint closestDropPoint = null;
            float closestDistance = dropPointRange;
            
            foreach (var dropPoint in dropPoints)
            {
                if (excludeVehicleDropPoints && dropPoint is VehicleDropPoint)
                {
                    continue;
                }
                
                float distance = Vector3.Distance(transform.position, dropPoint.transform.position);
                if (distance < closestDistance)
                {
                    closestDropPoint = dropPoint;
                    closestDistance = distance;
                }
            }
            
            nearestDropPoint = closestDropPoint;
            
            // Auto-transfer items to the drop point
            if (nearestDropPoint != null && stack.Count > 0)
            {
                TryTransferToDropPoint(nearestDropPoint);
            }
        }
        
        private void TryTransferToDropPoint(DropPoint dropPoint)
        {
            // Transfer items one by one until full or no more items
            for (int i = stack.Count - 1; i >= 0; i--)
            {
                InventoryItem item = stack[i];
                if (item != null && dropPoint.CanAcceptItem(item))
                {
                    // Remove from our stack
                    stack.RemoveAt(i);
                    
                    // Let the drop point handle placement
                    dropPoint.TryPlaceItem(item);
                    
                    onInventoryChanged?.Invoke(stack.Count);
                }
                
                // If drop point is full, stop trying
                if (dropPoint.IsFull())
                {
                    break;
                }
            }
        }
        
        public DropPoint GetNearestDropPoint()
        {
            return nearestDropPoint;
        }
        
        public List<InventoryItem> GetBackpackItems()
        {
            return new List<InventoryItem>(stack);
        }
        
        public bool RemoveItemFromBackpack(InventoryItem item)
        {
            if (stack.Contains(item))
            {
                stack.Remove(item);
                onInventoryChanged?.Invoke(stack.Count);
                return true;
            }
            return false;
        }
        
        private void OnDrawGizmosSelected()
        {
            Gizmos.color = Color.cyan;
            Gizmos.DrawWireSphere(transform.position, pickupRange);
            
            Gizmos.color = Color.magenta;
            Gizmos.DrawWireSphere(transform.position, dropPointRange);
            
            if (backpackSlot != null)
            {
                Gizmos.color = Color.yellow;
                for (int i = 0; i < maxCapacity; i++)
                {
                    Vector3 pos = backpackSlot.TransformPoint(CalculateStackPosition(i));
                    Gizmos.DrawWireCube(pos, itemSpacing * 0.8f);
                }
            }
        }
    }
}
