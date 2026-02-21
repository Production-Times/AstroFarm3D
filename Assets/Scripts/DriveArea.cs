using UnityEngine;

[RequireComponent(typeof(BoxCollider))]
[AddComponentMenu("Gameplay/Drive Area")]
public class DriveArea : MonoBehaviour
{
    [Header("Vehicle / Particle - Entry")]
    [Tooltip("Prefab of the vehicle to spawn/switch to when player enters the drive area.")]
    public GameObject vehiclePrefab;
    [Tooltip("Optional particle effect prefab to play at the mount position when player enters.")]
    public GameObject enterParticlePrefab;
    [Tooltip("Scale factor for the enter particle effect.")]
    public Vector3 enterParticleScale = Vector3.one;

    [Header("Exit / Respawn")]
    [Tooltip("Optional particle effect prefab to play when player respawns on vehicle exit.")]
    public GameObject exitParticlePrefab;
    [Tooltip("Scale factor for the exit particle effect.")]
    public Vector3 exitParticleScale = Vector3.one;

    [Header("Spawn / Mount")]
    [Tooltip("Optional Transform to use as the vehicle spawn point inside the drive area. If null, uses the DriveArea's world position.")]
    public Transform vehicleSpawnPoint;
    [Tooltip("Offset (X, Y, Z) from the player's position to spawn the vehicle.")]
    public Vector3 spawnOffset = new Vector3(0f, 0f, 3f);
    [Tooltip("Prefab of the player to re-instantiate when vehicle exits. If not set, the player will be re-enabled instead of re-created.")]
    public GameObject playerPrefab;
    [Tooltip("Optional Transform to use as the player respawn point on exit. If null, player will be spawned at vehicle exit position.")]
    public Transform playerRespawnPoint;
    [Tooltip("If true and `playerPrefab` is not assigned, the player's `SmoothPlayerController` (or whole player GameObject) will be disabled instead of destroyed on mount.")]
    public bool disablePlayerOnMount = true;
    [Tooltip("Tag used to identify the player object that can mount the vehicle.")]
    public string playerTag = "Player";

    [Header("Camera")]
    [Tooltip("Optional SmoothCameraFollow component to retarget when player mounts vehicle.")]
    public SmoothCameraFollow cameraFollow;

    [Header("Options")]
    [Tooltip("If true the drive area BoxCollider will be treated as a square in the Scene view (useful for visualizing).")]
    public bool drawSquareGizmo = true;
    [Tooltip("If true, spawn the vehicle rotated 180 degrees on the Y axis (useful if vehicle model faces backward).")]
    public bool invertSpawnRotation = false;
    [Tooltip("If true, always spawn vehicle facing forward (ignoring vehicleSpawnPoint and player rotation). If false, uses vehicleSpawnPoint rotation or player rotation.")]
    public bool useDefaultForwardRotation = false;
    
    [Header("Inventory Drop")]
    [Tooltip("Optional VehicleDropPoint where the vehicle should drop its items when exiting the drive area. If not set, will auto-detect in the scene.")]
    public Transform vehicleDropPoint;
    
    private Inventory.VehicleDropPoint cachedDropPoint;

    // runtime
    GameObject spawnedVehicle;
    GameObject mountedPlayer;
    // runtime: remember which joystick spawner was used so we can assign it to vehicle/player on mount/dismount
    DynamicJoystickSpawner assignedJoystickSpawner;

    void Reset()
    {
        var bc = GetComponent<BoxCollider>();
        bc.isTrigger = true;
    }

    void Awake()
    {
        var bc = GetComponent<BoxCollider>();
        bc.isTrigger = true;
    }

