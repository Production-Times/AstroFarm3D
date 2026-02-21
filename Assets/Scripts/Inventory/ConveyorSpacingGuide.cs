using UnityEngine;

namespace Inventory
{
    public class ConveyorSpacingGuide : MonoBehaviour
    {
        [Header("References")]
        public DropPoint dropPoint;
        public ConveyorBelt conveyorBelt;
        
        [Header("Read-Only Status")]
        [SerializeField] private float dropPointDelay;
        [SerializeField] private float beltSpacing;
        [SerializeField] private bool spacingEnforced;
        [SerializeField] private int itemsQueued;
        [SerializeField] private int itemsOnBelt;
        [SerializeField] private bool beltCanAccept;
        
        private void Start()
        {
            if (dropPoint == null)
                dropPoint = GetComponent<DropPoint>();
            
            ValidateSetup();
        }
        
        private void Update()
        {
            UpdateStatus();
        }
        
        private void UpdateStatus()
        {
            if (dropPoint != null)
            {
                dropPointDelay = dropPoint.conveyorSendDelay;
                itemsQueued = dropPoint.GetConveyorQueueCount();
            }
            
            if (conveyorBelt != null)
            {
                beltSpacing = conveyorBelt.minimumItemSpacing;
                spacingEnforced = conveyorBelt.enforceSpacing;
                itemsOnBelt = conveyorBelt.GetItemCount();
                beltCanAccept = conveyorBelt.CanAcceptItem();
            }
        }
        
        [ContextMenu("Validate Setup")]
        private void ValidateSetup()
        {
            bool isValid = true;
            
            if (dropPoint == null)
            {
                Debug.LogError("Missing DropPoint reference!", this);
                isValid = false;
            }
            
            if (conveyorBelt == null)
            {
                Debug.LogError("Missing ConveyorBelt reference!", this);
                isValid = false;
            }
            
            if (dropPoint != null && conveyorBelt != null)
            {
                if (!dropPoint.sendToConveyorBelt)
                {
                    Debug.LogWarning("DropPoint 'Send To Conveyor Belt' is disabled!", dropPoint);
                }
                
                if (dropPoint.targetConveyorBelt != conveyorBelt)
                {
                    Debug.LogWarning("DropPoint target conveyor doesn't match this conveyor!", dropPoint);
                }
                
                if (!conveyorBelt.enforceSpacing)
                {
                    Debug.LogWarning("ConveyorBelt spacing enforcement is disabled! Items may overlap.", conveyorBelt);
                }
                
                if (isValid)
                {
                    Debug.Log($"✓ Setup Valid!\n" +
                             $"  Drop Delay: {dropPoint.conveyorSendDelay}s\n" +
                             $"  Belt Spacing: {conveyorBelt.minimumItemSpacing * 100}% of belt length\n" +
                             $"  Spacing Enforced: {conveyorBelt.enforceSpacing}", this);
                }
            }
        }
        
        [ContextMenu("Recommended Settings - Slow")]
        public void SetSlowSettings()
        {
            if (dropPoint != null)
                dropPoint.conveyorSendDelay = 2.5f;
            
            if (conveyorBelt != null)
            {
                conveyorBelt.minimumItemSpacing = 0.3f;
                conveyorBelt.enforceSpacing = true;
                conveyorBelt.conveyorSpeed = 1.5f;
            }
            
            Debug.Log("Applied SLOW settings: 2.5s delay, 30% spacing, 1.5 speed");
        }
        
        [ContextMenu("Recommended Settings - Normal")]
        public void SetNormalSettings()
        {
            if (dropPoint != null)
                dropPoint.conveyorSendDelay = 1.5f;
            
            if (conveyorBelt != null)
            {
                conveyorBelt.minimumItemSpacing = 0.2f;
                conveyorBelt.enforceSpacing = true;
                conveyorBelt.conveyorSpeed = 2f;
            }
            
            Debug.Log("Applied NORMAL settings: 1.5s delay, 20% spacing, 2.0 speed");
        }
        
        [ContextMenu("Recommended Settings - Fast")]
        public void SetFastSettings()
        {
            if (dropPoint != null)
                dropPoint.conveyorSendDelay = 1f;
            
            if (conveyorBelt != null)
            {
                conveyorBelt.minimumItemSpacing = 0.15f;
                conveyorBelt.enforceSpacing = true;
                conveyorBelt.conveyorSpeed = 3f;
            }
            
            Debug.Log("Applied FAST settings: 1s delay, 15% spacing, 3.0 speed");
        }
    }
}
