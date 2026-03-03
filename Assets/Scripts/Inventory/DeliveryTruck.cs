using UnityEngine;
using UnityEngine.Events;
using System.Collections;
using System.Collections.Generic;

namespace Inventory
{
    public class DeliveryTruck : MonoBehaviour
    {
        [Header("Movement Points")]
        public Transform deliveryPoint;
        public Transform destinationPoint;
        
        [Header("Floating Animation")]
        public bool enableFloating = true;
        public float floatingAmplitude = 0.5f;
        public float floatingSpeed = 1f;
        public Vector3 floatingAxis = Vector3.up;
        
        [Header("Movement Settings")]
        public float movementSpeed = 5f;
        public float rotationSpeed = 5f;
        public float arrivalThreshold = 0.5f;
        
        [Header("Cargo Settings")]
        public Transform cargoHold;
        public int maxCargoCapacity = 50;
        public float loadingRadius = 3f;
        public float loadingInterval = 0.3f;
        
        [Header("Cargo Stacking")]
        public StackMode stackMode = StackMode.Vertical;
        
        [Tooltip("Grid dimensions for stacking (e.g. 3x3 base) - Used in Grid mode")]
        public Vector2Int gridDimensions = new Vector2Int(3, 3);
        [Tooltip("Spacing between items - For Vertical mode, only Y is used")]
        public Vector3 itemSpacing = new Vector3(0.5f, 0.5f, 0.5f);
        
        [Tooltip("For Vertical mode: offset from center (X, Z)")]
        public Vector2 verticalOffset = Vector2.zero;
        
        [Tooltip("Scale items when loaded into cargo (1 = normal size, 0.5 = half size)")]
        [Range(0.1f, 2f)]
        public float cargoItemScale = 0.5f;
        
        public enum StackMode
        {
            Vertical,
            Grid
        }
        
        [Header("Storage Integration")]
        public StorageSystemManager storageManager;
        public DropPoint deliveryDropPoint;
        
        [Header("Smart Loading Rules")]
        [Tooltip("If delivery point < 10 items, wait for 9 items")]
        public int tier1StorageThreshold = 10;
        public int tier1RequiredItems = 9;
        
        [Tooltip("If delivery point < 20 items, load 10 items")]
        public int tier2StorageThreshold = 20;
        public int tier2RequiredItems = 10;
        
        [Tooltip("If delivery point < 50 items, load 20 items")]
        public int tier3StorageThreshold = 50;
        public int tier3RequiredItems = 20;
        
        [Tooltip("If delivery point >= 70 items, wait for 50 items")]
        public int tier4StorageThreshold = 70;
        public int tier4RequiredItems = 50;
        
        [Header("Loop Settings")]
        public bool enableContinuousLoop = true;
        public float delayBetweenTrips = 2f;
        
        [Header("Coin Rewards")]
        public int coinsPerDelivery = 100;
        public int coinsPerItem = 10;
        public bool grantCoinsOnArrival = true;
        
        [Header("Visual Effects")]
        public GameObject loadingEffectPrefab;
        public GameObject arrivalEffectPrefab;
        public Vector3 effectOffset = Vector3.zero;

        [Header("Cash Prefab Stacking")]
        [Tooltip("Prefab used to visually represent cash (e.g. Cash3D model)")]
        public GameObject cashPrefab;
        [Tooltip("Parent/anchor where spawned cash prefabs will be stacked")]
        public Transform cashStackParent;
        public StackMode cashStackMode = StackMode.Grid;
        [Tooltip("Grid dimensions used when stacking cash prefabs in Grid mode")]
        public Vector2Int cashGridDimensions = new Vector2Int(3, 3);
        [Tooltip("Spacing between cash prefabs when stacked")]
        public Vector3 cashItemSpacing = new Vector3(0.5f, 0.2f, 0.5f);
        [Tooltip("Offset from center for vertical stacking (X,Z)")]
        public Vector2 cashVerticalOffset = Vector2.zero;
        [Tooltip("How much cash a single prefab represents (e.g. 10 = one prefab = 10 cash)")]
        public int cashValuePerPrefab = 10;
        [Tooltip("Fixed rotation (in Euler angles) applied to all spawned cash prefabs")]
        public Vector3 cashPrefabRotation = Vector3.zero;
        [Tooltip("Layer mask for cash to stack on (raycast detection)")]
        public LayerMask cashLayer = 1; // Default layer 0
        
