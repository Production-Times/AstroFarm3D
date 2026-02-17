using UnityEngine;
using System.Collections.Generic;
#if UNITY_EDITOR
using UnityEditor;
#endif

[RequireComponent(typeof(Rigidbody))]
[AddComponentMenu("Gameplay/Vehicle Controller")]
public class VehicleController : MonoBehaviour
{
    [System.Serializable]
    public class Tyre
    {
        [Tooltip("Transform of the tyre object (visual). Its local X axis is used for rotation by default.")]
        public Transform tyreTransform;
        [Tooltip("Alternative: assign a Tyre GameObject (the script will use its transform). Useful when assigning GameObjects in the inspector).")]
        public GameObject tyreGameObject;
        [Tooltip("Is this tyre turned when steering?")]
        public bool isSteering = false;
        [Tooltip("Does this tyre receive drive (visual rotates regardless, driven flag is optional).")]
        public bool isDriven = true;
        [Tooltip("Tyre radius in world units (meters). Used to convert linear vehicle speed -> wheel rotation.")]
        public float radius = 0.35f;
        [Tooltip("Invert rotation direction for this tyre (useful if tyre model is mirrored).")]
        public bool invertRotation = false;

        public enum RotationAxisChoice { LocalX = 0, LocalY = 1, LocalZ = 2, Custom = 3 }
        [Tooltip("Axis used for tyre rotation. Choose LocalX/Y/Z or Custom and set CustomAxis.")]
        public RotationAxisChoice rotationAxisChoice = RotationAxisChoice.LocalX;
        [Tooltip("Custom rotation axis in local tyre space (used when RotationAxisChoice == Custom)")]
        public Vector3 customRotationAxis = Vector3.right;

        // runtime
        [HideInInspector] public Quaternion initialLocalRotation;
        [HideInInspector] public float spinAngle;
        [HideInInspector] public float currentSteerAngle;
    }

    [Header("Tyres (up to 6)")]
    [Tooltip("Assign up to 6 tyre Transforms. Each tyre supports individual radius/steer/drive flags.")]
    public List<Tyre> tyres = new List<Tyre>(4);

    [Header("Drive / Steering")]
    public float maxMotorForce = 1500f;     // force applied for throttle
    public float maxSteerAngle = 30f;       // degrees
    public float maxSpeed = 30f;            // m/s clamp
    public float brakeStrength = 8f;        // simple brake multiplier
    public float steerResponsiveness = 4f;  // lerp speed for visual tyre steer

    [Header("Input")]
    public string throttleAxis = "Vertical"; // forward/back
    public string steerAxis = "Horizontal";  // left/right
    public string brakeButton = "Fire1";     // optional brake button
    public bool useRigidbodyPhysics = true;    // toggle physics (Rigidbody) vs transform kinematic
    [Tooltip("Optional: dynamic joystick spawner for input (uses its ActiveJoystick). If assigned, joystick input overrides axes.")]
    public DynamicJoystickSpawner moveJoystickSpawner;
    [Tooltip("Optional: drag-and-drop a DynamicJoystick directly here.")]
    public DynamicJoystick dynamicJoystick;
    [Tooltip("Optional: static on-screen joystick fallback.")]
    public MobileJoystick moveStaticJoystick;
    [Tooltip("If true and using a joystick, provide a small forward throttle when the stick is moved only sideways (helps turning in place feel like driving).")]
    public bool enableSteerFallbackForward = true;
    [Tooltip("Fraction of joystick magnitude to apply as forward throttle when using steer-only fallback.")]
    [Range(0f, 2f)] public float steerFallbackForwardAmount = 1.0f;
    [Tooltip("Invert forward/backward control.")]
    public bool invertJoystickVertical = false;
    [Tooltip("Minimum joystick magnitude to consider fallback (avoid reacting to tiny touches).")]
    public float steerFallbackMinMagnitude = 0.2f;
    [Tooltip("When true, log joystick inputs each FixedUpdate for debugging.")]
    public bool debugLogJoystickInput = false;

    [Header("Debug")]
    [Tooltip("Show vehicle forward direction gizmo in Scene view.")]
    public bool debugShowForward = true;
    [Tooltip("Show tyre rotation gizmos in Scene view.")]
    public bool debugShowTyreGizmos = true;
    [Tooltip("In Edit mode, play a rotate preview for tyres when enabled.")]
    public bool previewRotate = false;
    [Tooltip("Degrees per second to rotate tyres in preview mode.")]
    public float previewRotateSpeed = 90f;
    [Tooltip("Flip the forward gizmo direction (useful if your vehicle model faces negative Z).")]
    public bool debugFlipForward = false;

