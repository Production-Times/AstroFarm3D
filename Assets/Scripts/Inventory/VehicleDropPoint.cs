using UnityEngine;
using UnityEngine.Events;
using System.Collections.Generic;

namespace Inventory
{
    public class VehicleDropPoint : DropPoint
    {
        [Header("Vehicle Drop Settings")]
        public Transform unloadPosition;
        public bool autoUnloadOnExit = true;
        
        [Header("Vehicle Events")]
        public UnityEvent onVehicleEntered;
        public UnityEvent onVehicleExited;
        public UnityEvent onUnloadComplete;
        
        private bool vehicleInRange = false;
        
        private void Awake()
        {
            base.Awake();
            
            if (unloadPosition == null)
            {
                GameObject unloadObj = new GameObject("UnloadPosition");
                unloadObj.transform.SetParent(transform);
                unloadObj.transform.localPosition = Vector3.forward * 2f;
                unloadPosition = unloadObj.transform;
            }
        }
        
        public void OnVehicleEnterDropZone()
        {
            vehicleInRange = true;
            onVehicleEntered?.Invoke();
        }
        
        public void OnVehicleExitDropZone()
        {
            vehicleInRange = false;
            
            if (autoUnloadOnExit && storedItems.Count > 0)
            {
                UnloadAllItems();
            }
            
            onVehicleExited?.Invoke();
        }
        
        public void UnloadAllItems()
        {
            if (unloadPosition == null || storedItems.Count == 0)
                return;
            
            foreach (var item in storedItems)
            {
                if (item != null)
                {
                    item.transform.SetParent(unloadPosition);
                    item.OnTractorUnloaded();
                }
            }
            
            onUnloadComplete?.Invoke();
        }
        
        public bool IsVehicleInRange()
        {
            return vehicleInRange;
        }
        
        public List<InventoryItem> GetStoredItems()
        {
            return new List<InventoryItem>(storedItems);
        }
        
        public InventoryItem RemoveItem(InventoryItem item)
        {
            if (storedItems.Contains(item))
            {
                storedItems.Remove(item);
                return item;
            }
            return null;
        }
        
        private void OnTriggerEnter(Collider other)
        {
            if (other.CompareTag("Vehicle"))
            {
                OnVehicleEnterDropZone();
            }
        }
        
        private void OnTriggerExit(Collider other)
        {
            if (other.CompareTag("Vehicle"))
            {
                OnVehicleExitDropZone();
            }
        }
    }
}
