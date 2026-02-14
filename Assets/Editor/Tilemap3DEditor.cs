using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(Tilemap3D))]
public class Tilemap3DEditor : Editor
{
    private Tilemap3D tilemap;
    private bool paintMode = false;
    private int paintLayer = 0;
    private int selectedPrefabIndex = 0;

    void OnEnable()
    {
        tilemap = (Tilemap3D)target;
        SceneView.duringSceneGui += OnSceneGUI;
    }

    void OnDisable()
    {
        SceneView.duringSceneGui -= OnSceneGUI;
    }

    public override void OnInspectorGUI()
    {
        serializedObject.Update();

        EditorGUILayout.PropertyField(serializedObject.FindProperty("width"));
        EditorGUILayout.PropertyField(serializedObject.FindProperty("height"));
        EditorGUILayout.PropertyField(serializedObject.FindProperty("depth"));
        EditorGUILayout.PropertyField(serializedObject.FindProperty("cellSize"));

        EditorGUILayout.Space();
        EditorGUILayout.PropertyField(serializedObject.FindProperty("palette"));
        EditorGUILayout.PropertyField(serializedObject.FindProperty("singlePrefab"));
        EditorGUILayout.PropertyField(serializedObject.FindProperty("tileParent"));

        // placement rotation controls
        var usePrefabRotProp = serializedObject.FindProperty("usePrefabRotation");
        var placeRotProp = serializedObject.FindProperty("placeRotation");
        EditorGUILayout.PropertyField(usePrefabRotProp, new GUIContent("Use Prefab Rotation"));
        EditorGUI.BeginDisabledGroup(usePrefabRotProp.boolValue);
        EditorGUILayout.PropertyField(placeRotProp, new GUIContent("Place Rotation (Euler)"));
        EditorGUI.EndDisabledGroup();

        // prefab selection (palette-backed if available)
        GameObject[] options = null;
        string[] optionNames = null;
        if (tilemap.palette != null && tilemap.palette.prefabs != null && tilemap.palette.prefabs.Count > 0)
        {
            options = tilemap.palette.prefabs.ToArray();
            optionNames = new string[options.Length];
            for (int i = 0; i < options.Length; i++) optionNames[i] = options[i] != null ? options[i].name : "(Empty)";
            selectedPrefabIndex = Mathf.Clamp(selectedPrefabIndex, 0, options.Length - 1);
            selectedPrefabIndex = EditorGUILayout.Popup("Selected Prefab (palette)", selectedPrefabIndex, optionNames);
        }
        else
        {
            // no palette: show singlePrefab only
            EditorGUILayout.HelpBox("No Tile Palette assigned — using Single Prefab field.", MessageType.Info);
            options = new GameObject[] { (GameObject)serializedObject.FindProperty("singlePrefab").objectReferenceValue };
            optionNames = new string[] { options[0] != null ? options[0].name : "(None)" };
            selectedPrefabIndex = 0;
        }

        EditorGUILayout.Space();
        paintLayer = EditorGUILayout.IntSlider("Paint layer (Y)", paintLayer, 0, Mathf.Max(0, tilemap.height - 1));

        EditorGUILayout.BeginHorizontal();
        if (GUILayout.Button(paintMode ? "Exit Paint Mode" : "Enter Paint Mode"))
        {
            paintMode = !paintMode;
            SceneView.RepaintAll();
        }
        if (GUILayout.Button("Clear All"))
        {
            if (EditorUtility.DisplayDialog("Clear Tilemap?", "Remove all spawned tiles from this Tilemap?", "Yes", "Cancel"))
            {
                Undo.RecordObject(tilemap, "Clear Tilemap");
                tilemap.Clear();
            }
        }
        EditorGUILayout.EndHorizontal();

        EditorGUILayout.Space();
        if (GUILayout.Button("Fill Layer with Selected Prefab"))
        {
            GameObject prefab = options != null && options.Length > 0 ? options[selectedPrefabIndex] : null;
            if (prefab == null)
            {
                EditorUtility.DisplayDialog("No prefab", "Please assign a prefab (palette or singlePrefab).", "OK");
            }
            else
            {
                Undo.RecordObject(tilemap, "Fill Layer");
                tilemap.FillLayer(paintLayer, prefab);
            }
        }

        EditorGUILayout.Space();
        EditorGUILayout.HelpBox("Painting: Left-click to place, Right-click to remove. Use the Selected Prefab above.\nWhen done, exit Paint Mode.", MessageType.None);

        serializedObject.ApplyModifiedProperties();
    }

    void OnSceneGUI(SceneView sv)
    {
        if (!paintMode) return;
        Event e = Event.current;
        Ray r = HandleUtility.GUIPointToWorldRay(e.mousePosition);

        // plane at tilemap's base
        Plane p = new Plane(Vector3.up, tilemap.transform.position);
        if (!p.Raycast(r, out float enter)) return;
        Vector3 worldHit = r.GetPoint(enter);
        Vector3Int cell = tilemap.WorldToCell(worldHit);

        // clamp so preview stays inside grid
        if (!tilemap.IsInside(cell)) return;

        Vector3 center = tilemap.GetWorldPosition(cell);
        Vector3 size = tilemap.cellSize;

        // find selected prefab for preview
        GameObject prefabPreview = null;
        if (tilemap.palette != null && tilemap.palette.prefabs.Count > 0) prefabPreview = tilemap.palette.prefabs[selectedPrefabIndex];
        else prefabPreview = tilemap.singlePrefab;

        Quaternion rotPreview = tilemap.usePrefabRotation ? (prefabPreview != null ? prefabPreview.transform.rotation : Quaternion.identity) : Quaternion.Euler(tilemap.placeRotation);

        // preview (rotated)
        Handles.color = new Color(0f, 1f, 0f, 0.35f);
        Matrix4x4 prev = Handles.matrix;
        Handles.matrix = Matrix4x4.TRS(center + size * 0.5f - new Vector3(0, size.y * 0.5f, 0), rotPreview, Vector3.one);
        Handles.DrawWireCube(Vector3.zero, size);
        Handles.matrix = prev;

        // handle mouse events
        if (e.type == EventType.MouseDown && e.button == 0 && !e.alt)
        {
            // left click = place
            GameObject prefabToUse = null;
            if (tilemap.palette != null && tilemap.palette.prefabs.Count > 0) prefabToUse = tilemap.palette.prefabs[selectedPrefabIndex];
            else prefabToUse = tilemap.singlePrefab;

            if (prefabToUse != null)
            {
                Undo.RegisterCompleteObjectUndo(tilemap, "Place Tile");
                tilemap.PlaceTile(cell, prefabToUse);
                e.Use();
            }
        }
        else if (e.type == EventType.MouseDown && e.button == 1 && !e.alt)
        {
            // right click = remove
            Undo.RegisterCompleteObjectUndo(tilemap, "Remove Tile");
            tilemap.RemoveTile(cell);
            e.Use();
        }

        // ensure scene repaints while painting
        if (e.type == EventType.MouseMove) SceneView.RepaintAll();
    }
}