    Rigidbody rb;

    // internal
    float steerLerp = 0f;
    float currentDirectionState = 1f; // 1 = Forward, -1 = Reverse

    const int kMaxTyres = 6;

    void Reset()
    {
        // sensible default tyre placeholders (empty) so inspector shows size
        tyres = new List<Tyre>();
    }

    void Awake()
    {
        rb = GetComponent<Rigidbody>();
        if (rb == null) rb = gameObject.AddComponent<Rigidbody>();

        // cache initial local rotations for steering offsets
        foreach (var t in tyres)
        {
            if (t.tyreTransform == null && t.tyreGameObject != null) t.tyreTransform = t.tyreGameObject.transform;
            if (t.tyreTransform != null) t.initialLocalRotation = t.tyreTransform.localRotation;
        }
    }

    void OnValidate()
    {
        if (tyres == null) return;
        if (tyres.Count > kMaxTyres)
        {
            Debug.LogWarning($"VehicleController: max {kMaxTyres} tyres supported — trimming list.");
            tyres.RemoveRange(kMaxTyres, tyres.Count - kMaxTyres);
        }
        for (int i = 0; i < tyres.Count; i++)
        {
            if (tyres[i].radius <= 0.01f) tyres[i].radius = 0.35f;
            if (tyres[i].rotationAxisChoice == Tyre.RotationAxisChoice.Custom && tyres[i].customRotationAxis == Vector3.zero) tyres[i].customRotationAxis = Vector3.right;
            // support assigning a GameObject instead of a Transform in the inspector
            if (tyres[i].tyreTransform == null && tyres[i].tyreGameObject != null)
            {
                tyres[i].tyreTransform = tyres[i].tyreGameObject.transform;
            }
        }
    }

