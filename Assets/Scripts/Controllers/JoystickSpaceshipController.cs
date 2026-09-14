using UnityEngine;
using Terresquall;

public class JoystickSpaceshipController : MonoBehaviour
{
    [Header("Settings")]
    public Ship ship;

    [Header("Boost Settings")]
    public bool boostActive = false;

    [Header("Gyro")]
    public GyroController gyroController;

    [Header("Events")]
    public SpaceshipEvents events;

    public MovementController movementController;

    // Convenience accessor: always fetch the currently equipped engine
    Engine CurrentEngine
    {
        get
        {
            var em = ship != null ? ship.GetEquipmentManager() : null;
            return em != null ? em.equippedEngine : null;
        }
    }

    void Awake()
    {
        ship = GetComponent<Ship>();
        movementController = new MovementController(transform);

        // Get or add GyroController
        gyroController = GetComponent<GyroController>();
        if (gyroController == null)
        {
            gyroController = gameObject.AddComponent<GyroController>();
        }
    }

    void Update()
    {
        if (movementController == null || ship == null) return;

        // Get the engine fresh every frame so changes apply immediately
        var engine = CurrentEngine;
        if (engine == null) return;

        // Get input from VirtualJoystick
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

        Vector3 input = new Vector3(moveX, moveY, 1f); // z=1 = constant forward

        // Boost multiplier from the engine
        float speedMultiplier = boostActive ? Mathf.Max(1f, engine.boostMultiplier) : 1f;

        float dt = Time.deltaTime;

        movementController.Move(input, engine, speedMultiplier, dt);
        movementController.Rotate(input, engine, dt);

        events?.onMoved?.Invoke(movementController.Velocity);
        events?.onPositionChanged?.Invoke(movementController.Position);
    }

    // UI hooks (e.g., from your BoostButton)
    public void SetBoost(bool active) => boostActive = active;
    public void OnBoostDown() => boostActive = true;
    public void OnBoostUp() => boostActive = false;

    // Gyro control methods (can be called from UI)
    public void ToggleGyro() => gyroController?.ToggleGyro();
    public void SetGyroEnabled(bool enabled) => gyroController?.SetGyroEnabled(enabled);
    public void SetInvertVertical(bool invert) => gyroController?.SetInvertVertical(invert);
    public void SetInvertHorizontal(bool invert) => gyroController?.SetInvertHorizontal(invert);
    public void RecalibrateGyro() => gyroController?.RecalibrateGyro();
}