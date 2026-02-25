using UnityEngine;

[RequireComponent(typeof(CharacterController))]
public class SmoothPlayerController : MonoBehaviour
{
    [Header("Movement")]
    [Tooltip("Base walk speed (m/s)")]
    public float baseMoveSpeed = 5f;
    [HideInInspector]
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

    [Header("Animation (optional)")]
    [Tooltip("Animator component that drives player animations (optional).")]
    public Animator animator;
    [Tooltip("Animator trigger name for the Walk/Run animation.")]
    public string walkTrigger = "Walk";
    [Tooltip("Optional trigger fired when the player stops walking (useful to return to Idle). Leave empty to do nothing.")]
    public string stopWalkTrigger = "";

    [Tooltip("Movement speed (units/sec) that should produce Animator playback = 1.0. Example: set to 2 if your walk animation matches moveSpeed=2.")]
    public float walkAnimReferenceSpeed = 2f;
    [Tooltip("Optional: Animator float parameter name to receive computed playback multiplier. If empty, the script will set Animator.speed directly while walking.")]
    public string walkAnimSpeedParameter = "";

    // Mapping modes to avoid unrealistic/frenetic playback at high movement speeds
    public enum WalkAnimMapping { Linear = 0, Exponential = 1, Curve = 2 }
    [Tooltip("How movement speed maps to animation playback speed. Use 'Exponential' (default) to compress high speeds.")]
    public WalkAnimMapping walkAnimMapping = WalkAnimMapping.Exponential;
    [Tooltip("Exponent used when mapping=Exponential. Use values < 1 to reduce animation speed growth at high player speeds (0.6 is a good default).")]
    [Range(0.25f, 1f)] public float walkAnimExponent = 0.6f;
    [Tooltip("If mapping==Curve, sample this curve: x = speedRatio (currentSpeed / walkAnimReferenceSpeed), y = playbackMultiplier.")]
    public AnimationCurve walkAnimSpeedCurve = AnimationCurve.Linear(0f, 0f, 2f, 2f);

    [Tooltip("Minimum allowed playback multiplier when syncing animation speed.")]
    public float walkAnimSpeedMin = 0.5f;
    [Tooltip("Maximum allowed playback multiplier when syncing animation speed.")]
    public float walkAnimSpeedMax = 2.5f;
    [Tooltip("Smoothing time (seconds) for animation speed changes.")]
    public float walkAnimSpeedSmoothTime = 0.08f;

    // internal smoothing for animator speed
    float currentAnimSpeed = 1f;
    float animSpeedSmoothVelocity = 0f;

    // internal animation state (prevents re-firing triggers every frame)
    enum AnimState { Unknown, Idle, Walking }
    AnimState currentAnimState = AnimState.Unknown;

    [Header("Mobile Input (optional)")]
    [Tooltip("Dynamic joystick spawner. If assigned, it overrides Horizontal/Vertical axes with dynamic left stick.")]
    public DynamicJoystickSpawner moveJoystickSpawner;
    [Tooltip("Static joystick for movement (legacy, use spawner if possible). If assigned, it overrides Horizontal/Vertical axes.")]
    public MobileJoystick moveJoystick;
    [Tooltip("Optional on-screen button for Jump. If assigned, it overrides the Jump input.")]
    public MobileButton jumpButton;
    [Tooltip("Optional on-screen button for Sprint (hold).")]
    public MobileButton sprintButton;

    CharacterController controller;
    float speedSmoothVelocity;
    float currentSpeed;
    float rotationVelocity;
    Vector3 verticalVelocity;

    void Awake()
    {
        controller = GetComponent<CharacterController>();
        if (cam == null && Camera.main != null) cam = Camera.main.transform;
        
        ApplyUpgrade();
    }
    
    public void ApplyUpgrade()
    {
        if (Inventory.UpgradeManager.Instance != null && Inventory.UpgradeManager.Instance.upgradeDatabase != null)
        {
            float upgradedSpeed = Inventory.UpgradeManager.Instance.GetUpgradeValue(Inventory.UpgradeType.PlayerMoveSpeed);
            if (upgradedSpeed > 0)
            {
                moveSpeed = upgradedSpeed;
            }
            else
            {
                moveSpeed = baseMoveSpeed;
            }
        }
        else
        {
            moveSpeed = baseMoveSpeed;
        }
    }

