using UnityEngine;

namespace Inventory
{
    [RequireComponent(typeof(Collider))]
    [RequireComponent(typeof(Rigidbody))]
    public class CashPickup : MonoBehaviour
    {
        [Tooltip("Optional VFX played on pickup")]
        public GameObject pickupEffect;

        [Tooltip("Tag expected on the collector (e.g. Player or Collector)")]
        public string collectorTag = "Player";

        private InventoryItem linkedItem;
        private int fallbackCashValue = 10;
        
        [Tooltip("Direct cash value for this prefab (set by DeliveryTruck when spawning)")]
        private int directCashValue = 10;

        [Tooltip("Particle system to play on pickup (optional)")]
        public ParticleSystem pickupParticles;

        [Tooltip("Scale multiplier for particle system on pickup")]
        [Range(0.1f, 3f)]
        public float particleScale = 1f;

        [Tooltip("Audio clip to play on pickup (ASMR stacking sound)")]
        public AudioClip pickupSound;

        [Header("Stacking Animation")]
        [Tooltip("Enable upward stacking animation on spawn")]
        public bool enableStackingAnimation = true;

        [Tooltip("Duration of stacking animation (seconds)")]
        public float stackingDuration = 0.3f;

        [Tooltip("Height to pop up before settling")]
        public float stackPopHeight = 0.2f;

        [Tooltip("Delay before cash animates falling down")]
        public float delayBeforeFalling = 1f;

        [Tooltip("Speed of falling animation (units per second)")]
        public float fallingSpeed = 2f;

        [Tooltip("LayerMask for ground/base detection")]
        public LayerMask groundLayer = 1; // Default layer 0

        [Tooltip("LayerMask for detecting other cash to stack on")]
        public LayerMask stackDetectionLayer;

        [Tooltip("LayerMask to EXCLUDE from detection (e.g. ground/base)")]
        public LayerMask excludeLayer;

        [Tooltip("Raycast distance for stack detection")]
        public float stackRayDistance = 100f;

        [Tooltip("Radius for fallback pickup detection")]
        public float pickupRadius = 1f;

        private Vector3 spawnPosition;
        private bool isCollected = false;
        private Rigidbody rb;
        public bool isFalling { get; private set; } = false;
        private Collider cachedCollider;

        private void Reset()
        {
            // Ensure primary collider is trigger for pickup detection
            Collider c = GetComponent<Collider>();
            if (c != null)
                c.isTrigger = true;

            // Add a second collider for raycasting (non-trigger) if not present
            Collider[] allColliders = GetComponents<Collider>();
            if (allColliders.Length < 2)
            {
                // Create a physics collider for raycasting
                if (c is BoxCollider box)
                {
                    BoxCollider physicsCollider = gameObject.AddComponent<BoxCollider>();
                    physicsCollider.size = box.size;
                    physicsCollider.center = box.center;
                    physicsCollider.isTrigger = false;
                    Debug.Log($"[CashPickup] Added second BoxCollider (non-trigger) for raycasting");
                }
                else if (c is SphereCollider sphere)
                {
                    SphereCollider physicsCollider = gameObject.AddComponent<SphereCollider>();
                    physicsCollider.radius = sphere.radius;
                    physicsCollider.center = sphere.center;
                    physicsCollider.isTrigger = false;
                    Debug.Log($"[CashPickup] Added second SphereCollider (non-trigger) for raycasting");
                }
            }

            Rigidbody rb = GetComponent<Rigidbody>();
            if (rb == null)
                rb = gameObject.AddComponent<Rigidbody>();

            rb.isKinematic = true;
            rb.constraints = RigidbodyConstraints.FreezeRotationX | RigidbodyConstraints.FreezeRotationZ;
        }

        private void Start()
        {
            rb = GetComponent<Rigidbody>();
            cachedCollider = GetComponent<Collider>();
            spawnPosition = transform.position;

            // Ensure all colliders are on the same layer as this gameObject
            int objectLayer = gameObject.layer;
            Collider[] colliders = GetComponents<Collider>();
            Debug.Log($"[CashPickup] Start: Object layer={LayerMask.LayerToName(objectLayer)}, Found {colliders.Length} colliders");
            
            foreach (Collider col in colliders)
            {
                if (col.gameObject.layer != objectLayer)
                {
                    col.gameObject.layer = objectLayer;
                    Debug.Log($"[CashPickup] Updated collider layer to {LayerMask.LayerToName(objectLayer)}");
                }
                Debug.Log($"  - {col.GetType().Name}: isTrigger={col.isTrigger}, layer={LayerMask.LayerToName(col.gameObject.layer)}");
            }
            
            // Auto-detect stack layer if not set
            if (stackDetectionLayer == 0)
            {
                stackDetectionLayer = (1 << objectLayer);
                Debug.Log($"[CashPickup] Auto-set stackDetectionLayer to {LayerMask.LayerToName(objectLayer)}");
            }
            
            // Raycast down to stack on top of existing cash
            StackOnPreviousCash();
            
            if (enableStackingAnimation)
            {
                StartCoroutine(StackingAnimationRoutine());
            }
            
            StartCoroutine(FallAfterDelayRoutine());
            StartCoroutine(FallbackPickupDetection());
        }

