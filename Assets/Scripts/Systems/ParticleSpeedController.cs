
using UnityEngine;

public class ParticleSpeedController : MonoBehaviour
{
    [Header("Particle System Settings")]
    public ParticleSystem _particleSystem; // The particle system to control
    public Engine engine; // Reference to the engine (or your spaceship)

    [Header("Speed Modifier Settings")]
    [Range(0.01f, 1f)] public float speedModifierMultiplier = 0.01f; // Multiplier to adjust the effect of moveSpeed on particle speed
    private ParticleSystem.VelocityOverLifetimeModule velocityModule;
    private ParticleSystem.EmissionModule emissionModule;

    public void Initialize(Engine engine)
    {
        _particleSystem = GetComponent<ParticleSystem>(); // Automatically get the attached ParticleSystem if not assigned
        velocityModule = _particleSystem.velocityOverLifetime;
        emissionModule = _particleSystem.emission;
        this.engine = engine;
    }

    void Update()
    {
        if (engine != null)
        {
            // Calculate speed modifier based on engine moveSpeed
            float speedModifier = engine.moveSpeed * speedModifierMultiplier;

            // Apply the speed modifier to the particle system's velocity based on the ship's direction
            Vector3 shipForwardDirection = transform.forward; // Get the ship's forward direction
            Vector3 velocityDirection = shipForwardDirection * speedModifier;

            // Apply the velocity to all axes (x, y, z) of the particles' movement
            velocityModule.x = velocityDirection.x;
            velocityModule.y = velocityDirection.y;
            velocityModule.z = velocityDirection.z;

            // Disable emission if speed is zero, otherwise enable it
            emissionModule.enabled = (engine.moveSpeed != 0);
        }
    }
}