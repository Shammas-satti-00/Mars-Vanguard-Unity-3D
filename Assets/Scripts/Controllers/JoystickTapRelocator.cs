using UnityEngine;
using UnityEngine.EventSystems;

public class JoystickTapRelocator : MonoBehaviour, IPointerDownHandler, IPointerUpHandler
{
    [Header("Assign in Inspector")]
    public Terresquall.VirtualJoystick joystick;
    public RectTransform movePanel;

    private Vector2 originalPos;
    private bool joystickActive = false;

    void Start()
    {
        if (joystick == null)
        {
            Debug.LogError("JoystickTapRelocator: No joystick assigned!");
            enabled = false;
            return;
        }

        originalPos = joystick.transform.position;
    }

    // When panel is tapped, move joystick to that tap position
    public void OnPointerDown(PointerEventData eventData)
    {
        if (joystickActive) return;

        joystickActive = true;

        // Convert UI tap to world/screen space
        Vector2 tapPos = eventData.position;

        joystick.transform.position = tapPos;
        joystick.desiredPosition = tapPos;

        // Pass the tap to the joystick as if user touched it
        joystick.Uproot(tapPos, eventData.pointerId);
    }

    // When the joystick is released, return to original position
    public void OnPointerUp(PointerEventData eventData)
    {
        joystickActive = false;

        // Reset joystick instantly
        joystick.transform.position = originalPos;
        joystick.desiredPosition = originalPos;
    }
}
