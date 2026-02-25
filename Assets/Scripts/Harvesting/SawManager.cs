using UnityEngine;
using UnityEngine.Events;
using System.Collections.Generic;

namespace Harvesting
{
    public class SawManager : MonoBehaviour
    {
        [Header("Configuration")]
        [Tooltip("The saw configuration database")]
        public SawConfiguration sawConfiguration;
        
        [Header("Saw Parent")]
        [Tooltip("Parent transform where saws will be spawned")]
        public Transform sawContainer;
        
        [Header("Current State")]
        [Tooltip("Current number of saws (1-6)")]
        [SerializeField] private int currentSawCount = 1;
        
        [Header("Events")]
        public UnityEvent<int> onSawCountChanged;
        public UnityEvent<int> onSawPurchased;
        
        private List<HarvesterTool> activeSaws = new List<HarvesterTool>();
        
        private void Awake()
        {
            if (sawContainer == null)
            {
                sawContainer = transform;
            }
            
            if (sawConfiguration != null)
            {
                SawPurchaseSystem.Initialize(sawConfiguration);
            }
            
            currentSawCount = SawPurchaseSystem.GetCurrentSawCount();
        }
        
        private void Start()
        {
            currentSawCount = SawPurchaseSystem.GetCurrentSawCount();
            ApplySawConfiguration();
        }
        
        public int GetCurrentSawCount()
        {
            return SawPurchaseSystem.GetCurrentSawCount();
        }
        
        public int GetMaxSawCount()
        {
            return SawPurchaseSystem.GetMaxSawCount();
        }
        
        public bool CanPurchaseNextSaw()
        {
            return SawPurchaseSystem.CanPurchaseNextSaw();
        }
        
        public int GetCostForNextSaw()
        {
            return SawPurchaseSystem.GetCostForNextSaw();
        }
        
        public bool TryPurchaseNextSaw()
        {
            bool success = SawPurchaseSystem.TryPurchaseNextSaw();
            
            if (success)
            {
                currentSawCount = SawPurchaseSystem.GetCurrentSawCount();
                ApplySawConfiguration();
                onSawPurchased?.Invoke(currentSawCount);
                onSawCountChanged?.Invoke(currentSawCount);
            }
            
            return success;
        }
        
        public void ApplySawConfiguration()
        {
            if (sawConfiguration == null)
            {
                Debug.LogError("SawManager: No saw configuration assigned!");
                return;
            }
            
            ClearAllSaws();
            
            SawLayout layout = sawConfiguration.GetLayoutForCount(currentSawCount);
            
            if (sawConfiguration.sawPrefab == null)
            {
                Debug.LogError("SawManager: No saw prefab assigned in configuration!");
                return;
            }
            
            for (int i = 0; i < layout.sawCount; i++)
            {
                GameObject sawObj = Instantiate(sawConfiguration.sawPrefab, sawContainer);
                sawObj.name = $"Saw_{i + 1}";
                
                if (i < layout.sawTransforms.Count)
                {
                    layout.sawTransforms[i].ApplyToTransform(sawObj.transform);
                }
                
                HarvesterTool saw = sawObj.GetComponent<HarvesterTool>();
                if (saw == null)
                {
                    saw = sawObj.AddComponent<HarvesterTool>();
                }
                
                activeSaws.Add(saw);
            }
            
            Debug.Log($"SawManager: Applied configuration for {currentSawCount} saws");
            onSawCountChanged?.Invoke(currentSawCount);
        }
        
        private void ClearAllSaws()
        {
            foreach (var saw in activeSaws)
            {
                if (saw != null)
                {
                    if (Application.isPlaying)
                    {
                        Destroy(saw.gameObject);
                    }
                    else
                    {
                        DestroyImmediate(saw.gameObject);
                    }
                }
            }
            
            activeSaws.Clear();
        }
        
        public List<HarvesterTool> GetActiveSaws()
        {
            return new List<HarvesterTool>(activeSaws);
        }
        
        public bool IsMaxSaws()
        {
            return SawPurchaseSystem.IsMaxSaws();
        }
    }
}
