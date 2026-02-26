using UnityEngine;
using UnityEditor;
using System.Collections.Generic;

namespace Inventory.Editor
{
    [CustomEditor(typeof(ProcessingMachineUpgradeConfig))]
    public class ProcessingMachineUpgradeConfigEditor : UnityEditor.Editor
    {
        private ProcessingMachineUpgradeConfig config;
        private List<bool> tierFoldouts = new List<bool>();
        private static readonly Color tierHeaderColor  = new Color(0.2f, 0.6f, 1f, 0.3f);
        private static readonly Color modelBakedColor  = new Color(0.2f, 0.8f, 0.3f, 0.3f);
        private static readonly Color modelMissingColor= new Color(1f, 0.6f, 0.2f, 0.2f);
        
        private void OnEnable()
        {
            config = (ProcessingMachineUpgradeConfig)target;
            SyncFoldouts();
        }
        
        private void SyncFoldouts()
        {
            while (tierFoldouts.Count < config.tiers.Count) tierFoldouts.Add(true);
            while (tierFoldouts.Count > config.tiers.Count) tierFoldouts.RemoveAt(tierFoldouts.Count - 1);
        }
        
        public override void OnInspectorGUI()
        {
            serializedObject.Update();
            SyncFoldouts();
            
            DrawMachineInfo();
            DrawBaseStats();
            DrawTiers();
            DrawAddRemoveTierButtons();
            
            serializedObject.ApplyModifiedProperties();
        }
        
        private void DrawMachineInfo()
        {
            EditorGUILayout.Space(4);
            EditorGUILayout.LabelField("Machine Info", EditorStyles.boldLabel);
            EditorGUILayout.PropertyField(serializedObject.FindProperty("machineName"));
            EditorGUILayout.Space(6);
        }
        
        private void DrawBaseStats()
        {
            EditorGUILayout.LabelField("Base Stats (Level 0 — No Upgrades)", EditorStyles.boldLabel);
            
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            EditorGUILayout.PropertyField(serializedObject.FindProperty("baseProcessingDuration"), new GUIContent("Processing Duration (s)"));
            EditorGUILayout.PropertyField(serializedObject.FindProperty("baseLoadDuration"),       new GUIContent("Load Duration (s)"));
            EditorGUILayout.PropertyField(serializedObject.FindProperty("baseUnloadDuration"),     new GUIContent("Unload Duration (s)"));
            EditorGUILayout.PropertyField(serializedObject.FindProperty("baseMaxCapacity"),        new GUIContent("Max Capacity"));
            EditorGUILayout.EndVertical();
            
            EditorGUILayout.Space(8);
            EditorGUILayout.LabelField($"Upgrade Tiers  ({config.tiers.Count} levels)", EditorStyles.boldLabel);
            EditorGUILayout.Space(2);
        }
        
        private void DrawTiers()
        {
            for (int i = 0; i < config.tiers.Count; i++)
            {
                DrawTier(i);
            }
        }
        
