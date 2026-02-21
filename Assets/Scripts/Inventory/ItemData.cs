using UnityEngine;

namespace Inventory
{
    [CreateAssetMenu(fileName = "NewItem", menuName = "AstroFarm/Item Data")]
    public class ItemData : ScriptableObject
    {
        [Header("Item Info")]
        public string itemName;
        public GameObject prefab;
        public Sprite icon;
        
        [Header("Value")]
        public bool hasValue = true;
        public int value = 10;
        
        [Header("Processing")]
        public bool canBeProcessed = false;
        public ItemData processedResult;
    }
}
