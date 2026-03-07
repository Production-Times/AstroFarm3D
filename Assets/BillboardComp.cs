using UnityEngine;

/// <summary>
/// Billboard component that makes any UI element rotate to face the camera.
/// Simply attach this script to your UI button or 3D object.
/// </summary>
public class BillboardComp : MonoBehaviour
{
    [Header("Billboard Settings")]
    [SerializeField] private Camera targetCamera;
    
    [Tooltip("Keep the object upright (Y-axis remains vertical)")]
    [SerializeField] private bool keepUpright = true;

    [Header("Lock Axes")]
    [SerializeField] private bool lockX = false;
    [SerializeField] private bool lockY = false;
    [SerializeField] private bool lockZ = false;

    void Start()
    {
        // Auto-find main camera if not assigned
        if (targetCamera == null)
        {
            targetCamera = Camera.main;
        }
    }

    void Update()
    {
        if (targetCamera == null) return;

        // Store original rotation for locked axes
        Vector3 originalEuler = transform.eulerAngles;

        if (keepUpright)
        {
            // Face camera while keeping object upright
            Vector3 directionToCamera = targetCamera.transform.position - transform.position;
            transform.LookAt(transform.position + directionToCamera, Vector3.up);
        }
        else
        {
            // Face camera completely (full rotation)
            transform.LookAt(targetCamera.transform);
        }

        // Apply axis locks
        Vector3 newEuler = transform.eulerAngles;
        if (lockX) newEuler.x = originalEuler.x;
        if (lockY) newEuler.y = originalEuler.y;
        if (lockZ) newEuler.z = originalEuler.z;
        transform.eulerAngles = newEuler;
    }
}