        [Header("Events")]
        public UnityEvent onArrivedAtDeliveryPoint;
        public UnityEvent onLoadingStarted;
        public UnityEvent onLoadingComplete;
        public UnityEvent onDepartedToDestination;
        public UnityEvent onArrivedAtDestination;
        public UnityEvent<int> onCoinsAwarded;
        public UnityEvent<int> onCashAwarded;
        
        private List<InventoryItem> cargo = new List<InventoryItem>();
        private Vector3 basePosition;
        private Vector3 startPosition;
        private float floatingOffset;
        private TruckState currentState = TruckState.Idle;
        private Coroutine currentRoutine;
        
        public enum TruckState
        {
            Idle,
            WaitingForItems,
            Loading,
            MovingToDeliveryPoint,
            AtDeliveryPoint,
            MovingToDestination,
            AtDestination,
            ReturningToStart
        }
        
        private void Awake()
        {
            if (cargoHold == null)
                cargoHold = transform;
            
            basePosition = transform.position;
            startPosition = transform.position;
            
            if (storageManager == null)
                storageManager = FindFirstObjectByType<StorageSystemManager>();
            
            if (deliveryDropPoint == null)
            {
                DeliveryPoint dp = FindFirstObjectByType<DeliveryPoint>();
                if (dp != null)
                    deliveryDropPoint = dp;
            }
        }
        
        private void Update()
        {
            if (enableFloating)
            {
                ApplyFloatingAnimation();
            }
        }
        
        private void ApplyFloatingAnimation()
        {
            floatingOffset += Time.deltaTime * floatingSpeed;
            float wave = Mathf.Sin(floatingOffset) * floatingAmplitude;
            
            Vector3 targetPosition = basePosition + (floatingAxis.normalized * wave);
            transform.position = Vector3.Lerp(transform.position, targetPosition, Time.deltaTime * 5f);
        }
        
        public void StartDeliverySequence()
        {
            if (currentRoutine != null)
            {
                StopCoroutine(currentRoutine);
            }
            
            currentRoutine = StartCoroutine(DeliverySequenceRoutine());
        }
        
        private IEnumerator DeliverySequenceRoutine()
        {
            while (enableContinuousLoop)
            {
                int deliveryPointCount = GetDeliveryPointItemCount();
                int storageCount = GetStorageItemCount();
                int requiredItems = GetRequiredItemsForDeliveryPoint(deliveryPointCount);
                
                if (deliveryPointCount < requiredItems)
                {
                    currentState = TruckState.WaitingForItems;
                    Debug.Log($"[DeliveryTruck] Waiting for items. DeliveryPoint: {deliveryPointCount}, Storage: {storageCount}, Required: {requiredItems}");
                    yield return new WaitForSeconds(1f);
                    continue;
                }
                
                currentState = TruckState.Loading;
                Debug.Log("[DeliveryTruck] Starting to load at start position");
                
                yield return LoadInventory(requiredItems);
                
                if (cargo.Count == 0)
                {
                    Debug.Log("[DeliveryTruck] No items loaded. Waiting before retry.");
                    yield return new WaitForSeconds(delayBetweenTrips);
                    continue;
                }
                
                Debug.Log($"[DeliveryTruck] Loaded {cargo.Count} items. Now flying to delivery point.");
                
                yield return MoveToDeliveryPoint();
                
                Debug.Log("[DeliveryTruck] Passed delivery point. Now flying to destination.");
                
                yield return MoveToDestination();
                
                AwardCoins();
                
                yield return ReturnToStart();
                
                yield return new WaitForSeconds(delayBetweenTrips);
            }
            
            currentState = TruckState.Idle;
        }
        
        private int GetDeliveryPointItemCount()
        {
            if (deliveryDropPoint != null)
            {
                return deliveryDropPoint.GetStoredItemCount();
            }
            return 0;
        }
        
        private int GetStorageItemCount()
        {
            if (storageManager != null)
            {
                return storageManager.GetTotalStoredItems();
            }
            return 0;
        }
        
