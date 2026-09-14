using UnityEngine;

public class LauncherSlot : MonoBehaviour
{
    [Header("Launcher Settings")]
    [SerializeField] private Transform firePoint;
    public Radar radar;

    public string label;
    public Sprite sprite;
    public string description;
    public int projectileDamage;
    public int fireRate;
    public float lifetime;
    public int projectileSpeed;
    public string targetTag;
    public GameObject projectilePrefab;
    public GameObject impactEffectPrefab;
    public GameObject muzzleFlashPrefab;

    // --- RESTORED INDEXES ---
    public int projectilePrefabIndex;
    public int muzzleFlashPrefabIndex;
    public int impactEffectPrefabIndex;

    [Header("Magazine")]
    public int magazineCapacity = 6;
    public int ammoPerShot = 1;

    [Header("Recovery")]
    public int recoveryRatePerSecond = 10;
    public bool pauseRecoveryDuringCooldown = true;
    public bool startFull = true;

    public int currentAmmo;
    private float timeSinceLastShot;
    private float recoveryTimer;

    public int CurrentAmmo => currentAmmo;

    void Awake()
    {
        if (startFull) currentAmmo = magazineCapacity;
    }

    void OnEnable()
    {
        if (startFull) currentAmmo = magazineCapacity;
        timeSinceLastShot = 0f;
        recoveryTimer = 0f;
    }

    void Update()
    {
        timeSinceLastShot += Time.deltaTime;

        if (currentAmmo < magazineCapacity)
        {
            float interval = GetRecoveryIntervalSeconds();

            if (interval > 0f)
            {
                bool canRecover = true;

                if (pauseRecoveryDuringCooldown)
                    canRecover = timeSinceLastShot >= GetFireIntervalSeconds();

                if (canRecover)
                {
                    recoveryTimer += Time.deltaTime;

                    while (recoveryTimer >= interval && currentAmmo < magazineCapacity)
                    {
                        recoveryTimer -= interval;
                        currentAmmo++;
                    }
                }
            }
        }
    }

    public bool CanFire()
    {
        if (!projectilePrefab) return false;
        if (!firePoint) return false;

        float interval = GetFireIntervalSeconds();
        if (timeSinceLastShot < interval) return false;

        if (currentAmmo < ammoPerShot) return false;

        return true;
    }

    public void FireMissile()
    {
        if (!CanFire()) return;

        // Instantiate missile (NO POOL)
        GameObject missileGO = Instantiate(projectilePrefab, firePoint.position, firePoint.rotation);
        Missile missile = missileGO.GetComponent<Missile>();

        if (missile != null)
        {
            missile.damage = projectileDamage;
            missile.speed = projectileSpeed;
            missile.lifetime = lifetime;
            missile.impactEffectPrefab = impactEffectPrefab;
            missile.targetTag = targetTag;

            missile.SetTarget(radar.currentTarget);
        }

        // Instantiate muzzle flash
        if (muzzleFlashPrefab != null)
        {
            GameObject fx = Instantiate(muzzleFlashPrefab, firePoint.position, firePoint.rotation);
            Destroy(fx, 0.1f);
        }

        timeSinceLastShot = 0f;
        currentAmmo -= ammoPerShot;
    }

    public void FireMissile(Transform target)
    {
        if (!CanFire()) return;
        if (!target) return;

        GameObject missileGO = Instantiate(projectilePrefab, firePoint.position, firePoint.rotation);
        Missile missile = missileGO.GetComponent<Missile>();

        if (missile != null)
        {
            missile.damage = projectileDamage;
            missile.speed = projectileSpeed;
            missile.lifetime = lifetime;
            missile.impactEffectPrefab = impactEffectPrefab;
            missile.targetTag = targetTag;

            missile.SetTarget(target);
        }

        if (muzzleFlashPrefab != null)
        {
            GameObject fx = Instantiate(muzzleFlashPrefab, firePoint.position, firePoint.rotation);
            Destroy(fx, 0.1f);
        }

        timeSinceLastShot = 0f;
        currentAmmo -= ammoPerShot;
    }

    public void Refill()
    {
        currentAmmo = magazineCapacity;
    }

    private float GetFireIntervalSeconds()
    {
        if (fireRate <= 0) return 0;
        return 1f / fireRate;
    }

    private float GetRecoveryIntervalSeconds()
    {
        if (recoveryRatePerSecond > 0)
            return recoveryRatePerSecond;

        return 0f;
    }
}