    void Update()
    {
        Vector2 input;
        if (moveJoystickSpawner != null)
        {
            // Get input from active dynamic joystick (if any)
            DynamicJoystick activeJoystick = GetActiveDynamicJoystick();
            if (activeJoystick != null)
            {
                input = new Vector2(activeJoystick.Horizontal, activeJoystick.Vertical);
            }
            else
            {
                input = Vector2.zero;
            }
        }
        else if (moveJoystick != null)
        {
            input = new Vector2(moveJoystick.Horizontal, moveJoystick.Vertical);
        }
        else
        {
            input = new Vector2(Input.GetAxisRaw("Horizontal"), Input.GetAxisRaw("Vertical"));
        }
        Vector3 inputDir = input.normalized;

        bool sprint = Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift);
        if (sprintButton != null) sprint = sprint || sprintButton.IsPressed;
        float targetSpeed = moveSpeed * (sprint ? sprintMultiplier : 1f) * inputDir.magnitude;

        currentSpeed = Mathf.SmoothDamp(currentSpeed, targetSpeed, ref speedSmoothVelocity, accelerationTime);

        // Sync animator playback speed with movement speed
        if (animator != null)
        {
            bool walking = currentSpeed > 0.1f && inputDir.sqrMagnitude > 0.001f;

            float desiredAnimSpeed = 1f;
            if (walking && walkAnimReferenceSpeed > 0f)
            {
                float ratio = currentSpeed / walkAnimReferenceSpeed; // 1.0 means "perfect match"
                float rawMultiplier = 1f;

                switch (walkAnimMapping)
                {
                    case WalkAnimMapping.Linear:
                        rawMultiplier = ratio;
                        break;
                    case WalkAnimMapping.Exponential:
                        // compress growth so very large movement speeds don't produce unrealistic step cadence
                        rawMultiplier = Mathf.Pow(Mathf.Max(ratio, 0f), walkAnimExponent);
                        break;
                    case WalkAnimMapping.Curve:
                        rawMultiplier = walkAnimSpeedCurve.Evaluate(ratio);
                        break;
                }

                desiredAnimSpeed = Mathf.Clamp(rawMultiplier, walkAnimSpeedMin, walkAnimSpeedMax);
            }

            currentAnimSpeed = Mathf.SmoothDamp(currentAnimSpeed, desiredAnimSpeed, ref animSpeedSmoothVelocity, walkAnimSpeedSmoothTime);

            if (!string.IsNullOrEmpty(walkAnimSpeedParameter))
            {
                animator.SetFloat(walkAnimSpeedParameter, currentAnimSpeed);
            }
            else
            {
                // Apply globally while walking; restore to 1 when idle
                animator.speed = currentAnimSpeed;
            }

            // Animation state -> trigger only when state changes (only fire Walk trigger; Idle is default)
            if (walking && currentAnimState != AnimState.Walking)
            {
                if (!string.IsNullOrEmpty(walkTrigger)) animator.SetTrigger(walkTrigger);
                currentAnimState = AnimState.Walking;
            }
            else if (!walking && currentAnimState == AnimState.Walking)
            {
                // Fire optional stop trigger so Animator can transition back to Idle
                if (!string.IsNullOrEmpty(stopWalkTrigger)) animator.SetTrigger(stopWalkTrigger);
                currentAnimState = AnimState.Idle;
            }
        }

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
            if ((jumpButton != null && jumpButton.GetButtonDown()) || Input.GetButtonDown("Jump"))
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

    // Helper: get the active joystick instance from spawner
    DynamicJoystick GetActiveDynamicJoystick()
    {
        if (moveJoystickSpawner == null) return null;

        // Prefer the spawner's exposed runtime instance if available
        var active = moveJoystickSpawner.ActiveJoystick;
        if (active != null && active.gameObject.activeSelf && !active.IsFading) return active;

        // Fallback: search the assigned canvas for an active joystick
        if (moveJoystickSpawner.canvas == null) return null;
        foreach (DynamicJoystick js in moveJoystickSpawner.canvas.GetComponentsInChildren<DynamicJoystick>())
        {
            if (js.gameObject.activeSelf && !js.IsFading)
            {
                return js;
            }
        }
        return null;
    }
}
