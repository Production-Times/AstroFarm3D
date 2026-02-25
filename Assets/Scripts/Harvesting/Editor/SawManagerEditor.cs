using UnityEngine;
using UnityEditor;

namespace Harvesting.Editor
{
    [CustomEditor(typeof(SawManager))]
    public class SawManagerEditor : UnityEditor.Editor
    {
        private SawManager manager;
        
        private void OnEnable()
        {
            manager = (SawManager)target;
        }
        
        public override void OnInspectorGUI()
        {
            DrawDefaultInspector();
            
            EditorGUILayout.Space(10);
            EditorGUILayout.LabelField("Runtime Controls", EditorStyles.boldLabel);
            
            if (!Application.isPlaying)
            {
                EditorGUILayout.HelpBox("Enter Play Mode to test saw purchasing and management.", MessageType.Info);
                return;
            }
            
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            
            int currentCount = manager.GetCurrentSawCount();
            int maxCount = manager.GetMaxSawCount();
            
            EditorGUILayout.LabelField($"Current Saws: {currentCount} / {maxCount}", EditorStyles.boldLabel);
            
            EditorGUILayout.Space(5);
            
            GUI.enabled = !manager.IsMaxSaws();
            if (GUILayout.Button($"Purchase Next Saw (${manager.GetCostForNextSaw()})", GUILayout.Height(35)))
            {
                manager.TryPurchaseNextSaw();
            }
            GUI.enabled = true;
            
            EditorGUILayout.Space(5);
            
            EditorGUILayout.BeginHorizontal();
            
            if (GUILayout.Button("➕ Add Saw (Debug)"))
            {
                manager.SendMessage("DebugAddSaw");
            }
            
            if (GUILayout.Button("➖ Remove Saw (Debug)"))
            {
                manager.SendMessage("DebugRemoveSaw");
            }
            
            EditorGUILayout.EndHorizontal();
            
            if (GUILayout.Button("🔄 Reset to 1 Saw"))
            {
                manager.SendMessage("ResetSaws");
            }
            
            EditorGUILayout.EndVertical();
            
            EditorGUILayout.Space(5);
            
            if (Inventory.CashManager.Instance != null)
            {
                EditorGUILayout.LabelField($"Cash: ${Inventory.CashManager.Instance.GetCurrentCash()}");
            }
        }
    }
}
