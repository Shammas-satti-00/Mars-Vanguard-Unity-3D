   using UnityEngine;

public class MovementController
{
    private readonly Transform transform;

    private Vector3 currentVelocity;
    public Vector3 Velocity => currentVelocity;
    public Vector3 Position => transform.position;

    // Pitch momentum (for looping feel)
    private float currentPitchVelocity = 0f;
    private const float pitchAcceleration = 2.5f;
    private const float maxPitchVelocity = 1.8f;
    private const float pitchDamping = 0.6f;

    public MovementController(Transform transform)
    {
        this.transform = transform;
    }

    public void Move(Vector3 input, Engine engine, float speedMultiplier, float deltaTime)
    {
        // NO sensitivity applied here. Movement is purely based on speed settings.
        // float sensitivity = DataHolder.Instance.sensitivity;
        // input *= sensitivity; // <-- REMOVED: This line incorrectly increased speed.

        // Move based on engine.moveSpeed (live)
        float speed = engine != null ? engine.moveSpeed : 0f;

        // input.z is now the raw, un-multiplied input axis (e.g., -1.0 to 1.0)
        currentVelocity = transform.forward * (input.z * speed * speedMultiplier);

        transform.position += currentVelocity * deltaTime;
    }

    public void Rotate(Vector3 input, Engine engine, float deltaTime)
    {
        if (engine == null) return;

        // Apply sensitivity to the rotation inputs (input.x and input.y)
        float sensitivity = DataHolder.Instance.sensitivity;
        input *= sensitivity; // This is acceptable as input.z is unused here

        float pitchRotationSpeed = engine.pitchRotationSpeed;
        float yawRotationSpeed = engine.yawRotationSpeed;
        float rollRotationSpeed = engine.rollRotationSpeed;

        // -----------------------------
        // DIRECT, NO-INERTIA ROTATION
        // -----------------------------

        // PITCH (uses the scaled input.y)
        float pitchDelta = -input.y * pitchRotationSpeed * deltaTime;

        // YAW (uses the scaled input.x)
        float yawDelta = input.x * yawRotationSpeed * deltaTime;

        // ROLL (uses the scaled input.x)
        float rollDelta = -input.x * rollRotationSpeed * deltaTime * 0.3f;

        // Apply instantly — ZERO smoothing
        Quaternion pitchRot = Quaternion.Euler(pitchDelta, 0f, 0f);
        Quaternion yawRot = Quaternion.Euler(0f, yawDelta, 0f);
        Quaternion rollRot = Quaternion.Euler(0f, 0f, rollDelta);

        transform.rotation = transform.rotation * yawRot * pitchRot * rollRot;
    }


}
