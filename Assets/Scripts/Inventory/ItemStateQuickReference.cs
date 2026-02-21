using UnityEngine;

namespace Inventory
{
    public class ItemStateQuickReference : MonoBehaviour
    {
        [Header("Quick State Changers")]
        [Tooltip("The item to control")]
        public InventoryItem targetItem;
        
        [ContextMenu("1. Set FREE State")]
        public void SetFree() => targetItem?.OnDropped();
        
        [ContextMenu("2. Set PLAYER BAG ATTACHED State")]
        public void SetPlayerBagAttached() => targetItem?.OnAttachedToPlayerBag();
        
        [ContextMenu("3. Set PLAYER CARRIED State")]
        public void SetPlayerCarried() => targetItem?.OnPickedUp();
        
        [ContextMenu("4. Set VACUUM CAPTURED State")]
        public void SetVacuumCaptured() => targetItem?.OnVacuumCaptured();
        
        [ContextMenu("5. Set TRACTOR UNLOADED State")]
        public void SetTractorUnloaded() => targetItem?.OnTractorUnloaded();
        
        [ContextMenu("6. Set DROP POINT PLACED State")]
        public void SetDropPointPlaced() => targetItem?.OnPlaced();
        
        [ContextMenu("7. Set CONVEYOR BELT PROCESSING State")]
        public void SetConveyorBeltProcessing() => targetItem?.OnConveyorBeltProcessing();
        
        [ContextMenu("8. Set PROCESSING MACHINE LOADING State")]
        public void SetProcessingMachineLoading() => targetItem?.OnProcessingMachineLoading();
        
        [ContextMenu("9. Set PROCESSING MACHINE PROCESSING State")]
        public void SetProcessingMachineProcessing() => targetItem?.OnProcessingMachineProcessing();
        
        [ContextMenu("10. Set PROCESSING MACHINE UNLOADING State")]
        public void SetProcessingMachineUnloading() => targetItem?.OnProcessingMachineUnloading();
        
        [ContextMenu("11. Set DELIVERY POINT State")]
        public void SetDeliveryPoint() => targetItem?.OnDeliveryPoint();
        
        [ContextMenu("12. Set SELLING State")]
        public void SetSelling() => targetItem?.OnSelling();
        
        [ContextMenu("13. Set CUSTOM1 State")]
        public void SetCustom1() => targetItem?.SetState(ItemState.Custom1);
        
        [ContextMenu("14. Set CUSTOM2 State")]
        public void SetCustom2() => targetItem?.SetState(ItemState.Custom2);
        
        [ContextMenu("15. Set CUSTOM3 State")]
        public void SetCustom3() => targetItem?.SetState(ItemState.Custom3);
        
        [ContextMenu("--- Get Current State ---")]
        public void GetCurrentState()
        {
            if (targetItem != null)
            {
                Debug.Log($"Current State: {targetItem.GetCurrentState()}", targetItem);
            }
        }
        
        private void Start()
        {
            if (targetItem == null)
            {
                targetItem = GetComponent<InventoryItem>();
            }
        }
    }
}