    void FixedUpdate()
    {
        // read input from joystick(s) if assigned, otherwise fall back to axes
        float throttle = 0f;
        float steer = 0f;

        if (moveJoystickSpawner != null && moveJoystickSpawner.ActiveJoystick != null)
        {
            var j = moveJoystickSpawner.ActiveJoystick;
            throttle = j.Vertical;
            steer = j.Horizontal;

            if (debugLogJoystickInput)
            {
                Debug.Log($"Vehicle joystick input (Spawner) — H:{steer:F2} V:{throttle:F2} Mag:{j.Magnitude:F2}");
            }
        }
        else if (dynamicJoystick != null)
        {
            throttle = dynamicJoystick.Vertical;
            steer = dynamicJoystick.Horizontal;
            
            
            if (debugLogJoystickInput)
            {
                Debug.Log($"Vehicle joystick input (Direct) — H:{steer:F2} V:{throttle:F2} Mag:{dynamicJoystick.Magnitude:F2}");
            }
        }
        else if (moveStaticJoystick != null)
        {
            throttle = moveStaticJoystick.Vertical;
            steer = moveStaticJoystick.Horizontal;
        }
        else
        {
            throttle = Input.GetAxis(throttleAxis);
            steer = Input.GetAxis(steerAxis);
        }

        // Global Input Adjustments
        // 1. Invert Vertical if requested (only affects raw stick input)
        if (invertJoystickVertical && Mathf.Abs(throttle) > 0.01f)
        {
            throttle = -throttle;
        }

        // 2. Generalize "Steer Fallback Forward" logic for ALL input methods
        // If user steers left/right significantly but provides near-zero throttle, add FULL forward power.
        // User wants "turning to have same amount of forward force as normal forward".
        // So we use inputMag directly (1.0 ratio).
        float inputMag = new Vector2(steer, throttle).magnitude;
        if (enableSteerFallbackForward && inputMag >= steerFallbackMinMagnitude && Mathf.Abs(throttle) < 0.01f && Mathf.Abs(steer) > 0.01f)
        {
            // Force 1.0 multiplier effectively to match "normal forward" speed.
            throttle = inputMag; 
        }

        bool braking = Input.GetButton(brakeButton) || (throttle < -0.1f && !Mathf.Approximately(throttle, 0f));

        if (useRigidbodyPhysics)
        {
            // HYPERCASUAL MOVEMENT
            // 1. Rotation: Direct turn based on Input.Horizontal
            float turnInput = steer;
            if (Mathf.Abs(throttle) < 0.01f && Mathf.Abs(steer) > 0.01f)
            {
                // Pivot turn: Allow turning in place
                turnInput = steer;
            }
            
            // Rotate the Rigidbody directly (instant response)
            // INCREASED TURN SPEED: Multiplier changed from 2f to 4f for snappier turns
            float turnAmount = turnInput * maxSteerAngle * 4f * Time.fixedDeltaTime; 
            if (!Mathf.Approximately(turnAmount, 0f))
            {
                Quaternion delta = Quaternion.Euler(0f, turnAmount, 0f);
                rb.MoveRotation(rb.rotation * delta);
            }

            // 2. Movement: Direct velocity based on Directional Magnitude
            // Logic:
            // - Latching Reverse: Only switch to Reverse if throttle < -0.5.
            // - Only switch back to Forward if throttle > -0.1.
            // This prevents glitchy flipping when steering sideways with slightly downward stick.
            
            float moveMag = new Vector2(throttle, steer).magnitude;
            float targetSpeed = moveMag * maxSpeed;

            // Override Logic:
            // "If turning don't switch to reverse until I release the joystick or take it fully down"
            // Start Reverse (Enter -1 state) ONLY if stick is FULLY pulled back (> 90%).
            // Otherwise, stay in current state (Forward).
            // Exit Reverse ONLY if stick returns to near-neutral (> -0.1).
            
            if (throttle < -0.9f) 
            {
                currentDirectionState = -1f; // Reverse (Hard engage)
            }
            else if (throttle > -0.1f)
            {
                currentDirectionState = 1f; // Forward (Default)
            }
            // Else: Keep current state (Hysteresis zone -0.9 to -0.1)
            // e.g. Turning with Stick at (-0.7, 0.7) will stay Forward.

            // Apply direction
            if (currentDirectionState < 0f)
            {
                targetSpeed *= -1f;
            }
            
            // Preserve vertical velocity (gravity)
            Vector3 currentVelocity = rb.linearVelocity;
            Vector3 newVelocity = transform.forward * targetSpeed;
            newVelocity.y = currentVelocity.y;

            // Instant acceleration/deceleration
            rb.linearVelocity = newVelocity;

            // Update tyre visuals using the target speed (fake it to match input)
            UpdateTyresVisual(targetSpeed);
        }
        else
        {
            // Simple transform movement (fallback if Rigidbody is disabled)
            float moveDist = throttle * maxSpeed * Time.deltaTime;
            transform.Translate(Vector3.forward * moveDist);

            float turnDist = steer * maxSteerAngle * 2f * Time.deltaTime;
            transform.Rotate(0f, turnDist, 0f);

            UpdateTyresVisual(throttle * maxSpeed);
        }

        // smooth tyre steering visuals
        steerLerp = Mathf.Lerp(steerLerp, steer, Time.fixedDeltaTime * steerResponsiveness * 2f); // faster visual lerp
        UpdateSteeringVisuals(steerLerp);
    }

    void UpdateTyresVisual(float signedSpeed)
    {
        // signedSpeed is in units/second; wheel rotation (degrees per frame) = (signedSpeed / circumference) * 360 * deltaTime
        foreach (var t in tyres)
        {
            if (t.tyreTransform == null) continue;

            float r = Mathf.Max(0.001f, t.radius);
            float circumference = 2f * Mathf.PI * r;
            float degreesPerSecond = (signedSpeed / circumference) * 360f;
            float degrees = degreesPerSecond * Time.fixedDeltaTime;
            if (t.invertRotation) degrees = -degrees;
            // accumulate spin angle (degrees)
            t.spinAngle += degrees;

            // apply combined transform (steer + spin)
            ApplyTyreTransform(t);
        }
    }

    void UpdateSteeringVisuals(float steerValue)
    {
        float steerAngle = steerValue * maxSteerAngle;
        foreach (var t in tyres)
        {
            if (t.tyreTransform == null) continue;
            // set current steer angle per-tyre (only for steering tyres)
            t.currentSteerAngle = t.isSteering ? steerAngle : 0f;
            // apply combined transform (steer + spin)
            ApplyTyreTransform(t);
        }
    }