        private void DrawTier(int i)
        {
            ProcessingMachineTier tier = config.tiers[i];
            bool hasModel = tier.HasModel;
            bool hasBakedTransform = hasModel && tier.modelLocalScale != Vector3.zero;
            
            // Header color
            Color bg = hasModel ? modelBakedColor : tierHeaderColor;
            Rect headerRect = EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            EditorGUI.DrawRect(new Rect(headerRect.x, headerRect.y, headerRect.width, 2), hasModel ? new Color(0.2f, 0.8f, 0.3f) : new Color(0.2f, 0.6f, 1f));
            
            // Foldout header
            EditorGUILayout.BeginHorizontal();
            tierFoldouts[i] = EditorGUILayout.Foldout(tierFoldouts[i], $"  Level {i + 1}  —  {tier.tierName}  |  Cost: ${tier.upgradeCost}  |  Speed: ×{tier.speedMultiplier:F2}  |  Cap: {tier.maxCapacity}", true, EditorStyles.foldoutHeader);
            
            if (GUILayout.Button("✕", GUILayout.Width(22), GUILayout.Height(18)))
            {
                Undo.RecordObject(config, "Remove Upgrade Tier");
                config.tiers.RemoveAt(i);
                tierFoldouts.RemoveAt(i);
                EditorUtility.SetDirty(config);
                EditorGUILayout.EndHorizontal();
                EditorGUILayout.EndVertical();
                return;
            }
            EditorGUILayout.EndHorizontal();
            
            if (!tierFoldouts[i])
            {
                EditorGUILayout.EndVertical();
                EditorGUILayout.Space(2);
                return;
            }
            
            EditorGUI.indentLevel++;
            
            // Tier Name & Cost
            EditorGUILayout.Space(4);
            tier.tierName    = EditorGUILayout.TextField("Tier Name", tier.tierName);
            tier.upgradeCost = EditorGUILayout.IntField("Upgrade Cost ($)", tier.upgradeCost);
            
            EditorGUILayout.Space(4);
            EditorGUILayout.LabelField("Stats", EditorStyles.boldLabel);
            tier.speedMultiplier = EditorGUILayout.Slider("Speed Multiplier", tier.speedMultiplier, 0.1f, 2f);
            EditorGUILayout.HelpBox($"Processing: {config.baseProcessingDuration * tier.speedMultiplier:F2}s   Load: {config.baseLoadDuration * tier.speedMultiplier:F2}s   Unload: {config.baseUnloadDuration * tier.speedMultiplier:F2}s", MessageType.None);
            tier.maxCapacity = EditorGUILayout.IntField("Max Capacity", tier.maxCapacity);
            
            EditorGUILayout.Space(6);
            EditorGUILayout.LabelField("Model Change", EditorStyles.boldLabel);
            tier.modelPrefab = (GameObject)EditorGUILayout.ObjectField("Model Prefab", tier.modelPrefab, typeof(GameObject), false);
            
            if (hasModel)
            {
                EditorGUILayout.Space(2);
                
                // Bake Transform section
                EditorGUILayout.BeginVertical(EditorStyles.helpBox);
                EditorGUILayout.LabelField("Transform Baking", EditorStyles.boldLabel);
                
                if (hasBakedTransform)
                {
                    EditorGUILayout.LabelField($"Position:  {tier.modelLocalPosition.ToString("F3")}", EditorStyles.miniLabel);
                    EditorGUILayout.LabelField($"Rotation:  {tier.BakedEuler.ToString("F1")}",       EditorStyles.miniLabel);
                    EditorGUILayout.LabelField($"Scale:     {tier.modelLocalScale.ToString("F3")}",   EditorStyles.miniLabel);
                }
                else
                {
                    EditorGUILayout.HelpBox("No transform baked yet. Place the model in the scene, align it, then bake.", MessageType.Warning);
                }
                
                EditorGUILayout.BeginHorizontal();
                
                if (GUILayout.Button("📸  Bake from Scene", GUILayout.Height(28)))
                {
                    BakeModelTransform(i);
                }
                
                if (hasBakedTransform && GUILayout.Button("🔄  Apply to Scene", GUILayout.Height(28)))
                {
                    ApplyModelTransformToScene(i);
                }
                
                EditorGUILayout.EndHorizontal();
                EditorGUILayout.EndVertical();
            }
            else
            {
                EditorGUILayout.HelpBox("No model assigned — machine keeps the previous model at this tier.", MessageType.None);
            }
            
            EditorGUI.indentLevel--;
            EditorGUILayout.EndVertical();
            EditorGUILayout.Space(3);
            
            if (GUI.changed)
                EditorUtility.SetDirty(config);
        }
        
