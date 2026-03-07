using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace Inventory
{
    public class ProcessingMachineUpgradeButton : MonoBehaviour
    {
        [Header("Target")]
        [Tooltip("The ConveyorBelt component to upgrade")]
        public ConveyorBelt targetConveyorBelt;
        
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
            
            if (targetConveyorBelt != null)
                targetConveyorBelt.onUpgradePurchased.AddListener(_ => Refresh());
            
            Refresh();
        }
        
        private void OnDestroy()
        {
            if (CashManager.Instance != null)
                CashManager.Instance.onCashChanged.RemoveListener(_ => Refresh());
        }
        
        private void OnClicked()
        {
            if (targetConveyorBelt != null)
                targetConveyorBelt.TryUpgrade();
        }
        
        public void Refresh()
        {
            if (targetConveyorBelt == null) return;
            
            int level    = targetConveyorBelt.GetCurrentUpgradeLevel();
            int maxLevel = targetConveyorBelt.GetMaxUpgradeLevel();
            bool isMaxed = targetConveyorBelt.IsMaxLevel();
            int cost     = targetConveyorBelt.GetUpgradeCost();
            bool canAfford = targetConveyorBelt.CanUpgrade();
            
            if (tierNameText != null)
                tierNameText.text = targetConveyorBelt.GetCurrentTierName();
            
            if (levelText != null)
                levelText.text = isMaxed
                    ? maxLevelText
                    : string.Format(levelFormat, level, maxLevel);
            
            if (costText != null)
            {
                costText.text  = isMaxed ? "" : string.Format(costFormat, cost);
                costText.color = canAfford ? Color.white : Color.red;
            }
            
            if (statsText != null && targetConveyorBelt.upgradeSystemEnabled)
            {
                float speedMult = targetConveyorBelt.GetCurrentSpeedMultiplier();
                
                if (isMaxed)
                {
                    statsText.text = $"{speedMult:F2}x";
                }
                else
                {
                    int nextLevel = level + 1;
                    float nextMult = nextLevel <= targetConveyorBelt.upgradeTiers.Count 
                        ? targetConveyorBelt.upgradeTiers[nextLevel - 1].speedMultiplier 
                        : speedMult;
                    
                    statsText.text = $"{speedMult:F2}x → {nextMult:F2}x";
                }
            }
            
            if (button != null)
                button.interactable = canAfford && !isMaxed;
        }
    }
}