    void ApplyTyreTransform(Tyre t)
    {
        if (t.tyreTransform == null) return;

        // determine local rotation axis
        Vector3 localAxis = Vector3.right;
        switch (t.rotationAxisChoice)
        {
            case Tyre.RotationAxisChoice.LocalX: localAxis = Vector3.right; break;
            case Tyre.RotationAxisChoice.LocalY: localAxis = Vector3.up; break;
            case Tyre.RotationAxisChoice.LocalZ: localAxis = Vector3.forward; break;
            case Tyre.RotationAxisChoice.Custom: localAxis = t.customRotationAxis.normalized; break;
        }

        // steer offset (yaw) applied on top of initial local rotation
        Quaternion steerOffset = Quaternion.Euler(0f, t.currentSteerAngle, 0f);

        // spin rotation around localAxis
        float spinDeg = t.spinAngle * (t.invertRotation ? -1f : 1f);
        Quaternion spinRot = Quaternion.AngleAxis(spinDeg, localAxis.normalized);

        t.tyreTransform.localRotation = t.initialLocalRotation * steerOffset * spinRot;
    }

    // Optional API: Set speed limit (useful when switching control contexts)
    public void SetMaxSpeed(float newMax) { maxSpeed = newMax; }

    // Optional API: Get current forward speed
    public float GetForwardSpeed() { return Vector3.Dot(rb.linearVelocity, transform.forward); }

#if UNITY_EDITOR
    static double s_lastEditorTime = 0.0;

    void Update()
    {
        // Editor preview rotation for tyres
        if (!Application.isPlaying && previewRotate)
        {
            double now = EditorApplication.timeSinceStartup;
            double dt = s_lastEditorTime > 0.0 ? (now - s_lastEditorTime) : (1.0 / 60.0);
            s_lastEditorTime = now;
            float deg = previewRotateSpeed * (float)dt;
            foreach (var t in tyres)
            {
                if (t.tyreTransform == null) continue;
                // compute local axis like runtime
                Vector3 localAxis = Vector3.right;
                switch (t.rotationAxisChoice)
                {
                    case Tyre.RotationAxisChoice.LocalX: localAxis = Vector3.right; break;
                    case Tyre.RotationAxisChoice.LocalY: localAxis = Vector3.up; break;
                    case Tyre.RotationAxisChoice.LocalZ: localAxis = Vector3.forward; break;
                    case Tyre.RotationAxisChoice.Custom: localAxis = t.customRotationAxis.normalized; break;
                }
                float d = deg * (t.invertRotation ? -1f : 1f);
                t.tyreTransform.Rotate(localAxis, d, Space.Self);
            }
            // Ensure scene view updates
            SceneView.RepaintAll();
        }
        else if (!previewRotate)
        {
            s_lastEditorTime = 0.0;
        }
    }

    void OnDrawGizmos()
    {
        if (debugShowForward)
        {
            Gizmos.color = Color.green;
            Vector3 dir = transform.forward * (debugFlipForward ? -1f : 1f);
            Gizmos.DrawLine(transform.position, transform.position + dir * 2f);
            Gizmos.DrawIcon(transform.position + dir * 2f, "sv_label_0", true);
        }

        if (debugShowTyreGizmos && tyres != null)
        {
            foreach (var t in tyres)
            {
                if (t.tyreTransform == null) continue;
                Vector3 pos = t.tyreTransform.position;
                // Draw disc perpendicular to rotationAxis
                #if UNITY_EDITOR
                Handles.color = Color.yellow;
                // compute local axis and convert to world
                Vector3 localAxis = Vector3.right;
                switch (t.rotationAxisChoice)
                {
                    case Tyre.RotationAxisChoice.LocalX: localAxis = Vector3.right; break;
                    case Tyre.RotationAxisChoice.LocalY: localAxis = Vector3.up; break;
                    case Tyre.RotationAxisChoice.LocalZ: localAxis = Vector3.forward; break;
                    case Tyre.RotationAxisChoice.Custom: localAxis = t.customRotationAxis.normalized; break;
                }
                Vector3 worldAxis = t.tyreTransform.TransformDirection(localAxis).normalized;
                Handles.DrawWireDisc(pos, worldAxis, Mathf.Max(0.05f, t.radius));
                #else
                Gizmos.color = Color.yellow;
                Gizmos.DrawWireSphere(pos, Mathf.Max(0.05f, t.radius));
                #endif

                // draw forward marker of tyre rotation (local forward)
                Vector3 forwardDir = t.tyreTransform.TransformDirection(localAxis).normalized;
                Gizmos.color = Color.cyan;
                Gizmos.DrawLine(pos, pos + forwardDir * (t.radius + 0.1f));
            }
        }
    }
#endif
}
