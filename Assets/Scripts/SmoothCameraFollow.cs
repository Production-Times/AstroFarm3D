using UnityEngine;

[AddComponentMenu("Camera/Smooth Camera Follow")]
public class SmoothCameraFollow : MonoBehaviour
{
    [Tooltip("Transform the camera follows (usually the player)")]
    public Transform target;
    [Tooltip("Local offset from the target")] 
    public Vector3 offset = new Vector3(0f, 1.8f, -3.5f);
    [Tooltip("Time for the camera to catch up (smaller = snappier)")]
    public float followSmoothTime = 0.12f;
    [Tooltip("Rotation smoothing time")]
    public float rotationSmoothTime = 0.12f;
    [Tooltip("Additional offset applied to the camera's look-at point (world units). Use to bias what the camera focuses on.")]
    public Vector3 lookAtOffset = Vector3.zero;

    [Header("Orbit (optional)")]
    public bool enableMouseOrbit = true;
    public float mouseSensitivity = 150f;
    public float minPitch = -35f;
    public float maxPitch = 60f;
    public bool lockCursor = false;

    Vector3 currentVelocity;
    float yaw;
    float pitch;

    void Start()
    {
        Vector3 angles = transform.eulerAngles;
        yaw = angles.y;
        pitch = angles.x;
        if (lockCursor) Cursor.lockState = CursorLockMode.Locked;
    }

    void LateUpdate()
    {
        if (target == null) return;

        if (enableMouseOrbit)
        {
            yaw += Input.GetAxis("Mouse X") * mouseSensitivity * Time.deltaTime;
            pitch -= Input.GetAxis("Mouse Y") * mouseSensitivity * Time.deltaTime;
            pitch = Mathf.Clamp(pitch, minPitch, maxPitch);
        }

        Quaternion lookRotation = Quaternion.Euler(pitch, yaw, 0f);
        Vector3 desiredPosition = target.position + lookRotation * offset;
        transform.position = Vector3.SmoothDamp(transform.position, desiredPosition, ref currentVelocity, followSmoothTime);

        // Smoothly look at the player (slight vertical offset so camera looks at chest/head)
        Vector3 lookAtTarget = target.position + Vector3.up * 1.5f + lookAtOffset;
        Quaternion desiredLook = Quaternion.LookRotation(lookAtTarget - transform.position);
        transform.rotation = Quaternion.Slerp(transform.rotation, desiredLook, 1f - Mathf.Exp(-rotationSmoothTime * Time.deltaTime));
    }

    public void SetTarget(Transform newTarget)
    {
        target = newTarget;
    }
}
