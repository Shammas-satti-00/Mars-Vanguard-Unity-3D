using UnityEngine;
using System.Collections;
using System;

public class CannonSlot : MonoBehaviour
{
    [Header("Cannon Settings")]
    public int slotNumber;
    [SerializeField] private Transform firePoint;
    [Tooltip("Is this cannon controlled by the player? If false, uses predictive bullet logic.")]
    public bool isPlayerControlled = false;

    // Former "Weapon/Cannon" bits
    public string label;
    public Sprite sprite;
    [Tooltip("Visual travel speed for the trail (units/sec). Also determines hit delay.")]
    public int projectileSpeed = 500;
    [Tooltip("Maximum range for the raycast. Shots cannot hit beyond this distance.")]
    public float maxRange = 1000f;
    [Tooltip("Damage applied to the hit target (if tagged).")]
    public int projectileDamage = 10;
    [Tooltip("Shots per second.")]
    public int fireRate = 6;
    [Tooltip("Lifetime for trail visual fadeout.")]
    public float lifetime = 5f;
    [Tooltip("Only objects with this tag are considered valid targets for damage.")]
    public string targetTag = "Enemy";

    [Header("Visual FX")]
    [Tooltip("Optional muzzle flash (pooled).")]
    public GameObject muzzleFlashPrefab;
    [Tooltip("Optional impact effect (pooled).")]
    public GameObject impactEffectPrefab;
    [Tooltip("TrailRenderer prefab used as the 'bullet' visual.")]
    public TrailRenderer bulletTrail;
    public int projectilePrefabIndex;
    public int muzzleFlashPrefabIndex;
    public int impactEffectPrefabIndex;

    [Header("Magazine")]
    [Tooltip("Maximum shells in magazine.")]
    public int magazineCapacity = 60;
    [Tooltip("Shells consumed per shot.")]
    public int ammoPerShot = 1;
    [Tooltip("Ammo recovered per second (integer-based). 5 = +1 every 0.2s.")]
    public int recoveryRatePerSecond = 5;
    [Tooltip("Delay (seconds) before recovery begins after last shot.")]
    public float recoveryDelay = 2f;
    [Tooltip("Start full on Awake/Enable.")]
    public bool startFull = true;

    [Header("Raycast")]
    [Tooltip("Physics layers to consider for the raycast (targets/obstacles). ~0 = everything.")]
    public LayerMask raycastLayers = ~0;
    [Tooltip("Should triggers be ignored by the raycast?")]
    public QueryTriggerInteraction triggerInteraction = QueryTriggerInteraction.Ignore;

    // Runtime
    public int currentAmmo;
    private float timeSinceLastShot;
    private float recoveryTimer;
    private float recoveryCooldownTimer;

    public Transform FirePoint => firePoint;
    public int CurrentAmmo => currentAmmo;

    void Awake()
    {
        if (startFull) currentAmmo = magazineCapacity;
        Debug.Log($"CannonSlot Awake: SlotNumber={slotNumber}, Label={label}, CurrentAmmo={currentAmmo} on {gameObject.name}");

        if (transform.root.CompareTag("Player"))
            isPlayerControlled = true;
    }

    void OnEnable()
    {
        if (startFull) currentAmmo = magazineCapacity;
        timeSinceLastShot = 0f;
        recoveryTimer = 0f;
        recoveryCooldownTimer = 0f;
        Debug.Log($"CannonSlot OnEnable: SlotNumber={slotNumber}, Label={label}, CurrentAmmo={currentAmmo} on {gameObject.name}");
    }

