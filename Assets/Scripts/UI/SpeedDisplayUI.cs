using UnityEngine;
using TMPro; // ✅ Added for TextMeshPro support

public class SpeedDisplayUI : MonoBehaviour
{
    [Header("References")]
    [Tooltip("The movement controller (optional - for velocity fluctuation)")]
    public JoystickSpaceshipController spaceshipController;

    [Tooltip("The TMP Text component to display speed")]
    public TextMeshProUGUI speedText; // ✅ Changed from Text to TextMeshProUGUI

    [Header("Display Settings")]
    [Tooltip("Multiplier for converting speed units (e.g., to km/s)")]
    public float speedMultiplier = 1f;

    [Tooltip("Unit label to display (e.g., 'km/s', 'm/s', 'knots')")]
    public string unitLabel = "km/s";

    [Tooltip("Number of decimal places to show")]
    [Range(0, 3)]
    public int decimalPlaces = 1;

    [Header("Display Format")]
    [Tooltip("Prefix text before speed value (e.g., 'Speed: ')")]
    public string prefixText = "Speed: ";

    [Tooltip("Suffix text after unit (e.g., ' ⚡')")]
    public string suffixText = "";

    [Header("Dynamic Fluctuation")]
    [Tooltip("Enable speed fluctuation for more believable display")]
    public bool enableFluctuation = true;

    [Tooltip("Fluctuation range (+/-)")]
    public float fluctuationRange = 5f;

    [Tooltip("Speed of fluctuation changes")]
    public float fluctuationSpeed = 2f;

    [Tooltip("Additional fluctuation based on input (turning/pitching)")]
    public float inputBasedFluctuation = 10f;

    public Engine shipEngine;
    private float fluctuationTime = 0f;
    private MovementController movementController;

    public void Initialize(JoystickSpaceshipController controller, Engine engine)
    {
        shipEngine = engine;
        spaceshipController = controller;
        movementController = controller.movementController;
    }

    void Update()
    {
        if (speedText == null) return;
        UpdateSpeedDisplay();
    }

    void UpdateSpeedDisplay()
    {
        float currentSpeed = GetShipSpeed();

        // If speed is 0, stop fluctuations
        if (currentSpeed == 0f)
        {
            speedText.text = BuildSpeedString(currentSpeed); // Simply display speed with no fluctuation
            return;
        }

        // Fluctuation logic (only if speed is greater than 0)
        float fluctuation = 0f;
        if (enableFluctuation)
        {
            fluctuationTime += Time.deltaTime * fluctuationSpeed;
            float sineWave = Mathf.Sin(fluctuationTime) * 0.5f + 0.5f;
            fluctuation = (sineWave * 2f - 1f) * fluctuationRange;

            if (movementController != null)
            {
                Vector3 velocity = movementController.Velocity;
                float velocityVariation = velocity.magnitude - currentSpeed;
                fluctuation += velocityVariation * inputBasedFluctuation * 0.1f;
            }

            fluctuation += Random.Range(-fluctuationRange * 0.2f, fluctuationRange * 0.2f);
        }

        // No dramatic increase, just display the adjusted speed with fluctuation
        float displaySpeed = currentSpeed * speedMultiplier + fluctuation;
        displaySpeed = Mathf.Max(0f, displaySpeed); // Ensure speed doesn't go negative

        string speedString = BuildSpeedString(displaySpeed);
        speedText.text = speedString;
    }

    float GetShipSpeed()
    {
        return shipEngine.moveSpeed;
    }

    string BuildSpeedString(float displaySpeed)
    {
        string formatString = "F" + decimalPlaces;
        return $"{prefixText}{displaySpeed.ToString(formatString)} {unitLabel}{suffixText}";
    }
}