        private int GetRequiredItemsForDeliveryPoint(int deliveryPointCount)
        {
            if (deliveryPointCount >= tier4StorageThreshold)
            {
                return tier4RequiredItems;
            }
            else if (deliveryPointCount >= tier3StorageThreshold)
            {
                return tier3RequiredItems;
            }
            else if (deliveryPointCount >= tier2StorageThreshold)
            {
                return tier2RequiredItems;
            }
            else if (deliveryPointCount >= tier1StorageThreshold)
            {
                return tier1RequiredItems;
            }
            else
            {
                return tier1RequiredItems;
            }
        }

        private Vector3 GetCashSpawnPosition(int spawnIndex)
        {
            // Get grid-based local position
            Vector3 basePos;
            if (cashStackMode == StackMode.Vertical)
            {
                basePos = new Vector3(cashVerticalOffset.x, spawnIndex * cashItemSpacing.y, cashVerticalOffset.y);
            }
            else // Grid mode
            {
                int itemsPerLayer = cashGridDimensions.x * cashGridDimensions.y;
                int layer = spawnIndex / itemsPerLayer;
                int posInLayer = spawnIndex % itemsPerLayer;
                
                int x = posInLayer % cashGridDimensions.x;
                int z = posInLayer / cashGridDimensions.x;
                
                basePos = new Vector3(
                    x * cashItemSpacing.x - (cashGridDimensions.x * cashItemSpacing.x * 0.5f) + (cashItemSpacing.x * 0.5f),
                    layer * cashItemSpacing.y,
                    z * cashItemSpacing.z
                );
            }

            // Convert to world position and return (CashPickup handles stacking)
            return cashStackParent.TransformPoint(basePos);
        }
        
        private IEnumerator LoadInventory(int targetItemCount)
        {
            currentState = TruckState.Loading;
            onLoadingStarted?.Invoke();
            
            int loadedCount = 0;
            
            Debug.Log($"[DeliveryTruck] Starting to load items. Target: {targetItemCount}");
            
            while (loadedCount < targetItemCount && loadedCount < maxCargoCapacity)
            {
                if (deliveryDropPoint == null)
                {
                    Debug.LogWarning("[DeliveryTruck] No delivery drop point assigned!");
                    break;
                }
                
                List<InventoryItem> availableItems = deliveryDropPoint.GetStoredItems();
                
                if (availableItems.Count == 0)
                {
                    Debug.Log("[DeliveryTruck] No more items available to load");
                    break;
                }
                
                InventoryItem itemToLoad = availableItems[0];
                
                int itemIndex = deliveryDropPoint.GetStoredItems().IndexOf(itemToLoad);
                if (itemIndex >= 0)
                {
                    deliveryDropPoint.RemoveItem(itemIndex);
                    
                    LoadItem(itemToLoad);
                    loadedCount++;
                    
                    if (loadingEffectPrefab != null)
                    {
                        Instantiate(loadingEffectPrefab, itemToLoad.transform.position + effectOffset, Quaternion.identity);
                    }
                    
                    Debug.Log($"[DeliveryTruck] Loaded item {loadedCount}/{targetItemCount}: {itemToLoad.name}");
                    
                    yield return new WaitForSeconds(loadingInterval);
                }
                else
                {
                    Debug.LogWarning("[DeliveryTruck] Item not found in delivery point");
                    break;
                }
            }
            
            onLoadingComplete?.Invoke();
            Debug.Log($"[DeliveryTruck] Loading complete. Loaded {cargo.Count} items into cargo");
        }
        
        private void LoadItem(InventoryItem item)
        {
            if (item == null || cargo.Count >= maxCargoCapacity)
                return;
            
            cargo.Add(item);
            
            item.OnAttachedToPlayerBag();
            item.transform.SetParent(cargoHold);
            
            Vector3 stackPosition = CalculateStackPosition(cargo.Count - 1);
            item.transform.localPosition = stackPosition;
            item.transform.localRotation = Quaternion.identity;
            item.transform.localScale = Vector3.one * cargoItemScale;
            item.gameObject.SetActive(true);
            
            Debug.Log($"[DeliveryTruck] Loaded {item.name} at local position {stackPosition} (index {cargo.Count - 1}, mode: {stackMode}, scale: {cargoItemScale})");
        }
        
