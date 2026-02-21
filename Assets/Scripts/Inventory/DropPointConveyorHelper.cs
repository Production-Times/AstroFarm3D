using UnityEngine;

namespace Inventory
{
    public class DropPointConveyorHelper : MonoBehaviour
    {
        [Header("Reference")]
        public DropPoint dropPoint;
        
        [Header("Display Info (Read Only)")]
        [SerializeField] private int itemsInStorage;
        [SerializeField] private int itemsInQueue;
        [SerializeField] private bool currentlySending;
        [SerializeField] private float sendDelay;
        
        private void Awake()
        {
            if (dropPoint == null)
            {
                dropPoint = GetComponent<DropPoint>();
            }
        }
        
        private void Update()
        {
            if (dropPoint != null)
            {
                itemsInStorage = dropPoint.GetCurrentCount();
                itemsInQueue = dropPoint.GetConveyorQueueCount();
                currentlySending = dropPoint.IsSendingToConveyor();
                sendDelay = dropPoint.conveyorSendDelay;
            }
        }
        
        [ContextMenu("Send All Items to Conveyor Now")]
        public void SendAllToConveyor()
        {
            if (dropPoint != null)
            {
                dropPoint.SendAllItemsToConveyor();
                Debug.Log($"Queued {dropPoint.GetConveyorQueueCount()} items to send to conveyor belt with {dropPoint.conveyorSendDelay}s delay between each.");
            }
        }
        
        [ContextMenu("Stop Sending to Conveyor")]
        public void StopSending()
        {
            if (dropPoint != null)
            {
                dropPoint.StopSendingToConveyor();
                Debug.Log("Stopped sending items to conveyor belt and cleared queue.");
            }
        }
        
        [ContextMenu("Show Status")]
        public void ShowStatus()
        {
            if (dropPoint != null)
            {
                Debug.Log($"=== Drop Point Conveyor Status ===\n" +
                         $"Items in Storage: {dropPoint.GetCurrentCount()}\n" +
                         $"Items Queued for Conveyor: {dropPoint.GetConveyorQueueCount()}\n" +
                         $"Currently Sending: {dropPoint.IsSendingToConveyor()}\n" +
                         $"Send Delay: {dropPoint.conveyorSendDelay} seconds\n" +
                         $"Auto-send Enabled: {dropPoint.sendToConveyorBelt}\n" +
                         $"Target Belt: {(dropPoint.targetConveyorBelt != null ? dropPoint.targetConveyorBelt.name : "None")}",
                         gameObject);
            }
        }
        
        private void OnDrawGizmosSelected()
        {
            if (dropPoint == null || !dropPoint.sendToConveyorBelt || dropPoint.targetConveyorBelt == null)
                return;
            
            Gizmos.color = Color.cyan;
            Gizmos.DrawLine(transform.position, dropPoint.targetConveyorBelt.transform.position);
            
            if (dropPoint.IsSendingToConveyor())
            {
                Gizmos.color = Color.green;
                Gizmos.DrawWireSphere(transform.position + Vector3.up * 2f, 0.5f);
            }
        }
    }
}
