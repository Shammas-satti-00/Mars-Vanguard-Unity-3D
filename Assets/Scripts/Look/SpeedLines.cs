using UnityEngine;

[RequireComponent(typeof(ParticleSystem))]
public class SpeedLines : MonoBehaviour
{
    [Header("Target")]
    public Rigidbody targetRigidbody;   // Rigidbody of the spaceship or object
    public Transform targetTransform;   // Fallback if Rigidbody not assigned

    [Header("Particle Settings")]
    public Color particleColor = Color.white;
    public float velocityToSize = 0.05f; // Scale of particles based on speed
    public float velocityToLength = 0.5f; // Stretch factor
    public float maxSize = 2f;            // Max particle size
    public float maxLength = 50f;         // Max particle length
    public float frontOffset = 1f;        // How far in front of ship
    public float followDistance = 0f;     // Distance behind the ship

    private ParticleSystem ps;
    private ParticleSystem.MainModule main;
    private ParticleSystem.VelocityOverLifetimeModule velocityModule;
    private ParticleSystem.SizeOverLifetimeModule sizeModule;

    void Awake()
    {
        ps = GetComponent<ParticleSystem>();
        ConfigureParticleSystem();
    }

    void Update()
    {
        Vector3 velocity = Vector3.zero;
        Vector3 position = transform.position;
        Vector3 forward = transform.forward;
        Vector3 up = transform.up;

        if (targetRigidbody != null)
        {
            velocity = targetRigidbody.linearVelocity;
            position = targetRigidbody.position;
            forward = targetRigidbody.transform.forward;
            up = targetRigidbody.transform.up;
        }
        else if (targetTransform != null)
        {
            position = targetTransform.position;
            forward = targetTransform.forward;
            up = targetTransform.up;
        }

        UpdateParticleEffect(velocity, position, forward, up);
    }

    void ConfigureParticleSystem()
    {
        ps.Stop();
        ps.Clear();

        // Main settings
        main = ps.main;
        main.loop = true;
        main.playOnAwake = true;
        main.startLifetime = 0.2f;
        main.startSpeed = 0f;
        main.startSize = 0.1f;
        main.startColor = particleColor;
        main.simulationSpace = ParticleSystemSimulationSpace.World;

        // Shape as cone behind ship
        var shape = ps.shape;
        shape.enabled = true;
        shape.shapeType = ParticleSystemShapeType.Cone;
        shape.angle = 0;
        shape.radius = 0.1f;
        shape.rotation = new Vector3(0, 0, 0);
        shape.position = Vector3.zero;
        shape.length = 0.1f;

        // Velocity over lifetime
        velocityModule = ps.velocityOverLifetime;
        velocityModule.enabled = true;
        velocityModule.space = ParticleSystemSimulationSpace.World;

        // Size over lifetime (stretching effect)
        sizeModule = ps.sizeOverLifetime;
        sizeModule.enabled = true;
        sizeModule.size = new ParticleSystem.MinMaxCurve(1.0f, AnimationCurve.Linear(0, 0, 1, 1));

        ps.Play();
    }

    void UpdateParticleEffect(Vector3 velocity, Vector3 position, Vector3 forward, Vector3 up)
    {
        float speed = velocity.magnitude;

        // Adjust particle size and length based on speed
        float particleSize = Mathf.Clamp(speed * velocityToSize, 0.05f, maxSize);
        main.startSize = particleSize;

        float particleLength = Mathf.Clamp(speed * velocityToLength, 0.1f, maxLength);
        velocityModule.z = -particleLength; // Stretch particles backward

        // Position the particle system in front of the ship
        transform.position = position + forward * frontOffset - forward * followDistance;

        // Align the particle system with the movement direction
        if (velocity != Vector3.zero)
            transform.rotation = Quaternion.LookRotation(velocity.normalized, up);
    }
}
