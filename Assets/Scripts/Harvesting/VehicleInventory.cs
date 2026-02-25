using UnityEngine;
using UnityEngine.Events;
using System.Collections.Generic;

namespace Harvesting
{
    public class VehicleInventory : MonoBehaviour
    {
        [Header("Stacking Settings")]
        [Tooltip("Point where the stack begins.")]
        public Transform trunkPoint;
        [Tooltip("Maximum number of items.")]
        public int baseMaxCapacity = 40;
        [HideInInspector]
        public int maxCapacity = 40;
        [Tooltip("Spacing between items in the stack.")]
        public Vector3 itemSpacing = new Vector3(0.5f, 0.5f, 0.5f);
        [Tooltip("Grid dimensions for stacking (e.g. 3x3 base).")]
        public Vector2Int gridDimensions = new Vector2Int(3, 3);
        
        [Header("Vacuum Settings")]
        [Tooltip("Radius to attract collectibles.")]
        public float baseVacuumRadius = 6.0f;
        [HideInInspector]
        public float vacuumRadius = 6.0f;
        [Tooltip("Force/Speed at which collectibles are pulled.")]
        public float vacuumSpeed = 15f;
        [Tooltip("Distance at which the item is considered 'collected'.")]
        public float collectionDistance = 0.5f;

        [Tooltip("Layers to include in the vacuum check.")]
        public LayerMask vacuumLayers = -1;
        
        [Header("Drop Settings")]
        public Transform dropLocation;
        public bool autoDropOnExit = false;
        
        [Header("Events")]
        public UnityEvent<int> onInventoryChanged;
        public UnityEvent onInventoryFull;
        public UnityEvent onInventoryDropped;

        private List<Collectible> stack = new List<Collectible>();
        
        private Collider[] hitColliders = new Collider[20];
        
        private void Awake()
        {
            ApplyUpgrades();
        }
        
        public void ApplyUpgrades()
        {
            if (Inventory.UpgradeManager.Instance != null && Inventory.UpgradeManager.Instance.upgradeDatabase != null)
            {
                float upgradedRadius = Inventory.UpgradeManager.Instance.GetUpgradeValue(Inventory.UpgradeType.VehicleVacuumRadius);
                int upgradedCapacity = Mathf.RoundToInt(Inventory.UpgradeManager.Instance.GetUpgradeValue(Inventory.UpgradeType.VehicleMaxCapacity));
                
                vacuumRadius = upgradedRadius > 0 ? upgradedRadius : baseVacuumRadius;
                maxCapacity = upgradedCapacity > 0 ? upgradedCapacity : baseMaxCapacity;
            }
            else
            {
                vacuumRadius = baseVacuumRadius;
                maxCapacity = baseMaxCapacity;
            }
        }

        private void Update()
        {
            // Vacuum Logic
            if (stack.Count < maxCapacity)
            {
                PerformVacuum();
            }
        }

        private void PerformVacuum()
        {
            int numColliders = Physics.OverlapSphereNonAlloc(transform.position, vacuumRadius, hitColliders, vacuumLayers);
            
            for (int i = 0; i < numColliders; i++)
            {
                var col = hitColliders[i];
                if (col == null) continue;

                Collectible collectible = col.GetComponent<Collectible>();
                if (collectible == null)
                {
                    // Check parent just in case collider is on a child
                    collectible = col.GetComponentInParent<Collectible>();
                }

                if (collectible != null && !collectible.isCollected)
                {
                    // Check distance to trunk
                    float distToTrunk = Vector3.Distance(trunkPoint.position, collectible.transform.position);

                    if (distToTrunk <= collectionDistance)
                    {
                        AddToStack(collectible);
                    }
                    else
                    {
                        // Attract towards trunk
                        collectible.AttractTo(trunkPoint.position, vacuumSpeed);
                    }
                }
            }
        }

