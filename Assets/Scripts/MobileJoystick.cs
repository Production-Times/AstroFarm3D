using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

[RequireComponent(typeof(RectTransform))]
public class MobileJoystick : MonoBehaviour, IPointerDownHandler, IPointerUpHandler, IDragHandler
{
    [Tooltip("Background RectTransform (usually the joystick base)")]
    public RectTransform background;
    [Tooltip("Handle RectTransform (the moving knob)")]
    public RectTransform handle;
    [Tooltip("Maximum distance (in pixels) the handle can move from centre")]
    public float handleRange = 60f;
    [Tooltip("Small dead zone to ignore tiny touches")]
    public float deadZone = 0.1f;

    [Header("UI Button Safety")]
    [SerializeField] private UIButtonTouchManager buttonTouchManager;

    Vector2 input = Vector2.zero;

    void Start()
    {
        if (background == null) background = GetComponent<RectTransform>();
        if (handle == null && background.childCount > 0) handle = background.GetChild(0) as RectTransform;
        if (handle == null) Debug.LogWarning("MobileJoystick: handle not assigned and no child found.", this);

        if (buttonTouchManager == null)
            buttonTouchManager = FindObjectOfType<UIButtonTouchManager>();
    }

    public virtual void OnPointerDown(PointerEventData eventData)
    {
        // Ignore if pointer is over a UI button
        if (buttonTouchManager != null && buttonTouchManager.ShouldIgnoreJoystickInput())
            return;

        OnDrag(eventData);
    }

    public virtual void OnDrag(PointerEventData eventData)
    {
        // Ignore if pointer is over a UI button
        if (buttonTouchManager != null && buttonTouchManager.ShouldIgnoreJoystickInput())
            return;

        if (background == null) return;

        Vector2 localPoint;
        RectTransformUtility.ScreenPointToLocalPointInRectangle(background, eventData.position, eventData.pressEventCamera, out localPoint);

        float radius = (handleRange > 0f) ? handleRange : (background.sizeDelta.x * 0.5f);
        input = localPoint / radius;

        if (input.magnitude > 1f) input = input.normalized;
        if (input.magnitude < deadZone) input = Vector2.zero;

        if (handle != null) handle.anchoredPosition = input * radius;
    }

    public virtual void OnPointerUp(PointerEventData eventData)
    {
        input = Vector2.zero;
        if (handle != null) handle.anchoredPosition = Vector2.zero;
    }

    // Normalized axes (-1 .. 1)
    public float Horizontal => input.x;
    public float Vertical => input.y;
    public Vector2 Direction => input;
    public float Magnitude => input.magnitude;
}
