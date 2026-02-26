using UnityEngine;
using UnityEditor;

namespace Inventory.Editor
{
    [CustomEditor(typeof(ProcessingMachineUpgrader))]
    public class ProcessingMachineUpgraderEditor : UnityEditor.Editor
    {
        public override void OnInspectorGUI()
        {
            DrawDefaultInspector();
            
            ProcessingMachineUpgrader upgrader = (ProcessingMachineUpgrader)target;
            
            EditorGUILayout.Space(8);
            EditorGUILayout.LabelField("Status", EditorStyles.boldLabel);
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            
            if (upgrader.config == null)
            {
                EditorGUILayout.HelpBox("Assign a ProcessingMachineUpgradeConfig to see status.", MessageType.Warning);
                EditorGUILayout.EndVertical();
                return;
            }
            
            int level = Application.isPlaying ? upgrader.GetCurrentLevel() : PlayerPrefs.GetInt($"PM_Level_{upgrader.machineID}", 0);
            int max   = upgrader.GetMaxLevel();
            
            EditorGUILayout.LabelField("Current Level", $"{level} / {max}");
            EditorGUILayout.LabelField("Current Tier",  Application.isPlaying ? upgrader.GetTierName() : (level == 0 ? "Base" : upgrader.config.GetTier(level)?.tierName ?? "Base"));
            
            if (level < max)
            {
                int cost = upgrader.config.GetCostForLevel(level + 1);
                EditorGUILayout.LabelField("Next Upgrade Cost", $"${cost}");
                
                if (Application.isPlaying)
                {
                    EditorGUILayout.LabelField("Cash Available", $"${CashManager.Instance?.GetCurrentCash() ?? 0}");
                    EditorGUILayout.Space(4);
                    
                    GUI.enabled = upgrader.CanUpgrade();
                    if (GUILayout.Button($"Purchase Upgrade → Level {level + 1}  (${cost})", GUILayout.Height(32)))
                    {
                        upgrader.TryUpgrade();
                    }
                    GUI.enabled = true;
                }
            }
            else
            {
                EditorGUILayout.LabelField("Status", "MAX LEVEL REACHED");
            }
            
            EditorGUILayout.EndVertical();
            
            if (!Application.isPlaying)
            {
                EditorGUILayout.Space(4);
                EditorGUILayout.HelpBox("Enter Play Mode to test upgrades.", MessageType.Info);
                
                if (GUILayout.Button("Reset Saved Level (Debug)"))
                {
                    if (EditorUtility.DisplayDialog("Reset Level", $"Reset saved upgrade level for '{upgrader.machineID}'?", "Yes", "Cancel"))
                    {
                        PlayerPrefs.DeleteKey($"PM_Level_{upgrader.machineID}");
                        PlayerPrefs.Save();
                        Debug.Log($"Reset upgrade level for {upgrader.machineID}");
                    }
                }
            }
        }
    }
}
