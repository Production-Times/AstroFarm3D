using UnityEngine;
using UnityEngine.Events;

namespace Inventory
{
    public class CoinManager : MonoBehaviour
    {
        [Header("Coin Settings")]
        public int currentCoins = 0;
        
        [Header("Events")]
        public UnityEvent<int> onCoinsChanged;
        public UnityEvent<int> onCoinsAdded;
        public UnityEvent<int> onCoinsSpent;
        
        private static CoinManager instance;
        
        public static CoinManager Instance
        {
            get
            {
                if (instance == null)
                {
                    instance = FindFirstObjectByType<CoinManager>();
                    if (instance == null)
                    {
                        GameObject go = new GameObject("CoinManager");
                        instance = go.AddComponent<CoinManager>();
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
        }
        
        public void AddCoins(int amount)
        {
            if (amount <= 0)
                return;
            
            currentCoins += amount;
            onCoinsAdded?.Invoke(amount);
            onCoinsChanged?.Invoke(currentCoins);
        }
        
        public bool TrySpendCoins(int amount)
        {
            if (amount <= 0 || currentCoins < amount)
                return false;
            
            currentCoins -= amount;
            onCoinsSpent?.Invoke(amount);
            onCoinsChanged?.Invoke(currentCoins);
            return true;
        }
        
        public int GetCurrentCoins()
        {
            return currentCoins;
        }
        
        public bool HasEnoughCoins(int amount)
        {
            return currentCoins >= amount;
        }
    }
}
