using UnityEngine;

public class GyroController : MonoBehaviour
{
    [Header("Gyro Settings")]
    [Tooltip("Enable/disable gyroscope control")]
    public bool gyroEnabled;

    [Tooltip("Sensitivity multiplier for gyro input")]
    public float gyroSensitivity = 1.0f;

    [Header("Invert Options")]
    [Tooltip("Invert vertical (pitch) axis")]
    public bool invertVertical = false;

    [Tooltip("Invert horizontal (yaw) axis")]
    public bool invertHorizontal = false;

    [Header("Smoothing")]
    [Tooltip("Smoothing factor for gyro input (0 = no smoothing, 1 = max smoothing)")]
    [Range(0f, 0.95f)]
    public float smoothing = 0.5f;

    // Internal state
    private bool gyroInitialized = false;
    private Quaternion gyroInitialRotation;
    private Vector2 smoothedInput;

    void Start()
    {
        if (PlayerPrefs.GetInt("GyroEnabled") == 1)
        InitializeGyro();
    }

    public void InitializeGyro()
    {
        if (SystemInfo.supportsGyroscope)
        {
            Input.gyro.enabled = true;
            gyroInitialRotation = Input.gyro.attitude;
            gyroInitialized = true;
            gyroEnabled = true;
            Debug.Log("Gyroscope initialized successfully");
        }
        else
        {
            Debug.LogWarning("Gyroscope not supported on this device");
            gyroEnabled = false;
        }
    }

    /// <summary>
    /// Get gyro input as a Vector2 (x = horizontal/yaw, y = vertical/pitch)
    /// Returns Vector2.zero if gyro is disabled or not available
    /// </summary>
    public Vector2 GetGyroInput()
    {
        if (!gyroEnabled || !gyroInitialized)
            return Vector2.zero;

        // Get current gyro attitude
        Quaternion gyroAttitude = Input.gyro.attitude;

        // Calculate relative rotation from initial position
        Quaternion relativeRotation = Quaternion.Inverse(gyroInitialRotation) * gyroAttitude;

        // Convert to Euler angles for easier processing
        Vector3 euler = relativeRotation.eulerAngles;

        // Normalize angles to -180 to 180 range
        float pitch = NormalizeAngle(euler.x);
        float yaw = NormalizeAngle(euler.y);

        // Map to input range (-1 to 1) with sensitivity
        // Divide by a reasonable tilt angle (e.g., 45 degrees) to map to -1 to 1
        float horizontal = (yaw / 45f) * gyroSensitivity;
        float vertical = (pitch / 45f) * gyroSensitivity;

        // Apply invert settings
        if (invertHorizontal)
            horizontal = -horizontal;
        if (invertVertical)
            vertical = -vertical;

        // Clamp to -1 to 1 range
        horizontal = Mathf.Clamp(horizontal, -1f, 1f);
        vertical = Mathf.Clamp(vertical, -1f, 1f);

        // Apply smoothing
        Vector2 rawInput = new Vector2(horizontal, vertical);
        smoothedInput = Vector2.Lerp(rawInput, smoothedInput, smoothing);

        return smoothedInput;
    }

    /// <summary>
    /// Normalize angle to -180 to 180 range
    /// </summary>
    private float NormalizeAngle(float angle)
    {
        if (angle > 180f)
            angle -= 360f;
        return angle;
    }

    /// <summary>
    /// Recalibrate the gyroscope to current position as neutral
    /// </summary>
    public void RecalibrateGyro()
    {
        if (gyroInitialized)
        {
            gyroInitialRotation = Input.gyro.attitude;
            smoothedInput = Vector2.zero;
            Debug.Log("Gyroscope recalibrated");
        }
    }

    /// <summary>
    /// Toggle gyro on/off
    /// </summary>
    public void ToggleGyro()
    {
        gyroEnabled = !gyroEnabled;
        if (gyroEnabled && !gyroInitialized)
        {
            InitializeGyro();
        }
    }

    public void ToggleGyro(bool b)
    {
        gyroEnabled = b;
        if (gyroEnabled && !gyroInitialized)
        {
            InitializeGyro();

        }
    }

    /// <summary>
    /// Set gyro enabled state
    /// </summary>
    public void SetGyroEnabled(bool enabled)
    {
        gyroEnabled = enabled;
        if (gyroEnabled && !gyroInitialized)
        {
            InitializeGyro();
        }
    }

    /// <summary>
    /// Set vertical invert
    /// </summary>
    public void SetInvertVertical(bool invert)
    {
        invertVertical = invert;
    }

    /// <summary>
    /// Set horizontal invert
    /// </summary>
    public void SetInvertHorizontal(bool invert)
    {
        invertHorizontal = invert;
    }

    /// <summary>
    /// Check if gyro is available and enabled
    /// </summary>
    public bool IsGyroActive()
    {
        return gyroEnabled && gyroInitialized;
    }
}