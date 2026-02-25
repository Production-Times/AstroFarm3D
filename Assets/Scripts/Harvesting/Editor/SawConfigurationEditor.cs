using UnityEngine;
using UnityEditor;
using System.Collections.Generic;

namespace Harvesting.Editor
{
    [CustomEditor(typeof(SawConfiguration))]
    public class SawConfigurationEditor : UnityEditor.Editor
    {
        private SawConfiguration config;
        
        private void OnEnable()
        {
            config = (SawConfiguration)target;
        }
        
        public override void OnInspectorGUI()
        {
            serializedObject.Update();
            
            DrawDefaultInspector();
            
            EditorGUILayout.Space(10);
            EditorGUILayout.LabelField("Transform Baking Tools", EditorStyles.boldLabel);
            EditorGUILayout.HelpBox("Select your vehicle in the scene, position the saws where you want them, then click 'Capture Transforms' to save the layout.", MessageType.Info);
            
            if (config.referenceVehicle == null)
            {
                EditorGUILayout.HelpBox("Assign a Reference Vehicle to use the baking tools!", MessageType.Warning);
            }
            
            EditorGUILayout.Space(5);
            
            for (int i = 0; i < config.layouts.Count; i++)
            {
                DrawLayoutBakingSection(i);
            }
            
            serializedObject.ApplyModifiedProperties();
        }
        
        private void DrawLayoutBakingSection(int layoutIndex)
        {
            SawLayout layout = config.layouts[layoutIndex];
            
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            
            EditorGUILayout.LabelField($"Layout: {layout.sawCount} Saw{(layout.sawCount > 1 ? "s" : "")}", EditorStyles.boldLabel);
            
            if (config.referenceVehicle != null)
            {
                EditorGUILayout.BeginHorizontal();
                
                if (GUILayout.Button($"📸 Capture Transforms ({layout.sawCount} saws)", GUILayout.Height(30)))
                {
                    CaptureTransforms(layoutIndex);
                }
                
                if (GUILayout.Button($"🔄 Apply to Scene", GUILayout.Height(30), GUILayout.Width(150)))
                {
                    ApplyTransformsToScene(layoutIndex);
                }
                
                EditorGUILayout.EndHorizontal();
                
                EditorGUILayout.Space(5);
                EditorGUILayout.LabelField($"Current: {CountSawsInVehicle()} saws in reference vehicle");
                
                if (layout.sawTransforms.Count > 0)
                {
                    EditorGUILayout.Space(3);
                    EditorGUI.indentLevel++;
                    for (int i = 0; i < Mathf.Min(layout.sawTransforms.Count, layout.sawCount); i++)
                    {
                        var data = layout.sawTransforms[i];
                        EditorGUILayout.LabelField($"Saw {i + 1}:", $"Euler: {data.localEulerAngles.ToString("F1")}", EditorStyles.miniLabel);
                        EditorGUILayout.LabelField("", $"Pos: {data.localPosition.ToString("F2")}", EditorStyles.miniLabel);
                    }
                    EditorGUI.indentLevel--;
                }
            }
            else
            {
                EditorGUILayout.HelpBox("Assign Reference Vehicle to use baking tools!", MessageType.Warning);
            }
            
            EditorGUILayout.EndVertical();
            EditorGUILayout.Space(5);
        }
        
