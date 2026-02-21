using UnityEngine;
using UnityEngine.Events;
using System.Collections;
using System.Collections.Generic;

namespace Inventory
{
    public class ProcessingMachine : MonoBehaviour
    {
        [Header("Machine Points")]
        public Transform loadPoint;
        public Transform processingPoint;
        public Transform unloadPoint;
        
        [Header("Processing Settings")]
        public float loadDuration = 1f;
        public float processingDuration = 3f;
        public float unloadDuration = 1f;
        
        [Header("Capacity")]
        public int maxCapacity = 1;
        
        [Header("Visual Effects")]
        public GameObject loadParticlePrefab;
        public GameObject processingParticlePrefab;
        public GameObject unloadParticlePrefab;
        public Vector3 particleOffset = Vector3.zero;
        
        [Header("Events")]
        public UnityEvent<InventoryItem> onItemStartedLoading;
        public UnityEvent<InventoryItem> onItemLoaded;
        public UnityEvent<InventoryItem> onItemStartedProcessing;
        public UnityEvent<InventoryItem> onItemProcessed;
        public UnityEvent<InventoryItem> onItemStartedUnloading;
        public UnityEvent<InventoryItem> onItemUnloaded;
        
        private List<InventoryItem> itemsInMachine = new List<InventoryItem>();
        private bool isProcessing = false;
        
        private void Awake()
        {
            if (loadPoint == null)
                loadPoint = transform;
            if (processingPoint == null)
                processingPoint = transform;
            if (unloadPoint == null)
                unloadPoint = transform;
        }
        
        public bool CanAcceptItem(InventoryItem item)
        {
            if (item == null || item.itemData == null)
                return false;
            
            if (itemsInMachine.Count >= maxCapacity)
                return false;
            
            if (isProcessing)
                return false;
            
            return item.itemData.canBeProcessed && item.itemData.processedResult != null;
        }
        
        public void LoadItem(InventoryItem item)
        {
            if (!CanAcceptItem(item))
                return;
            
            itemsInMachine.Add(item);
            StartCoroutine(ProcessItemSequence(item));
        }
        
        private IEnumerator ProcessItemSequence(InventoryItem item)
        {
            isProcessing = true;
            
            yield return StartCoroutine(LoadingPhase(item));
            yield return StartCoroutine(ProcessingPhase(item));
            yield return StartCoroutine(UnloadingPhase(item));
            
            isProcessing = false;
        }
        
        private IEnumerator LoadingPhase(InventoryItem item)
        {
            onItemStartedLoading?.Invoke(item);
            
            item.OnProcessingMachineLoading();
            item.transform.SetParent(transform);
            
            float elapsed = 0f;
            Vector3 startPos = item.transform.position;
            
            while (elapsed < loadDuration)
            {
                elapsed += Time.deltaTime;
                float t = elapsed / loadDuration;
                item.transform.position = Vector3.Lerp(startPos, loadPoint.position, t);
                yield return null;
            }
            
            item.transform.position = loadPoint.position;
            
            if (loadParticlePrefab != null)
            {
                Instantiate(loadParticlePrefab, item.transform.position + particleOffset, Quaternion.identity);
            }
            
            onItemLoaded?.Invoke(item);
        }
        
        private IEnumerator ProcessingPhase(InventoryItem item)
        {
            onItemStartedProcessing?.Invoke(item);
            
            item.OnProcessingMachineProcessing();
            
            float elapsed = 0f;
            Vector3 startPos = item.transform.position;
            
            while (elapsed < processingDuration)
            {
                elapsed += Time.deltaTime;
                float t = elapsed / processingDuration;
                item.transform.position = Vector3.Lerp(startPos, processingPoint.position, t);
                yield return null;
            }
            
            item.transform.position = processingPoint.position;
            
            if (item.itemData.canBeProcessed && item.itemData.processedResult != null)
            {
                ItemData newItemData = item.itemData.processedResult;
                
                if (processingParticlePrefab != null)
                {
                    Instantiate(processingParticlePrefab, item.transform.position + particleOffset, Quaternion.identity);
                }
                
                if (newItemData.prefab != null)
                {
                    Vector3 itemPos = item.transform.position;
                    Quaternion itemRot = item.transform.rotation;
                    
                    int itemIndex = itemsInMachine.IndexOf(item);
                    itemsInMachine.Remove(item);
                    Destroy(item.gameObject);
                    
                    GameObject newItemObj = Instantiate(newItemData.prefab, itemPos, itemRot);
                    InventoryItem newItem = newItemObj.GetComponent<InventoryItem>();
                    
                    if (newItem == null)
                    {
                        newItem = newItemObj.AddComponent<InventoryItem>();
                    }
                    
                    newItem.itemData = newItemData;
                    newItem.OnProcessingMachineProcessing();
                    newItem.transform.SetParent(transform);
                    itemsInMachine.Insert(itemIndex, newItem);
                    item = newItem;
                }
                else
                {
                    item.itemData = newItemData;
                }
            }
            
            onItemProcessed?.Invoke(item);
        }
        
        private IEnumerator UnloadingPhase(InventoryItem item)
        {
            onItemStartedUnloading?.Invoke(item);
            
            item.OnProcessingMachineUnloading();
            
            float elapsed = 0f;
            Vector3 startPos = item.transform.position;
            
            while (elapsed < unloadDuration)
            {
                elapsed += Time.deltaTime;
                float t = elapsed / unloadDuration;
                item.transform.position = Vector3.Lerp(startPos, unloadPoint.position, t);
                yield return null;
            }
            
            item.transform.position = unloadPoint.position;
            item.transform.SetParent(null);
            item.OnDropped();
            
            if (unloadParticlePrefab != null)
            {
                Instantiate(unloadParticlePrefab, item.transform.position + particleOffset, Quaternion.identity);
            }
            
            itemsInMachine.Remove(item);
            onItemUnloaded?.Invoke(item);
        }
        
        private void OnTriggerEnter(Collider other)
        {
            InventoryItem item = other.GetComponent<InventoryItem>();
            if (item == null)
            {
                item = other.GetComponentInParent<InventoryItem>();
            }
            
            if (item != null && !item.isBeingCarried && !item.isPlaced)
            {
                if (!itemsInMachine.Contains(item))
                {
                    LoadItem(item);
                }
            }
        }
        
        private void OnDrawGizmosSelected()
        {
            Transform load = loadPoint != null ? loadPoint : transform;
            Transform process = processingPoint != null ? processingPoint : transform;
            Transform unload = unloadPoint != null ? unloadPoint : transform;
            
            Gizmos.color = Color.green;
            Gizmos.DrawWireSphere(load.position, 0.3f);
            
            Gizmos.color = Color.yellow;
            Gizmos.DrawWireSphere(process.position, 0.3f);
            
            Gizmos.color = Color.red;
            Gizmos.DrawWireSphere(unload.position, 0.3f);
            
            Gizmos.color = Color.cyan;
            Gizmos.DrawLine(load.position, process.position);
            Gizmos.DrawLine(process.position, unload.position);
        }
    }
}