        private void AddToStack(Collectible item)
        {
            if (stack.Count >= maxCapacity)
            {
                onInventoryFull?.Invoke();
                return;
            }

            item.OnCollected();
            stack.Add(item);

            item.transform.SetParent(trunkPoint);
            
            Vector3 targetPos = CalculateStackPosition(stack.Count - 1);
            
            item.transform.localPosition = targetPos;
            item.transform.localRotation = Quaternion.identity;

            StartCoroutine(SquashAndStretch(item.transform));
            
            onInventoryChanged?.Invoke(stack.Count);
        }
        
        public void DropAllItems()
        {
            if (stack.Count == 0)
            {
                Debug.Log("VehicleInventory: No items to drop.");
                return;
            }
            
            Transform targetLocation = dropLocation != null ? dropLocation : transform;
            Debug.Log($"VehicleInventory: Dropping {stack.Count} items at {targetLocation.name} (position: {targetLocation.position})");
            
            Inventory.VehicleDropPoint vehicleDropPoint = targetLocation.GetComponent<Inventory.VehicleDropPoint>();
            
            if (vehicleDropPoint != null && vehicleDropPoint.skipVehicleDropPoint)
            {
                Inventory.StorageSystemManager storageManager = FindFirstObjectByType<Inventory.StorageSystemManager>();
                
                if (storageManager != null)
                {
                    Debug.Log($"VehicleInventory: Skipping drop point, transferring {stack.Count} items directly to storage.");
                    
                    for (int i = 0; i < stack.Count; i++)
                    {
                        var collectible = stack[i];
                        if (collectible != null)
                        {
                            Inventory.InventoryItem inventoryItem = collectible.GetComponent<Inventory.InventoryItem>();
                            
                            if (inventoryItem != null)
                            {
                                collectible.transform.SetParent(null);
                                bool success = storageManager.TransferItemToStorage(inventoryItem);
                                
                                if (success)
                                {
                                    Debug.Log($"VehicleInventory: Transferred '{collectible.name}' directly to storage.");
                                }
                                else
                                {
                                    Debug.LogWarning($"VehicleInventory: Failed to transfer '{collectible.name}' to storage. Dropping at vehicle position.");
                                    collectible.transform.position = targetLocation.position + new Vector3((i % 3) * 1.5f - 1.5f, 0.5f, (i / 3) * 1.5f);
                                    collectible.EnablePhysics();
                                    inventoryItem.OnDropped();
                                }
                            }
                        }
                    }
                }
                else
                {
                    Debug.LogWarning("VehicleInventory: Skip Vehicle Drop Point enabled but no StorageSystemManager found. Falling back to normal drop.");
                    DropItemsNormally(vehicleDropPoint, targetLocation);
                }
            }
            else if (vehicleDropPoint != null)
            {
                DropItemsNormally(vehicleDropPoint, targetLocation);
            }
            else
            {
                for (int i = 0; i < stack.Count; i++)
                {
                    var item = stack[i];
                    if (item != null)
                    {
                        item.transform.SetParent(null);
                        
                        Vector3 dropOffset = new Vector3(
                            (i % 3) * 1.5f - 1.5f,
                            0.5f,
                            (i / 3) * 1.5f
                        );
                        
                        item.transform.position = targetLocation.position + dropOffset;
                        item.transform.rotation = Quaternion.identity;
                        
                        item.EnablePhysics();
                        
                        Inventory.InventoryItem inventoryItem = item.GetComponent<Inventory.InventoryItem>();
                        if (inventoryItem != null)
                        {
                            inventoryItem.OnDropped();
                        }
                        
                        Debug.Log($"VehicleInventory: Dropped '{item.name}' at position {item.transform.position}");
                    }
                }
            }
            
            stack.Clear();
            onInventoryDropped?.Invoke();
            onInventoryChanged?.Invoke(0);
        }
        
