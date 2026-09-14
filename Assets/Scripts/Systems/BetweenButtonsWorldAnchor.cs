using UnityEngine;

public class BetweenButtonsWorldAnchor : MonoBehaviour
{
    [Header("UI")]
    public RectTransform buttonA;        // assign your first button
    public RectTransform buttonB;        // assign your second button
    public Canvas canvas;                // the canvas those buttons live on

    [Header("World")]
    public Camera worldCamera;           // usually Camera.main
    public Transform worldObject;        // the object you want to position
    public float distanceFromCamera = 10f; // how far from the world camera (in world units)

    void Reset()
    {
        if (!worldCamera) worldCamera = Camera.main;
    }

    void LateUpdate()
    {
        if (!buttonA || !buttonB || !canvas || !worldCamera || !worldObject) return;

        // 1) Get the UI camera (depends on canvas render mode)
        Camera uiCam = canvas.renderMode == RenderMode.ScreenSpaceCamera ? canvas.worldCamera : null;

        // 2) Buttons' screen positions
        Vector2 screenA = RectTransformUtility.WorldToScreenPoint(uiCam, buttonA.position);
        Vector2 screenB = RectTransformUtility.WorldToScreenPoint(uiCam, buttonB.position);

        // 3) Midpoint in screen space
        Vector2 midScreen = (screenA + screenB) * 0.5f;

        // 4A) Place at a fixed distance from the world camera along the midpoint ray:
        Vector3 worldPos = worldCamera.ScreenToWorldPoint(new Vector3(midScreen.x, midScreen.y, distanceFromCamera));
        worldObject.position = worldPos;

        // Optional: face the camera so it’s always visible
        // worldObject.forward = (worldObject.position - worldCamera.transform.position).normalized;
    }
}
