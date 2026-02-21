using UnityEngine;
using UnityEngine.Events;
using System.Collections.Generic;

namespace Inventory
{
    public class VehicleCargo : MonoBehaviour
    {
        [Header("Cargo Settings")]
        public Transform cargoHold;
        public int maxCapacity = 50;
        
        [Header("Unload Settings")]
        public Transform unloadSpawnPoint;
        public float unloadInterval = 0.2f;
        public float unloadForce = 2f;
        public Vector3 unloadDirection = Vector3.back;
        
        [Header("Visual Effects")]
        public GameObject loadParticlePrefab;
        public GameObject unloadParticlePrefab;
        public Vector3 particleOffset = Vector3.zero;
        
        [Header("Events")]
        public UnityEvent<InventoryItem> onItemLoaded;
        public UnityEvent<InventoryItem> onItemUnloaded;
        public UnityEvent onCargoFull;
        public UnityEvent onCargoEmpty;
        
        private List<InventoryItem> cargo = new List<InventoryItem>();
        private bool isUnloading = false;
        private float unloadTimer = 0f;
        
        private void Awake()
        {
            if (cargoHold == null)
                cargoHold = transform;
            if (unloadSpawnPoint == null)
                unloadSpawnPoint = transform;
        }
        
        private void Update()
        {
            if (isUnloading && cargo.Count > 0)
            {
                unloadTimer += Time.deltaTime;
                
                if (unloadTimer >= unloadInterval)
                {
                    UnloadSingleItem();
                    unloadTimer = 0f;
                }
            }
            else if (isUnloading && cargo.Count == 0)
            {
                StopUnloading();
            }
        }
        
        public bool CanLoadItem(InventoryItem item)
        {
            if (item == null || item.itemData == null)
                return false;
            
            return cargo.Count < maxCapacity;
        }
        
        public bool LoadItem(InventoryItem item)
        {
            if (!CanLoadItem(item))
                return false;
            
            cargo.Add(item);
            
            item.OnAttachedToPlayerBag();
            item.transform.SetParent(cargoHold);
            item.transform.localPosition = Vector3.zero;
            item.gameObject.SetActive(false);
            
            if (loadParticlePrefab != null)
            {
                Instantiate(loadParticlePrefab, cargoHold.position + particleOffset, Quaternion.identity);
            }
            
            onItemLoaded?.Invoke(item);
            
            if (cargo.Count >= maxCapacity)
            {
                onCargoFull?.Invoke();
            }
            
            return true;
        }
        
        public void StartUnloading()
        {
            if (cargo.Count == 0)
                return;
            
            isUnloading = true;
            unloadTimer = 0f;
        }
        
        public void StopUnloading()
        {
            isUnloading = false;
            unloadTimer = 0f;
        }
        
        public void UnloadAllItemsInstant()
        {
            while (cargo.Count > 0)
            {
                UnloadSingleItem();
            }
        }
        
        private void UnloadSingleItem()
        {
            if (cargo.Count == 0)
                return;
            
            InventoryItem item = cargo[0];
            cargo.RemoveAt(0);
            
            item.transform.SetParent(null);
            item.transform.position = unloadSpawnPoint.position;
            item.transform.rotation = unloadSpawnPoint.rotation;
            item.gameObject.SetActive(true);
            
            item.OnTractorUnloaded();
            
            Rigidbody rb = item.GetComponent<Rigidbody>();
            if (rb != null && unloadForce > 0f)
            {
                Vector3 force = unloadSpawnPoint.TransformDirection(unloadDirection.normalized) * unloadForce;
                rb.AddForce(force, ForceMode.VelocityChange);
            }
            
            if (unloadParticlePrefab != null)
            {
                Instantiate(unloadParticlePrefab, item.transform.position + particleOffset, Quaternion.identity);
            }
            
            onItemUnloaded?.Invoke(item);
            
            if (cargo.Count == 0)
            {
                onCargoEmpty?.Invoke();
            }
        }
        
        public int GetCargoCount()
        {
            return cargo.Count;
        }
        
        public bool IsFull()
        {
            return cargo.Count >= maxCapacity;
        }
        
        public bool IsEmpty()
        {
            return cargo.Count == 0;
        }
        
        public float GetCargoFillPercentage()
        {
            return (float)cargo.Count / maxCapacity;
        }
        
        private void OnDrawGizmosSelected()
        {
            Transform hold = cargoHold != null ? cargoHold : transform;
            Transform unload = unloadSpawnPoint != null ? unloadSpawnPoint : transform;
            
            Gizmos.color = Color.green;
            Gizmos.DrawWireCube(hold.position, Vector3.one * 0.5f);
            
            Gizmos.color = Color.red;
            Gizmos.DrawWireSphere(unload.position, 0.3f);
            
            if (unloadSpawnPoint != null)
            {
                Gizmos.color = Color.yellow;
                Vector3 direction = unloadSpawnPoint.TransformDirection(unloadDirection.normalized);
                Gizmos.DrawRay(unloadSpawnPoint.position, direction * 2f);
            }
        }
    }
}
