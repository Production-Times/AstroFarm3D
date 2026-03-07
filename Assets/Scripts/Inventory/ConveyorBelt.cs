using UnityEngine;
using UnityEngine.Events;
using System.Collections.Generic;

namespace Inventory
{
    [System.Serializable]
    public class ConveyorBeltUpgradeTier
    {
        public string tierName = "Tier 1";
        public int upgradeCost = 100;
        [Range(0.1f, 2f)]
        public float speedMultiplier = 1f;
    }

    [System.Serializable]
    public class ProcessingMachineToSpawn
    {
        [Tooltip("Machine prefab to spawn when upgrading")]
        public GameObject machinePrefab;
        
        [Tooltip("At which upgrade level this machine appears (1 = first upgrade, 2 = second, 3 = third)")]
        public int spawnAtLevel = 1;
        
        [Tooltip("Where to place this machine relative to the conveyor")]
        public Vector3 position = Vector3.zero;
        
        [Tooltip("Rotation of the machine")]
        public Vector3 rotation = Vector3.zero;
        
        [Tooltip("Scale of the machine")]
        public Vector3 scale = Vector3.one;
    }

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
        
        [Header("Upgrade System")]
        public bool upgradeSystemEnabled = true;
        public string machineID = "ConveyorBelt_0";
        
        [Header("Upgrade Tiers")]
        [Tooltip("Add upgrade levels here - each costs more and is faster")]
        public List<ConveyorBeltUpgradeTier> upgradeTiers = new List<ConveyorBeltUpgradeTier>();
        
        [Header("Processing Machines to Add")]
        [Tooltip("Machines to ADD to conveyor at each upgrade (NOT replacing the belt)")]
        public List<ProcessingMachineToSpawn> machineSpawnsOnUpgrade = new List<ProcessingMachineToSpawn>();
        
        [Header("Events")]
        public UnityEvent<InventoryItem> onItemEntered;
        public UnityEvent<InventoryItem> onItemProcessed;
        public UnityEvent<InventoryItem> onItemExited;
        public UnityEvent<int> onUpgradePurchased;
        public UnityEvent onMaxLevelReached;
        
        private List<ConveyorItemData> itemsOnBelt = new List<ConveyorItemData>();
        
        private int currentUpgradeLevel = 0;
        private List<GameObject> spawnedMachines = new List<GameObject>();
        
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