    void Update()
    {
        timeSinceLastShot += Time.deltaTime;

        // ---- RECOVERY LOGIC ----
        float fireInterval = 1f / Mathf.Max(1, fireRate);
        bool canRecover = timeSinceLastShot >= fireInterval;

        if (canRecover)
        {
            recoveryCooldownTimer += Time.deltaTime;

            if (recoveryCooldownTimer >= recoveryDelay && currentAmmo < magazineCapacity && recoveryRatePerSecond > 0)
            {
                float interval = 1f / recoveryRatePerSecond;
                recoveryTimer += Time.deltaTime;

                while (recoveryTimer >= interval)
                {
                    recoveryTimer -= interval;
                    currentAmmo++;
                    Debug.Log($"CannonSlot Update: Recovered ammo, CurrentAmmo={currentAmmo} on {gameObject.name}");

                    if (currentAmmo >= magazineCapacity)
                    {
                        currentAmmo = magazineCapacity;
                        Debug.Log($"CannonSlot Update: Magazine full, CurrentAmmo={currentAmmo} on {gameObject.name}");
                        break;
                    }
                }
            }
        }
    }

    /// <summary>
    /// Can this cannon fire right now?
    /// </summary>
    public bool CanFire()
    {
        if (firePoint == null)
        {
            Debug.LogWarning($"CannonSlot CanFire: Missing firePoint on {gameObject.name}");
            return false;
        }

        if (timeSinceLastShot < 1f / Mathf.Max(1, fireRate))
        {
            Debug.Log($"CannonSlot CanFire: Fire rate cooldown not ready, TimeSinceLastShot={timeSinceLastShot:F2} on {gameObject.name}");
            return false;
        }

        if (currentAmmo < ammoPerShot)
        {
            Debug.LogWarning($"CannonSlot CanFire: Insufficient ammo, CurrentAmmo={currentAmmo}, AmmoPerShot={ammoPerShot} on {gameObject.name}");
            return false;
        }

        return true;
    }

    /// <summary>
    /// Attempts to fire. Returns true if a shot was fired.
    /// </summary>
    public bool TryFire()
    {
        if (!CanFire())
        {
            Debug.LogWarning($"CannonSlot TryFire: Cannot fire on {gameObject.name}");
            return false;
        }

        FireCannon();
        return true;
    }

    /// <summary>
    /// Fires without re-checks. Prefer TryFire() for safety.
    /// </summary>
    public void FireCannon()
    {
        if (!CanFire())
        {
            Debug.LogWarning($"CannonSlot FireCannon: Guard check failed on {gameObject.name}");
            return;
        }

        // Muzzle flash
        if (muzzleFlashPrefab != null)
        {
            var flash = PoolManager.Spawn(muzzleFlashPrefab, firePoint.position, firePoint.rotation);
            if (flash != null) PoolManager.Release(flash, 0.1f);
        }

        if (isPlayerControlled)
        {
            // Player shot: instant raycast hit
            FirePlayerShot();
        }
        else
        {
            // AI shot: predictive bullet logic with collision checking
            FireAIShot();
        }

        // Timers & ammo
        timeSinceLastShot = 0f;
        currentAmmo = Mathf.Max(0, currentAmmo - ammoPerShot);
        recoveryCooldownTimer = 0f;
        recoveryTimer = 0f;
    }