        private Vector3 CalculateStackPosition(int index)
        {
            if (stackMode == StackMode.Vertical)
            {
                return new Vector3(verticalOffset.x, index * itemSpacing.y, verticalOffset.y);
            }
            else // Grid mode
            {
                int itemsPerLayer = gridDimensions.x * gridDimensions.y;
                int layer = index / itemsPerLayer;
                int posInLayer = index % itemsPerLayer;
                
                int x = posInLayer % gridDimensions.x;
                int z = posInLayer / gridDimensions.x;
                
                return new Vector3(
                    x * itemSpacing.x - (gridDimensions.x * itemSpacing.x * 0.5f) + (itemSpacing.x * 0.5f),
                    layer * itemSpacing.y,
                    z * itemSpacing.z
                );
            }
        }

        private Vector3 CalculateCashStackPosition(int index)
        {
            if (cashStackMode == StackMode.Vertical)
            {
                return new Vector3(cashVerticalOffset.x, index * cashItemSpacing.y, cashVerticalOffset.y);
            }
            else // Grid mode
            {
                int itemsPerLayer = cashGridDimensions.x * cashGridDimensions.y;
                int layer = index / itemsPerLayer;
                int posInLayer = index % itemsPerLayer;

                int x = posInLayer % cashGridDimensions.x;
                int z = posInLayer / cashGridDimensions.x;

                return new Vector3(
                    x * cashItemSpacing.x - (cashGridDimensions.x * cashItemSpacing.x * 0.5f) + (cashItemSpacing.x * 0.5f),
                    layer * cashItemSpacing.y,
                    z * cashItemSpacing.z
                );
            }
        }
        
        private IEnumerator MoveToDeliveryPoint()
        {
            if (deliveryPoint == null)
            {
                Debug.LogWarning("[DeliveryTruck] Delivery point not assigned!");
                yield break;
            }
            
            currentState = TruckState.MovingToDeliveryPoint;
            
            while (Vector3.Distance(basePosition, deliveryPoint.position) > arrivalThreshold)
            {
                basePosition = Vector3.MoveTowards(basePosition, deliveryPoint.position, movementSpeed * Time.deltaTime);
                
                Vector3 direction = (deliveryPoint.position - transform.position).normalized;
                if (direction != Vector3.zero)
                {
                    Quaternion targetRotation = Quaternion.LookRotation(direction);
                    transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, rotationSpeed * Time.deltaTime);
                }
                
                yield return null;
            }
            
            currentState = TruckState.AtDeliveryPoint;
            onArrivedAtDeliveryPoint?.Invoke();
        }
        
        private IEnumerator MoveToDestination()
        {
            if (destinationPoint == null)
            {
                Debug.LogWarning("[DeliveryTruck] Destination point not assigned!");
                yield break;
            }
            
            currentState = TruckState.MovingToDestination;
            onDepartedToDestination?.Invoke();
            
            while (Vector3.Distance(basePosition, destinationPoint.position) > arrivalThreshold)
            {
                basePosition = Vector3.MoveTowards(basePosition, destinationPoint.position, movementSpeed * Time.deltaTime);
                
                Vector3 direction = (destinationPoint.position - transform.position).normalized;
                if (direction != Vector3.zero)
                {
                    Quaternion targetRotation = Quaternion.LookRotation(direction);
                    transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, rotationSpeed * Time.deltaTime);
                }
                
                yield return null;
            }
            
            currentState = TruckState.AtDestination;
            onArrivedAtDestination?.Invoke();
            
