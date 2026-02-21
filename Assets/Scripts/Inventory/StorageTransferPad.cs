using UnityEngine;
using UnityEngine.Events;
using System.Collections;
using System.Collections.Generic;

namespace Inventory
{
    public class StorageTransferPad : MonoBehaviour
    {
        [Header("Pad Settings")]
        public Color padColor = Color.white;
        public Renderer padRenderer;
        public string colorPropertyName = "_BaseColor";
        
        [Header("Transfer Settings")]
        public DropPoint storageDropPoint;
        public float transferInterval = 0.3f;
        
        [Header("Player Detection")]
        public LayerMask playerLayer;
        public float detectionRadius = 1.5f;
        
        [Header("Visual Feedback")]
        public GameObject transferParticlePrefab;
        public Vector3 particleOffset = Vector3.zero;
        public GameObject activeIndicatorPrefab;
        
        [Header("Events")]
        public UnityEvent onTransferStart;
        public UnityEvent onTransferComplete;
        public UnityEvent<InventoryItem> onItemDropped;
        
        private PlayerBackpack playerBackpack;
        private bool isPlayerOnPad = false;
        private bool isTransferring = false;
        private Coroutine transferCoroutine;
        private Material padMaterial;
        private GameObject activeIndicator;
        private Collider[] hitColliders = new Collider[5];
        
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
            
            if (isPlayerOnPad && playerBackpack != null && playerBackpack.GetItemCount() > 0)
            {
                if (!isTransferring)
                {
                    StartTransfer();
                }
            }
            else if (isTransferring)
            {
                StopTransfer();
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
            
            if (activeIndicator != null)
            {
                activeIndicator.SetActive(isPlayerOnPad && playerBackpack != null && playerBackpack.GetItemCount() > 0);
            }
        }
        
        private void StartTransfer()
        {
            if (playerBackpack == null)
                return;
            
            if (transferCoroutine == null)
            {
                isTransferring = true;
                transferCoroutine = StartCoroutine(TransferRoutine());
                onTransferStart?.Invoke();
                
                if (activeIndicatorPrefab != null && activeIndicator == null)
                {
                    activeIndicator = Instantiate(activeIndicatorPrefab, transform.position + Vector3.up * 2f, Quaternion.identity, transform);
                }
            }
        }
        
        private void StopTransfer()
        {
            if (transferCoroutine != null)
            {
                StopCoroutine(transferCoroutine);
                transferCoroutine = null;
                isTransferring = false;
                onTransferComplete?.Invoke();
            }
        }
        
        private IEnumerator TransferRoutine()
        {
            while (isTransferring && playerBackpack != null)
            {
                if (playerBackpack.GetItemCount() == 0)
                {
                    yield break;
                }
                
                DropOneItemFromPlayer();
                
                yield return new WaitForSeconds(transferInterval);
            }
        }
        
        private void DropOneItemFromPlayer()
        {
            if (playerBackpack == null || playerBackpack.GetItemCount() == 0)
                return;
            
            if (storageDropPoint == null)
                return;
            
            List<InventoryItem> backpackItems = playerBackpack.GetBackpackItems();
            
            if (backpackItems.Count == 0)
                return;
            
            InventoryItem itemToDrop = backpackItems[backpackItems.Count - 1];
            
            if (itemToDrop == null)
                return;
            
            playerBackpack.RemoveItemFromBackpack(itemToDrop);
            
            bool placed = storageDropPoint.TryPlaceItem(itemToDrop);
            
            if (placed)
            {
                if (transferParticlePrefab != null)
                {
                    Instantiate(transferParticlePrefab, itemToDrop.transform.position + particleOffset, Quaternion.identity);
                }
                
                onItemDropped?.Invoke(itemToDrop);
            }
        }
        
        public void ManualDropAll()
        {
            if (playerBackpack != null)
            {
                while (playerBackpack.GetItemCount() > 0)
                {
                    DropOneItemFromPlayer();
                }
            }
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
        }
    }
}
