using UnityEngine;
using UnityEngine.Events;

namespace Harvesting
{
    public static class SawPurchaseSystem
    {
        private const string SAVE_KEY = "PlayerSawCount";
        private const int MAX_SAWS = 6;
        
        public static UnityEvent<int> onSawCountChanged = new UnityEvent<int>();
        public static UnityEvent<int> onSawPurchased = new UnityEvent<int>();
        
        private static SawConfiguration cachedConfig;
        
        public static void Initialize(SawConfiguration config)
        {
            cachedConfig = config;
        }
        
        public static int GetCurrentSawCount()
        {
            return PlayerPrefs.GetInt(SAVE_KEY, 1);
        }
        
        public static void SetSawCount(int count)
        {
            count = Mathf.Clamp(count, 1, MAX_SAWS);
            PlayerPrefs.SetInt(SAVE_KEY, count);
            PlayerPrefs.Save();
            onSawCountChanged?.Invoke(count);
        }
        
        public static int GetMaxSawCount()
        {
            return MAX_SAWS;
        }
        
        public static bool CanPurchaseNextSaw()
        {
            int currentCount = GetCurrentSawCount();
            
            if (currentCount >= MAX_SAWS)
                return false;
            
            int cost = GetCostForNextSaw();
            return Inventory.CashManager.Instance != null && Inventory.CashManager.Instance.HasEnoughCash(cost);
        }
        
        public static int GetCostForNextSaw()
        {
            int currentCount = GetCurrentSawCount();
            
            if (currentCount >= MAX_SAWS)
                return 0;
            
            int nextCount = currentCount + 1;
            
            if (cachedConfig != null)
            {
                return cachedConfig.GetCostForCount(nextCount);
            }
            
            return GetDefaultCost(nextCount);
        }
        
        private static int GetDefaultCost(int sawCount)
        {
            int[] defaultCosts = { 0, 250, 500, 1000, 2000, 4000 };
            
            if (sawCount >= 1 && sawCount <= 6)
            {
                return defaultCosts[sawCount - 1];
            }
            
            return 100 * sawCount * sawCount;
        }
        
        public static bool TryPurchaseNextSaw()
        {
            int currentCount = GetCurrentSawCount();
            
            if (currentCount >= MAX_SAWS)
            {
                Debug.Log("SawPurchaseSystem: Already at maximum saw count!");
                return false;
            }
            
            int cost = GetCostForNextSaw();
            
            if (Inventory.CashManager.Instance == null)
            {
                Debug.LogError("SawPurchaseSystem: CashManager not found!");
                return false;
            }
            
            if (Inventory.CashManager.Instance.TrySpendCash(cost))
            {
                currentCount++;
                SetSawCount(currentCount);
                
                onSawPurchased?.Invoke(currentCount);
                
                Debug.Log($"SawPurchaseSystem: Purchased saw! Now have {currentCount} saws for ${cost}");
                return true;
            }
            else
            {
                Debug.Log($"SawPurchaseSystem: Not enough cash! Need ${cost}, have ${Inventory.CashManager.Instance.GetCurrentCash()}");
                return false;
            }
        }
        
        public static bool IsMaxSaws()
        {
            return GetCurrentSawCount() >= MAX_SAWS;
        }
    }
}
