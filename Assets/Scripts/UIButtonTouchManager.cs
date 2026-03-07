using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

/// <summary>
/// Global script that makes UI buttons immune to MobileJoystick touches.
/// Buttons have priority - joystick only responds if no button is being touched.
/// Attach this to any GameObject in your scene (usually GameManager or Canvas).
/// </summary>
public class UIButtonTouchManager : MonoBehaviour
{
    [Tooltip("Reference to the MobileJoystick component")]
    [SerializeField] private MobileJoystick mobileJoystick;

    [Tooltip("Layer or tag to identify UI buttons (empty = all UI buttons)")]
    [SerializeField] private string buttonTag = "";

    private GraphicRaycaster graphicRaycaster;
    private EventSystem eventSystem;

    void Start()
    {
        if (mobileJoystick == null)
        {
            mobileJoystick = FindObjectOfType<MobileJoystick>();
        }

        if (graphicRaycaster == null)
        {
            graphicRaycaster = FindObjectOfType<GraphicRaycaster>();
        }

        if (eventSystem == null)
        {
            eventSystem = EventSystem.current;
        }

        if (mobileJoystick == null)
            Debug.LogWarning("UIButtonTouchManager: MobileJoystick not found in scene!");
    }

    /// <summary>
    /// Checks if the pointer is currently over a UI button.
    /// </summary>
    public bool IsPointerOverButton()
    {
        if (eventSystem == null || graphicRaycaster == null)
            return false;

        PointerEventData pointerData = new PointerEventData(eventSystem);
        pointerData.position = Input.mousePosition;

        System.Collections.Generic.List<RaycastResult> results = new System.Collections.Generic.List<RaycastResult>();
        graphicRaycaster.Raycast(pointerData, results);

        foreach (RaycastResult result in results)
        {
            // Check if the raycast hit a button
            if (result.gameObject.GetComponent<Button>() != null)
                return true;

            // Optional: Check by tag if specified
            if (!string.IsNullOrEmpty(buttonTag) && result.gameObject.CompareTag(buttonTag))
                return true;
        }

        return false;
    }

    /// <summary>
    /// Call this from MobileJoystick to check if joystick should be disabled.
    /// Returns true if a button is being touched (joystick should be ignored).
    /// </summary>
    public bool ShouldIgnoreJoystickInput()
    {
        return IsPointerOverButton();
    }
}
