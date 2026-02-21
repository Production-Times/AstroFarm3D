using UnityEngine;
using UnityEngine.InputSystem;

namespace Inventory
{
    [RequireComponent(typeof(PlayerBackpack))]
    public class PlayerInventoryController : MonoBehaviour
    {
        [Header("Input Settings")]
        public InputActionReference dropAction;
        
        [Header("Fallback Keys")]
        public KeyCode dropKey = KeyCode.Q;
        
        [Header("Auto Drop on Delivery")]
        public bool autoDropAtDeliveryPoints = true;
        
        private PlayerBackpack backpack;
        private DeliveryPoint nearestDeliveryPoint;
        
        private void Awake()
        {
            backpack = GetComponent<PlayerBackpack>();
        }
        
        private void OnEnable()
        {
            if (dropAction != null && dropAction.action != null)
            {
                dropAction.action.Enable();
                dropAction.action.performed += OnDropPerformed;
            }
        }
        
        private void OnDisable()
        {
            if (dropAction != null && dropAction.action != null)
            {
                dropAction.action.performed -= OnDropPerformed;
                dropAction.action.Disable();
            }
        }
        
        private void Update()
        {
            if (dropAction == null && Input.GetKeyDown(dropKey))
            {
                TryDrop();
            }
            
            // Auto drop at delivery points
            if (autoDropAtDeliveryPoints && nearestDeliveryPoint != null && backpack.GetItemCount() > 0)
            {
                TryAutoDelivery();
            }
        }
        
        private void OnDropPerformed(InputAction.CallbackContext context)
        {
            TryDrop();
        }
        
        private void TryDrop()
        {
            if (backpack.GetItemCount() > 0)
            {
                backpack.DropAllItems();
            }
        }
        
        private void TryAutoDelivery()
        {
            if (nearestDeliveryPoint != null)
            {
                var items = backpack.GetItemCount();
                if (items > 0)
                {
                    // Drop items at delivery point would go here
                    // For now just a placeholder
                }
            }
        }
        
        public void SetNearestDeliveryPoint(DeliveryPoint deliveryPoint)
        {
            nearestDeliveryPoint = deliveryPoint;
        }
        
        public void ClearNearestDeliveryPoint()
        {
            nearestDeliveryPoint = null;
        }
    }
}
