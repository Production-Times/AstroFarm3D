using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace Inventory
{
    public class ProcessingMachineUpgradeButton : MonoBehaviour
    {
        [Header("Target")]
        [Tooltip("The upgrader component on the processing machine")]
        public ProcessingMachineUpgrader targetUpgrader;
        
        [Header("UI References")]
        public Button button;
        public TextMeshProUGUI tierNameText;
        public TextMeshProUGUI levelText;
        public TextMeshProUGUI costText;
        public TextMeshProUGUI statsText;
        
        [Header("Formats")]
        public string levelFormat  = "Level {0} / {1}";
        public string costFormat   = "${0}";
        public string maxLevelText = "MAX LEVEL";
        
        private void Start()
        {
            if (button == null) button = GetComponent<Button>();
            if (button != null) button.onClick.AddListener(OnClicked);
            
            if (CashManager.Instance != null)
                CashManager.Instance.onCashChanged.AddListener(_ => Refresh());
            
            if (targetUpgrader != null)
                targetUpgrader.onUpgradePurchased.AddListener(_ => Refresh());
            
            Refresh();
        }
        
        private void OnDestroy()
        {
            if (CashManager.Instance != null)
                CashManager.Instance.onCashChanged.RemoveListener(_ => Refresh());
        }
        
        private void OnClicked()
        {
            if (targetUpgrader != null)
                targetUpgrader.TryUpgrade();
        }
        
        public void Refresh()
        {
            if (targetUpgrader == null || targetUpgrader.config == null) return;
            
            int level    = targetUpgrader.GetCurrentLevel();
            int maxLevel = targetUpgrader.GetMaxLevel();
            bool isMaxed = targetUpgrader.IsMaxLevel();
            int cost     = targetUpgrader.GetUpgradeCost();
            bool canAfford = targetUpgrader.CanUpgrade();
            
            if (tierNameText != null)
                tierNameText.text = targetUpgrader.GetTierName();
            
            if (levelText != null)
                levelText.text = isMaxed
                    ? maxLevelText
                    : string.Format(levelFormat, level, maxLevel);
            
            if (costText != null)
            {
                costText.text  = isMaxed ? "" : string.Format(costFormat, cost);
                costText.color = canAfford ? Color.white : Color.red;
            }
            
            if (statsText != null && targetUpgrader.config != null)
            {
                if (isMaxed)
                {
                    int cap   = targetUpgrader.config.GetCapacityAtLevel(level);
                    float spd = targetUpgrader.config.GetProcessingDurationAtLevel(level);
                    statsText.text = $"Speed: {spd:F1}s  |  Capacity: {cap}";
                }
                else
                {
                    int nextLevel   = level + 1;
                    int capNow      = targetUpgrader.config.GetCapacityAtLevel(level);
                    int capNext     = targetUpgrader.config.GetCapacityAtLevel(nextLevel);
                    float spdNow    = targetUpgrader.config.GetProcessingDurationAtLevel(level);
                    float spdNext   = targetUpgrader.config.GetProcessingDurationAtLevel(nextLevel);
                    
                    statsText.text = $"Speed: {spdNow:F1}s → {spdNext:F1}s  |  Capacity: {capNow} → {capNext}";
                }
            }
            
            if (button != null)
                button.interactable = canAfford && !isMaxed;
        }
    }
}
