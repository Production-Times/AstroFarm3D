using UnityEngine;

namespace Inventory
{
    [System.Serializable]
    public class UpgradeData
    {
        [Header("Upgrade Info")]
        public UpgradeType upgradeType;
        public string upgradeName;
        [TextArea(2, 4)]
        public string description;
        
        [Header("Level Configuration")]
        public int maxLevel = 10;
        public int baseCost = 100;
        public float costMultiplier = 1.5f;
        
        [Header("Value Configuration")]
        public float baseValue;
        public float valuePerLevel;
        
        public int GetCostForLevel(int level)
        {
            if (level <= 0 || level > maxLevel)
                return 0;
            
            return Mathf.RoundToInt(baseCost * Mathf.Pow(costMultiplier, level - 1));
        }
        
        public float GetValueAtLevel(int level)
        {
            return baseValue + (valuePerLevel * level);
        }
    }
}
