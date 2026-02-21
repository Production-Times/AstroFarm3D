using UnityEngine;
using UnityEngine.Events;
using System.Collections.Generic;

namespace Inventory
{
    public class StorageSystemManager : MonoBehaviour
    {
        [Header("References")]
        public StorageTransferPad transferPad;
        public List<StorageVacuumPad> storagePads = new List<StorageVacuumPad>();
        
        [Header("Statistics")]
        public bool showDebugInfo = true;
        
        [Header("Events")]
        public UnityEvent onStorageSystemReady;
        public UnityEvent onAllStorageFull;
        public UnityEvent onAllStorageEmpty;
        
        private void Start()
        {
            ValidateSetup();
            
            if (transferPad != null)
            {
                foreach (var pad in storagePads)
                {
                    if (pad != null)
                    {
                        pad.onStorageFull.AddListener(() => CheckAllStorageFull());
                    }
                }
                
                onStorageSystemReady?.Invoke();
            }
        }
        
        private void ValidateSetup()
        {
            if (transferPad == null)
            {
                Debug.LogWarning("[StorageSystemManager] StorageTransferPad not assigned!");
            }
            
            if (storagePads.Count == 0)
            {
                Debug.LogWarning("[StorageSystemManager] No StorageVacuumPads assigned!");
            }
            
            HashSet<Color> usedColors = new HashSet<Color>();
            foreach (var pad in storagePads)
            {
                if (pad != null)
                {
                    if (usedColors.Contains(pad.padColor))
                    {
                        Debug.LogWarning($"[StorageSystemManager] Duplicate pad color detected: {pad.padName}");
                    }
                    usedColors.Add(pad.padColor);
                }
            }
        }
        
        public void ManualDropAllFromPlayer()
        {
            if (transferPad != null)
            {
                transferPad.ManualDropAll();
            }
        }
        
        public void ClearAllStorage()
        {
            foreach (var pad in storagePads)
            {
                if (pad != null)
                {
                    pad.RemoveAllItems();
                }
            }
            
            onAllStorageEmpty?.Invoke();
        }
        
        public void EnableAllStorage(bool enable)
        {
            foreach (var pad in storagePads)
            {
                if (pad != null)
                {
                    pad.SetActive(enable);
                }
            }
        }
        
        public int GetTotalStoredItems()
        {
            int total = 0;
            foreach (var pad in storagePads)
            {
                if (pad != null)
                {
                    total += pad.GetStoredCount();
                }
            }
            return total;
        }
        
        public int GetTotalStorageCapacity()
        {
            int total = 0;
            foreach (var pad in storagePads)
            {
                if (pad != null)
                {
                    total += pad.maxCapacity;
                }
            }
            return total;
        }
        
        public float GetStorageFillPercentage()
        {
            int capacity = GetTotalStorageCapacity();
            if (capacity == 0)
                return 0f;
            
            return (float)GetTotalStoredItems() / capacity * 100f;
        }
        
        public StorageVacuumPad GetPadByColor(Color color)
        {
            foreach (var pad in storagePads)
            {
                if (pad != null && pad.padColor == color)
                {
                    return pad;
                }
            }
            return null;
        }
        
        public StorageVacuumPad GetPadForItemType(ItemData itemData)
        {
            foreach (var pad in storagePads)
            {
                if (pad != null)
                {
                    if (pad.acceptAllItems || pad.acceptedItemTypes.Contains(itemData))
                    {
                        return pad;
                    }
                }
            }
            return null;
        }
        
        public Dictionary<StorageVacuumPad, int> GetStorageReport()
        {
            Dictionary<StorageVacuumPad, int> report = new Dictionary<StorageVacuumPad, int>();
            
            foreach (var pad in storagePads)
            {
                if (pad != null)
                {
                    report[pad] = pad.GetStoredCount();
                }
            }
            
            return report;
        }
        
        private void CheckAllStorageFull()
        {
            bool allFull = true;
            
            foreach (var pad in storagePads)
            {
                if (pad != null && !pad.IsFull())
                {
                    allFull = false;
                    break;
                }
            }
            
            if (allFull)
            {
                onAllStorageFull?.Invoke();
            }
        }
        
        private void OnGUI()
        {
            if (!showDebugInfo)
                return;
            
            GUILayout.BeginArea(new Rect(10, 10, 300, 400));
            GUILayout.BeginVertical("box");
            
            GUILayout.Label($"<b>Storage System Status</b>", new GUIStyle(GUI.skin.label) { richText = true });
            GUILayout.Space(10);
            
            GUILayout.Label($"Total Items: {GetTotalStoredItems()} / {GetTotalStorageCapacity()}");
            GUILayout.Label($"Fill: {GetStorageFillPercentage():F1}%");
            GUILayout.Space(10);
            
            GUILayout.Space(10);
            
            foreach (var pad in storagePads)
            {
                if (pad != null)
                {
                    Color originalColor = GUI.color;
                    GUI.color = pad.padColor;
                    
                    string status = pad.IsFull() ? "FULL" : $"{pad.GetStoredCount()}/{pad.maxCapacity}";
                    GUILayout.Label($"{pad.padName}: {status}");
                    
                    GUI.color = originalColor;
                }
            }
            
            GUILayout.Space(10);
            
            if (GUILayout.Button("Clear Storage"))
            {
                ClearAllStorage();
            }
            
            GUILayout.EndVertical();
            GUILayout.EndArea();
        }
        
        private void OnDrawGizmos()
        {
            if (storagePads.Count == 0 || transferPad == null)
                return;
            
            Gizmos.color = Color.white;
            foreach (var pad in storagePads)
            {
                if (pad != null && transferPad != null)
                {
                    Gizmos.DrawLine(transferPad.transform.position, pad.transform.position);
                }
            }
        }
    }
}