    /// <summary>
    /// Player-controlled shot: instant raycast hit (guaranteed hit on target)
    /// </summary>
    private void FirePlayerShot()
    {
        Vector3 start = firePoint.position;
        Vector3 dir = firePoint.forward;
        Ray ray = new Ray(start, dir);
        RaycastHit[] hits = Physics.RaycastAll(ray, maxRange, raycastLayers, triggerInteraction);
        System.Array.Sort(hits, (a, b) => a.distance.CompareTo(b.distance));

        bool didHit = false;
        Vector3 hitPoint = start + dir * maxRange;
        Vector3 hitNormal = -dir;
        float hitDistance = maxRange;

        RaycastHit? firstAnyHit = hits.Length > 0 ? hits[0] : (RaycastHit?)null;
        RaycastHit? firstTargetHit = null;

        foreach (var h in hits)
        {
            if (h.collider.CompareTag(targetTag))
            {
                firstTargetHit = h;
                break;
            }
        }

        DamageHandler targetHandler = null;

        if (firstTargetHit.HasValue)
        {
            didHit = true;
            hitPoint = firstTargetHit.Value.point;
            hitNormal = firstTargetHit.Value.normal;
            hitDistance = firstTargetHit.Value.distance;
            targetHandler = firstTargetHit.Value.collider.GetComponentInParent<DamageHandler>();

            if (targetHandler == null)
            {
                Debug.LogError($"CannonSlot FirePlayerShot: No DamageHandler on {firstTargetHit.Value.collider.name}");
            }
        }
        else if (firstAnyHit.HasValue)
        {
            didHit = true;
            hitPoint = firstAnyHit.Value.point;
            hitNormal = firstAnyHit.Value.normal;
            hitDistance = firstAnyHit.Value.distance;
        }

        float hitDelay = hitDistance / Mathf.Max(1f, projectileSpeed);

        if (targetHandler != null)
        {
            StartCoroutine(ApplyDelayedDamage(targetHandler, hitDelay));
        }

        if (didHit && impactEffectPrefab != null)
        {
            StartCoroutine(SpawnDelayedImpact(hitPoint, hitNormal, hitDelay));
        }

        if (bulletTrail != null)
        {
            TrailRenderer trail = Instantiate(bulletTrail, firePoint.position, Quaternion.identity);
            trail.time = UnityEngine.Random.Range(0.1f, 0.6f);
            StartCoroutine(AnimateTrail(trail, hitPoint, hitDelay));
        }

        Debug.Log($"CannonSlot FirePlayerShot: Fired. Hit={(didHit ? "Yes" : "No")}, Distance={hitDistance:F1}, Delay={hitDelay:F3}s, CurrentAmmo={currentAmmo} on {gameObject.name}");
    }

    /// <summary>
    /// AI-controlled shot: predictive bullet logic with collision checking during flight
    /// </summary>
    private void FireAIShot()
    {
        Vector3 start = firePoint.position;
        Vector3 dir = firePoint.forward;

        if (bulletTrail != null)
        {
            TrailRenderer trail = Instantiate(bulletTrail, firePoint.position, Quaternion.identity);
            trail.time = UnityEngine.Random.Range(0.1f, 0.6f);
            StartCoroutine(AnimateAIBullet(trail, start, dir));
        }
        else
        {
            Debug.LogWarning($"CannonSlot FireAIShot: No bulletTrail assigned for AI shot on {gameObject.name}");
        }

        Debug.Log($"CannonSlot FireAIShot: Fired predictive bullet, CurrentAmmo={currentAmmo} on {gameObject.name}");
    }

