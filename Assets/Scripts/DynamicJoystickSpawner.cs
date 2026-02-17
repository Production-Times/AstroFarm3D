using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using System.Collections.Generic;

/// <summary>
/// Spawns joysticks at touch location and fades them out after idle timeout.
/// Attach to any GameObject. Assign the touch-detection Image in the Inspector.
/// Robust pooling system for reuse and performance.
/// </summary>
public class DynamicJoystickSpawner : MonoBehaviour, IPointerDownHandler, IPointerUpHandler, IDragHandler
{
    [Header("Joystick Prefab")]
    [Tooltip("Joystick prefab to instantiate at touch location. Should have DynamicJoystick component.")]
    public GameObject joystickPrefab;
    [Tooltip("Canvas to parent spawned joysticks. If null, uses parent canvas.")]
    public Canvas canvas;
    [Tooltip("Touch detection Image (the invisible full-screen Image). Drag & drop your Image here.")]
    public RectTransform touchDetectionImage;

    [Header("Fade & Timing")]
    [Tooltip("Seconds of non-interaction before fade starts.")]
    public float idleTimeBeforeFade = 2f;
    [Tooltip("Duration of fade-out animation (seconds).")]
    public float fadeDuration = 0.5f;
    [Tooltip("Initial alpha when joystick first appears.")]
    [Range(0, 1)] public float spawnAlpha = 1f;

    [Header("Positioning")]
    [Tooltip("Offset from touch position (pixels).")]
    public Vector2 spawnOffset = new Vector2(0, -80f);
    [Tooltip("Maximum distance joystick can move before respawning at new touch.")]
    public float respawnDistance = 300f;

    [Header("Pool Settings")]
    [Tooltip("Initial pool size.")]
    public int initialPoolSize = 2;

    // Active joystick instance
    DynamicJoystick activeJoystick;
    // Public accessor so other scripts (e.g. player) can read the runtime joystick instance
    public DynamicJoystick ActiveJoystick => activeJoystick;
    Vector2 lastTouchPos;
    CanvasGroup activeCanvasGroup;

    // Pool
    Queue<DynamicJoystick> joystickPool = new Queue<DynamicJoystick>();
    HashSet<DynamicJoystick> activeInstances = new HashSet<DynamicJoystick>();