        private void DropItemsNormally(Inventory.VehicleDropPoint vehicleDropPoint, Transform targetLocation)
        {
            for (int i = 0; i < stack.Count; i++)
            {
                var collectible = stack[i];
                if (collectible != null)
                {
                    Inventory.InventoryItem inventoryItem = collectible.GetComponent<Inventory.InventoryItem>();
                    
                    if (inventoryItem != null)
                    {
                        Transform dropTransform = vehicleDropPoint.dropPointTransform != null ? vehicleDropPoint.dropPointTransform : vehicleDropPoint.transform;
                        
                        Vector3 localGridPos = vehicleDropPoint.stackSettings.GetStackPosition(i);
                        Vector3 worldPos = dropTransform.TransformPoint(localGridPos);
                        
                        collectible.transform.SetParent(null);
                        collectible.transform.position = worldPos;
                        collectible.transform.rotation = dropTransform.rotation;
                        
                        collectible.EnablePhysics();
                        inventoryItem.OnDropped();
                        
                        Debug.Log($"VehicleInventory: Placed '{collectible.name}' at grid position {worldPos} (local: {localGridPos}, index {i})");
                    }
                    else
                    {
                        Debug.LogWarning($"VehicleInventory: {collectible.name} has no InventoryItem component!");
                        collectible.transform.SetParent(null);
                        collectible.transform.position = targetLocation.position + new Vector3((i % 3) * 1.5f - 1.5f, 0.5f, (i / 3) * 1.5f);
                        collectible.EnablePhysics();
                    }
                }
            }
        }
        
        public int GetItemCount()
        {
            return stack.Count;
        }
        
        public bool IsFull()
        {
            return stack.Count >= maxCapacity;
        }

        private Vector3 CalculateStackPosition(int index)
        {
            // Grid math
            // Layer index = index / (rows * cols)
            // Position in layer = index % (rows * cols)
            
            int itemsPerLayer = gridDimensions.x * gridDimensions.y;
            int layer = index / itemsPerLayer;
            int posInLayer = index % itemsPerLayer;
            
            int x = posInLayer % gridDimensions.x;
            int z = posInLayer / gridDimensions.x; // Z is forward/back on the trunk usually
            
            // Center the stack maybe? 
            // For now, start from 0,0,0 and expand
            
            return new Vector3(
                x * itemSpacing.x - (gridDimensions.x * itemSpacing.x * 0.5f) + (itemSpacing.x * 0.5f), // Center X
                layer * itemSpacing.y,
                z * itemSpacing.z
            );
        }

        private System.Collections.IEnumerator SquashAndStretch(Transform target)
        {
            Vector3 originalScale = target.localScale;
            Vector3 squashScale = new Vector3(originalScale.x * 1.2f, originalScale.y * 0.8f, originalScale.z * 1.2f);
            Vector3 stretchScale = new Vector3(originalScale.x * 0.8f, originalScale.y * 1.2f, originalScale.z * 0.8f);

            float duration = 0.1f;
            float elapsed = 0f;

            // Squash
            while (elapsed < duration)
            {
                target.localScale = Vector3.Lerp(originalScale, squashScale, elapsed / duration);
                elapsed += Time.deltaTime;
                yield return null;
            }
            
            elapsed = 0f;
            // Stretch
            while (elapsed < duration)
            {
                target.localScale = Vector3.Lerp(squashScale, stretchScale, elapsed / duration);
                elapsed += Time.deltaTime;
                yield return null;
            }

            // Back to normal
            target.localScale = originalScale;
        }
        
        // Debug Gizmos for Vacuum Radius & Stack Grid
        private void OnDrawGizmosSelected()
        {
            Gizmos.color = Color.yellow;
            Gizmos.DrawWireSphere(transform.position, vacuumRadius);

            if (trunkPoint != null)
            {
                Gizmos.matrix = trunkPoint.localToWorldMatrix;
                Gizmos.color = new Color(0, 1, 0, 0.5f);
                
                for (int i = 0; i < maxCapacity; i++)
                {
                    Vector3 localPos = CalculateStackPosition(i);
                    Gizmos.DrawWireCube(localPos, itemSpacing * 0.9f);
                }
                
                // Reset matrix to avoid affecting other gizmos
                Gizmos.matrix = Matrix4x4.identity;
            }
        }
    }
}
