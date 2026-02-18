using UnityEngine;
using System.Collections.Generic;

namespace Harvesting
{
    public class VehicleInventory : MonoBehaviour
    {
        [Header("Stacking Settings")]
        [Tooltip("Point where the stack begins.")]
        public Transform trunkPoint;
        [Tooltip("Maximum number of items.")]
        public int maxCapacity = 40;
        [Tooltip("Spacing between items in the stack.")]
        public Vector3 itemSpacing = new Vector3(0.5f, 0.5f, 0.5f);
        [Tooltip("Grid dimensions for stacking (e.g. 3x3 base).")]
        public Vector2Int gridDimensions = new Vector2Int(3, 3);
        
        [Header("Vacuum Settings")]
        [Tooltip("Radius to attract collectibles.")]
        public float vacuumRadius = 6.0f;
        [Tooltip("Force/Speed at which collectibles are pulled.")]
        public float vacuumSpeed = 15f;
        [Tooltip("Distance at which the item is considered 'collected'.")]
        public float collectionDistance = 0.5f;

        [Tooltip("Layers to include in the vacuum check.")]
        public LayerMask vacuumLayers = -1; // Default to Everything

        private List<Collectible> stack = new List<Collectible>();
        
        // References
        private Collider[] hitColliders = new Collider[20]; // Buffer for overlap sphere

        private void Update()
        {
            // Vacuum Logic
            if (stack.Count < maxCapacity)
            {
                PerformVacuum();
            }
        }

        private void PerformVacuum()
        {
            int numColliders = Physics.OverlapSphereNonAlloc(transform.position, vacuumRadius, hitColliders, vacuumLayers);
            
            for (int i = 0; i < numColliders; i++)
            {
                var col = hitColliders[i];
                if (col == null) continue;

                Collectible collectible = col.GetComponent<Collectible>();
                if (collectible == null)
                {
                    // Check parent just in case collider is on a child
                    collectible = col.GetComponentInParent<Collectible>();
                }

                if (collectible != null && !collectible.isCollected)
                {
                    // Check distance to trunk
                    float distToTrunk = Vector3.Distance(trunkPoint.position, collectible.transform.position);

                    if (distToTrunk <= collectionDistance)
                    {
                        AddToStack(collectible);
                    }
                    else
                    {
                        // Attract towards trunk
                        collectible.AttractTo(trunkPoint.position, vacuumSpeed);
                    }
                }
            }
        }

        private void AddToStack(Collectible item)
        {
            if (stack.Count >= maxCapacity) return;

            item.OnCollected();
            stack.Add(item);

            // Parent to trunk
            item.transform.SetParent(trunkPoint);
            
            // Calculate position in stack
            Vector3 targetPos = CalculateStackPosition(stack.Count - 1);
            
            // Animate to position (simple snap for now, can be tweened)
            item.transform.localPosition = targetPos;
            item.transform.localRotation = Quaternion.identity;

            // Visual Juice: Squash and Stretch
            StartCoroutine(SquashAndStretch(item.transform));
        }

        private Vector3 CalculateStackPosition(int index)
        {
            // Grid math
            // Layer index = index / (rows * cols)
            // Position in layer = index % (rows * cols)
            
            int itemsPerLayer = gridDimensions.x * gridDimensions.y;
            int layer = index / itemsPerLayer;
            int posInLayer = index % itemsPerLayer;
            
            int x = posInLayer % gridDimensions.x;
            int z = posInLayer / gridDimensions.x; // Z is forward/back on the trunk usually
            
            // Center the stack maybe? 
            // For now, start from 0,0,0 and expand
            
            return new Vector3(
                x * itemSpacing.x - (gridDimensions.x * itemSpacing.x * 0.5f) + (itemSpacing.x * 0.5f), // Center X
                layer * itemSpacing.y,
                z * itemSpacing.z
            );
        }

        private System.Collections.IEnumerator SquashAndStretch(Transform target)
        {
            Vector3 originalScale = target.localScale;
            Vector3 squashScale = new Vector3(originalScale.x * 1.2f, originalScale.y * 0.8f, originalScale.z * 1.2f);
            Vector3 stretchScale = new Vector3(originalScale.x * 0.8f, originalScale.y * 1.2f, originalScale.z * 0.8f);

            float duration = 0.1f;
            float elapsed = 0f;

            // Squash
            while (elapsed < duration)
            {
                target.localScale = Vector3.Lerp(originalScale, squashScale, elapsed / duration);
                elapsed += Time.deltaTime;
                yield return null;
            }
            
            elapsed = 0f;
            // Stretch
            while (elapsed < duration)
            {
                target.localScale = Vector3.Lerp(squashScale, stretchScale, elapsed / duration);
                elapsed += Time.deltaTime;
                yield return null;
            }

            // Back to normal
            target.localScale = originalScale;
        }
        
        // Debug Gizmos for Vacuum Radius & Stack Grid
        private void OnDrawGizmosSelected()
        {
            Gizmos.color = Color.yellow;
            Gizmos.DrawWireSphere(transform.position, vacuumRadius);

            if (trunkPoint != null)
            {
                Gizmos.matrix = trunkPoint.localToWorldMatrix;
                Gizmos.color = new Color(0, 1, 0, 0.5f);
                
                for (int i = 0; i < maxCapacity; i++)
                {
                    Vector3 localPos = CalculateStackPosition(i);
                    Gizmos.DrawWireCube(localPos, itemSpacing * 0.9f);
                }
                
                // Reset matrix to avoid affecting other gizmos
                Gizmos.matrix = Matrix4x4.identity;
            }
        }
    }
}
