using UnityEngine;
using UnityEngine.Events;
using System.Collections;
using System.Collections.Generic;

namespace Inventory
{
    public class StorageRetrievalPad : MonoBehaviour
    {
        [Header("Pad Settings")]
        public string padName = "Retrieval Pad";
        public Color padColor = Color.blue;
        public Renderer padRenderer;
        public string colorPropertyName = "_BaseColor";
        
        [Header("Storage Reference")]
        public DropPoint storageDropPoint;
        public float retrievalInterval = 0.5f;
        
        [Header("Retrieval Settings")]
        public List<ItemData> retrievalItemTypes = new List<ItemData>();
        public bool retrieveAllTypes = false;
        
        [Header("Player Detection")]
        public LayerMask playerLayer;
        public float detectionRadius = 1.5f;
        
        [Header("Drop Settings")]
        public Vector3 dropOffset = new Vector3(0, 0.5f, 1f);
        
        [Header("Events")]
        public UnityEvent onRetrievalStart;
        public UnityEvent onRetrievalComplete;
        public UnityEvent<InventoryItem> onItemRetrieved;
        
        private PlayerBackpack playerBackpack;
        private bool isPlayerOnPad = false;
        private bool isRetrieving = false;
        private Material padMaterial;
        private Collider[] hitColliders = new Collider[5];
        private Coroutine retrievalCoroutine;
        
        private void Awake()
        {
            if (padRenderer != null)
            {
                padMaterial = padRenderer.material;
                SetPadColor(padColor);
            }
        }
        
        private void Update()
        {
            DetectPlayer();
            
            if (isPlayerOnPad && playerBackpack != null && !isRetrieving)
            {
                StartRetrieval();
            }
            else if (!isPlayerOnPad && isRetrieving)
            {
                StopRetrieval();
            }
        }
        
        private void DetectPlayer()
        {
            int count = Physics.OverlapSphereNonAlloc(transform.position, detectionRadius, hitColliders, playerLayer);
            
            isPlayerOnPad = false;
            playerBackpack = null;
            
            for (int i = 0; i < count; i++)
            {
                if (hitColliders[i] == null) continue;
                
                PlayerBackpack backpack = hitColliders[i].GetComponent<PlayerBackpack>();
                if (backpack == null)
                {
                    backpack = hitColliders[i].GetComponentInParent<PlayerBackpack>();
                }
                
                if (backpack != null)
                {
                    isPlayerOnPad = true;
                    playerBackpack = backpack;
                    break;
                }
            }
        }
        
        private void StartRetrieval()
        {
            if (storageDropPoint == null)
                return;
            
            if (retrievalCoroutine == null)
            {
                isRetrieving = true;
                retrievalCoroutine = StartCoroutine(RetrievalRoutine());
                onRetrievalStart?.Invoke();
            }
        }
        
        private void StopRetrieval()
        {
            if (retrievalCoroutine != null)
            {
                StopCoroutine(retrievalCoroutine);
                retrievalCoroutine = null;
                isRetrieving = false;
                onRetrievalComplete?.Invoke();
            }
        }
        
        private IEnumerator RetrievalRoutine()
        {
            while (isRetrieving && storageDropPoint != null)
            {
                bool retrieved = TryRetrieveOneItem();
                
                yield return new WaitForSeconds(retrievalInterval);
                
                if (!retrieved)
                {
                    yield return new WaitForSeconds(retrievalInterval);
                }
            }
        }
        
        private bool TryRetrieveOneItem()
        {
            if (storageDropPoint == null)
                return false;
            
            List<InventoryItem> storedItems = storageDropPoint.GetStoredItems();
            
            if (storedItems.Count == 0)
                return false;
            
            foreach (var item in storedItems)
            {
                if (CanRetrieveItem(item))
                {
                    InventoryItem retrievedItem = storageDropPoint.RemoveItem(storedItems.IndexOf(item));
                    
                    if (retrievedItem != null)
                    {
                        DeliverItemToPlayer(retrievedItem);
                        return true;
                    }
                }
            }
            
            return false;
        }
        
        private bool CanRetrieveItem(InventoryItem item)
        {
            if (item == null || item.itemData == null)
                return false;
            
            if (retrieveAllTypes)
                return true;
            
            return retrievalItemTypes.Contains(item.itemData);
        }
        
        private void DeliverItemToPlayer(InventoryItem item)
        {
            Vector3 dropPosition = transform.position + transform.TransformDirection(dropOffset);
            item.transform.position = dropPosition;
            
            Rigidbody rb = item.GetComponent<Rigidbody>();
            if (rb != null)
            {
                rb.isKinematic = false;
                rb.useGravity = true;
                rb.linearVelocity = Vector3.zero;
            }
            
            Collider col = item.GetComponent<Collider>();
            if (col != null)
            {
                col.enabled = true;
                col.isTrigger = false;
            }
            
            onItemRetrieved?.Invoke(item);
        }
        
        private void SetPadColor(Color color)
        {
            if (padMaterial != null)
            {
                padMaterial.SetColor(colorPropertyName, color);
            }
        }
        
        private void OnDrawGizmosSelected()
        {
            Gizmos.color = new Color(padColor.r, padColor.g, padColor.b, 0.5f);
            Gizmos.DrawWireSphere(transform.position, detectionRadius);
            
            if (storageDropPoint != null)
            {
                Gizmos.color = Color.cyan;
                Gizmos.DrawLine(transform.position, storageDropPoint.transform.position);
            }
            
            Gizmos.color = Color.yellow;
            Gizmos.DrawWireCube(transform.position + transform.TransformDirection(dropOffset), Vector3.one * 0.3f);
        }
    }
}