            if (arrivalEffectPrefab != null)
            {
                Instantiate(arrivalEffectPrefab, transform.position + effectOffset, Quaternion.identity);
            }
        }
        
        private IEnumerator ReturnToStart()
        {
            currentState = TruckState.ReturningToStart;
            
            while (Vector3.Distance(basePosition, startPosition) > arrivalThreshold)
            {
                basePosition = Vector3.MoveTowards(basePosition, startPosition, movementSpeed * Time.deltaTime);
                
                Vector3 direction = (startPosition - transform.position).normalized;
                if (direction != Vector3.zero)
                {
                    Quaternion targetRotation = Quaternion.LookRotation(direction);
                    transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, rotationSpeed * Time.deltaTime);
                }
                
                yield return null;
            }
            
            currentState = TruckState.Idle;
            Debug.Log("[DeliveryTruck] Returned to start position");
        }
        
        private void AwardCoins()
        {
            if (!grantCoinsOnArrival)
                return;
            
            int totalCoins = coinsPerDelivery + (cargo.Count * coinsPerItem);
            
            if (CoinManager.Instance != null)
            {
                CoinManager.Instance.AddCoins(totalCoins);
            }
            
            int totalCash = 0;
            foreach (InventoryItem item in cargo)
            {
                if (item != null && item.itemData != null && item.itemData.hasValue)
                {
                    totalCash += item.itemData.value;
                }
            }
            
            if (totalCash > 0)
            {
                // Spawn visual cash prefabs based on each item's actual value
                // Player must collect them to receive the cash
                if (cashPrefab == null)
                {
                    Debug.LogWarning("[DeliveryTruck] cashPrefab not assigned!");
                }
                else if (cashStackParent == null)
                {
                    Debug.LogWarning("[DeliveryTruck] cashStackParent not assigned! Using truck position as fallback.");
                    cashStackParent = transform;
                }

                if (cashPrefab != null && cashStackParent != null && cashValuePerPrefab > 0)
                {
                    Debug.Log($"[DeliveryTruck] Cash stack parent: {cashStackParent.name} at world pos {cashStackParent.position}");
                    int spawnIndex = 0;
                    
                    foreach (InventoryItem item in cargo)
                    {
                        if (item != null && item.itemData != null && item.itemData.hasValue)
                        {
                            // Calculate how many cash prefabs this item's value represents
                            int itemValue = item.itemData.value;
                            int preflabsForItem = Mathf.CeilToInt((float)itemValue / (float)cashValuePerPrefab);
                            preflabsForItem = Mathf.Max(1, preflabsForItem);

                            Debug.Log($"[DeliveryTruck] Item {item.name} value={itemValue}, spawning {preflabsForItem} prefabs");

                            for (int i = 0; i < preflabsForItem; i++)
                            {
                                // Use grid-based positioning with raycast stacking
                                Vector3 spawnPos = GetCashSpawnPosition(spawnIndex);
                                
                                GameObject go = Instantiate(cashPrefab, spawnPos, Quaternion.Euler(cashPrefabRotation));
                                go.transform.SetParent(cashStackParent);

                                // Calculate this prefab's value (remainder goes to last prefab)
                                int cashValueForThisPrefab = (i == preflabsForItem - 1) 
                                    ? itemValue - (i * cashValuePerPrefab)
                                    : cashValuePerPrefab;

                                // Link the item and set direct value
                                CashPickup pickup = go.GetComponent<CashPickup>();
                                if (pickup != null)
                                {
                                    pickup.SetLinkedItem(item);
                                    pickup.SetDirectCashValue(cashValueForThisPrefab);
                                }

                                go.SetActive(true);
                                Debug.Log($"[DeliveryTruck] Spawned cash #{spawnIndex} at {spawnPos}, value={cashValueForThisPrefab}");
                                spawnIndex++;
                            }
                        }
                    }

                    onCashAwarded?.Invoke(totalCash);
                    Debug.Log($"[DeliveryTruck] Spawned ${totalCash} worth of cash prefabs from {cargo.Count} items. Player must collect them!");
                }
            }
            
            Debug.Log($"[DeliveryTruck] Awarded {totalCoins} coins ({cargo.Count} items delivered)");
            onCoinsAwarded?.Invoke(totalCoins);
            
            foreach (InventoryItem item in cargo)
            {
                if (item != null)
                {
                    Destroy(item.gameObject);
                }
            }
            
            cargo.Clear();
        }
        
        public void StopDeliverySequence()
        {
            if (currentRoutine != null)
            {
                StopCoroutine(currentRoutine);
                currentRoutine = null;
            }
            
            currentState = TruckState.Idle;
        }
        
        public void ManualLoadInventory()
        {
            if (currentRoutine != null)
            {
                StopCoroutine(currentRoutine);
            }
            
            int deliveryPointCount = GetDeliveryPointItemCount();
            int requiredItems = GetRequiredItemsForDeliveryPoint(deliveryPointCount);
            currentRoutine = StartCoroutine(LoadInventory(requiredItems));
        }
        
        public void ManualMoveToDestination()
        {
            if (currentRoutine != null)
            {
                StopCoroutine(currentRoutine);
            }
            
            currentRoutine = StartCoroutine(MoveToDestination());
        }
        
        public int GetCargoCount()
        {
            return cargo.Count;
        }
        
        public bool IsCargoFull()
        {
            return cargo.Count >= maxCargoCapacity;
        }
        
        public TruckState GetCurrentState()
        {
            return currentState;
        }
        
        private void OnDrawGizmosSelected()
        {
            Vector3 currentPos = Application.isPlaying ? transform.position : (cargoHold != null ? cargoHold.position : transform.position);
            
            if (deliveryPoint != null)
            {
                Gizmos.color = Color.yellow;
                Gizmos.DrawWireSphere(deliveryPoint.position, 0.5f);
                Gizmos.DrawLine(currentPos, deliveryPoint.position);
                
                Gizmos.color = new Color(1f, 1f, 0f, 0.3f);
                Gizmos.DrawWireSphere(deliveryPoint.position, loadingRadius);
                Gizmos.color = Color.yellow;
                Gizmos.DrawLine(deliveryPoint.position, deliveryPoint.position + Vector3.up * 2f);
            }
            
            if (destinationPoint != null)
            {
                Gizmos.color = Color.green;
                Gizmos.DrawWireSphere(destinationPoint.position, 0.5f);
                Gizmos.DrawLine(currentPos, destinationPoint.position);
                Gizmos.DrawLine(destinationPoint.position, destinationPoint.position + Vector3.up * 2f);
            }
            
            Gizmos.color = Color.cyan;
            Gizmos.DrawWireSphere(currentPos, loadingRadius);
            
            Transform hold = cargoHold != null ? cargoHold : transform;
            Gizmos.color = Color.magenta;
            Gizmos.DrawWireCube(hold.position, Vector3.one);
            Gizmos.DrawLine(hold.position, hold.position + Vector3.up * 0.5f);
            
            if (cargoHold != null)
            {
                Gizmos.matrix = cargoHold.localToWorldMatrix;
                Gizmos.color = new Color(0, 1, 0, 0.3f);
                
                for (int i = 0; i < maxCargoCapacity; i++)
                {
                    Vector3 localPos = CalculateStackPosition(i);
                    
                    if (stackMode == StackMode.Vertical)
                    {
                        Gizmos.DrawWireCube(localPos, new Vector3(itemSpacing.x, itemSpacing.y * 0.8f, itemSpacing.z));
                    }
                    else
                    {
                        Gizmos.DrawWireCube(localPos, itemSpacing * 0.8f);
                    }
                }
                
                Gizmos.matrix = Matrix4x4.identity;
            }
            
            if (Application.isPlaying)
            {
                UnityEditor.Handles.Label(hold.position + Vector3.up * 1.5f, $"Cargo: {cargo.Count}/{maxCargoCapacity}");
                
                int deliveryPointCount = GetDeliveryPointItemCount();
                int storageCount = GetStorageItemCount();
                int required = GetRequiredItemsForDeliveryPoint(deliveryPointCount);
                UnityEditor.Handles.Label(currentPos + Vector3.up * 3f, 
                    $"State: {currentState}\nDelivery Point: {deliveryPointCount}\nStorage: {storageCount}\nRequired: {required}");
            }

            // Draw cash stack grid visualization
            if (cashStackParent != null && cashPrefab != null)
            {
                Gizmos.matrix = cashStackParent.localToWorldMatrix;
                Gizmos.color = new Color(1, 0.84f, 0, 0.3f); // Gold

                for (int i = 0; i < maxCargoCapacity; i++)
                {
                    Vector3 localPos = CalculateCashStackPosition(i);

                    if (cashStackMode == StackMode.Vertical)
                    {
                        Gizmos.DrawWireCube(localPos, new Vector3(0.2f, cashItemSpacing.y * 0.8f, 0.2f));
                    }
                    else
                    {
                        Gizmos.DrawWireCube(localPos, cashItemSpacing * 0.8f);
                    }
                }

                Gizmos.matrix = Matrix4x4.identity;
                
                // Label for cash stack
                UnityEditor.Handles.Label(cashStackParent.position + Vector3.up * 0.5f, "Cash Stack Area");
            }
        }
    }
}
