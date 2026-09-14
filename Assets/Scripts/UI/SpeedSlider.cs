using UnityEngine;
using UnityEngine.UI; // Needed for the Slider component

[DisallowMultipleComponent]
public class SpeedSlider : MonoBehaviour
{
    [Header("UI References")]
    public Slider speedSlider; // Reference to the UI Slider
    public Text sliderValueText; // Reference to display the current slider value (optional)

    [Header("Speed Settings")]
    public float maxBaseSpeed = 20f; // Max base speed (100%, formerly moveSpeed)

    [Header("Camera Zoom Settings")]
    public Camera mainCamera; // Reference to the main camera (assign in Inspector or auto-find)
    public float baseFOV = 60f; // Base field of view
    public float maxFOVIncrease = 30f; // Maximum FOV increase at top speed for dramatic effect
    public float zoomSmoothSpeed = 5f; // Speed of FOV interpolation

    private Engine equippedEngine; // Reference to the Engine component

    void Start()
    {
    }

    public void SetEngine(Engine engine, Camera camera)
    {


        mainCamera = camera;
        baseFOV = mainCamera.fieldOfView; // Capture initial FOV
        equippedEngine = engine; // Allow external scripts to assign the engine
        maxBaseSpeed = engine.moveSpeed; // Capture original max (assuming pre-init)
        // Update slider to reflect current base
        speedSlider.value = engine.baseMoveSpeed / maxBaseSpeed;
        speedSlider.onValueChanged.AddListener(UpdateSpeed);
        if (sliderValueText != null)
        {
            sliderValueText.text = (speedSlider.value * 100).ToString("F0") + "%";
        }
    }

    void Update()
    {
        if (equippedEngine == null || mainCamera == null) return;

        // Calculate max possible speed (base max * boost multiplier)
        float maxPossibleSpeed = maxBaseSpeed * equippedEngine.boostMultiplier;

        // Avoid division by zero
        if (maxPossibleSpeed <= 0f) return;

        // Calculate speed ratio (0 to 1)
        float speedRatio = Mathf.Clamp01(equippedEngine.CurrentSpeed / maxPossibleSpeed);

        // Calculate target FOV for dramatic zoom-out effect
        float targetFOV = baseFOV + maxFOVIncrease * speedRatio;

        // Smoothly interpolate to the target FOV
        mainCamera.fieldOfView = Mathf.Lerp(mainCamera.fieldOfView, targetFOV, Time.deltaTime * zoomSmoothSpeed);
    }

    // Update the base speed based on the slider value
    private void UpdateSpeed(float value)
    {
        // Calculate the target base speed based on the slider value (0 to 1)
        float targetBaseSpeed = Mathf.Lerp(0f, maxBaseSpeed, value);

        // Update the baseMoveSpeed of the Engine (Tick will handle computing moveSpeed)
        if (equippedEngine != null)
        {
            equippedEngine.baseMoveSpeed = targetBaseSpeed;
        }

        // Optionally display the current value as a percentage
        if (sliderValueText != null)
        {
            sliderValueText.text = (value * 100).ToString("F0") + "%";
        }

        // Debug log to see the current base speed and slider value
        Debug.Log($"Slider Value: {value * 100:F0}% | Current Base Speed: {targetBaseSpeed:F2} | Effective Speed: {equippedEngine?.CurrentSpeed:F2}");
    }
}