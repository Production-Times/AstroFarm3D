using UnityEngine;

namespace Harvesting
{
    public class Crop : MonoBehaviour
    {
        [Header("Settings")]
        [Tooltip("Maximum health of the crop.")]
        public float maxHP = 100f;
        [Tooltip("Prefab to spawn when the crop is destroyed.")]
        public GameObject collectiblePrefab;
        [Tooltip("Force applied to the collectible upon spawning.")]
        public float spawnForce = 5f;
        [Tooltip("Visual mesh to disable upon death.")]
        public GameObject growthMesh;

        [Header("Effects")]
        [Tooltip("Particle effect to play when taking damage.")]
        public ParticleSystem hitParticles;
        [Tooltip("Shake intensity when taking damage.")]
        public float shakeIntensity = 0.2f;
        [Tooltip("Shake duration when taking damage.")]
        public float shakeDuration = 0.1f;

        [Tooltip("Scale multiplier for the hit particle effect.")]
        public float hitParticleSystemScale = 1.0f;

        private float currentHP;
        private Vector3 originalPosition;
        private float shakeTimer;

        private void Start()
        {
            currentHP = maxHP;
            originalPosition = transform.localPosition;
            if (growthMesh == null)
            {
                // Try to find a mesh renderer if not assigned (optional fallback)
                var meshRenderer = GetComponentInChildren<MeshRenderer>();
                if (meshRenderer != null) growthMesh = meshRenderer.gameObject;
            }

            if (hitParticles != null)
            {
                hitParticles.transform.localScale = Vector3.one * hitParticleSystemScale;
            }
        }

        private void Update()
        {
            if (shakeTimer > 0)
            {
                transform.localPosition = originalPosition + Random.insideUnitSphere * shakeIntensity;
                shakeTimer -= Time.deltaTime;
            }
            else
            {
                transform.localPosition = originalPosition;
            }
        }

        public void TakeDamage(float amount)
        {
            if (currentHP <= 0) return;

            currentHP -= amount;
            
            // Visual feedback
            if (hitParticles != null) hitParticles.Play();
            shakeTimer = shakeDuration;

            if (currentHP <= 0)
            {
                Die();
            }
        }

        private void Die()
        {
            // Hide the crop mesh
            if (growthMesh != null) growthMesh.SetActive(false);
            
            // Disable collider to prevent further interaction
            var collider = GetComponent<Collider>();
            if (collider != null) collider.enabled = false;

            // Spawn collectible
            if (collectiblePrefab != null)
            {
                GameObject collectible = Instantiate(collectiblePrefab, transform.position + Vector3.up * 0.5f, Quaternion.identity);
                Rigidbody rb = collectible.GetComponent<Rigidbody>();
                if (rb != null)
                {
                    rb.AddForce(Vector3.up * spawnForce, ForceMode.Impulse);
                    // Add random torque for juice
                    rb.AddTorque(Random.insideUnitSphere * 10f, ForceMode.Impulse);
                }
            }
            
            // Destroy the crop object after a delay or immediately if we just want to pool it
            Destroy(gameObject, 2f); // Give time for particles to finish or just cleanup
        }
    }
}
