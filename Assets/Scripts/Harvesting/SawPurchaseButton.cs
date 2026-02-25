using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace Harvesting
{
    public class SawPurchaseButton : MonoBehaviour
    {
        [Header("UI Components")]
        public Button purchaseButton;
        public TextMeshProUGUI buttonText;
        public TextMeshProUGUI costText;
        public TextMeshProUGUI currentCountText;
        
        [Header("Display Formats")]
        public string buttonTextFormat = "Add Saw";
        public string costFormat = "${0}";
        public string countFormat = "Saws: {0}/6";
        public string maxLevelText = "MAX SAWS";
        
        private void Start()
        {
            if (purchaseButton == null)
            {
                purchaseButton = GetComponent<Button>();
            }
            
            if (purchaseButton != null)
            {
                purchaseButton.onClick.AddListener(OnPurchaseClicked);
            }
            
            UpdateDisplay();
            
            SawPurchaseSystem.onSawCountChanged.AddListener(OnSawCountChanged);
            
            if (Inventory.CashManager.Instance != null)
            {
                Inventory.CashManager.Instance.onCashChanged.AddListener(OnCashChanged);
            }
        }
        
        private void OnDestroy()
        {
            SawPurchaseSystem.onSawCountChanged.RemoveListener(OnSawCountChanged);
            
            if (Inventory.CashManager.Instance != null)
            {
                Inventory.CashManager.Instance.onCashChanged.RemoveListener(OnCashChanged);
            }
        }
        
        private void OnPurchaseClicked()
        {
            SawPurchaseSystem.TryPurchaseNextSaw();
        }
        
        private void OnSawCountChanged(int newCount)
        {
            UpdateDisplay();
        }
        
        private void OnCashChanged(int newCash)
        {
            UpdateDisplay();
        }
        
        private void UpdateDisplay()
        {
            int currentCount = SawPurchaseSystem.GetCurrentSawCount();
            int maxCount = SawPurchaseSystem.GetMaxSawCount();
            bool isMaxed = SawPurchaseSystem.IsMaxSaws();
            int cost = SawPurchaseSystem.GetCostForNextSaw();
            bool canAfford = SawPurchaseSystem.CanPurchaseNextSaw();
            
            if (buttonText != null)
            {
                if (isMaxed)
                {
                    buttonText.text = maxLevelText;
                }
                else
                {
                    buttonText.text = buttonTextFormat;
                }
            }
            
            if (costText != null)
            {
                if (isMaxed)
                {
                    costText.text = "";
                }
                else
                {
                    costText.text = string.Format(costFormat, cost);
                    costText.color = canAfford ? Color.white : Color.red;
                }
            }
            
            if (currentCountText != null)
            {
                currentCountText.text = string.Format(countFormat, currentCount, maxCount);
            }
            
            if (purchaseButton != null)
            {
                purchaseButton.interactable = canAfford && !isMaxed;
            }
        }
    }
}
