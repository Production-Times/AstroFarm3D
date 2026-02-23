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
        
        [Header("Events")]
        public UnityEvent onArrivedAtDeliveryPoint;
        public UnityEvent onLoadingStarted;
        public UnityEvent onLoadingComplete;
        public UnityEvent onDepartedToDestination;
        public UnityEvent onArrivedAtDestination;
        public UnityEvent<int> onCoinsAwarded;
        
        private List<InventoryItem> cargo = new List<InventoryItem>();
        private Vector3 basePosition;
        private Vector3 startPosition;
        private float floatingOffset;
        private TruckState currentState = TruckState.Idle;
        private float loadingTimer = 0f;
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
        
        private IEnumerator LoadInventory(int targetItemCount)
        {
            currentState = TruckState.Loading;
            onLoadingStarted?.Invoke();
            
            loadingTimer = 0f;
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
        }
    }
}