    void Start()
    {
        if (canvas == null) canvas = GetComponentInParent<Canvas>();
        if (joystickPrefab == null) Debug.LogError("DynamicJoystickSpawner: joystickPrefab not assigned!", this);
        bool isPlaying = Application.isPlaying;
        if (touchDetectionImage == null)
        {
            // Attempt to auto-find a suitable full-screen Image under the canvas
            if (canvas != null)
            {
                var imgs = canvas.GetComponentsInChildren<Image>(true);
                foreach (var im in imgs)
                {
                    var rt = im.GetComponent<RectTransform>();
                    if (rt != null && rt.anchorMin == Vector2.zero && rt.anchorMax == Vector2.one)
                    {
                        touchDetectionImage = rt;
                        Debug.LogWarning($"DynamicJoystickSpawner: auto-assigned touchDetectionImage to '{im.name}' found under canvas.", this);
                        break;
                    }
                }
                if (touchDetectionImage == null && imgs.Length > 0)
                {
                    touchDetectionImage = imgs[0].GetComponent<RectTransform>();
                    Debug.LogWarning($"DynamicJoystickSpawner: fallback assigned touchDetectionImage to '{imgs[0].name}'.", this);
                }
            }

            if (touchDetectionImage == null)
            {
                if (canvas != null)
                {
                    if (isPlaying)
                    {
                        // Create a transparent full-screen Image under the canvas to receive pointer events
                        GameObject go = new GameObject("DynamicJoystickTouchImage", typeof(RectTransform), typeof(CanvasRenderer), typeof(UnityEngine.UI.Image));
                        go.transform.SetParent(canvas.transform, false);
                        var imgComp = go.GetComponent<UnityEngine.UI.Image>();
                        imgComp.color = new Color(0f, 0f, 0f, 0f);
                        imgComp.raycastTarget = true;
                        var rt = go.GetComponent<RectTransform>();
                        rt.anchorMin = Vector2.zero;
                        rt.anchorMax = Vector2.one;
                        rt.offsetMin = Vector2.zero;
                        rt.offsetMax = Vector2.zero;
                        touchDetectionImage = rt;
                        Debug.LogWarning($"DynamicJoystickSpawner: created transparent touch-detection Image '{go.name}' under canvas.", this);
                    }
                    else
                    {
                        Debug.LogError("DynamicJoystickSpawner: touchDetectionImage not assigned and auto-search failed. Assign a touch-detection Image in the Inspector before entering Play mode.", this);
                        return;
                    }
                }
                else
                {
                    Debug.LogError("DynamicJoystickSpawner: touchDetectionImage not assigned and auto-search failed. Drag your touch-detection Image into the Inspector.", this);
                    return;
                }
            }
        }

        // **Important**: The spawner script is expected to be ON the Image GameObject to receive pointer events.
        // If it's on a different GameObject, create a small runtime relay on the Image to forward pointer events
        // instead of moving/destroying this component (that breaks inspector references at runtime).
        if (touchDetectionImage.gameObject != gameObject)
        {
            if (isPlaying)
            {
                Debug.LogWarning("DynamicJoystickSpawner is not on the same GameObject as touchDetectionImage. Adding runtime event relay to the touch Image so inspector references remain valid.", this);

                // Add/assign a pointer-relay on the touch image so UI events reach this spawner instance
                var relay = touchDetectionImage.gameObject.GetComponent<DynamicJoystickSpawnerPointerRelay>();
                if (relay == null) relay = touchDetectionImage.gameObject.AddComponent<DynamicJoystickSpawnerPointerRelay>();
                relay.spawner = this;

                // Ensure the touch image can receive pointer events (Graphic + RaycastTarget)
                var img = touchDetectionImage.GetComponent<Graphic>();
                if (img == null)
                {
                    Debug.LogWarning("touchDetectionImage has no Graphic component. Add an Image with Raycast Target enabled so pointer events are detected.", touchDetectionImage);
                }
            }
            else
            {
                Debug.LogWarning("DynamicJoystickSpawner is not on the same GameObject as touchDetectionImage. In Edit mode the spawner will not attach a runtime relay—attach the spawner to the touch Image or assign touchDetectionImage in the Inspector.", this);
            }

            // Do NOT move/destroy this component - keep original instance so inspector references remain intact.
        }

        // Force-configure the touch detection Image's RectTransform to stretch full screen (only at runtime)
        if (isPlaying)
        {
            touchDetectionImage.anchorMin = Vector2.zero;
            touchDetectionImage.anchorMax = Vector2.one;
            touchDetectionImage.offsetMin = Vector2.zero;
            touchDetectionImage.offsetMax = Vector2.zero;
        }
        Debug.Log($"✓ Spawner ready! Touch Image: {touchDetectionImage.name}", touchDetectionImage);

        // Validate canvas setup - print diagnostics
        if (canvas != null)
        {
            RectTransform canvasRect = canvas.GetComponent<RectTransform>();
            Debug.Log($"\n=== SPAWNER SETUP ===\nCanvas: {canvas.name}\nRenderMode: {canvas.renderMode}\nCanvas Rect: {canvasRect.rect}\nCanvas Size: {canvasRect.sizeDelta}\nCanvas Anchors: {canvasRect.anchorMin} to {canvasRect.anchorMax}\nTouch Image: {touchDetectionImage.name}\nTouch Area Size: {touchDetectionImage.sizeDelta}\nTouch Area Anchors: {touchDetectionImage.anchorMin} to {touchDetectionImage.anchorMax}\nSettings: Idle={idleTimeBeforeFade}s, Fade={fadeDuration}s, Offset={spawnOffset}");

            // Warn if Canvas is not Screen Space - Overlay (preferred for dynamic UI)
            if (canvas.renderMode != RenderMode.ScreenSpaceOverlay)
            {
                Debug.LogWarning($"Canvas renderMode is {canvas.renderMode}. For best joystick positioning, use 'Screen Space - Overlay'.", canvas);
            }
        }

        // Pre-populate pool
        for (int i = 0; i < initialPoolSize; i++)
        {
            CreatePooledJoystick();
        }
    }

    public void OnPointerDown(PointerEventData eventData)
    {
        // Check distance from last spawn—if too far, spawn new joystick
        if (activeJoystick != null && Vector2.Distance(eventData.position, lastTouchPos) < respawnDistance)
        {
            // Reuse active joystick
            activeJoystick.ResetIdleTimer();
            return;
        }

        // Spawn new joystick at touch
        SpawnJoystickAtTouch(eventData.position);

        // Forward initial pointer down to the joystick so the knob is positioned
        if (activeJoystick != null)
        {
            Canvas canvasToUse = canvas != null ? canvas : GetComponentInParent<Canvas>();
            Camera uiCamera = (canvasToUse != null && canvasToUse.renderMode == RenderMode.ScreenSpaceOverlay) ? null : (canvasToUse != null ? (canvasToUse.worldCamera ?? Camera.main) : Camera.main);
            activeJoystick.ProcessPointerDown(eventData.position, uiCamera);
        }
    }

    public void OnDrag(PointerEventData eventData)
    {
        if (activeJoystick == null) return;

        Canvas canvasToUse = canvas != null ? canvas : GetComponentInParent<Canvas>();
        if (canvasToUse == null) return;

        Camera uiCamera = (canvasToUse.renderMode == RenderMode.ScreenSpaceOverlay) ? null : (canvasToUse.worldCamera ?? Camera.main);

        // Forward the drag position to the active joystick so only the knob (handle) moves
        activeJoystick.ProcessPointerDrag(eventData.position, uiCamera);
        activeJoystick.ResetIdleTimer();
    }

