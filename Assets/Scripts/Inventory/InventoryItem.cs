using UnityEngine;
using UnityEngine.Events;

namespace Inventory
{
    public enum ItemState
    {
        Free,
        PlayerBagAttached,
        PlayerCarried,
        VacuumCaptured,
        TractorUnloaded,
        DropPointPlaced,
        ConveyorBeltProcessing,
        ProcessingMachineLoading,
        ProcessingMachineProcessing,
        ProcessingMachineUnloading,
        DeliveryPoint,
        Selling,
        Custom1,
        Custom2,
        Custom3
    }
    
    [System.Serializable]
    public class ItemPhysicsSettings
    {
        [Header("Rigidbody Settings")]
        public bool useGravity = true;
        public bool isKinematic = false;
        public RigidbodyConstraints constraints = RigidbodyConstraints.None;
        public CollisionDetectionMode collisionDetection = CollisionDetectionMode.Discrete;
        public float mass = 1f;
        public float drag = 0f;
        public float angularDrag = 0.05f;
        
        [Header("Collision Settings")]
        public bool collidersEnabled = true;
        public bool isTrigger = false;
        
        [Header("Layer Settings")]
        public bool changeLayer = false;
        public string targetLayer = "Default";
    }
    
    public class InventoryItem : MonoBehaviour
    {
        [Header("Item Reference")]
        public ItemData itemData;
        
        [Header("Current State")]
        [SerializeField] private ItemState currentState = ItemState.Free;
        
        [Header("Legacy State Flags")]
        public bool isPlaced = false;
        public bool isBeingCarried = false;
        
        [Header("Physics Presets")]
        public ItemPhysicsSettings freeState = new ItemPhysicsSettings
        {
            useGravity = true,
            isKinematic = false,
            collidersEnabled = true,
            isTrigger = false
        };
        
        public ItemPhysicsSettings playerBagAttachedState = new ItemPhysicsSettings
        {
            useGravity = false,
            isKinematic = true,
            collidersEnabled = false,
            isTrigger = false
        };
        
        public ItemPhysicsSettings playerCarriedState = new ItemPhysicsSettings
        {
            useGravity = false,
            isKinematic = true,
            collidersEnabled = false,
            isTrigger = false
        };
        
        public ItemPhysicsSettings vacuumCapturedState = new ItemPhysicsSettings
        {
            useGravity = false,
            isKinematic = true,
            collidersEnabled = false,
            isTrigger = false
        };
        
        public ItemPhysicsSettings tractorUnloadedState = new ItemPhysicsSettings
        {
            useGravity = true,
            isKinematic = false,
            collidersEnabled = true,
            isTrigger = false
        };
        
        public ItemPhysicsSettings dropPointPlacedState = new ItemPhysicsSettings
        {
            useGravity = false,
            isKinematic = true,
            collidersEnabled = true,
            isTrigger = true
        };
        
        public ItemPhysicsSettings conveyorBeltProcessingState = new ItemPhysicsSettings
        {
            useGravity = false,
            isKinematic = true,
            collidersEnabled = false,
            isTrigger = false
        };
        
        public ItemPhysicsSettings processingMachineLoadingState = new ItemPhysicsSettings
        {
            useGravity = false,
            isKinematic = true,
            collidersEnabled = false,
            isTrigger = false
        };
        
        public ItemPhysicsSettings processingMachineProcessingState = new ItemPhysicsSettings
        {
            useGravity = false,
            isKinematic = true,
            collidersEnabled = false,
            isTrigger = false
        };
        
        public ItemPhysicsSettings processingMachineUnloadingState = new ItemPhysicsSettings
        {
            useGravity = false,
            isKinematic = true,
            collidersEnabled = true,
            isTrigger = false
        };
        
        public ItemPhysicsSettings deliveryPointState = new ItemPhysicsSettings
        {
            useGravity = false,
            isKinematic = true,
            collidersEnabled = true,
            isTrigger = true
        };
        
        public ItemPhysicsSettings sellingState = new ItemPhysicsSettings
        {
            useGravity = false,
            isKinematic = true,
            collidersEnabled = false,
            isTrigger = false
        };
        
        public ItemPhysicsSettings custom1State = new ItemPhysicsSettings();
        public ItemPhysicsSettings custom2State = new ItemPhysicsSettings();
        public ItemPhysicsSettings custom3State = new ItemPhysicsSettings();
        
        [Header("Events")]
        public UnityEvent<ItemState> onStateChanged;
        
        private Rigidbody rb;
        private Collider[] colliders;
        private int originalLayer;
        
        private void Awake()
        {
            rb = GetComponent<Rigidbody>();
            colliders = GetComponentsInChildren<Collider>(true);
            originalLayer = gameObject.layer;
            
            ApplyPhysicsSettings(GetSettingsForState(currentState));
        }
        