        private void StackOnPreviousCash()
        {
            Vector3 raycastStart = transform.position + Vector3.up * 100f;
            RaycastHit[] hits;
            
            // Raycast ONLY on stackDetectionLayer (Money), excluding everything else
            hits = Physics.RaycastAll(raycastStart, Vector3.down, stackRayDistance, stackDetectionLayer);
            
            // Filter out self and excluded layer
            RaycastHit? validHit = null;
            float closestDistance = float.MaxValue;
            
            foreach (RaycastHit hit in hits)
            {
                // Skip self
                if (hit.collider.gameObject == gameObject)
                    continue;
                
                // Skip excluded layers
                if ((excludeLayer.value & (1 << hit.collider.gameObject.layer)) != 0)
                    continue;
                
                // Find the closest valid hit
                if (hit.distance < closestDistance)
                {
                    closestDistance = hit.distance;
                    validHit = hit;
                }
            }
            
            if (validHit.HasValue)
            {
                RaycastHit hit = validHit.Value;
                
                // Stack flush on top to maintain grid
                float stackHeight = hit.collider.bounds.max.y;
                Vector3 newPos = transform.position;
                newPos.y = stackHeight;
                transform.position = newPos;
                spawnPosition = newPos;
                
                // Sync physics to make subsequent cash stacked in the same frame see this one
                Physics.SyncTransforms();
                
                Debug.Log($"[CashPickup] Stacked on {hit.collider.gameObject.name} (Money layer) at Y={stackHeight}");
            }
            else
            {
                Debug.Log($"[CashPickup] No cash on Money layer below, using spawnPosition Y={spawnPosition.y}");
            }
        }

        private System.Collections.IEnumerator StackingAnimationRoutine()
        {
            float elapsed = 0f;
            Vector3 targetPosition = spawnPosition + Vector3.up * stackPopHeight;

            // Pop up phase
            while (elapsed < stackingDuration)
            {
                elapsed += Time.deltaTime;
                float t = elapsed / stackingDuration;
                float easeT = Mathf.Sin(t * Mathf.PI * 0.5f); // Ease out
                transform.position = Vector3.Lerp(spawnPosition, targetPosition, easeT);
                yield return null;
            }

            // Settle back down
            elapsed = 0f;
            while (elapsed < stackingDuration * 0.5f)
            {
                elapsed += Time.deltaTime;
                float t = elapsed / (stackingDuration * 0.5f);
                float easeT = Mathf.Sin(t * Mathf.PI * 0.5f);
                transform.position = Vector3.Lerp(targetPosition, spawnPosition, easeT);
                yield return null;
            }

            transform.position = spawnPosition;
        }

        private System.Collections.IEnumerator FallAfterDelayRoutine()
        {
            yield return new WaitForSeconds(delayBeforeFalling);
            
            if (isCollected)
                yield break;

            isFalling = true;

            LayerMask combinedLayer = groundLayer | stackDetectionLayer;

            // Animate falling downward and detect ground or other cash
            while (isFalling && !isCollected)
            {
                Vector3 rayOrigin = transform.position + Vector3.up * 0.1f;
                float rayDistance = (fallingSpeed * Time.deltaTime) + 0.15f;
                
                RaycastHit[] hits = Physics.RaycastAll(rayOrigin, Vector3.down, rayDistance, combinedLayer);
                bool hitSurface = false;

                foreach (RaycastHit h in hits)
                {
                    if (h.collider.gameObject == gameObject) continue;
                    if ((excludeLayer.value & (1 << h.collider.gameObject.layer)) != 0) continue;
                    
                    if (((1 << h.collider.gameObject.layer) & stackDetectionLayer.value) != 0)
                    {
                        CashPickup hitCash = h.collider.GetComponent<CashPickup>();
                        // Ignore overlaps with other currently falling cash
                        if (hitCash != null && hitCash.isFalling) continue;
                        
                        hitSurface = true;
                        Vector3 pos = transform.position;
                        pos.y = h.collider.bounds.max.y;
                        transform.position = pos;
                        break;
                    }
                    else
                    {
                        hitSurface = true;
                        Vector3 pos = transform.position;
                        pos.y = h.point.y;
                        transform.position = pos;
                        break;
                    }
                }

                if (hitSurface)
                {
                    isFalling = false;
                    Debug.Log("[CashPickup] Hit surface, stopped falling");
                }
                else
                {
                    // Continue falling
                    transform.position -= Vector3.up * fallingSpeed * Time.deltaTime;
                }

                yield return null;
            }
        }

