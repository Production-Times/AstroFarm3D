using UnityEngine;
using System.Collections.Generic;

namespace Inventory
{
    [CreateAssetMenu(fileName = "UpgradeDatabase", menuName = "AstroFarm/Upgrade Database")]
    public class UpgradeDatabase : ScriptableObject
    {
        [Header("All Upgrades")]
        public List<UpgradeData> upgrades = new List<UpgradeData>();
        
        public UpgradeData GetUpgrade(UpgradeType type)
        {
            return upgrades.Find(u => u.upgradeType == type);
        }
        
        public bool HasUpgrade(UpgradeType type)
        {
            return upgrades.Exists(u => u.upgradeType == type);
        }
    }
}