        public ItemState GetCurrentState()
        {
            return currentState;
        }
        
        public void SetState(ItemState newState)
        {
            if (currentState == newState)
                return;
            
            currentState = newState;
            ItemPhysicsSettings settings = GetSettingsForState(newState);
            ApplyPhysicsSettings(settings);
            
            UpdateLegacyFlags();
            
            onStateChanged?.Invoke(newState);
        }
        
        private ItemPhysicsSettings GetSettingsForState(ItemState state)
        {
            switch (state)
            {
                case ItemState.Free:
                    return freeState;
                case ItemState.PlayerBagAttached:
                    return playerBagAttachedState;
                case ItemState.PlayerCarried:
                    return playerCarriedState;
                case ItemState.VacuumCaptured:
                    return vacuumCapturedState;
                case ItemState.TractorUnloaded:
                    return tractorUnloadedState;
                case ItemState.DropPointPlaced:
                    return dropPointPlacedState;
                case ItemState.ConveyorBeltProcessing:
                    return conveyorBeltProcessingState;
                case ItemState.ProcessingMachineLoading:
                    return processingMachineLoadingState;
                case ItemState.ProcessingMachineProcessing:
                    return processingMachineProcessingState;
                case ItemState.ProcessingMachineUnloading:
                    return processingMachineUnloadingState;
                case ItemState.DeliveryPoint:
                    return deliveryPointState;
                case ItemState.Selling:
                    return sellingState;
                case ItemState.Custom1:
                    return custom1State;
                case ItemState.Custom2:
                    return custom2State;
                case ItemState.Custom3:
                    return custom3State;
                default:
                    return freeState;
            }
        }
        
        private void ApplyPhysicsSettings(ItemPhysicsSettings settings)
        {
            if (rb != null)
            {
                rb.isKinematic = settings.isKinematic;
                rb.useGravity = settings.useGravity;
                rb.constraints = settings.constraints;
                rb.collisionDetectionMode = settings.collisionDetection;
                rb.mass = settings.mass;
                rb.linearDamping = settings.drag;
                rb.angularDamping = settings.angularDrag;
            }
            
            foreach (var col in colliders)
            {
                if (col != null)
                {
                    col.enabled = settings.collidersEnabled;
                    col.isTrigger = settings.isTrigger;
                }
            }
            
            if (settings.changeLayer)
            {
                int layerIndex = LayerMask.NameToLayer(settings.targetLayer);
                if (layerIndex != -1)
                {
                    gameObject.layer = layerIndex;
                }
            }
            else
            {
                gameObject.layer = originalLayer;
            }
        }
        
        private void UpdateLegacyFlags()
        {
            isBeingCarried = (currentState == ItemState.PlayerCarried || currentState == ItemState.PlayerBagAttached);
            isPlaced = (currentState == ItemState.DropPointPlaced || 
                       currentState == ItemState.DeliveryPoint ||
                       currentState == ItemState.ConveyorBeltProcessing ||
                       currentState == ItemState.ProcessingMachineLoading ||
                       currentState == ItemState.ProcessingMachineProcessing ||
                       currentState == ItemState.ProcessingMachineUnloading ||
                       currentState == ItemState.Selling);
        }
        
        public void SetPhysicsEnabled(bool enabled)
        {
            if (rb != null)
            {
                rb.isKinematic = !enabled;
                rb.useGravity = enabled;
            }
            
            foreach (var col in colliders)
            {
                if (col != null)
                {
                    col.enabled = enabled;
                }
            }
        }
        
        public void OnPickedUp()
        {
            SetState(ItemState.PlayerCarried);
        }
        
        public void OnDropped()
        {
            SetState(ItemState.Free);
        }
        
        public void OnPlaced()
        {
            SetState(ItemState.DropPointPlaced);
        }
        
        public void OnAttachedToPlayerBag()
        {
            SetState(ItemState.PlayerBagAttached);
        }
        
        public void OnVacuumCaptured()
        {
            SetState(ItemState.VacuumCaptured);
        }
        
        public void OnTractorUnloaded()
        {
            SetState(ItemState.TractorUnloaded);
        }
        
        public void OnConveyorBeltProcessing()
        {
            SetState(ItemState.ConveyorBeltProcessing);
        }
        
        public void OnProcessingMachineLoading()
        {
            SetState(ItemState.ProcessingMachineLoading);
        }
        
        public void OnProcessingMachineProcessing()
        {
            SetState(ItemState.ProcessingMachineProcessing);
        }
        
        public void OnProcessingMachineUnloading()
        {
            SetState(ItemState.ProcessingMachineUnloading);
        }
        
        public void OnDeliveryPoint()
        {
            SetState(ItemState.DeliveryPoint);
        }
        
        public void OnSelling()
        {
            SetState(ItemState.Selling);
        }
    }
}