    /// <summary>
    /// Animates AI bullet with collision checking during flight
    /// </summary>
    private IEnumerator AnimateAIBullet(TrailRenderer trail, Vector3 startPos, Vector3 direction)
    {
        float travelDistance = 0f;
        Vector3 currentPos = startPos;
        Vector3 previousPos = startPos;
        bool hasHit = false;

        float maxTravelTime = maxRange / Mathf.Max(1f, projectileSpeed);
        float elapsed = 0f;

        while (elapsed < maxTravelTime && !hasHit)
        {
            float deltaTime = Time.deltaTime;
            elapsed += deltaTime;

            float step = projectileSpeed * deltaTime;
            previousPos = currentPos;
            currentPos += direction * step;
            travelDistance += step;

            // Update trail position
            trail.transform.position = currentPos;

            // Check for collision along the movement path
            Vector3 rayDir = (currentPos - previousPos).normalized;
            float rayDist = Vector3.Distance(previousPos, currentPos);

            RaycastHit[] hits = Physics.RaycastAll(previousPos, rayDir, rayDist, raycastLayers, triggerInteraction);
            System.Array.Sort(hits, (a, b) => a.distance.CompareTo(b.distance));

            // Check for target hit first
            foreach (var hit in hits)
            {
                if (hit.collider.CompareTag(targetTag))
                {
                    hasHit = true;

                    // Apply damage
                    DamageHandler handler = hit.collider.GetComponentInParent<DamageHandler>();
                    if (handler != null)
                    {
                        handler.TakeDamage(projectileDamage);
                        Debug.Log($"CannonSlot AnimateAIBullet: Hit target at distance {travelDistance:F1}");
                    }

                    // Spawn impact effect
                    if (impactEffectPrefab != null)
                    {
                        Quaternion rot = Quaternion.LookRotation(hit.normal);
                        var fx = PoolManager.Spawn(impactEffectPrefab, hit.point, rot);
                        if (fx != null) PoolManager.Release(fx, 1f);
                    }

                    // Move trail to hit point and stop
                    trail.transform.position = hit.point;
                    break;
                }
            }

            // If no target hit, check for environment collision
            if (!hasHit && hits.Length > 0)
            {
                hasHit = true;
                var hit = hits[0];

                // Spawn impact effect on environment
                if (impactEffectPrefab != null)
                {
                    Quaternion rot = Quaternion.LookRotation(hit.normal);
                    var fx = PoolManager.Spawn(impactEffectPrefab, hit.point, rot);
                    if (fx != null) PoolManager.Release(fx, 1f);
                }

                trail.transform.position = hit.point;
                Debug.Log($"CannonSlot AnimateAIBullet: Hit environment at distance {travelDistance:F1}");
            }

            // Check if we've exceeded max range
            if (travelDistance >= maxRange)
            {
                hasHit = true;
                Debug.Log($"CannonSlot AnimateAIBullet: Reached max range {maxRange}");
            }

            yield return null;
        }

        // Let trail fade naturally before destroy
        Destroy(trail.gameObject, trail.time);
    }

    /// <summary>
    /// Applies damage after the calculated delay
    /// </summary>
    private IEnumerator ApplyDelayedDamage(DamageHandler handler, float delay)
    {
        yield return new WaitForSeconds(delay);

        if (handler != null)
        {
            handler.TakeDamage(projectileDamage);
            Debug.Log($"CannonSlot: Damage applied after {delay:F3}s delay");
        }
    }

    /// <summary>
    /// Spawns impact effect after the calculated delay
    /// </summary>
    private IEnumerator SpawnDelayedImpact(Vector3 position, Vector3 normal, float delay)
    {
        yield return new WaitForSeconds(delay);

        Quaternion rot = Quaternion.LookRotation(normal);
        var fx = PoolManager.Spawn(impactEffectPrefab, position, rot);
        if (fx != null) PoolManager.Release(fx, 1f);
    }

    /// <summary>
    /// Moves a TrailRenderer from 'start' to 'end' over the specified duration
    /// </summary>
    private IEnumerator AnimateTrail(TrailRenderer trail, Vector3 hitPoint, float duration)
    {
        Vector3 startPosition = trail.transform.position;
        float elapsed = 0f;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / duration);
            trail.transform.position = Vector3.Lerp(startPosition, hitPoint, t);
            yield return null;
        }

        // Snap to hit point
        trail.transform.position = hitPoint;
        // Let trail fade naturally before destroy
        Destroy(trail.gameObject, trail.time);
    }

    public void Refill()
    {
        currentAmmo = magazineCapacity;
        Debug.Log($"CannonSlot Refill: Magazine refilled, CurrentAmmo={currentAmmo} on {gameObject.name}");
    }

    /// <summary>Adds ammo (clamped to capacity).</summary>
    public void AddAmmo(int amount)
    {
        if (amount <= 0) return;
        currentAmmo = Mathf.Min(magazineCapacity, currentAmmo + amount);
        Debug.Log($"CannonSlot AddAmmo: Added {amount}, CurrentAmmo={currentAmmo} on {gameObject.name}");
    }
}