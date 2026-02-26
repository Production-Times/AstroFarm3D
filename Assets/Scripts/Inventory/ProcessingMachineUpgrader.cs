using UnityEngine;
using UnityEngine.Events;

namespace Inventory
{
    public class ProcessingMachineUpgrader : MonoBehaviour
    {
        [Header("Configuration")]
        [Tooltip("The upgrade config ScriptableObject for this machine type")]
        public ProcessingMachineUpgradeConfig config;
        
        [Header("References")]
        [Tooltip("The ProcessingMachine component to upgrade")]
        public ProcessingMachine machine;
        
        [Tooltip("Parent transform where the model sits — the spawned model will be childed here")]
        public Transform modelContainer;
        
        [Header("Persistence")]
        [Tooltip("Unique ID used to save this machine's upgrade level. Must be unique per machine in the scene.")]
        public string machineID = "ProcessingMachine_0";
        
        [Header("Events")]
        public UnityEvent<int> onUpgradePurchased;
        public UnityEvent onMaxLevelReached;
        
        private int currentLevel = 0;
        private GameObject spawnedModel;
        
        private void Awake()
        {
            if (machine == null) machine = GetComponent<ProcessingMachine>();
            if (modelContainer == null) modelContainer = transform;
            
            currentLevel = PlayerPrefs.GetInt($"PM_Level_{machineID}", 0);
        }
        
        private void Start()
        {
            ApplyCurrentLevel();
        }
        
        public int GetCurrentLevel() => currentLevel;
        public int GetMaxLevel() => config != null ? config.MaxLevel : 0;
        public bool IsMaxLevel() => currentLevel >= GetMaxLevel();
        
        public int GetUpgradeCost()
        {
            if (config == null || IsMaxLevel()) return 0;
            return config.GetCostForLevel(currentLevel + 1);
        }
        
        public bool CanUpgrade()
        {
            if (config == null || IsMaxLevel()) return false;
            return CashManager.Instance != null && CashManager.Instance.HasEnoughCash(GetUpgradeCost());
        }
        
        public bool TryUpgrade()
        {
            if (!CanUpgrade()) return false;
            
            int cost = GetUpgradeCost();
            
            if (!CashManager.Instance.TrySpendCash(cost)) return false;
            
            currentLevel++;
            PlayerPrefs.SetInt($"PM_Level_{machineID}", currentLevel);
            PlayerPrefs.Save();
            
            ApplyCurrentLevel();
            
            onUpgradePurchased?.Invoke(currentLevel);
            
            if (IsMaxLevel())
                onMaxLevelReached?.Invoke();
            
            Debug.Log($"[{machineID}] Upgraded to level {currentLevel} for ${cost}");
            return true;
        }
        
        private void ApplyCurrentLevel()
        {
            if (config == null || machine == null) return;
            
            machine.processingDuration = config.GetProcessingDurationAtLevel(currentLevel);
            machine.loadDuration       = config.GetLoadDurationAtLevel(currentLevel);
            machine.unloadDuration     = config.GetUnloadDurationAtLevel(currentLevel);
            machine.maxCapacity        = config.GetCapacityAtLevel(currentLevel);
            
            SwapModel();
        }
        
        private void SwapModel()
        {
            if (config == null) return;
            
            ProcessingMachineTier modelTier = config.GetModelTierAtLevel(currentLevel);
            
            if (modelTier == null || !modelTier.HasModel) return;
            
            if (spawnedModel != null)
                Destroy(spawnedModel);
            
            spawnedModel = Instantiate(modelTier.modelPrefab, modelContainer);
            modelTier.ApplyModelTransform(spawnedModel.transform);
        }
        
        public ProcessingMachineTier GetCurrentModelTier()
        {
            if (config == null) return null;
            return config.GetModelTierAtLevel(currentLevel);
        }
        
        public string GetTierName()
        {
            if (config == null || currentLevel == 0) return "Base";
            var tier = config.GetTier(currentLevel);
            return tier != null ? tier.tierName : "Base";
        }
    }
}
