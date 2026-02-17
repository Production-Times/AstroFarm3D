using UnityEngine;
using System.Collections;

public class WindSimulation : MonoBehaviour
{
    [Header("Wind Settings")]
    [Tooltip("Strength of the idle wind swaying.")]
    public float windStrength = 5f;
    [Tooltip("Speed of the idle wind swaying.")]
    public float windSpeed = 2f;
    [Tooltip("Random offset to make multiple plants not sway in perfect sync.")]
    public float timeOffset = 0f;

    [Header("Interaction Settings")]
    [Tooltip("How much the plant bends when touched by player/vehicle.")]
    public float interactionBending = 15f;
    [Tooltip("Speed at which the plant bends away from the object.")]
    public float bendSpeed = 5f;
    [Tooltip("Speed at which the plant recovers to its idle state.")]
    public float recoverySpeed = 3f;

    [Header("Setup")]
    [Tooltip("The Transform to rotate. If null, uses this object's Transform.")]
    public Transform pivot;
    
    // Internal state
    private Quaternion initialRotation;
    private Quaternion targetInteractionRotation;
    private float currentBendWeight = 0f; // 0 = idle, 1 = fully bent
    private Vector3 bendDirection;
    private bool isInteracting = false;
    private Transform interactor;

    void Start()
    {
        if (pivot == null) pivot = transform;
        initialRotation = pivot.localRotation;
        
        // Randomize start time if not set manually to avoid unison swaying
        if (timeOffset == 0f) timeOffset = Random.Range(0f, 10f);
    }

    void Update()
    {
        // 1. Calculate Idle Wind Rotation
        // Simple sine wave on X/Z for swaying
        float time = Time.time * windSpeed + timeOffset;
        float swayX = Mathf.Sin(time) * windStrength;
        float swayZ = Mathf.Cos(time * 0.7f) * windStrength; // Different frequency for variety

        Quaternion windRotation = Quaternion.Euler(swayX, 0f, swayZ);

        // 2. Handle Interaction Logic
        if (isInteracting && interactor != null)
        {
            // Calculate direction away from interactor
            // We want to bend away from the interactor
            Vector3 directionToPlant = (pivot.position - interactor.position).normalized;
            
            // Project onto XZ plane for simple bending
            directionToPlant.y = 0; 
            if (directionToPlant == Vector3.zero) directionToPlant = pivot.forward;

            // Calculate bend axis (perpendicular to direction)
            Vector3 bendAxis = Vector3.Cross(Vector3.up, directionToPlant);

            // Create target rotation for bending
            // We rotate around the bendAxis by interactionBending amount
            targetInteractionRotation = Quaternion.AngleAxis(interactionBending, bendAxis);

            // Smoothly increase bend weight
            currentBendWeight = Mathf.MoveTowards(currentBendWeight, 1f, Time.deltaTime * bendSpeed);
        }
        else
        {
            // Smoothly return to 0 bend
            currentBendWeight = Mathf.MoveTowards(currentBendWeight, 0f, Time.deltaTime * recoverySpeed);
            targetInteractionRotation = Quaternion.identity; // Reset target
        }

        // 3. Combine Rotations
        // Base is Initial * Wind
        Quaternion baseRot = initialRotation * windRotation;

        // Apply Interaction on top
        if (currentBendWeight > 0.001f)
        {
             // Spherical interpolation between Wind State and Bent State
             // Note: Adding the interaction rotation to the base
             Quaternion bentState = baseRot * targetInteractionRotation;
             pivot.localRotation = Quaternion.Slerp(baseRot, bentState, currentBendWeight);
        }
        else
        {
            pivot.localRotation = baseRot;
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        if (IsValidInteractor(other))
        {
            isInteracting = true;
            interactor = other.transform;
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (other.transform == interactor)
        {
            isInteracting = false;
            interactor = null;
        }
    }

    private bool IsValidInteractor(Collider other)
    {
        // Simple check for Player or Vehicle
        // You can add tags or specific component checks here
        return other.CompareTag("Player") || other.GetComponent<VehicleController>() != null || other.GetComponent<CharacterController>() != null || other.attachedRigidbody != null;
    }
}
