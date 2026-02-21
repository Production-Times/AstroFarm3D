using UnityEngine;

namespace Inventory
{
    [System.Serializable]
    public class StackSettings
    {
        public enum StackMode
        {
            Vertical,
            Grid
        }
        
        [Header("Stack Type")]
        public StackMode stackMode = StackMode.Vertical;
        
        [Header("Vertical Settings")]
        public Vector3 verticalSpacing = new Vector3(0, 0.5f, 0);
        
        [Header("Grid Settings")]
        public Vector2Int gridDimensions = new Vector2Int(3, 3);
        public Vector3 gridSpacing = new Vector3(0.5f, 0.5f, 0.5f);
        
        public Vector3 GetStackPosition(int index)
        {
            if (stackMode == StackMode.Vertical)
            {
                return verticalSpacing * index;
            }
            else
            {
                int itemsPerLayer = gridDimensions.x * gridDimensions.y;
                int layer = index / itemsPerLayer;
                int posInLayer = index % itemsPerLayer;
                
                int x = posInLayer % gridDimensions.x;
                int z = posInLayer / gridDimensions.x;
                
                return new Vector3(
                    x * gridSpacing.x - (gridDimensions.x * gridSpacing.x * 0.5f) + (gridSpacing.x * 0.5f),
                    layer * gridSpacing.y,
                    z * gridSpacing.z
                );
            }
        }
        
        public Vector3 GetGizmoSize()
        {
            if (stackMode == StackMode.Vertical)
            {
                return verticalSpacing * 0.8f;
            }
            else
            {
                return gridSpacing * 0.9f;
            }
        }
    }
}
