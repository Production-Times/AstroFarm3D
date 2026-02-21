using UnityEngine;
using Inventory;

public class VacuumDiagnostics : MonoBehaviour
{
    public PlayerBackpack playerBackpack;
    
    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.V))
        {
            DiagnoseVacuum();
        }
    }
    
    [ContextMenu("Diagnose Vacuum")]
    public void DiagnoseVacuum()
    {
        if (playerBackpack == null)
        {
            playerBackpack = FindObjectOfType<PlayerBackpack>();
        }
        
        if (playerBackpack == null)
        {
            Debug.LogError("No PlayerBackpack found!");
            return;
        }
        
        Debug.Log("=== VACUUM DIAGNOSTICS ===");
        Debug.Log($"Pickup Range: {playerBackpack.pickupRange}");
        Debug.Log($"Item Layer Mask: {playerBackpack.itemLayerMask.value}");
        Debug.Log($"Max Capacity: {playerBackpack.maxCapacity}");
        Debug.Log($"Current Stack Count: {playerBackpack.GetItemCount()}");
        Debug.Log($"Debug Pickup Checks: {playerBackpack.debugPickupChecks}");
        
        Collider[] nearbyColliders = Physics.OverlapSphere(
            playerBackpack.transform.position,
            playerBackpack.pickupRange,
            playerBackpack.itemLayerMask
        );
        
        Debug.Log($"Found {nearbyColliders.Length} colliders in range");
        
        foreach (var col in nearbyColliders)
        {
            InventoryItem item = col.GetComponent<InventoryItem>();
            if (item == null)
                item = col.GetComponentInParent<InventoryItem>();
            
            if (item != null)
            {
                Debug.Log($"  - {item.name}:");
                Debug.Log($"      Layer: {LayerMask.LayerToName(item.gameObject.layer)}");
                Debug.Log($"      State: {item.GetCurrentState()}");
                Debug.Log($"      isPlaced: {item.isPlaced}");
                Debug.Log($"      isBeingCarried: {item.isBeingCarried}");
                Debug.Log($"      Parent: {(item.transform.parent != null ? item.transform.parent.name : "NULL")}");
                Debug.Log($"      Distance: {Vector3.Distance(playerBackpack.transform.position, item.transform.position)}");
            }
            else
            {
                Harvesting.Collectible collectible = col.GetComponent<Harvesting.Collectible>();
                if (collectible == null)
                    collectible = col.GetComponentInParent<Harvesting.Collectible>();
                
                if (collectible != null)
                {
                    Debug.Log($"  - {collectible.name} (Collectible only, no InventoryItem):");
                    Debug.Log($"      Layer: {LayerMask.LayerToName(collectible.gameObject.layer)}");
                    Debug.Log($"      isCollected: {collectible.isCollected}");
                }
                else
                {
                    Debug.Log($"  - {col.name} (No InventoryItem or Collectible component)");
                }
            }
        }
    }
}
