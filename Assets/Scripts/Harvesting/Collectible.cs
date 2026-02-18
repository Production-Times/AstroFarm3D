using UnityEngine;

namespace Harvesting
{
    public class Collectible : MonoBehaviour
    {
        [HideInInspector]
        public bool isCollected = false;

        private Rigidbody rb;
        private Collider col;

        private void Awake()
        {
            rb = GetComponent<Rigidbody>();
            col = GetComponent<Collider>();
        }

        /// <summary>
        /// Moves the collectible towards a target position with a given speed.
        /// </summary>
        /// <param name="targetPosition">The position to move towards.</param>
        /// <param name="speed">The speed of movement.</param>
        /// <param name="stoppingDistance">Distance at which to stop interacting with physics (if getting sucked in).</param>
        public void AttractTo(Vector3 targetPosition, float speed)
        {
            if (isCollected) return;

            // Calculate direction
            Vector3 direction = (targetPosition - transform.position).normalized;
            
            // S-Curve or smooth lerp simulation (simplified to move towards for now with physics)
            // Ideally, we might want to disable gravity if it's being sucked up powerfully
            // For now, we'll just add force or modify velocity directly for responsiveness
            
            if (rb != null)
            {
                // Disable gravity and use kinematic to ensure smooth movement without physics interference
                rb.useGravity = false;
                rb.isKinematic = true;
                
                // Move towards target using Lerp for smooth attraction
                float t = speed * Time.deltaTime;
                transform.position = Vector3.Lerp(transform.position, targetPosition, t);
            }
            else
            {
                transform.position = Vector3.Lerp(transform.position, targetPosition, speed * Time.deltaTime);
            }
        }

        public void OnCollected()
        {
            isCollected = true;
            if (rb != null)
            {
                rb.isKinematic = true;
                rb.useGravity = false;
            }
            if (col != null)
            {
                col.enabled = false;
            }
        }

        public void EnablePhysics()
        {
            isCollected = false;
            if (rb != null)
            {
                rb.isKinematic = false;
                rb.useGravity = true;
            }
            if (col != null)
            {
                col.enabled = true;
            }
        }
    }
}
