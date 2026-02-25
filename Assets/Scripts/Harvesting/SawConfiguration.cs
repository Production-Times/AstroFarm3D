using UnityEngine;
using System.Collections.Generic;

namespace Harvesting
{
    [System.Serializable]
    public class SawTransformData
    {
        public Vector3 localPosition;
        public Vector3 localEulerAngles;
        public Vector3 localScale = Vector3.one;
        
        [Tooltip("Quaternion stored as Vector4 (x, y, z, w)")]
        public Vector4 localRotationQuat;
        
        public SawTransformData()
        {
            localPosition = Vector3.zero;
            localEulerAngles = Vector3.zero;
            localScale = Vector3.one;
            localRotationQuat = new Vector4(0, 0, 0, 1);
        }
        
        public SawTransformData(Transform transform)
        {
            localPosition = transform.localPosition;
            localEulerAngles = transform.localEulerAngles;
            localScale = transform.localScale;
            
            Quaternion rot = transform.localRotation;
            localRotationQuat = new Vector4(rot.x, rot.y, rot.z, rot.w);
        }
        
        public void ApplyToTransform(Transform transform)
        {
            transform.localPosition = localPosition;
            
            Quaternion rotation = new Quaternion(
                localRotationQuat.x,
                localRotationQuat.y,
                localRotationQuat.z,
                localRotationQuat.w
            );
            transform.localRotation = rotation;
            
            transform.localScale = localScale;
        }
    }
    
    [System.Serializable]
    public class SawLayout
    {
        [Header("Configuration")]
        [Tooltip("Number of saws for this configuration (1-6)")]
        public int sawCount = 1;
        
        [Header("Transform Data")]
        [Tooltip("Transform data for each saw in this layout")]
        public List<SawTransformData> sawTransforms = new List<SawTransformData>();
        
        [Header("Purchase Settings")]
        [Tooltip("Cost to unlock this saw count")]
        public int purchaseCost = 100;
        
        public SawLayout(int count)
        {
            sawCount = count;
            sawTransforms = new List<SawTransformData>();
            for (int i = 0; i < count; i++)
            {
                sawTransforms.Add(new SawTransformData());
            }
        }
        
        public void EnsureSawCount()
        {
            while (sawTransforms.Count < sawCount)
            {
                sawTransforms.Add(new SawTransformData());
            }
            
            while (sawTransforms.Count > sawCount)
            {
                sawTransforms.RemoveAt(sawTransforms.Count - 1);
            }
        }
    }
    
    [CreateAssetMenu(fileName = "SawConfiguration", menuName = "AstroFarm/Saw Configuration")]
    public class SawConfiguration : ScriptableObject
    {
        [Header("Saw Prefab")]
        [Tooltip("The saw prefab to spawn")]
        public GameObject sawPrefab;
        
        [Header("Layouts (1-6 saws)")]
        [Tooltip("Transform configurations for each saw count")]
        public List<SawLayout> layouts = new List<SawLayout>();
        
        [Header("Reference for Baking")]
        [Tooltip("Reference to the vehicle/object that holds saws (used in editor for baking)")]
        public GameObject referenceVehicle;
        
        private void OnValidate()
        {
            while (layouts.Count < 6)
            {
                layouts.Add(new SawLayout(layouts.Count + 1));
            }
            
            while (layouts.Count > 6)
            {
                layouts.RemoveAt(layouts.Count - 1);
            }
            
            for (int i = 0; i < layouts.Count; i++)
            {
                layouts[i].sawCount = i + 1;
                layouts[i].EnsureSawCount();
            }
        }
        
        public SawLayout GetLayoutForCount(int sawCount)
        {
            sawCount = Mathf.Clamp(sawCount, 1, 6);
            return layouts[sawCount - 1];
        }
        
        public int GetCostForCount(int sawCount)
        {
            sawCount = Mathf.Clamp(sawCount, 1, 6);
            return layouts[sawCount - 1].purchaseCost;
        }
    }
}
