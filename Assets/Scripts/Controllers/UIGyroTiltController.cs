using Terresquall;
using UnityEngine;

public class UIGyroTiltController : MonoBehaviour
{
    [Header("References")]
    public GyroController gyroController;
    public RectTransform uiObject;

    [Header("Behavior")]
    [Tooltip("If true, controller will try to FindObjectOfType<GyroController>() when gyroController is null.")]
    public bool autoFindGyro = true;
    [Tooltip("When true, only use gyro input (ignore joystick).")]
    public bool useGyroOnly = true;

    [Header("Tilt Settings")]
    public float maxTiltX = 20f; // pitch (up/down) — applied to local X if desired
    public float maxTiltY = 20f; // yaw / roll (left/right) — applied to local Z (common for UI)
    [Range(1f, 30f)] public float tiltSpeed = 10f;
    [Tooltip("Small deadzone — inputs whose magnitude is below this are treated as zero.")]
    public float inputDeadzone = 0.02f;

    [Header("Debug")]
    public bool debugLogs = false;

    private Quaternion defaultRot;
    private Quaternion targetRot;

    void Awake()
    {
        // Ensure uiObject is set
        if (uiObject == null)
        {
            uiObject = GetComponent<RectTransform>();
            if (uiObject == null && debugLogs)
                Debug.LogWarning($"[{name}] UIGyroTiltController: no RectTransform found on this GameObject.");
        }

        // Optionally try to auto-find gyro later if null
        if (gyroController == null && autoFindGyro)
        {
            gyroController = FindObjectOfType<GyroController>();
            if (gyroController != null && debugLogs)
                Debug.Log($"[{name}] UIGyroTiltController: auto-found GyroController on {gyroController.name}");
        }
    }

    void Start()
    {
        if (uiObject != null)
            defaultRot = uiObject.localRotation;
        else
            defaultRot = Quaternion.identity;

        targetRot = defaultRot;
    }

    // Optional public initializer (keeps existing API)
    public void Initialize(GyroController gyro)
    {
        if (uiObject == null)
            uiObject = GetComponent<RectTransform>();

        gyroController = gyro;
        if (uiObject != null)
            defaultRot = uiObject.localRotation;

        targetRot = defaultRot;
    }

    void Update()
    {
        if (uiObject == null)
            return; // nothing to do

        // keep trying to find gyro if allowed and missing
        if (gyroController == null && autoFindGyro)
            gyroController = FindObjectOfType<GyroController>();

        // If no gyro or gyro inactive -> lerp back to default rotation
        if (gyroController == null || !gyroController.IsGyroActive())
        {
            if (debugLogs && (gyroController == null))
                Debug.Log($"[{name}] UIGyroTiltController: gyroController is null or not active. Returning to default.");

            uiObject.localRotation = Quaternion.Lerp(uiObject.localRotation, defaultRot, Time.deltaTime * tiltSpeed);
            return;
        }

        // Read raw gyro input
        Vector2 g = gyroController.GetGyroInput(); // expected range approx -1..1
        if (debugLogs) Debug.Log($"[{name}] gyro input raw = {g}");

        // apply deadzone
        if (Mathf.Abs(g.x) < inputDeadzone) g.x = 0f;
        if (Mathf.Abs(g.y) < inputDeadzone) g.y = 0f;

        // If using joystick + gyro in other systems, here you could combine them.
        // This controller is gyro-first; if useGyroOnly==false you can still read VirtualJoystick here.
        if (!useGyroOnly)
        {
            float jx = VirtualJoystick.GetAxis("Horizontal");
            float jy = VirtualJoystick.GetAxis("Vertical");
            // Blend joystick lightly so UI can be controlled by both if desired:
            // choose whichever is stronger:
            if (Mathf.Abs(jx) > Mathf.Abs(g.x)) g.x = jx;
            if (Mathf.Abs(jy) > Mathf.Abs(g.y)) g.y = jy;
        }

        // Convert gyro input to tilt angles
        // For UI it's common to tilt around Z for left/right lean (roll)
        // and around X for up/down pitch if the UI is 3D. Adjust to taste.
        float pitch = -g.y * maxTiltX; // applied to X
        float rollZ = g.x * maxTiltY;  // applied to Z (so UI appears to 'lean' left/right)

        // Compose target rotation relative to default
        // We apply pitch (X) and roll (Z). Y is left zero for most UI elements.
        targetRot = defaultRot * Quaternion.Euler(pitch, 0f, rollZ);

        // Smoothly interpolate
        uiObject.localRotation = Quaternion.Lerp(uiObject.localRotation, targetRot, Time.deltaTime * tiltSpeed);
    }
}
