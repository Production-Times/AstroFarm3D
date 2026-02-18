using UnityEngine;

namespace Harvesting
{
    public class HarvesterTool : MonoBehaviour
    {
        [Header("Settings")]
        [Tooltip("The transform to rotate (the saw blade).")]
        public Transform sawTransform;
        public enum RotationAxis { X, Y, Z }
        [Tooltip("Axis to rotate around.")]
        public RotationAxis rotationAxis = RotationAxis.Y;
        
        [Tooltip("Rotation speed in degrees per second.")]
        public float rotationSpeed = 1080f;
        [Tooltip("Damage dealt per hit.")]
        public float damagePerHit = 25f;
        [Tooltip("Time interval between damage applications.")]
        public float damageInterval = 0.2f;

        private System.Collections.Generic.Dictionary<Collider, float> damageTimers = new System.Collections.Generic.Dictionary<Collider, float>();

        private void Update()
        {
            if (sawTransform != null)
            {
                Vector3 axis = Vector3.up;
                switch (rotationAxis)
                {
                    case RotationAxis.X: axis = Vector3.right; break;
                    case RotationAxis.Y: axis = Vector3.up; break;
                    case RotationAxis.Z: axis = Vector3.forward; break;
                }
                sawTransform.Rotate(axis * rotationSpeed * Time.deltaTime);
            }
            
            // Cleanup timers for objects no longer relevant (optional, or just clear on disable)
        }

        private void OnTriggerStay(Collider other)
        {
            if (other.CompareTag("Crop"))
            {
                float lastTime;
                if (!damageTimers.TryGetValue(other, out lastTime))
                {
                    lastTime = 0f;
                }

                if (Time.time >= lastTime + damageInterval)
                {
                    Crop crop = other.GetComponent<Crop>();
                    if (crop != null)
                    {
                        crop.TakeDamage(damagePerHit);
                        damageTimers[other] = Time.time;
                    }
                }
            }
        }
        
        private void OnTriggerExit(Collider other)
        {
            if (damageTimers.ContainsKey(other))
            {
                damageTimers.Remove(other);
            }
        }
    }
}
