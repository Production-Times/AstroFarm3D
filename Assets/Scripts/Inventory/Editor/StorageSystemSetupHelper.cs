using UnityEngine;
using UnityEditor;

namespace Inventory
{
    public class StorageSystemSetupHelper : EditorWindow
    {
        private VehicleDropPoint vehicleDropPoint;
        private Vector3 storageAreaCenter = Vector3.zero;
        private float padSpacing = 3f;
        private Material blueMaterial;
        private Material yellowMaterial;
        private Material redMaterial;
        private Material whiteMaterial;
        
        [MenuItem("AstroFarm/Setup Storage System")]
        public static void ShowWindow()
        {
            GetWindow<StorageSystemSetupHelper>("Storage Setup");
        }
        
        private void OnGUI()
        {
            GUILayout.Label("Storage System Quick Setup", EditorStyles.boldLabel);
            GUILayout.Space(10);
            
            EditorGUILayout.HelpBox(
                "This tool creates a complete storage system with:\n" +
                "• 3 colored vacuum pads (Blue, Yellow, Red)\n" +
                "• 1 white transfer pad\n" +
                "• Storage manager with debug UI",
                MessageType.Info
            );
            
            GUILayout.Space(10);
            
            vehicleDropPoint = EditorGUILayout.ObjectField(
                "Vehicle Drop Point",
                vehicleDropPoint,
                typeof(VehicleDropPoint),
                true
            ) as VehicleDropPoint;
            
            storageAreaCenter = EditorGUILayout.Vector3Field("Storage Area Center", storageAreaCenter);
            padSpacing = EditorGUILayout.FloatField("Pad Spacing", padSpacing);
            
            GUILayout.Space(10);
            GUILayout.Label("Materials (Optional)", EditorStyles.boldLabel);
            
            blueMaterial = EditorGUILayout.ObjectField("Blue Material", blueMaterial, typeof(Material), false) as Material;
            yellowMaterial = EditorGUILayout.ObjectField("Yellow Material", yellowMaterial, typeof(Material), false) as Material;
            redMaterial = EditorGUILayout.ObjectField("Red Material", redMaterial, typeof(Material), false) as Material;
            whiteMaterial = EditorGUILayout.ObjectField("White Material", whiteMaterial, typeof(Material), false) as Material;
            
            GUILayout.Space(20);
            
            GUI.enabled = vehicleDropPoint != null;
            
            if (GUILayout.Button("Create Storage System", GUILayout.Height(40)))
            {
                CreateStorageSystem();
            }
            
            GUI.enabled = true;
            
            if (vehicleDropPoint == null)
            {
                EditorGUILayout.HelpBox("Please assign a VehicleDropPoint to continue.", MessageType.Warning);
            }
        }
        
        private void CreateStorageSystem()
        {
            GameObject storageRoot = new GameObject("StorageSystem");
            storageRoot.transform.position = storageAreaCenter;
            
            Undo.RegisterCreatedObjectUndo(storageRoot, "Create Storage System");
            
            GameObject bluePad = CreateVacuumPad("BluePad", new Vector3(-padSpacing, 0, padSpacing), Color.blue, blueMaterial, storageRoot.transform);
            GameObject yellowPad = CreateVacuumPad("YellowPad", new Vector3(0, 0, padSpacing), Color.yellow, yellowMaterial, storageRoot.transform);
            GameObject redPad = CreateVacuumPad("RedPad", new Vector3(padSpacing, 0, padSpacing), Color.red, redMaterial, storageRoot.transform);
            GameObject whitePad = CreateTransferPad("WhitePad", Vector3.zero, Color.white, whiteMaterial, storageRoot.transform);
            
            StorageSystemManager manager = storageRoot.AddComponent<StorageSystemManager>();
            manager.transferPad = whitePad.GetComponent<StorageTransferPad>();
            manager.storagePads.Add(bluePad.GetComponent<StorageVacuumPad>());
            manager.storagePads.Add(yellowPad.GetComponent<StorageVacuumPad>());
            manager.storagePads.Add(redPad.GetComponent<StorageVacuumPad>());
            
            EditorUtility.SetDirty(manager);
            
            Selection.activeGameObject = storageRoot;
            
            Debug.Log($"[StorageSystemSetupHelper] Created storage system at {storageAreaCenter}");
            EditorUtility.DisplayDialog(
                "Storage System Created",
                "Storage system created successfully!\n\n" +
                "Next steps:\n" +
                "1. Assign ItemData to each colored pad's 'Accepted Item Types'\n" +
                "2. Adjust pad spacing and positions\n" +
                "3. Set Player layer in TransferPad\n" +
                "4. Test the system",
                "OK"
            );
        }
        
        private GameObject CreateVacuumPad(string name, Vector3 localPos, Color color, Material mat, Transform parent)
        {
            GameObject padObj = GameObject.CreatePrimitive(PrimitiveType.Plane);
            padObj.name = name;
            padObj.transform.SetParent(parent);
            padObj.transform.localPosition = localPos;
            padObj.transform.localScale = Vector3.one * 2f;
            
            if (mat != null)
            {
                padObj.GetComponent<MeshRenderer>().material = mat;
            }
            else
            {
                Material defaultMat = new Material(Shader.Find("Universal Render Pipeline/Lit"));
                defaultMat.color = color;
                padObj.GetComponent<MeshRenderer>().material = defaultMat;
            }
            
            Collider col = padObj.GetComponent<Collider>();
            if (col != null)
            {
                col.isTrigger = true;
            }
            
            StorageVacuumPad vacuumPad = padObj.AddComponent<StorageVacuumPad>();
            vacuumPad.padName = name;
            vacuumPad.padColor = color;
            vacuumPad.padRenderer = padObj.GetComponent<MeshRenderer>();
            vacuumPad.vacuumRadius = 4f;
            vacuumPad.maxCapacity = 30;
            
            return padObj;
        }
        
        private GameObject CreateTransferPad(string name, Vector3 localPos, Color color, Material mat, Transform parent)
        {
            GameObject padObj = GameObject.CreatePrimitive(PrimitiveType.Plane);
            padObj.name = name;
            padObj.transform.SetParent(parent);
            padObj.transform.localPosition = localPos;
            padObj.transform.localScale = Vector3.one * 2f;
            
            if (mat != null)
            {
                padObj.GetComponent<MeshRenderer>().material = mat;
            }
            else
            {
                Material defaultMat = new Material(Shader.Find("Universal Render Pipeline/Lit"));
                defaultMat.color = color;
                padObj.GetComponent<MeshRenderer>().material = defaultMat;
            }
            
            Collider col = padObj.GetComponent<Collider>();
            if (col != null)
            {
                col.isTrigger = true;
            }
            
            StorageTransferPad transferPad = padObj.AddComponent<StorageTransferPad>();
            transferPad.padColor = color;
            transferPad.padRenderer = padObj.GetComponent<MeshRenderer>();
            transferPad.transferInterval = 0.3f;
            transferPad.detectionRadius = 2f;
            
            return padObj;
        }
    }
}