        private void BakeModelTransform(int tierIndex)
        {
            ProcessingMachineTier tier = config.tiers[tierIndex];
            
            if (!tier.HasModel)
            {
                EditorUtility.DisplayDialog("No Model", "Assign a model prefab first!", "OK");
                return;
            }
            
            // Find the model in scene by matching the prefab asset
            GameObject[] allObjects = FindObjectsByType<GameObject>(FindObjectsSortMode.None);
            GameObject sceneInstance = null;
            
            foreach (var obj in allObjects)
            {
                GameObject prefabSrc = UnityEditor.PrefabUtility.GetCorrespondingObjectFromSource(obj);
                if (prefabSrc == tier.modelPrefab || obj.name.StartsWith(tier.modelPrefab.name))
                {
                    sceneInstance = obj;
                    break;
                }
            }
            
            if (sceneInstance == null)
            {
                // Try by name fallback
                sceneInstance = GameObject.Find(tier.modelPrefab.name);
            }
            
            if (sceneInstance == null)
            {
                EditorUtility.DisplayDialog(
                    "Model Not Found in Scene",
                    $"Could not find '{tier.modelPrefab.name}' in the scene.\n\nDrag the model into the scene, position it correctly, then bake.",
                    "OK"
                );
                return;
            }
            
            Undo.RecordObject(config, "Bake Model Transform");
            tier.BakeModelTransform(sceneInstance.transform);
            EditorUtility.SetDirty(config);
            AssetDatabase.SaveAssets();
            
            Debug.Log($"[ProcessingMachine Config] Baked transform for Tier {tierIndex + 1} ({tier.tierName}): Pos={tier.modelLocalPosition}, Euler={tier.BakedEuler}, Scale={tier.modelLocalScale}");
            EditorUtility.DisplayDialog("Baked!", $"Tier {tierIndex + 1} model transform baked successfully!\n\nPosition: {tier.modelLocalPosition.ToString("F3")}\nRotation: {tier.BakedEuler.ToString("F1")}\nScale:    {tier.modelLocalScale.ToString("F3")}", "OK");
        }
        
        private void ApplyModelTransformToScene(int tierIndex)
        {
            ProcessingMachineTier tier = config.tiers[tierIndex];
            
            if (!tier.HasModel)
            {
                EditorUtility.DisplayDialog("No Model", "Assign a model prefab first!", "OK");
                return;
            }
            
            GameObject[] allObjects = FindObjectsByType<GameObject>(FindObjectsSortMode.None);
            GameObject sceneInstance = null;
            
            foreach (var obj in allObjects)
            {
                GameObject prefabSrc = UnityEditor.PrefabUtility.GetCorrespondingObjectFromSource(obj);
                if (prefabSrc == tier.modelPrefab || obj.name.StartsWith(tier.modelPrefab.name))
                {
                    sceneInstance = obj;
                    break;
                }
            }
            
            if (sceneInstance == null)
                sceneInstance = GameObject.Find(tier.modelPrefab.name);
            
            if (sceneInstance == null)
            {
                EditorUtility.DisplayDialog("Model Not Found", $"Could not find '{tier.modelPrefab.name}' in the scene.", "OK");
                return;
            }
            
            Undo.RecordObject(sceneInstance.transform, "Apply Model Transform");
            tier.ApplyModelTransform(sceneInstance.transform);
            EditorUtility.SetDirty(sceneInstance);
            
            Debug.Log($"Applied baked transform to '{sceneInstance.name}' in scene.");
            EditorUtility.DisplayDialog("Applied!", $"Baked transform applied to '{sceneInstance.name}' in scene.", "OK");
        }
        
        private void DrawAddRemoveTierButtons()
        {
            EditorGUILayout.Space(4);
            EditorGUILayout.BeginHorizontal();
            
            if (GUILayout.Button("＋  Add Upgrade Tier", GUILayout.Height(30)))
            {
                Undo.RecordObject(config, "Add Upgrade Tier");
                
                int newIndex = config.tiers.Count + 1;
                config.tiers.Add(new ProcessingMachineTier
                {
                    tierName        = $"Tier {newIndex}",
                    upgradeCost     = 100 * newIndex,
                    speedMultiplier = Mathf.Max(0.1f, 1f - (newIndex - 1) * 0.1f),
                    maxCapacity     = newIndex,
                    modelLocalScale = Vector3.one
                });
                
                tierFoldouts.Add(true);
                EditorUtility.SetDirty(config);
            }
            
            EditorGUILayout.EndHorizontal();
            EditorGUILayout.Space(4);
        }
    }
}
