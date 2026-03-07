using UnityEngine;
using System.Collections.Generic;

#if UNITY_EDITOR
using UnityEditor;

namespace Inventory
{
    [CustomEditor(typeof(ConveyorBelt))]
    public class ConveyorBeltEditor : UnityEditor.Editor
    {
        private int selectedMachineIndex = -1;

        public override void OnInspectorGUI()
        {
            DrawDefaultInspector();

            ConveyorBelt conveyor = (ConveyorBelt)target;

            EditorGUILayout.Space(10);
            EditorGUILayout.LabelField("DEBUG TOOLS", EditorStyles.boldLabel);
            EditorGUILayout.HelpBox("Use these tools to test and debug the upgrade system", MessageType.Info);

            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("Reset All Levels", GUILayout.Height(30)))
            {
                ResetAllLevels(conveyor);
            }

            if (GUILayout.Button("Set to Level 1", GUILayout.Height(30)))
            {
                SetUpgradeLevel(conveyor, 1);
            }

            if (GUILayout.Button("Set to Level 2", GUILayout.Height(30)))
            {
                SetUpgradeLevel(conveyor, 2);
            }

            if (GUILayout.Button("Set to Level 3", GUILayout.Height(30)))
            {
                SetUpgradeLevel(conveyor, 3);
            }
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.Space(10);
            EditorGUILayout.LabelField("Machine Transform Setup", EditorStyles.boldLabel);
            
            GUILayout.Box("Select a machine config below, position/rotate/scale a prefab instance\n" +
                          "in the scene, then click 'Bake Transform' to save its transform.",
                EditorStyles.helpBox);

            EditorGUILayout.Space(5);

            // Show list of machine spawns
            if (conveyor.machineSpawnsOnUpgrade.Count == 0)
            {
                EditorGUILayout.HelpBox("No machines configured. Add machines to 'Machines to Add on Upgrade'", MessageType.Info);
                return;
            }

            EditorGUILayout.LabelField("Select Machine to Bake:", EditorStyles.boldLabel);

            for (int i = 0; i < conveyor.machineSpawnsOnUpgrade.Count; i++)
            {
                var machine = conveyor.machineSpawnsOnUpgrade[i];
                string label = machine.machinePrefab != null 
                    ? $"[Level {machine.spawnAtLevel}] {machine.machinePrefab.name}"
                    : $"[Level {machine.spawnAtLevel}] (No Prefab)";

                if (GUILayout.Button(label, selectedMachineIndex == i ? EditorStyles.toolbarButton : GUI.skin.button))
                {
                    selectedMachineIndex = i;
                }
            }

            EditorGUILayout.Space(5);

            if (selectedMachineIndex >= 0 && selectedMachineIndex < conveyor.machineSpawnsOnUpgrade.Count)
            {
                EditorGUILayout.LabelField("Bake Current Transform", EditorStyles.boldLabel);
                
                var selectedMachine = conveyor.machineSpawnsOnUpgrade[selectedMachineIndex];

                if (selectedMachine.machinePrefab == null)
                {
                    EditorGUILayout.HelpBox("Select a prefab first in the inspector above.", MessageType.Warning);
                    return;
                }

                EditorGUILayout.HelpBox(
                    "1. Find or spawn this machine in the scene\n" +
                    "2. Adjust its position, rotation, and scale\n" +
                    "3. Click 'Bake Transform' to save the values",
                    MessageType.Info
                );

                EditorGUILayout.Space(5);

                // Show current transform values
                EditorGUILayout.LabelField("Current Saved Values:", EditorStyles.boldLabel);
                EditorGUILayout.Vector3Field("Position", selectedMachine.position);
                EditorGUILayout.Vector3Field("Rotation (Euler)", selectedMachine.rotation);
                EditorGUILayout.Vector3Field("Scale", selectedMachine.scale);

                EditorGUILayout.Space(10);

                if (GUILayout.Button("Bake Transform from Selected Object", GUILayout.Height(40)))
                {
                    if (Selection.activeGameObject == null)
                    {
                        EditorUtility.DisplayDialog("Error", "Please select a GameObject in the scene first.", "OK");
                        return;
                    }

                    Transform selectedTransform = Selection.activeGameObject.transform;

                    selectedMachine.position = selectedTransform.localPosition;
                    selectedMachine.rotation = selectedTransform.localRotation.eulerAngles;
                    selectedMachine.scale = selectedTransform.localScale;

                    EditorUtility.SetDirty(conveyor);
                    Debug.Log($"[ConveyorBelt] Baked transform for machine at level {selectedMachine.spawnAtLevel}");
                }

                EditorGUILayout.Space(5);

                if (GUILayout.Button("Reset to Default", GUILayout.Height(30)))
                {
                    selectedMachine.position = Vector3.zero;
                    selectedMachine.rotation = Vector3.zero;
                    selectedMachine.scale = Vector3.one;

                    EditorUtility.SetDirty(conveyor);
                    Debug.Log($"[ConveyorBelt] Reset transform for machine at level {selectedMachine.spawnAtLevel}");
                }
            }
        }

        private void ResetAllLevels(ConveyorBelt conveyor)
        {
            if (EditorUtility.DisplayDialog(
                "Reset All Levels?",
                "This will reset all ConveyorBelt upgrade levels to 0 and destroy spawned machines.\n\nContinue?",
                "Yes",
                "Cancel"
            ))
            {
                // Find all ConveyorBelt objects in scene
                #pragma warning disable CS0618
                ConveyorBelt[] allConveyors = Object.FindObjectsOfType<ConveyorBelt>();
                #pragma warning restore CS0618
                
                foreach (var cb in allConveyors)
                {
                    ResetSingleLevel(cb);
                }

                EditorUtility.DisplayDialog("Success", $"Reset {allConveyors.Length} conveyor belt(s) to level 0", "OK");
                Debug.Log($"[DEBUG] Reset {allConveyors.Length} conveyor belts");
            }
        }

        private void SetUpgradeLevel(ConveyorBelt conveyor, int level)
        {
            if (conveyor == null) return;

            // Set the level using reflection or directly manipulate
            var levelField = typeof(ConveyorBelt).GetField("currentUpgradeLevel", 
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            
            if (levelField != null)
            {
                levelField.SetValue(conveyor, level);

                // Save to PlayerPrefs
                string key = $"CB_Level_{conveyor.machineID}";
                PlayerPrefs.SetInt(key, level);
                PlayerPrefs.Save();

                // Destroy existing spawned machines
                var machinesField = typeof(ConveyorBelt).GetField("spawnedMachines",
                    System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                
                if (machinesField != null)
                {
                    var spawnedMachines = (List<GameObject>)machinesField.GetValue(conveyor);
                    foreach (var machine in spawnedMachines)
                    {
                        if (machine != null)
                            Object.DestroyImmediate(machine);
                    }
                    spawnedMachines.Clear();
                }

                // Apply the new level (spawn machines)
                var applyMethod = typeof(ConveyorBelt).GetMethod("ApplyCurrentUpgradeLevel",
                    System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                
                if (applyMethod != null)
                {
                    applyMethod.Invoke(conveyor, null);
                }

                EditorUtility.SetDirty(conveyor);
                Debug.Log($"[DEBUG] Set {conveyor.machineID} to level {level}");
            }
        }

        private void ResetSingleLevel(ConveyorBelt conveyor)
        {
            if (conveyor == null) return;

            SetUpgradeLevel(conveyor, 0);
        }
    }
}
#endif
