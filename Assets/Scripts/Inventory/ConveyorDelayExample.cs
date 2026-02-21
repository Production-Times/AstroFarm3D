using UnityEngine;

namespace Inventory
{
    public class ConveyorDelayExample : MonoBehaviour
    {
        [Header("Example Setup")]
        [Tooltip("This example shows how the delayed conveyor sending works")]
        public DropPoint exampleDropPoint;
        public ConveyorBelt exampleConveyorBelt;
        
        [Header("Test Settings")]
        public GameObject testItemPrefab;
        public int numberOfItemsToSpawn = 5;
        
        private void Start()
        {
            if (exampleDropPoint != null)
            {
                Debug.Log($"Drop Point configured with {exampleDropPoint.conveyorSendDelay}s delay between items");
            }
        }
        
        [ContextMenu("Spawn Test Items")]
        public void SpawnTestItems()
        {
            if (exampleDropPoint == null || testItemPrefab == null)
            {
                Debug.LogWarning("Missing drop point or test item prefab!");
                return;
            }
            
            for (int i = 0; i < numberOfItemsToSpawn; i++)
            {
                GameObject itemObj = Instantiate(testItemPrefab, exampleDropPoint.transform.position + Vector3.up * (i * 0.5f), Quaternion.identity);
                InventoryItem item = itemObj.GetComponent<InventoryItem>();
                
                if (item != null)
                {
                    exampleDropPoint.TryPlaceItem(item);
                }
            }
            
            Debug.Log($"Spawned {numberOfItemsToSpawn} items. They will be sent to conveyor one by one with {exampleDropPoint.conveyorSendDelay}s delay.");
        }
        
        [ContextMenu("Test Instant Send")]
        public void TestInstantSend()
        {
            if (exampleDropPoint == null)
                return;
            
            float originalDelay = exampleDropPoint.conveyorSendDelay;
            exampleDropPoint.conveyorSendDelay = 0.1f;
            
            Debug.Log("Set delay to 0.1s for fast sending");
        }
        
        [ContextMenu("Test Slow Send")]
        public void TestSlowSend()
        {
            if (exampleDropPoint == null)
                return;
            
            exampleDropPoint.conveyorSendDelay = 3f;
            
            Debug.Log("Set delay to 3s for slow sending");
        }
        
        [ContextMenu("Reset to Default Delay")]
        public void ResetDelay()
        {
            if (exampleDropPoint == null)
                return;
            
            exampleDropPoint.conveyorSendDelay = 1.5f;
            
            Debug.Log("Reset delay to 1.5s");
        }
    }
}
