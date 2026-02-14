using UnityEngine;

[RequireComponent(typeof(CharacterController))]
public class SmoothPlayerController : MonoBehaviour
{
    [Header("Movement")]
    [Tooltip("Base walk speed (m/s)")]
    public float moveSpeed = 5f;
    [Tooltip("Multiplier when sprinting")]
    public float sprintMultiplier = 1.8f;
    [Tooltip("Time to reach target speed (smaller = snappier)")]
    public float accelerationTime = 0.08f;
    [Tooltip("Time to smooth rotation")]
    public float rotationSmoothTime = 0.12f;
    [Tooltip("Degrees to add to the target facing rotation (use negative values to rotate left). Useful for strafing/animation sync.")]
    public float rotationOffset = 0f;
    [Tooltip("Gravity value (negative)")]
    public float gravity = -30f;
    [Tooltip("Jump height in meters")]
    public float jumpHeight = 1.6f;

    [Header("References")]
    [Tooltip("Camera used to orient movement. If null, Camera.main is used.")]
    public Transform cam;
    [Tooltip("Should the player face the movement direction relative to camera?")]
    public bool orientToCamera = true;

    CharacterController controller;
    float speedSmoothVelocity;
    float currentSpeed;
    float rotationVelocity;
    Vector3 verticalVelocity;

    void Awake()
    {
        controller = GetComponent<CharacterController>();
        if (cam == null && Camera.main != null) cam = Camera.main.transform;
    }

    void Update()
    {
        Vector2 input = new Vector2(Input.GetAxisRaw("Horizontal"), Input.GetAxisRaw("Vertical"));
        Vector3 inputDir = input.normalized;

        bool sprint = Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift);
        float targetSpeed = moveSpeed * (sprint ? sprintMultiplier : 1f) * inputDir.magnitude;

        currentSpeed = Mathf.SmoothDamp(currentSpeed, targetSpeed, ref speedSmoothVelocity, accelerationTime);

        if (inputDir.sqrMagnitude > 0.001f)
        {
            float targetRotation = Mathf.Atan2(inputDir.x, inputDir.y) * Mathf.Rad2Deg;
            if (orientToCamera && cam != null) targetRotation += cam.eulerAngles.y;
            targetRotation += rotationOffset;
            float smoothed = Mathf.SmoothDampAngle(transform.eulerAngles.y, targetRotation, ref rotationVelocity, rotationSmoothTime);
            transform.rotation = Quaternion.Euler(0f, smoothed, 0f);
        }

        Vector3 moveDir = Vector3.zero;
        if (orientToCamera && cam != null)
        {
            Vector3 forward = cam.forward;
            forward.y = 0;
            forward.Normalize();
            Vector3 right = cam.right;
            right.y = 0;
            right.Normalize();
            moveDir = forward * inputDir.y + right * inputDir.x;
        }
        else
        {
            moveDir = new Vector3(inputDir.x, 0f, inputDir.y);
            if (moveDir.sqrMagnitude > 1f) moveDir.Normalize();
        }

        Vector3 horizontalVelocity = moveDir * currentSpeed;

        if (controller.isGrounded)
        {
            if (verticalVelocity.y < 0f) verticalVelocity.y = -2f; // small downward force to keep grounded
            if (Input.GetButtonDown("Jump"))
            {
                verticalVelocity.y = Mathf.Sqrt(jumpHeight * -2f * gravity);
            }
        }

        verticalVelocity.y += gravity * Time.deltaTime;

        controller.Move((horizontalVelocity + verticalVelocity) * Time.deltaTime);
    }

    // Useful runtime info
    public Vector3 Velocity => controller.velocity;
    public bool IsGrounded => controller.isGrounded;
}