    public void OnPointerUp(PointerEventData eventData)
    {
        if (activeJoystick != null)
        {
            activeJoystick.ProcessPointerUp();
        }
    }

    void SpawnJoystickAtTouch(Vector2 touchPos)
    {
        // Reuse or create
        if (activeJoystick != null && !activeJoystick.IsFading)
        {
            activeJoystick.ResetIdleTimer();
            return;
        }

        activeJoystick = GetFromPool();
        if (activeJoystick == null)
        {
            activeJoystick = CreatePooledJoystick();
        }

        activeInstances.Add(activeJoystick);

        // Get canvas
        Canvas canvasToUse = canvas != null ? canvas : GetComponentInParent<Canvas>();
        if (canvasToUse == null)
        {
            Debug.LogError("DynamicJoystickSpawner: No canvas found!", this);
            return;
        }

        RectTransform joystickRect = activeJoystick.GetComponent<RectTransform>();
        RectTransform canvasRect = canvasToUse.GetComponent<RectTransform>();

        // Ensure joystick is a child of canvas (use transform to avoid RectTransform confusion)
        joystickRect.SetParent(canvasToUse.transform, false);

        // In Screen Space - Overlay, pass null camera to ScreenPointToLocalPointInRectangle.
        Camera uiCamera = (canvasToUse.renderMode == RenderMode.ScreenSpaceOverlay) ? null : (canvasToUse.worldCamera ?? Camera.main);

        if (RectTransformUtility.ScreenPointToLocalPointInRectangle(canvasRect, touchPos, uiCamera, out Vector2 localPoint))
        {
            joystickRect.anchoredPosition = localPoint + spawnOffset;
            lastTouchPos = touchPos;

            Debug.Log($"Touch screen: {touchPos} → Canvas local: {localPoint + spawnOffset}", joystickRect);
        }
        else
        {
            Debug.LogWarning($"Failed to convert screen point {touchPos} to canvas local (camera: {(uiCamera ? uiCamera.name : "null")})", this);
        }

        // Show and reset state
        activeCanvasGroup = activeJoystick.GetComponent<CanvasGroup>();
        if (activeCanvasGroup == null) activeCanvasGroup = activeJoystick.gameObject.AddComponent<CanvasGroup>();
        activeCanvasGroup.alpha = spawnAlpha;
        activeCanvasGroup.blocksRaycasts = true;

        activeJoystick.gameObject.SetActive(true);
        activeJoystick.ResetIdleTimer();

        Debug.Log($"Canvas rect: {canvasRect.rect}, sizeDelta: {canvasRect.sizeDelta}, localPos: {localPoint + spawnOffset}", activeJoystick);
    }

    DynamicJoystick CreatePooledJoystick()
    {
        GameObject instance = Instantiate(joystickPrefab, canvas != null ? canvas.transform : null);
        DynamicJoystick joystick = instance.GetComponent<DynamicJoystick>();

        if (joystick == null)
        {
            joystick = instance.AddComponent<DynamicJoystick>();
        }

        // Ensure RectTransform is set up correctly for positioning
        RectTransform rect = instance.GetComponent<RectTransform>();
        if (rect != null)
        {
            rect.anchorMin = new Vector2(0.5f, 0.5f); // Center anchor
            rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.pivot = new Vector2(0.5f, 0.5f);      // Center pivot
            rect.sizeDelta = new Vector2(120, 120);     // Default size
        }

        // Keep pooled instances inactive until spawned
        instance.SetActive(false);

        joystick.SetSpawner(this);
        joystickPool.Enqueue(joystick);

        return joystick;
    }

    DynamicJoystick GetFromPool()
    {
        if (joystickPool.Count > 0)
        {
            return joystickPool.Dequeue();
        }
        return null;
    }

    public void ReturnToPool(DynamicJoystick joystick)
    {
        activeInstances.Remove(joystick);
        joystick.gameObject.SetActive(false);
        joystickPool.Enqueue(joystick);

        if (activeJoystick == joystick)
        {
            activeJoystick = null;
            activeCanvasGroup = null;
        }
    }

    public float GetIdleTimeBeforeFade() => idleTimeBeforeFade;
    public float GetFadeDuration() => fadeDuration;
}

/// <summary>
/// Runtime helper placed on the touch-detection Image (when the spawner lives on a different GameObject).
/// Forwards pointer events from the UI Image to the original `DynamicJoystickSpawner` instance so
/// inspector references are not broken by component moves.
/// </summary>
public class DynamicJoystickSpawnerPointerRelay : MonoBehaviour, IPointerDownHandler, IDragHandler, IPointerUpHandler
{
    [HideInInspector] public DynamicJoystickSpawner spawner;

    public void OnPointerDown(PointerEventData eventData)
    {
        spawner?.OnPointerDown(eventData);
    }

    public void OnDrag(PointerEventData eventData)
    {
        spawner?.OnDrag(eventData);
    }

    public void OnPointerUp(PointerEventData eventData)
    {
        spawner?.OnPointerUp(eventData);
    }
}