    void OnTriggerEnter(Collider other)
    {
        if (!other.CompareTag(playerTag)) return;
        if (vehiclePrefab == null)
        {
            Debug.LogWarning("DriveArea: vehiclePrefab is not assigned.");
            return;
        }

        // spawn particle at the drive-area spawn point (or drive area center)
        Vector3 offsetVector = (other.transform.right * spawnOffset.x) + (other.transform.up * spawnOffset.y) + (other.transform.forward * spawnOffset.z);
        Vector3 spawnPos = (vehicleSpawnPoint != null) ? vehicleSpawnPoint.position : other.transform.position + offsetVector;
        Quaternion spawnRot;
        
        if (useDefaultForwardRotation)
        {
            // Always face forward (Quaternion.identity = world forward)
            spawnRot = Quaternion.identity;
        }
        else if (vehicleSpawnPoint != null)
        {
            // Use the explicitly set spawn point rotation
            spawnRot = vehicleSpawnPoint.rotation;
        }
        else
        {
            // Fallback to player's rotation
            spawnRot = other.transform.rotation;
        }
        
        // Apply inversion if needed (flip 180 degrees on Y axis)
        if (invertSpawnRotation)
        {
            spawnRot = spawnRot * Quaternion.Euler(0f, 180f, 0f);
        }
        
        Debug.Log($"DriveArea: spawning vehicle at {spawnPos} with rotation {spawnRot.eulerAngles}", this);

        if (enterParticlePrefab != null)
        {
            var p = Instantiate(enterParticlePrefab, spawnPos, Quaternion.identity);
            p.transform.localScale = enterParticleScale;
            var ps = p.GetComponent<ParticleSystem>();
            if (ps != null) ps.Play();
        }

        // instantiate vehicle at the drive-area's spawn location
        spawnedVehicle = Instantiate(vehiclePrefab, spawnPos, spawnRot);

        // Reset Rigidbody velocity and position to ensure vehicle stays at spawn point
        var vehicleRB = spawnedVehicle.GetComponent<Rigidbody>();
        if (vehicleRB != null)
        {
            vehicleRB.linearVelocity = Vector3.zero;
            vehicleRB.angularVelocity = Vector3.zero;
            vehicleRB.MovePosition(spawnPos);
            vehicleRB.MoveRotation(spawnRot);
        }
        else
        {
            // Ensure transform is set correctly if no rigidbody
            spawnedVehicle.transform.position = spawnPos;
            spawnedVehicle.transform.rotation = spawnRot;
        }
        Debug.Log($"DriveArea: vehicle spawned at {spawnedVehicle.transform.position}", spawnedVehicle);

        // Determine the runtime DynamicJoystickSpawner to wire into the spawned vehicle.
        DynamicJoystickSpawner runtimeSpawner = null;
        var spc = other.GetComponent<SmoothPlayerController>();
        if (spc != null && spc.moveJoystickSpawner != null)
        {
            runtimeSpawner = spc.moveJoystickSpawner;
        }
        else
        {
            // fallback: find any active spawner in the scene
            runtimeSpawner = FindObjectOfType<DynamicJoystickSpawner>();
        }
        assignedJoystickSpawner = runtimeSpawner;

        // Remove or disable the player depending on whether a playerPrefab is provided
        mountedPlayer = other.gameObject;
        if (playerPrefab != null)
        {
            // destroy the player's runtime GameObject (we will re-instantiate on exit)
            Destroy(mountedPlayer);
            mountedPlayer = null;
        }
        else
        {
            // fallback to previous behaviour (disable the player controller or GameObject)
            var mountedSpc = mountedPlayer.GetComponent<SmoothPlayerController>();
            if (mountedSpc != null) mountedSpc.enabled = false;
            else mountedPlayer.SetActive(false);
        }

        // retarget camera if provided
        if (cameraFollow != null && spawnedVehicle != null)
        {
            cameraFollow.SetTarget(spawnedVehicle.transform);
        }

        // enable vehicle controller if present and assign joystick spawner so vehicle responds to on-screen input
        var vc = spawnedVehicle.GetComponent<VehicleController>();
        if (vc == null)
        {
            Debug.LogWarning("DriveArea: Spawned vehicle has no VehicleController component.");
        }
        else
        {
            if (assignedJoystickSpawner != null)
            {
                vc.moveJoystickSpawner = assignedJoystickSpawner;
                Debug.Log($"DriveArea: wired DynamicJoystickSpawner '{assignedJoystickSpawner.name}' into spawned vehicle.");
            }
        }
        
        // Setup vehicle inventory drop location
        var vehicleInventory = spawnedVehicle.GetComponentInChildren<Harvesting.VehicleInventory>();
        if (vehicleInventory != null)
        {
            Transform dropTarget = GetVehicleDropLocation();
            if (dropTarget != null)
            {
                vehicleInventory.dropLocation = dropTarget;
                Debug.Log($"DriveArea: assigned vehicle drop location to '{dropTarget.name}'.");
            }
            else
            {
                Debug.LogWarning("DriveArea: No VehicleDropPoint found in scene. Items will drop at vehicle position.");
            }
        }
    }

    void OnDrawGizmos()
    {
        if (!drawSquareGizmo) return;
        var bc = GetComponent<BoxCollider>();
        if (bc == null) return;
        Gizmos.color = new Color(0.1f, 0.6f, 0.9f, 0.25f);
        Matrix4x4 old = Gizmos.matrix;
        Gizmos.matrix = transform.localToWorldMatrix;
        Gizmos.DrawCube(bc.center, bc.size);
        Gizmos.color = new Color(0.1f, 0.6f, 0.9f, 1f);
        Gizmos.DrawWireCube(bc.center, bc.size);
        Gizmos.matrix = old;
    }