        private void CaptureTransforms(int layoutIndex)
        {
            if (config.referenceVehicle == null)
            {
                EditorUtility.DisplayDialog("Error", "No reference vehicle assigned!", "OK");
                Debug.LogError("SawConfiguration: No reference vehicle assigned!");
                return;
            }
            
            // Find all HarvesterTool components (these are on the spinning children)
            HarvesterTool[] sawTools = config.referenceVehicle.GetComponentsInChildren<HarvesterTool>();
            SawLayout layout = config.layouts[layoutIndex];
            
            if (sawTools.Length == 0)
            {
                EditorUtility.DisplayDialog("Error", $"No HarvesterTool components found in '{config.referenceVehicle.name}' or its children!\n\nMake sure you've added HarvesterTool to the saw blade children.", "OK");
                Debug.LogError($"SawConfiguration: No HarvesterTool components found in {config.referenceVehicle.name}");
                return;
            }
            
            // Get the PARENT transforms (the ones with rotation/position)
            Transform[] sawParents = new Transform[sawTools.Length];
            for (int i = 0; i < sawTools.Length; i++)
            {
                sawParents[i] = sawTools[i].transform.parent != null ? sawTools[i].transform.parent : sawTools[i].transform;
            }
            
            if (sawParents.Length != layout.sawCount)
            {
                bool proceed = EditorUtility.DisplayDialog(
                    "Saw Count Mismatch",
                    $"Found {sawParents.Length} saws in vehicle, but this layout is for {layout.sawCount} saws.\n\nDo you want to capture transforms from the first {layout.sawCount} saws?",
                    "Yes, Capture",
                    "Cancel"
                );
                
                if (!proceed)
                    return;
            }
            
            Undo.RecordObject(config, "Capture Saw Transforms");
            
            layout.sawTransforms.Clear();
            
            int captureCount = Mathf.Min(sawParents.Length, layout.sawCount);
            
            Debug.Log($"=== Capturing {captureCount} Saw Parent Transforms ===");
            
            for (int i = 0; i < captureCount; i++)
            {
                Transform sawTransform = sawParents[i];
                SawTransformData data = new SawTransformData(sawTransform);
                layout.sawTransforms.Add(data);
                
                Debug.Log($"Saw {i + 1} ({sawTransform.name}):");
                Debug.Log($"  Position: {data.localPosition}");
                Debug.Log($"  Rotation (Euler): {data.localEulerAngles}");
                Debug.Log($"  Rotation (Quat): ({data.localRotationQuat.x:F3}, {data.localRotationQuat.y:F3}, {data.localRotationQuat.z:F3}, {data.localRotationQuat.w:F3})");
                Debug.Log($"  Scale: {data.localScale}");
            }
            
            while (layout.sawTransforms.Count < layout.sawCount)
            {
                layout.sawTransforms.Add(new SawTransformData());
            }
            
            EditorUtility.SetDirty(config);
            AssetDatabase.SaveAssets();
            
            Debug.Log($"✓ Captured and saved {captureCount} saw PARENT transforms for layout '{layout.sawCount} saws'");
            EditorUtility.DisplayDialog("Success", $"Captured {captureCount} saw parent transforms!\n\nCheck the Inspector to see the saved data.", "OK");
        }
        
        private void ApplyTransformsToScene(int layoutIndex)
        {
            if (config.referenceVehicle == null)
            {
                EditorUtility.DisplayDialog("Error", "No reference vehicle assigned!", "OK");
                Debug.LogError("SawConfiguration: No reference vehicle assigned!");
                return;
            }
            
            HarvesterTool[] sawTools = config.referenceVehicle.GetComponentsInChildren<HarvesterTool>();
            SawLayout layout = config.layouts[layoutIndex];
            
            if (sawTools.Length == 0)
            {
                EditorUtility.DisplayDialog("Error", $"No saws found in '{config.referenceVehicle.name}'!", "OK");
                return;
            }
            
            // Get parent transforms
            Transform[] sawParents = new Transform[sawTools.Length];
            for (int i = 0; i < sawTools.Length; i++)
            {
                sawParents[i] = sawTools[i].transform.parent != null ? sawTools[i].transform.parent : sawTools[i].transform;
            }
            
            if (sawParents.Length != layout.sawCount)
            {
                EditorUtility.DisplayDialog("Warning", $"Scene has {sawParents.Length} saws but layout is for {layout.sawCount} saws.\n\nWill apply to the first {Mathf.Min(sawParents.Length, layout.sawCount)} saws.", "OK");
            }
            
            int applyCount = Mathf.Min(sawParents.Length, layout.sawTransforms.Count);
            
            for (int i = 0; i < applyCount; i++)
            {
                Undo.RecordObject(sawParents[i], "Apply Saw Transform");
                layout.sawTransforms[i].ApplyToTransform(sawParents[i]);
                EditorUtility.SetDirty(sawParents[i]);
                
                Debug.Log($"Applied transform to saw parent {i + 1} ({sawParents[i].name}): Pos={layout.sawTransforms[i].localPosition}, Euler={layout.sawTransforms[i].localEulerAngles}");
            }
            
            Debug.Log($"✓ Applied transforms for {applyCount} saw parents from layout data");
            EditorUtility.DisplayDialog("Success", $"Applied transforms to {applyCount} saw parents!", "OK");
        }
        
        private int CountSawsInVehicle()
        {
            if (config.referenceVehicle == null)
                return 0;
            
            HarvesterTool[] tools = config.referenceVehicle.GetComponentsInChildren<HarvesterTool>();
            return tools.Length;
        }
    }
}