        private System.Collections.IEnumerator FallbackPickupDetection()
        {
            // Expand radius slightly for a satisfying magnet pull
            float vacuumDetectRadius = Mathf.Max(pickupRadius, 3.5f);
            
            // Check distance-based vacuum
            while (!isCollected)
            {
                Collider[] nearbyColliders = Physics.OverlapSphere(transform.position, vacuumDetectRadius);
                
                foreach (Collider col in nearbyColliders)
                {
                    if (col.CompareTag(collectorTag))
                    {
                        if (!isCollected)
                        {
                            StartCoroutine(VacuumCollectRoutine(col.transform));
                        }
                        break;
                    }
                }
                
                yield return new WaitForSeconds(0.1f);
            }
        }

        public void SetLinkedItem(InventoryItem item)
        {
            linkedItem = item;
        }

        public void SetDirectCashValue(int value)
        {
            directCashValue = value;
        }

        private void OnTriggerEnter(Collider other)
        {
            if (isCollected)
                return;

            if (!other.CompareTag(collectorTag))
                return;

            // Start Vacuum collection coroutine
            StartCoroutine(VacuumCollectRoutine(other.transform));
        }

        private System.Collections.IEnumerator VacuumCollectRoutine(Transform collector)
        {
            isCollected = true;
            
            // Disable physics completely so kinematic pushes or gravity don't fight the movement
            if (rb != null)
            {
                rb.isKinematic = true;
                rb.useGravity = false;
            }

            // Disable all our colliders so it doesn't bump the player mid-flight or support top cash
            foreach (Collider c in GetComponents<Collider>())
            {
                c.enabled = false;
            }

            // Wake up cash resting directly above us so it falls
            RaycastHit[] aboveHits = Physics.RaycastAll(transform.position, Vector3.up, 2f, stackDetectionLayer);
            foreach (var ah in aboveHits)
            {
                if (ah.collider.gameObject != gameObject)
                {
                    CashPickup upperCash = ah.collider.GetComponent<CashPickup>();
                    if (upperCash != null && !upperCash.isFalling)
                    {
                        upperCash.StartCoroutine(upperCash.ForceFallRoutine());
                    }
                }
            }

            // Vacuum pull effect
            float vacuumSpeed = 0f; // Starts slow and speeds up
            float collectDistance = 0.5f;
            Vector3 startScale = transform.localScale;

            while (collector != null)
            {
                float distance = Vector3.Distance(transform.position, collector.position + Vector3.up);
                
                if (distance <= collectDistance)
                {
                    break;
                }

                // Accelerate vacuum speed for a satisfying pull
                vacuumSpeed += 25f * Time.deltaTime; 

                transform.position = Vector3.MoveTowards(transform.position, collector.position + Vector3.up, vacuumSpeed * Time.deltaTime);
                
                // Scale down slightly to look like it's being sucked in
                float scalePcnt = Mathf.Clamp01(distance / 3.5f);
                transform.localScale = Vector3.Lerp(Vector3.zero, startScale, scalePcnt);

                yield return null;
            }
            
            CollectCash();
        }



        private void CollectCash()
        {
            // Use direct cash value (set by DeliveryTruck when spawning)
            int cashToAward = directCashValue;

            if (CashManager.Instance != null)
            {
                CashManager.Instance.AddCash(cashToAward);
            }

            // Play ASMR pickup sound
            if (pickupSound != null)
            {
                AudioSource.PlayClipAtPoint(pickupSound, transform.position);
            }

            // Play particles with scale
            if (pickupParticles != null)
            {
                pickupParticles.transform.localScale = Vector3.one * particleScale;
                pickupParticles.Play();
            }

            // Spawn pickup effect
            if (pickupEffect != null)
            {
                Instantiate(pickupEffect, transform.position, Quaternion.identity);
            }

            Debug.Log($"[CashPickup] Collected ${cashToAward}");
            Destroy(gameObject);
        }

        public System.Collections.IEnumerator ForceFallRoutine()
        {
            // Forces checking of ground/stacks underneath again in case supports were removed
            yield return new WaitForSeconds(0.05f);
            if (!isCollected)
            {
                isFalling = true;
                StartCoroutine(FallAfterDelayRoutine());
            }
        }
    }
}
