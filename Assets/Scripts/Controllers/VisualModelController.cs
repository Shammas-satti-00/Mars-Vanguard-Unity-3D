using UnityEngine;
using Terresquall;

public class VisualModelController : MonoBehaviour
{
    [Header("References")]
    public Transform visualModel; // Assign the child GameObject that holds the visual 3D model mesh

    [Header("Tilt Settings")]
    public float pitchTiltAngle = 30f;    // Max tilt up/down in degrees
    public float yawTiltAngle = 30f;      // Max tilt left/right (lean like aircraft banking)
    public float tiltSpeed = 5f;          // How quickly the tilt lerps (higher = snappier)
    public bool invertPitch = true;       // Flip up/down tilt direction if needed
    public bool invertYawTilt = true;     // Flip left/right tilt direction if needed

    [Header("Gyro")]
    private GyroController gyroController;

    // Internal state
    private Quaternion defaultLocalRotation; // The model's default local rotation (e.g., facing backwards in local space)
    private Quaternion targetLocalRotation;
    private Quaternion currentLocalRotation;

    // Cache input
    private Vector3 lastInput = Vector3.zero;

    void Awake()
    {
        if (visualModel == null)
        {
            Debug.LogError("VisualModel not assigned in VisualModelController!");
            enabled = false;
            return;
        }

        // Capture the initial local rotation as the default
        defaultLocalRotation = visualModel.localRotation;
        currentLocalRotation = defaultLocalRotation;
        targetLocalRotation = defaultLocalRotation;

        // Get GyroController from parent
        if (transform.parent != null)
        {
            gyroController = transform.parent.GetComponent<GyroController>();
            if (gyroController == null)
            {
                Debug.LogWarning("GyroController not found on parent GameObject. Gyro input will not be available.");
            }
        }
        else
        {
            Debug.LogWarning("VisualModelController has no parent. Gyro input will not be available.");
        }

        // Optional warning
        Vector3 euler = defaultLocalRotation.eulerAngles;
        if (Mathf.Abs(euler.x) > 0.1f || (Mathf.Abs(euler.y - 180f) > 0.1f && Mathf.Abs(euler.y - (-180f)) > 0.1f) || Mathf.Abs(euler.z) > 0.1f)
        {
            Debug.LogWarning($"VisualModel's default local rotation is {euler}, but expected approx (0, 180, 0) or (0, -180, 0). Tilts will be applied relative to this anyway.");
        }
    }

    void Update()
    {
        if (visualModel == null) return;

        // Get joystick input
        
        float moveX = VirtualJoystick.GetAxis("Horizontal");
        float moveY = VirtualJoystick.GetAxis("Vertical");


        // Get gyro input and add it to joystick input if gyro is enabled
        if (gyroController != null && gyroController.IsGyroActive())
        {
            Vector2 gyroInput = gyroController.GetGyroInput();
            moveX += gyroInput.x;
            moveY += gyroInput.y;

            // Clamp combined input to -1 to 1 range
            moveX = Mathf.Clamp(moveX, -1f, 1f);
            moveY = Mathf.Clamp(moveY, -1f, 1f);
        }

        Vector3 input = new Vector3(moveX, moveY, 0f);

        // Smooth input
        lastInput = Vector3.Lerp(lastInput, input, Time.deltaTime * tiltSpeed);

        // Calculate multipliers with optional inversion
        float pitchSign = invertPitch ? -1f : 1f;
        float yawSign = invertYawTilt ? -1f : 1f;

        // Additive tilts
        Quaternion pitchTilt = Quaternion.AngleAxis(pitchSign * -lastInput.y * pitchTiltAngle, Vector3.right); // -y for up input tilts nose up; extra sign respects inversion
        Quaternion yawTilt = Quaternion.AngleAxis(yawSign * -lastInput.x * yawTiltAngle, Vector3.forward); // -x for right input leans right

        // Combine
        targetLocalRotation = defaultLocalRotation * pitchTilt * yawTilt;

        // Smooth
        currentLocalRotation = Quaternion.Lerp(currentLocalRotation, targetLocalRotation, Time.deltaTime * tiltSpeed);
        visualModel.localRotation = currentLocalRotation;
    }

    public void ResetTilt()
    {
        currentLocalRotation = defaultLocalRotation;
        targetLocalRotation = defaultLocalRotation;
        lastInput = Vector3.zero;
        visualModel.localRotation = defaultLocalRotation;
    }
}