            if (upgradeSystemEnabled)
            {
                LoadUpgradeLevel();
                Debug.Log($"[{machineID}] Awake - Loaded upgrade level: {currentUpgradeLevel}");
            }
        }

        private void Start()
        {
            if (upgradeSystemEnabled)
            {
                Debug.Log($"[{machineID}] Start - Applying current upgrade level ({currentUpgradeLevel})");
                ApplyCurrentUpgradeLevel();
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
            
            float beltDistance = Vector3.Distance(startPoint.position, endPoint.position);
            float speedMultiplier = upgradeSystemEnabled ? GetCurrentSpeedMultiplier() : 1f;
            float effectiveSpeed = conveyorSpeed * speedMultiplier;
            
            for (int i = itemsOnBelt.Count - 1; i >= 0; i--)
            {
                ConveyorItemData data = itemsOnBelt[i];
                
                data.progress += effectiveSpeed * Time.deltaTime / beltDistance;
                
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

        #region Upgrade System

        private void LoadUpgradeLevel()
        {
            currentUpgradeLevel = PlayerPrefs.GetInt($"CB_Level_{machineID}", 0);
            Debug.Log($"[{machineID}] LoadUpgradeLevel - Loaded: {currentUpgradeLevel} (Key: CB_Level_{machineID})");
        }

        private void SaveUpgradeLevel()
        {
            PlayerPrefs.SetInt($"CB_Level_{machineID}", currentUpgradeLevel);
            PlayerPrefs.Save();
            Debug.Log($"[{machineID}] SaveUpgradeLevel - Saved: {currentUpgradeLevel} (Key: CB_Level_{machineID})");
        }

        public int GetCurrentUpgradeLevel() => currentUpgradeLevel;
        public int GetMaxUpgradeLevel() => upgradeTiers.Count;
        public bool IsMaxLevel() => currentUpgradeLevel >= GetMaxUpgradeLevel();

        public int GetUpgradeCost()
        {
            if (!upgradeSystemEnabled || IsMaxLevel()) return 0;
            
            int nextLevel = currentUpgradeLevel + 1;
            if (nextLevel <= 0 || nextLevel > upgradeTiers.Count) return 0;
            
            return upgradeTiers[nextLevel - 1].upgradeCost;
        }

        public float GetCurrentSpeedMultiplier()
        {
            if (!upgradeSystemEnabled || currentUpgradeLevel <= 0) return 1f;
            if (currentUpgradeLevel > upgradeTiers.Count) return 1f;
            
            return upgradeTiers[currentUpgradeLevel - 1].speedMultiplier;
        }

        public bool CanUpgrade()
        {
            if (!upgradeSystemEnabled)
            {
                Debug.Log($"[{machineID}] CanUpgrade - FALSE: Upgrade system disabled");
                return false;
            }
            
            if (IsMaxLevel())
            {
                Debug.Log($"[{machineID}] CanUpgrade - FALSE: Already at max level ({currentUpgradeLevel}/{GetMaxUpgradeLevel()})");
                return false;
            }
            
            int cost = GetUpgradeCost();
            
            if (CashManager.Instance == null)
            {
                Debug.LogWarning($"[{machineID}] CanUpgrade - FALSE: CashManager.Instance is NULL");
                return false;
            }
            
            bool hasEnough = CashManager.Instance.HasEnoughCash(cost);
            Debug.Log($"[{machineID}] CanUpgrade - {(hasEnough ? "TRUE" : "FALSE")} (Need: ${cost}, Current level: {currentUpgradeLevel})");
            
            return hasEnough;
        }

        public bool TryUpgrade()
        {
            if (!CanUpgrade()) 
            {
                Debug.LogWarning($"[{machineID}] Cannot upgrade - CanUpgrade returned false");
                return false;
            }

            int cost = GetUpgradeCost();

            if (!CashManager.Instance.TrySpendCash(cost)) 
            {
                Debug.LogWarning($"[{machineID}] Cannot upgrade - TrySpendCash failed");
                return false;
            }

            currentUpgradeLevel++;
            SaveUpgradeLevel();
            
            Debug.Log($"[{machineID}] ✓ UPGRADED TO LEVEL {currentUpgradeLevel} (Cost: ${cost})");

            ApplyCurrentUpgradeLevel();

            onUpgradePurchased?.Invoke(currentUpgradeLevel);

            if (IsMaxLevel())
            {
                Debug.Log($"[{machineID}] ★ MAX LEVEL REACHED!");
                onMaxLevelReached?.Invoke();
            }

            return true;
        }

        private void ApplyCurrentUpgradeLevel()
        {
            if (!upgradeSystemEnabled)
            {
                Debug.LogWarning($"[{machineID}] ApplyCurrentUpgradeLevel - Upgrade system disabled!");
                return;
            }

            if (currentUpgradeLevel <= 0)
            {
                Debug.Log($"[{machineID}] ApplyCurrentUpgradeLevel - At base level (0), no machines to spawn");
                return;
            }

            Debug.Log($"[{machineID}] ApplyCurrentUpgradeLevel - At level {currentUpgradeLevel}, attempting to spawn machines...");
            SpawnProcessingMachines();
        }

        private void SpawnProcessingMachines()
        {
            Debug.Log($"[{machineID}] SpawnProcessingMachines() called - Current Level: {currentUpgradeLevel}");
            Debug.Log($"[{machineID}] Total machines in config: {machineSpawnsOnUpgrade.Count}");

            if (machineSpawnsOnUpgrade.Count == 0)
            {
                Debug.LogWarning($"[{machineID}] No machines configured in 'Machines to Add on Upgrade' list!");
                return;
            }

            // Spawn machines based on upgrade level
            foreach (var machineConfig in machineSpawnsOnUpgrade)
            {
                Debug.Log($"[{machineID}] Checking machine... SpawnAtLevel: {machineConfig.spawnAtLevel}, Prefab: {(machineConfig.machinePrefab != null ? machineConfig.machinePrefab.name : "NULL")}");

                // Only spawn if we're at or past the required level
                if (currentUpgradeLevel >= machineConfig.spawnAtLevel)
                {
                    Debug.Log($"[{machineID}] ✓ Level {currentUpgradeLevel} >= Required {machineConfig.spawnAtLevel} - SHOULD SPAWN");

                    // Check if already spawned
                    bool alreadySpawned = false;
                    foreach (var spawnedMachine in spawnedMachines)
                    {
                        if (spawnedMachine != null && spawnedMachine.name.Contains(machineConfig.machinePrefab.name))
                        {
                            alreadySpawned = true;
                            Debug.Log($"[{machineID}] Already spawned: {spawnedMachine.name}");
                            break;
                        }
                    }

                    // Spawn if not already present
                    if (!alreadySpawned && machineConfig.machinePrefab != null)
                    {
                        Debug.Log($"[{machineID}] ★ SPAWNING MACHINE: {machineConfig.machinePrefab.name}");
                        
                        GameObject spawnedMachine = Instantiate(
                            machineConfig.machinePrefab,
                            transform
                        );

                        spawnedMachine.transform.localPosition = machineConfig.position;
                        spawnedMachine.transform.localRotation = Quaternion.Euler(machineConfig.rotation);
                        spawnedMachine.transform.localScale = machineConfig.scale;

                        spawnedMachines.Add(spawnedMachine);
                        
                        Debug.Log($"[{machineID}] ✓ Successfully spawned: {spawnedMachine.name} at position {machineConfig.position}");
                    }
                    else if (machineConfig.machinePrefab == null)
                    {
                        Debug.LogError($"[{machineID}] Cannot spawn - Machine prefab is NULL at spawn level {machineConfig.spawnAtLevel}");
                    }
                }
                else
                {
                    Debug.Log($"[{machineID}] ✗ Level {currentUpgradeLevel} < Required {machineConfig.spawnAtLevel} - NOT YET");
                }
            }
        }

        public string GetCurrentTierName()
        {
            if (!upgradeSystemEnabled || currentUpgradeLevel == 0) return "Base";
            if (currentUpgradeLevel > upgradeTiers.Count) return "Unknown";
            
            return upgradeTiers[currentUpgradeLevel - 1].tierName;
        }

        public ConveyorBeltUpgradeTier GetCurrentTier()
        {
            if (currentUpgradeLevel <= 0 || currentUpgradeLevel > upgradeTiers.Count) return null;
            return upgradeTiers[currentUpgradeLevel - 1];
        }

        #endregion
    }
}