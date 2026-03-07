using UnityEngine;
using UnityEngine.EventSystems;
using System.Collections;

/// <summary>
/// Dynamic joystick that fades out after idle timeout.
/// Works with DynamicJoystickSpawner for robust pooled spawning.
/// Immune to UI button touches via UIButtonTouchManager.
/// </summary>
public class DynamicJoystick : MonoBehaviour, IPointerDownHandler, IPointerUpHandler, IDragHandler
{
    [Tooltip("Background RectTransform (the joystick base)")]
    public RectTransform background;
    [Tooltip("Handle RectTransform (the moving knob)")]
    public RectTransform handle;
    [Tooltip("Maximum distance (pixels) the handle can move from center")]
    public float handleRange = 60f;
    [Tooltip("Small dead zone to ignore tiny touches")]
    public float deadZone = 0.1f;

    [Header("UI Button Safety")]
    [SerializeField] private UIButtonTouchManager buttonTouchManager;

    // Input state
    Vector2 input = Vector2.zero;
    bool isPressed = false;

    // Fade & idle
    DynamicJoystickSpawner spawner;
    Coroutine fadeCoroutine;
    float idleTimer;
    bool isFading = false;
    CanvasGroup canvasGroup;

    public bool IsFading => isFading;

    void OnEnable()
    {
        ResetIdleTimer();
        isFading = false;
    }

    public void SetSpawner(DynamicJoystickSpawner sp)
    {
        spawner = sp;
    }

    void Start()
    {
        if (background == null) background = GetComponent<RectTransform>();
        if (handle == null && background.childCount > 0) handle = background.GetChild(0) as RectTransform;
        canvasGroup = GetComponent<CanvasGroup>();
        if (canvasGroup == null) canvasGroup = gameObject.AddComponent<CanvasGroup>();

        if (buttonTouchManager == null)
            buttonTouchManager = FindObjectOfType<UIButtonTouchManager>();
    }

    void Update()
    {
        if (isPressed) return; // Reset when touched

        idleTimer += Time.deltaTime;
        if (idleTimer >= spawner.GetIdleTimeBeforeFade() && !isFading && spawner != null)
        {
            StartFadeOut();
        }
    }

    public void OnPointerDown(PointerEventData eventData)
    {
        ProcessPointerDown(eventData.position, eventData.pressEventCamera);
    }

    public void OnDrag(PointerEventData eventData)
    {
        ProcessPointerDrag(eventData.position, eventData.pressEventCamera);
    }

    public void OnPointerUp(PointerEventData eventData)
    {
        ProcessPointerUp();
    }

    // Public API so external spawner can forward pointer events (screen space)
    public void ProcessPointerDown(Vector2 screenPosition, Camera uiCamera)
    {
        // Ignore if pointer is over a UI button
        if (buttonTouchManager != null && buttonTouchManager.ShouldIgnoreJoystickInput())
            return;

        isPressed = true;
        ResetIdleTimer();
        // fallthrough to drag logic to position the handle
        ProcessPointerDrag(screenPosition, uiCamera);
    }

    public void ProcessPointerDrag(Vector2 screenPosition, Camera uiCamera)
    {
        // Ignore if pointer is over a UI button
        if (buttonTouchManager != null && buttonTouchManager.ShouldIgnoreJoystickInput())
            return;

        if (background == null) return;

        Vector2 localPoint;
        RectTransformUtility.ScreenPointToLocalPointInRectangle(background, screenPosition, uiCamera, out localPoint);

        float radius = (handleRange > 0f) ? handleRange : (background.sizeDelta.x * 0.5f);
        input = localPoint / radius;

        if (input.magnitude > 1f) input = input.normalized;
        if (input.magnitude < deadZone) input = Vector2.zero;

        if (handle != null) handle.anchoredPosition = input * radius;
    }

    public void ProcessPointerUp()
    {
        isPressed = false;
        input = Vector2.zero;
        if (handle != null) handle.anchoredPosition = Vector2.zero;
        StartIdleTimer();
    }

    public void ResetIdleTimer()
    {
        idleTimer = 0f;
        if (isFading)
        {
            // Bring back to full alpha
            if (fadeCoroutine != null) StopCoroutine(fadeCoroutine);
            isFading = false;
            if (canvasGroup != null) canvasGroup.alpha = 1f;
        }
    }

    public void StartIdleTimer()
    {
        ResetIdleTimer();
    }

    void StartFadeOut()
    {
        if (isFading || spawner == null) return;

        isFading = true;
        if (fadeCoroutine != null) StopCoroutine(fadeCoroutine);
        fadeCoroutine = StartCoroutine(FadeOutRoutine());
    }

    IEnumerator FadeOutRoutine()
    {
        float fadeDuration = spawner.GetFadeDuration();
        float elapsed = 0f;

        while (elapsed < fadeDuration)
        {
            elapsed += Time.deltaTime;
            float alpha = Mathf.Lerp(1f, 0f, elapsed / fadeDuration);
            if (canvasGroup != null) canvasGroup.alpha = alpha;
            yield return null;
        }

        if (canvasGroup != null) canvasGroup.alpha = 0f;
        if (spawner != null) spawner.ReturnToPool(this);
    }

    // Public API for input
    public float Horizontal => input.x;
    public float Vertical => input.y;
    public Vector2 Direction => input;
    public float Magnitude => input.magnitude;
}
