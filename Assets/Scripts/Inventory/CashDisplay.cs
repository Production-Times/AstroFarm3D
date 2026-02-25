using UnityEngine;
using TMPro;

namespace Inventory
{
    public class CashDisplay : MonoBehaviour
    {
        [Header("UI References")]
        [SerializeField] private TextMeshProUGUI cashText;
        
        [Header("Display Settings")]
        [SerializeField] private string prefix = "$";
        [SerializeField] private string suffix = "";
        [SerializeField] private bool useThousandsSeparator = true;
        
        private void Start()
        {
            if (cashText == null)
            {
                cashText = GetComponent<TextMeshProUGUI>();
            }
            
            if (CashManager.Instance != null)
            {
                CashManager.Instance.onCashChanged.AddListener(UpdateDisplay);
                UpdateDisplay(CashManager.Instance.GetCurrentCash());
            }
        }
        
        private void OnDestroy()
        {
            if (CashManager.Instance != null)
            {
                CashManager.Instance.onCashChanged.RemoveListener(UpdateDisplay);
            }
        }
        
        private void UpdateDisplay(int amount)
        {
            if (cashText == null)
                return;
            
            string formattedAmount;
            if (useThousandsSeparator)
            {
                formattedAmount = amount.ToString("N0");
            }
            else
            {
                formattedAmount = amount.ToString();
            }
            
            cashText.text = $"{prefix}{formattedAmount}{suffix}";
        }
    }
}
