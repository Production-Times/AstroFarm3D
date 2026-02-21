using UnityEngine;
using System.Collections;

namespace Inventory
{
    public class FarmProductionChain : MonoBehaviour
    {
        [Header("Production Chain Components")]
        public VacuumCollector harvester;
        public DropPoint cropStorage;
        public ConveyorBelt processingBelt;
        public ProcessingMachine processor;
        public DeliveryPoint deliveryZone;
        
        [Header("Auto Production Settings")]
        public bool autoProduction = false;
        public float checkInterval = 2f;
        
        private float checkTimer = 0f;
        
        private void Update()
        {
            if (autoProduction)
            {
                checkTimer += Time.deltaTime;
                
                if (checkTimer >= checkInterval)
                {
                    RunProductionCycle();
                    checkTimer = 0f;
                }
            }
        }
        
        private void RunProductionCycle()
        {
            if (harvester != null && harvester.GetCollectedCount() > 0)
            {
                InventoryItem item = harvester.UnloadItem();
                
                if (item != null && cropStorage != null)
                {
                    if (cropStorage.TryPlaceItem(item))
                    {
                        Debug.Log($"Moved item from harvester to storage");
                    }
                }
            }
        }
        
        [ContextMenu("Start Vacuum Harvest")]
        public void StartHarvest()
        {
            if (harvester != null)
            {
                harvester.StartVacuum();
                Debug.Log("Started vacuuming crops");
            }
        }
        
        [ContextMenu("Stop Vacuum Harvest")]
        public void StopHarvest()
        {
            if (harvester != null)
            {
                harvester.StopVacuum();
                Debug.Log("Stopped vacuuming crops");
            }
        }
        
        [ContextMenu("Send Crops to Processing")]
        public void SendToProcessing()
        {
            if (cropStorage == null || processingBelt == null)
                return;
            
            InventoryItem item = cropStorage.RemoveLastItem();
            if (item != null)
            {
                processingBelt.AddItemToBelt(item);
                Debug.Log("Sent crop to conveyor belt");
            }
        }
        
        [ContextMenu("Complete Delivery")]
        public void CompleteDelivery()
        {
            if (deliveryZone != null)
            {
                deliveryZone.StartDelivery();
                Debug.Log("Started delivery processing");
            }
        }
        
        [ContextMenu("Show Production Status")]
        public void ShowStatus()
        {
            Debug.Log("=== Farm Production Chain Status ===");
            
            if (harvester != null)
                Debug.Log($"Harvester: {harvester.GetCollectedCount()} items collected");
            
            if (cropStorage != null)
                Debug.Log($"Crop Storage: {cropStorage.GetCurrentCount()}/{cropStorage.maxCapacity} items");
            
            if (deliveryZone != null)
                Debug.Log($"Delivery Zone: {deliveryZone.GetCurrentCount()} items ready");
        }
    }
}
