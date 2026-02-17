using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.Events;

public class MobileButton : MonoBehaviour, IPointerDownHandler, IPointerUpHandler
{
    /// <summary>
    /// True while the UI button is held.
    /// </summary>
    public bool IsPressed { get; private set; }

    // True only on the frame the button was pressed (consumed by GetButtonDown)
    bool pressedThisFrame;

    public UnityEvent onDown;
    public UnityEvent onUp;

    public void OnPointerDown(PointerEventData eventData)
    {
        IsPressed = true;
        pressedThisFrame = true;
        onDown?.Invoke();
    }

    public void OnPointerUp(PointerEventData eventData)
    {
        IsPressed = false;
        onUp?.Invoke();
    }

    /// <summary>
    /// Returns true once (and clears) if the button was pressed since last call.
    /// </summary>
    public bool GetButtonDown()
    {
        if (pressedThisFrame)
        {
            pressedThisFrame = false;
            return true;
        }
        return false;
    }
}
