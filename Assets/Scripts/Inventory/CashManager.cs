using UnityEngine;
using UnityEngine.Events;
using System.Collections.Generic;

namespace Inventory
{
    public class CashManager : MonoBehaviour
    {
        [Header("Cash Settings")]
        public int currentCash = 0;
        
        [Header("Events")]
        public UnityEvent<int> onCashChanged;
        public UnityEvent<int> onCashEarned;
        public UnityEvent<int> onCashSpent;
        
        private static CashManager instance;
        
        public static CashManager Instance
        {
            get
            {
                if (instance == null)
                {
                    instance = FindFirstObjectByType<CashManager>();
                    if (instance == null)
                    {
                        GameObject go = new GameObject("CashManager");
                        instance = go.AddComponent<CashManager>();
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
            
            LoadCash();
        }
        
        private void LoadCash()
        {
            currentCash = PlayerPrefs.GetInt("PlayerCash", 0);
            onCashChanged?.Invoke(currentCash);
        }
        
        private void SaveCash()
        {
            PlayerPrefs.SetInt("PlayerCash", currentCash);
            PlayerPrefs.Save();
        }
        
        public void AddCash(int amount)
        {
            if (amount <= 0)
                return;
            
            currentCash += amount;
            SaveCash();
            onCashEarned?.Invoke(amount);
            onCashChanged?.Invoke(currentCash);
            
            Debug.Log($"Cash earned: +${amount}. Total: ${currentCash}");
        }
        
        public bool TrySpendCash(int amount)
        {
            if (amount <= 0 || currentCash < amount)
                return false;
            
            currentCash -= amount;
            SaveCash();
            onCashSpent?.Invoke(amount);
            onCashChanged?.Invoke(currentCash);
            
            Debug.Log($"Cash spent: -${amount}. Remaining: ${currentCash}");
            return true;
        }
        
        public int GetCurrentCash()
        {
            return currentCash;
        }
        
        public bool HasEnoughCash(int amount)
        {
            return currentCash >= amount;
        }
        
        public int CalculateContainedObjectsValue(Transform container)
        {
            int totalValue = 0;
            
            InventoryItem[] items = container.GetComponentsInChildren<InventoryItem>();
            
            foreach (InventoryItem item in items)
            {
                if (item.itemData != null && item.itemData.hasValue)
                {
                    totalValue += item.itemData.value;
                }
            }
            
            return totalValue;
        }
        
        public int CalculateItemListValue(List<InventoryItem> items)
        {
            int totalValue = 0;
            
            foreach (InventoryItem item in items)
            {
                if (item != null && item.itemData != null && item.itemData.hasValue)
                {
                    totalValue += item.itemData.value;
                }
            }
            
            return totalValue;
        }
        
        public void EarnCashFromContainedObjects(Transform container)
        {
            int value = CalculateContainedObjectsValue(container);
            if (value > 0)
            {
                AddCash(value);
            }
        }
        
        public void EarnCashFromItemList(List<InventoryItem> items)
        {
            int value = CalculateItemListValue(items);
            if (value > 0)
            {
                AddCash(value);
            }
        }
    }
}
