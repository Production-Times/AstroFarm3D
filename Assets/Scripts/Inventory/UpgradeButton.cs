using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace Inventory
{
    public class UpgradeButton : MonoBehaviour
    {
        [Header("Upgrade Configuration")]
        public UpgradeType upgradeType;
        
        [Header("UI References")]
        public Button upgradeButton;
        public TextMeshProUGUI nameText;
        public TextMeshProUGUI levelText;
        public TextMeshProUGUI costText;
        public TextMeshProUGUI descriptionText;
        
        [Header("Display Format")]
        public string costFormat = "${0}";
        public string levelFormat = "Level {0}/{1}";
        public string maxLevelText = "MAX LEVEL";
        
        private void Start()
        {
            if (upgradeButton == null)
            {
                upgradeButton = GetComponent<Button>();
            }
            
            if (upgradeButton != null)
            {
                upgradeButton.onClick.AddListener(OnUpgradeClicked);
            }
            
            UpdateDisplay();
            
            if (UpgradeManager.Instance != null)
            {
                UpgradeManager.Instance.onUpgradeLevelChanged.AddListener(OnAnyUpgradeChanged);
            }
            
            if (CashManager.Instance != null)
            {
                CashManager.Instance.onCashChanged.AddListener(OnCashChanged);
            }
        }
        
        private void OnDestroy()
        {
            if (UpgradeManager.Instance != null)
            {
                UpgradeManager.Instance.onUpgradeLevelChanged.RemoveListener(OnAnyUpgradeChanged);
            }
            
            if (CashManager.Instance != null)
            {
                CashManager.Instance.onCashChanged.RemoveListener(OnCashChanged);
            }
        }
        
        private void OnUpgradeClicked()
        {
            if (UpgradeManager.Instance != null)
            {
                UpgradeManager.Instance.TryUpgrade(upgradeType);
            }
        }
        
        private void OnAnyUpgradeChanged(UpgradeType type, int level)
        {
            UpdateDisplay();
        }
        
        private void OnCashChanged(int newCash)
        {
            UpdateDisplay();
        }
        
        private void UpdateDisplay()
        {
            if (UpgradeManager.Instance == null)
                return;
            
            UpgradeData data = UpgradeManager.Instance.GetUpgradeData(upgradeType);
            if (data == null)
                return;
            
            int currentLevel = UpgradeManager.Instance.GetUpgradeLevel(upgradeType);
            bool isMaxLevel = UpgradeManager.Instance.IsMaxLevel(upgradeType);
            int cost = UpgradeManager.Instance.GetUpgradeCost(upgradeType);
            bool canAfford = UpgradeManager.Instance.CanUpgrade(upgradeType);
            
            if (nameText != null)
            {
                nameText.text = data.upgradeName;
            }
            
            if (levelText != null)
            {
                if (isMaxLevel)
                {
                    levelText.text = maxLevelText;
                }
                else
                {
                    levelText.text = string.Format(levelFormat, currentLevel, data.maxLevel);
                }
            }
            
            if (costText != null)
            {
                if (isMaxLevel)
                {
                    costText.text = "";
                }
                else
                {
                    costText.text = string.Format(costFormat, cost);
                    
                    if (!canAfford)
                    {
                        costText.color = Color.red;
                    }
                    else
                    {
                        costText.color = Color.white;
                    }
                }
            }
            
            if (descriptionText != null)
            {
                float currentValue = UpgradeManager.Instance.GetUpgradeValue(upgradeType);
                float nextValue = isMaxLevel ? currentValue : data.GetValueAtLevel(currentLevel + 1);
                
                string desc = data.description;
                desc += $"\n\nCurrent: {currentValue}";
                if (!isMaxLevel)
                {
                    desc += $"\nNext: {nextValue}";
                }
                
                descriptionText.text = desc;
            }
            
            if (upgradeButton != null)
            {
                upgradeButton.interactable = canAfford && !isMaxLevel;
            }
        }
    }
}
