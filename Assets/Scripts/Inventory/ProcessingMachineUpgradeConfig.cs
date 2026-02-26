using UnityEngine;
using System.Collections.Generic;

namespace Inventory
{
    [System.Serializable]
    public class ProcessingMachineTier
    {
        [Header("Info")]
        public string tierName = "Tier 1";
        public int upgradeCost = 100;
        
        [Header("Stats")]
        [Tooltip("Lower = faster. Multiplied against the machine's base durations.")]
        [Range(0.1f, 2f)]
        public float speedMultiplier = 1f;
        public int maxCapacity = 1;
        
        [Header("Model")]
        [Tooltip("Assign a new prefab/model here to swap the machine's visual at this tier. Leave null to keep the previous model.")]
        public GameObject modelPrefab;
        
        [Header("Model Transform (Baked)")]
        [HideInInspector] public Vector3 modelLocalPosition;
        [HideInInspector] public Vector4 modelLocalRotationQuat = new Vector4(0, 0, 0, 1);
        [HideInInspector] public Vector3 modelLocalScale = Vector3.one;
        
        public bool HasModel => modelPrefab != null;
        
        public void BakeModelTransform(Transform t)
        {
            modelLocalPosition = t.localPosition;
            modelLocalScale = t.localScale;
            Quaternion q = t.localRotation;
            modelLocalRotationQuat = new Vector4(q.x, q.y, q.z, q.w);
        }
        
        public void ApplyModelTransform(Transform t)
        {
            t.localPosition = modelLocalPosition;
            t.localScale = modelLocalScale;
            t.localRotation = new Quaternion(
                modelLocalRotationQuat.x,
                modelLocalRotationQuat.y,
                modelLocalRotationQuat.z,
                modelLocalRotationQuat.w
            );
        }
        
        public Vector3 BakedEuler => new Quaternion(
            modelLocalRotationQuat.x,
            modelLocalRotationQuat.y,
            modelLocalRotationQuat.z,
            modelLocalRotationQuat.w
        ).eulerAngles;
    }
    
    [CreateAssetMenu(fileName = "NewMachineConfig", menuName = "AstroFarm/Processing Machine Config")]
    public class ProcessingMachineUpgradeConfig : ScriptableObject
    {
        [Header("Machine Info")]
        public string machineName = "Processing Machine";
        
        [Header("Base Stats (Tier 0 - No upgrades)")]
        [Tooltip("Base processing duration before any upgrades")]
        public float baseProcessingDuration = 3f;
        [Tooltip("Base load duration before any upgrades")]
        public float baseLoadDuration = 1f;
        [Tooltip("Base unload duration before any upgrades")]
        public float baseUnloadDuration = 1f;
        [Tooltip("Base max capacity before any upgrades")]
        public int baseMaxCapacity = 1;
        
        [Space(10)]
        [Header("Upgrade Tiers")]
        [Tooltip("Each entry = one upgrade level. Add as many as you need.")]
        public List<ProcessingMachineTier> tiers = new List<ProcessingMachineTier>();
        
        public int MaxLevel => tiers.Count;
        
        public ProcessingMachineTier GetTier(int level)
        {
            if (level <= 0 || tiers.Count == 0) return null;
            return tiers[Mathf.Clamp(level - 1, 0, tiers.Count - 1)];
        }
        
        public int GetCostForLevel(int level)
        {
            var tier = GetTier(level);
            return tier != null ? tier.upgradeCost : 0;
        }
        
        public float GetProcessingDurationAtLevel(int level)
        {
            var tier = GetTier(level);
            return tier != null ? baseProcessingDuration * tier.speedMultiplier : baseProcessingDuration;
        }
        
        public float GetLoadDurationAtLevel(int level)
        {
            var tier = GetTier(level);
            return tier != null ? baseLoadDuration * tier.speedMultiplier : baseLoadDuration;
        }
        
        public float GetUnloadDurationAtLevel(int level)
        {
            var tier = GetTier(level);
            return tier != null ? baseUnloadDuration * tier.speedMultiplier : baseUnloadDuration;
        }
        
        public int GetCapacityAtLevel(int level)
        {
            var tier = GetTier(level);
            return tier != null ? tier.maxCapacity : baseMaxCapacity;
        }
        
        public GameObject GetModelAtLevel(int level)
        {
            for (int i = level - 1; i >= 0; i--)
            {
                if (tiers[i].HasModel)
                    return tiers[i].modelPrefab;
            }
            return null;
        }
        
        public ProcessingMachineTier GetModelTierAtLevel(int level)
        {
            for (int i = level - 1; i >= 0; i--)
            {
                if (tiers[i].HasModel)
                    return tiers[i];
            }
            return null;
        }
    }
}
