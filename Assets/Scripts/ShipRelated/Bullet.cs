using UnityEngine;

public class Bullet : PooledObject
{
    [HideInInspector] public int damage;
    [HideInInspector] public int speed = 100;
    [HideInInspector] public float lifetime = 5f;

    public GameObject impactEffectPrefab;
    public string targetTag = "Target";  // Only collide with this tag

    float _lifeTimer;

    Collider _col;
    TrailRenderer[] _trails;
    ParticleSystem[] _particles;

    void Awake()
    {
        _col = GetComponent<Collider>();
        _trails = GetComponentsInChildren<TrailRenderer>(true);
        _particles = GetComponentsInChildren<ParticleSystem>(true);
        Debug.Log($"Bullet ({gameObject.name}): Awake() - Components initialized.");
    }

    public override void OnSpawned()
    {
        _lifeTimer = lifetime;
        _col.enabled = true;
        // clear visuals
        if (_trails != null) foreach (var t in _trails) t.Clear();
        if (_particles != null) foreach (var p in _particles) { p.Clear(true); p.Play(true); }
        Debug.Log($"Bullet ({gameObject.name}): OnSpawned() - Lifetime set to {lifetime}, speed={speed}, damage={damage}. Visuals cleared.");
    }

    public override void OnDespawned()
    {
        if (_col) _col.enabled = false;
        Debug.Log($"Bullet ({gameObject.name}): OnDespawned() - Collider disabled.");
    }

    void Update()
    {
        transform.position += transform.forward * speed * Time.deltaTime;

        _lifeTimer -= Time.deltaTime;
        if (_lifeTimer <= 0f)
        {
            Debug.Log($"Bullet ({gameObject.name}): Update() - Lifetime expired, releasing to pool.");
            PoolManager.Release(gameObject);
        }
    }

    void OnTriggerEnter(Collider other)
    {
        // Only process collision if the collider's tag matches the target tag
        if (other.CompareTag(targetTag))
        {
            Debug.Log($"Bullet ({gameObject.name}): OnTriggerEnter - Hit target: {other.gameObject.name} (Tag: {other.gameObject.tag})");

            var handler = other.GetComponentInParent<DamageHandler>();
            if (handler) handler.TakeDamage(damage);
            else Debug.Log($"Bullet ({gameObject.name}): OnTriggerEnter - No DamageHandler found on {other.gameObject.name} or its parents.");

            if (impactEffectPrefab != null)
            {
                // Approximate impact at current position; face opposite flight direction
                Quaternion rot = Quaternion.LookRotation(-transform.forward);
                var fx = PoolManager.Spawn(impactEffectPrefab, transform.position, rot);
                PoolManager.Release(fx, 1f);
                Debug.Log($"Bullet ({gameObject.name}): OnTriggerEnter - Spawned impact effect.");
            }

            PoolManager.Release(gameObject);
        }
        else
        {
            Debug.Log($"Bullet ({gameObject.name}): OnTriggerEnter - Ignored non-target: {other.gameObject.name} (Tag: {other.gameObject.tag})");
        }
    }
}