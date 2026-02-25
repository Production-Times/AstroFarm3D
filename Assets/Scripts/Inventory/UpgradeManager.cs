using UnityEngine;
using UnityEngine.Events;
using System.Collections.Generic;

namespace Inventory
{
    public class UpgradeManager : MonoBehaviour
    {
        [Header("Configuration")]
        public UpgradeDatabase upgradeDatabase;
        
        [Header("Events")]
        public UnityEvent<UpgradeType, int> onUpgradePurchased;
        public UnityEvent<UpgradeType, int> onUpgradeLevelChanged;
        
        private Dictionary<UpgradeType, int> currentLevels = new Dictionary<UpgradeType, int>();
        
        private static UpgradeManager instance;
        
        public static UpgradeManager Instance
        {
            get
            {
                if (instance == null)
                {
                    instance = FindFirstObjectByType<UpgradeManager>();
                    if (instance == null)
                    {
                        GameObject go = new GameObject("UpgradeManager");
                        instance = go.AddComponent<UpgradeManager>();
                    }
                }
                return instance;
            }
        }
        
        private void Awake()
        {
            if (instance != null && instance != this)
            {
                Destroy(gameObject);
                return;
            }
            instance = this;
            
            LoadUpgradeLevels();
        }
        
        public int GetUpgradeLevel(UpgradeType type)
        {
            if (currentLevels.ContainsKey(type))
            {
                return currentLevels[type];
            }
            return 0;
        }
        
        public float GetUpgradeValue(UpgradeType type)
        {
            if (upgradeDatabase == null)
            {
                Debug.LogWarning("UpgradeManager: Upgrade database not assigned!");
                return 0f;
            }
            
            UpgradeData upgrade = upgradeDatabase.GetUpgrade(type);
            if (upgrade == null)
            {
                Debug.LogWarning($"UpgradeManager: Upgrade type {type} not found in database!");
                return 0f;
            }
            
            int level = GetUpgradeLevel(type);
            return upgrade.GetValueAtLevel(level);
        }
        
        public int GetUpgradeCost(UpgradeType type)
        {
            if (upgradeDatabase == null)
                return 0;
            
            UpgradeData upgrade = upgradeDatabase.GetUpgrade(type);
            if (upgrade == null)
                return 0;
            
            int currentLevel = GetUpgradeLevel(type);
            int nextLevel = currentLevel + 1;
            
            if (nextLevel > upgrade.maxLevel)
                return 0;
            
            return upgrade.GetCostForLevel(nextLevel);
        }
        
        public bool CanUpgrade(UpgradeType type)
        {
            if (upgradeDatabase == null)
                return false;
            
            UpgradeData upgrade = upgradeDatabase.GetUpgrade(type);
            if (upgrade == null)
                return false;
            
            int currentLevel = GetUpgradeLevel(type);
            if (currentLevel >= upgrade.maxLevel)
                return false;
            
            int cost = GetUpgradeCost(type);
            return CashManager.Instance.HasEnoughCash(cost);
        }
        
        public bool TryUpgrade(UpgradeType type)
        {
            if (!CanUpgrade(type))
            {
                Debug.Log($"Cannot upgrade {type}: Either max level reached or insufficient cash");
                return false;
            }
            
            int cost = GetUpgradeCost(type);
            
            if (CashManager.Instance.TrySpendCash(cost))
            {
                int currentLevel = GetUpgradeLevel(type);
                int newLevel = currentLevel + 1;
                
                currentLevels[type] = newLevel;
                SaveUpgradeLevel(type, newLevel);
                
                onUpgradePurchased?.Invoke(type, newLevel);
                onUpgradeLevelChanged?.Invoke(type, newLevel);
                
                Debug.Log($"Upgraded {type} to level {newLevel} for ${cost}");
                
                ApplyUpgrade(type);
                
                return true;
            }
            
            return false;
        }
        
        private void ApplyUpgrade(UpgradeType type)
        {
            switch (type)
            {
                case UpgradeType.PlayerBackpackCapacity:
                    ApplyPlayerBackpackUpgrade();
                    break;
                
                case UpgradeType.PlayerMoveSpeed:
                    ApplyPlayerSpeedUpgrade();
                    break;
                
                case UpgradeType.VehicleVacuumRadius:
                case UpgradeType.VehicleMaxCapacity:
                    ApplyVehicleUpgrade();
                    break;
                
                case UpgradeType.HarvesterDamage:
                    ApplyHarvesterUpgrade();
                    break;
            }
        }
        
        private void ApplyPlayerBackpackUpgrade()
        {
            PlayerBackpack backpack = FindFirstObjectByType<PlayerBackpack>();
            if (backpack != null)
            {
                backpack.ApplyUpgrade();
            }
        }
        
        private void ApplyPlayerSpeedUpgrade()
        {
            SmoothPlayerController player = FindFirstObjectByType<SmoothPlayerController>();
            if (player != null)
            {
                player.ApplyUpgrade();
            }
        }
        
        private void ApplyVehicleUpgrade()
        {
            Harvesting.VehicleInventory[] vehicles = FindObjectsByType<Harvesting.VehicleInventory>(FindObjectsSortMode.None);
            foreach (var vehicle in vehicles)
            {
                vehicle.ApplyUpgrades();
            }
        }
        
        private void ApplyHarvesterUpgrade()
        {
            Harvesting.HarvesterTool[] harvesters = FindObjectsByType<Harvesting.HarvesterTool>(FindObjectsSortMode.None);
            foreach (var harvester in harvesters)
            {
                harvester.ApplyUpgrade();
            }
        }
        
        private void SaveUpgradeLevel(UpgradeType type, int level)
        {
            PlayerPrefs.SetInt($"Upgrade_{type}", level);
            PlayerPrefs.Save();
        }
        
        private void LoadUpgradeLevels()
        {
            if (upgradeDatabase == null)
                return;
            
            foreach (var upgrade in upgradeDatabase.upgrades)
            {
                int level = PlayerPrefs.GetInt($"Upgrade_{upgrade.upgradeType}", 0);
                currentLevels[upgrade.upgradeType] = level;
            }
            
            ApplyAllUpgrades();
        }
        
        public void ApplyAllUpgrades()
        {
            foreach (var kvp in currentLevels)
            {
                if (kvp.Value > 0)
                {
                    ApplyUpgrade(kvp.Key);
                }
            }
        }
        
        public bool IsMaxLevel(UpgradeType type)
        {
            if (upgradeDatabase == null)
                return true;
            
            UpgradeData upgrade = upgradeDatabase.GetUpgrade(type);
            if (upgrade == null)
                return true;
            
            return GetUpgradeLevel(type) >= upgrade.maxLevel;
        }
        
        public UpgradeData GetUpgradeData(UpgradeType type)
        {
            if (upgradeDatabase == null)
                return null;
            
            return upgradeDatabase.GetUpgrade(type);
        }
    }
}
