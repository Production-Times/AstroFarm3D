using UnityEngine;

namespace Inventory
{
    public class ItemStateDebugger : MonoBehaviour
    {
        [Header("Debug Settings")]
        public bool showStateInInspector = true;
        public bool logStateChanges = true;
        public Color gizmoColor = Color.cyan;
        
        private InventoryItem item;
        private ItemState lastState;
        
        private void Awake()
        {
            item = GetComponent<InventoryItem>();
            
            if (item != null)
            {
                item.onStateChanged.AddListener(OnStateChanged);
                lastState = item.GetCurrentState();
            }
        }
        
        private void OnStateChanged(ItemState newState)
        {
            if (logStateChanges)
            {
                Debug.Log($"[{gameObject.name}] State changed: {lastState} -> {newState}", gameObject);
                LogPhysicsSettings(newState);
            }
            
            lastState = newState;
        }
        
        private void LogPhysicsSettings(ItemState state)
        {
            Rigidbody rb = GetComponent<Rigidbody>();
            Collider[] cols = GetComponentsInChildren<Collider>();
            
            string physicsInfo = $"Physics for {state}:\n";
            
            if (rb != null)
            {
                physicsInfo += $"  - Gravity: {rb.useGravity}\n";
                physicsInfo += $"  - Kinematic: {rb.isKinematic}\n";
                physicsInfo += $"  - Mass: {rb.mass}\n";
            }
            
            physicsInfo += $"  - Colliders: {cols.Length} total\n";
            foreach (var col in cols)
            {
                if (col != null)
                {
                    physicsInfo += $"    * {col.GetType().Name}: Enabled={col.enabled}, IsTrigger={col.isTrigger}\n";
                }
            }
            
            Debug.Log(physicsInfo, gameObject);
        }
        
        private void OnDrawGizmos()
        {
            if (item == null)
                item = GetComponent<InventoryItem>();
            
            if (item != null)
            {
                ItemState currentState = item.GetCurrentState();
                
                Gizmos.color = GetColorForState(currentState);
                Gizmos.DrawWireSphere(transform.position + Vector3.up * 0.5f, 0.3f);
                
                #if UNITY_EDITOR
                if (showStateInInspector)
                {
                    UnityEditor.Handles.Label(
                        transform.position + Vector3.up * 1f,
                        currentState.ToString(),
                        new GUIStyle() { normal = new GUIStyleState() { textColor = Gizmos.color } }
                    );
                }
                #endif
            }
        }
        
        private Color GetColorForState(ItemState state)
        {
            switch (state)
            {
                case ItemState.Free:
                    return Color.white;
                case ItemState.PlayerBagAttached:
                    return Color.blue;
                case ItemState.PlayerCarried:
                    return Color.cyan;
                case ItemState.VacuumCaptured:
                    return Color.magenta;
                case ItemState.TractorUnloaded:
                    return Color.yellow;
                case ItemState.DropPointPlaced:
                    return Color.green;
                case ItemState.ConveyorBeltProcessing:
                    return new Color(1f, 0.5f, 0f);
                case ItemState.ProcessingMachineLoading:
                    return new Color(0.5f, 0f, 1f);
                case ItemState.ProcessingMachineProcessing:
                    return new Color(1f, 0f, 0.5f);
                case ItemState.ProcessingMachineUnloading:
                    return new Color(0.5f, 1f, 0f);
                case ItemState.DeliveryPoint:
                    return Color.yellow;
                case ItemState.Selling:
                    return Color.red;
                default:
                    return gizmoColor;
            }
        }
        
        [ContextMenu("Log Current State")]
        public void LogCurrentState()
        {
            if (item != null)
            {
                Debug.Log($"Current State: {item.GetCurrentState()}", gameObject);
                LogPhysicsSettings(item.GetCurrentState());
            }
        }
        
        [ContextMenu("Test All States")]
        public void TestAllStates()
        {
            if (item == null)
                return;
            
            Debug.Log("=== Testing All States ===", gameObject);
            
            System.Array allStates = System.Enum.GetValues(typeof(ItemState));
            foreach (ItemState state in allStates)
            {
                item.SetState(state);
                Debug.Log($"Set to: {state}", gameObject);
            }
            
            item.SetState(ItemState.Free);
            Debug.Log("Reset to Free state", gameObject);
        }
    }
}