    void OnTriggerExit(Collider other)
    {
        // When the spawned vehicle exits the drive area, destroy it and restore/respawn the player
        bool isSpawnedVehicle = false;
        if (spawnedVehicle != null)
        {
            if (other.gameObject == spawnedVehicle) isSpawnedVehicle = true;
            else if (other.transform != null && other.transform.root != null && other.transform.root.gameObject == spawnedVehicle) isSpawnedVehicle = true;
            else if (other.attachedRigidbody != null && other.attachedRigidbody.gameObject == spawnedVehicle) isSpawnedVehicle = true;
        }

        if (isSpawnedVehicle)
        {
            Debug.Log("DriveArea: spawned vehicle exited area, dismounting.", this);
            
            // Drop vehicle inventory items before destroying the vehicle
            var vehicleInventory = spawnedVehicle.GetComponentInChildren<Harvesting.VehicleInventory>();
            if (vehicleInventory != null)
            {
                vehicleInventory.DropAllItems();
            }
            
            // Optionally respawn the playerPrefab at the configured respawn point or at the vehicle exit position
            if (playerPrefab != null)
            {
                Vector3 spawnPos = (playerRespawnPoint != null) ? playerRespawnPoint.position : other.transform.position;
                Quaternion spawnRot = (playerRespawnPoint != null) ? playerRespawnPoint.rotation : Quaternion.identity;

                // Spawn exit particle if configured
                if (exitParticlePrefab != null)
                {
                    var p = Instantiate(exitParticlePrefab, spawnPos, Quaternion.identity);
                    p.transform.localScale = exitParticleScale;
                    var ps = p.GetComponent<ParticleSystem>();
                    if (ps != null) ps.Play();
                }

                var newPlayer = Instantiate(playerPrefab, spawnPos, spawnRot);

                // Restore joystick spawner on the newly created player if it has a SmoothPlayerController
                var newSpc = newPlayer.GetComponent<SmoothPlayerController>();
                if (newSpc != null && assignedJoystickSpawner != null)
                {
                    newSpc.moveJoystickSpawner = assignedJoystickSpawner;
                    Debug.Log($"DriveArea: assigned DynamicJoystickSpawner '{assignedJoystickSpawner.name}' to respawned player.");
                }

                if (cameraFollow != null)
                {
                    cameraFollow.SetTarget(newPlayer.transform);
                }
            }
            else if (mountedPlayer != null)
            {
                // If we only disabled the player, re-enable it
                var spc2 = mountedPlayer.GetComponent<SmoothPlayerController>();
                if (spc2 != null)
                {
                    spc2.enabled = true;
                    if (assignedJoystickSpawner != null) spc2.moveJoystickSpawner = assignedJoystickSpawner;
                }
                else mountedPlayer.SetActive(true);

                if (cameraFollow != null)
                {
                    cameraFollow.SetTarget(mountedPlayer.transform);
                }
            }
            Destroy(spawnedVehicle);
            spawnedVehicle = null;
            mountedPlayer = null;
            assignedJoystickSpawner = null;
        }
    }

    private Transform GetVehicleDropLocation()
    {
        // Use manually assigned drop point if available
        if (vehicleDropPoint != null)
        {
            return vehicleDropPoint;
        }
        
        // Auto-detect VehicleDropPoint in the scene
        if (cachedDropPoint == null)
        {
            cachedDropPoint = FindAnyObjectByType<Inventory.VehicleDropPoint>();
        }
        
        if (cachedDropPoint != null)
        {
            // Use the unloadPosition if set, otherwise use the dropPoint transform itself
            return cachedDropPoint.unloadPosition != null 
                ? cachedDropPoint.unloadPosition 
                : cachedDropPoint.transform;
        }
        
        return null;
    }
    
    // Optional API to forcibly dismount and restore player
    public void DismountPlayer()
    {
        if (spawnedVehicle != null)
        {
            Destroy(spawnedVehicle);
            spawnedVehicle = null;
        }

        if (playerPrefab != null)
        {
            // respawn playerPrefab at the drive area center or specified respawn point
            Vector3 spawnPos = (playerRespawnPoint != null) ? playerRespawnPoint.position : transform.position;
            Quaternion spawnRot = (playerRespawnPoint != null) ? playerRespawnPoint.rotation : Quaternion.identity;
            var newPlayer = Instantiate(playerPrefab, spawnPos, spawnRot);
            if (cameraFollow != null) cameraFollow.SetTarget(newPlayer.transform);
        }
        else if (mountedPlayer != null)
        {
            var spc = mountedPlayer.GetComponent<SmoothPlayerController>();
            if (spc != null) spc.enabled = true;
            else mountedPlayer.SetActive(true);

            if (cameraFollow != null)
            {
                cameraFollow.SetTarget(mountedPlayer.transform);
            }

            mountedPlayer = null;
        }
    }
}
