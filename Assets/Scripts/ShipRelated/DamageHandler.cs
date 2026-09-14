using AStar3D.ECS;
using System;
using System.Collections;
using UnityEngine;
using UnityEngine.UIElements;
public class DamageHandler : MonoBehaviour
{
    [SerializeField] private bool isDead = false;
    bool invincible = false;
    [Header("Stats")]
    public int maxHealth = 100;
    public int currentHealth = 100;
    public int repairRate = 2; // Health per second
    public int maxShield = 100;
    public int currentShield = 100;
    public int regenerationRate = 5; // Shield per second
    [Header("Regeneration Timers")]
    [Tooltip("Delay before shield starts regenerating after taking damage (seconds)")]
    [NonSerialized] public float shieldRegenDelay = 3f;
    [Tooltip("Delay before health starts repairing after taking damage (seconds)")]
    [NonSerialized] public float healthRegenDelay = 5f;
    private float shieldRegenTimer;
    private float healthRegenTimer;
    [Header("Bounty / UI")]
    public string displayName;
    public int creditReward = 50;
    public int scoreReward = 100;
    // NEW: cache all colliders in this object and its children
    private Collider[] _colliders;
    // NEW: Event for damage taken (passes damage ratio relative to maxHealth)
    public event Action<float> OnDamageTaken;
    [Header("Death Drift")]
    [SerializeField] private float deathDriftSpeed = 10f;
    [SerializeField] private float deathDriftDuration = 6f;
    void Awake()
    {
        RefreshColliders();
        //SetCollidersAsTriggers();
        Debug.Log($"DamageHandler ({gameObject.name}): Awake() - Colliders refreshed and set to triggers.");
    }
    private void OnEnable()
    {
        isDead = false;
    }
    void Start()
    {
        shieldRegenTimer = 0f;
        healthRegenTimer = 0f;
        Debug.Log($"DamageHandler ({gameObject.name}): Start() - Timers initialized.");
    }
    void Update()
    {
        HandleRegeneration(Time.deltaTime);
    }
    // --- NEW: helpers for colliders ---
    private void RefreshColliders()
    {
        // includeInactive:true so we can still find/enable them when reviving
        _colliders = GetComponentsInChildren<Collider>(true);
        Debug.Log($"DamageHandler ({gameObject.name}): RefreshColliders() - Found {_colliders.Length} colliders.");
    }
    private void SetCollidersAsTriggers()
    {
        // If hierarchy changed at runtime, refresh
        if (_colliders == null || _colliders.Length == 0)
            RefreshColliders();
        foreach (var c in _colliders)
            if (c) c.isTrigger = true;
        Debug.Log($"DamageHandler ({gameObject.name}): SetCollidersAsTriggers() - All colliders set to triggers.");
    }
    private void SetCollidersEnabled(bool enabled)
    {
        // If hierarchy changed at runtime, refresh
        if (_colliders == null || _colliders.Length == 0)
            RefreshColliders();
        foreach (var c in _colliders)
            if (c) c.enabled = enabled;
        Debug.Log($"DamageHandler ({gameObject.name}): SetCollidersEnabled({enabled}) - Colliders enabled/disabled.");
    }
    // --- end helpers ---
    public void TakeDamage(int damageAmount)
    {
        // Block invalid or redundant calls
        if (damageAmount <= 0 || invincible || isDead) return;
        Debug.Log($"DamageHandler ({gameObject.name}): TakeDamage({damageAmount}) - before: H={currentHealth}, S={currentShield}");
        shieldRegenTimer = shieldRegenDelay;
        healthRegenTimer = healthRegenDelay;
        // Shield absorbs first
        if (currentShield > 0)
        {
            currentShield -= damageAmount;
            if (currentShield < 0)
            {
                currentHealth += currentShield; // overflow into health
                currentShield = 0;
            }
        }
        else
        {
            currentHealth -= damageAmount;
        }
        // NEW: Invoke event with damage ratio (before death check, so shake happens even if fatal)
        float damageRatio = (float)damageAmount / maxHealth;
        OnDamageTaken?.Invoke(damageRatio);
        Debug.Log($"DamageHandler ({gameObject.name}): After damage: H={currentHealth}, S={currentShield}");
        // Kill only once
        if (currentHealth <= 0 && !isDead)
        {
            Die();
        }
    }
    private float shieldRegenAccumulator = 0f;
    private float healthRegenAccumulator = 0f;
    void HandleRegeneration(float deltaTime)
    {
        if (shieldRegenTimer > 0f) shieldRegenTimer -= deltaTime;
        if (healthRegenTimer > 0f) healthRegenTimer -= deltaTime;

        // --- Shield regeneration ---
        if (shieldRegenTimer <= 0f && currentShield < maxShield && regenerationRate > 0)
        {
            shieldRegenAccumulator += maxShield * (regenerationRate * 0.001f) * deltaTime;
            int regenInt = Mathf.FloorToInt(shieldRegenAccumulator);
            if (regenInt > 0)
            {
                currentShield += regenInt;
                if (currentShield > maxShield) currentShield = maxShield;
                shieldRegenAccumulator -= regenInt; // keep leftover fraction
                Debug.Log($"DamageHandler ({gameObject.name}): Shield regenerated to {currentShield}.");
            }
        }

        // --- Health regeneration ---
        if (healthRegenTimer <= 0f && repairRate > 0 && currentHealth < maxHealth)
        {
            healthRegenAccumulator += maxHealth * (repairRate * 0.001f) * deltaTime;
            int healInt = Mathf.FloorToInt(healthRegenAccumulator);
            if (healInt > 0)
            {
                currentHealth += healInt;
                if (currentHealth > maxHealth) currentHealth = maxHealth;
                healthRegenAccumulator -= healInt; // keep leftover fraction
                Debug.Log($"DamageHandler ({gameObject.name}): Health repaired to {currentHealth}.");
            }
        }
    }
    public bool Invincible
    {
        get => invincible;
    }

    //private void OnTriggerEnter(Collider other)
    //{
    //    if (CompareTag("Enviroment"))
    //    {
    //        TakeDamage(currentHealth);
    //    }
    //}
    public void Die()
    {
        if (isDead) return; // Already dead, skip entirely
        isDead = true; // Mark dead immediately
        // Disable all colliders right away to block further hits
        SetCollidersEnabled(false);
        Debug.Log($"DamageHandler ({gameObject.name}): Die() - Entering Die method.");
        // --- PLAYER DEATH LOGIC ---
        if (CompareTag("Player"))
        {
            GetComponent<Ship>().enabled = false;
            GetComponent<JoystickSpaceshipController>().enabled = false;
            GetComponent<EquipmentManager>().enabled = false;
            GetComponent<Radar>().enabled = false;
            GetComponent<Engine>().enabled = false;
            GameManager.Instance.damageHandler = this;
            GameManager.Instance.FinializeScoresAndCredits();
            //GameManager.Instance.ShowReviveWindow();
            GameManager.Instance.GameOver();
            Debug.Log($"DamageHandler ({gameObject.name}): Die() - Player components disabled, revive window shown.");
            return;
        }
        // --- ENEMY DEATH LOGIC ---
        StartCoroutine(DeathSequence());
    }
    private IEnumerator DeathSequence()
    {
        string nameForLog = string.IsNullOrWhiteSpace(displayName) ? gameObject.name : displayName;
        // Optional: show kill log / feed
        CombatLogUI.Instance?.LogDestroy(nameForLog, creditReward);
        // Update player stats (only once)
        GameManager.Instance.UpdateValues(creditReward, scoreReward);
        Debug.Log($"DamageHandler ({gameObject.name}): DeathSequence() - Starting death sequence.");
        // Disable the AI
        var ai = GetComponent<ShipAI>();
        if (ai != null)
            ai.enabled = false;
        var p = GetComponent<ShipPathfindingBridge>();
        if (p != null)
            p.enabled = false;
        // Disable targeting
        var target = GetComponentInChildren<Targetable>();
        if (target != null)
            target.gameObject.SetActive(false);
        // Capture drift direction (current forward)
        Vector3 driftDirection = transform.forward;
        // Try to get current speed from Engine if available, otherwise use default
        float driftSpeed = deathDriftSpeed;
        var engine = GetComponent<Engine>();
        if (engine != null)
        {
            // Assuming Engine has a public float property like CurrentSpeed or Speed
            // If not, this will need adjustment based on actual Engine script
            driftSpeed = engine.CurrentSpeed; // Replace with actual property name if different
        }
        float elapsed = 0f;
        while (elapsed < deathDriftDuration)
        {
            transform.position += driftDirection * driftSpeed * Time.deltaTime;
            elapsed += Time.deltaTime;
            yield return null;
        }
        // After drift, disable trails and particles
        DisableTrailsAndParticlesInChildren(transform);
        // Release to pool or destroy
        PooledObject pooled = GetComponent<PooledObject>();
        if (pooled != null)
        {
            PoolManager.Release(gameObject);
            Debug.Log($"DamageHandler ({gameObject.name}): DeathSequence() - Released to pool after drift.");
        }
        else
        {
            Destroy(gameObject);
            Debug.Log($"DamageHandler ({gameObject.name}): DeathSequence() - Destroyed after drift.");
        }
    }
    void DisableTrailsAndParticlesInChildren(Transform parent)
    {
        // Get all TrailRenderer components in the current GameObject and its children
        TrailRenderer[] trails = parent.GetComponentsInChildren<TrailRenderer>(true);
        // Disable all TrailRenderers
        foreach (var trail in trails)
        {
            trail.enabled = false;
            Debug.Log($"Disabled trail on: {trail.gameObject.name}");
        }
        // Get all ParticleSystem components in the current GameObject and its children
        ParticleSystem[] particles = parent.GetComponentsInChildren<ParticleSystem>(true);
        // Disable all ParticleSystems
        foreach (var particle in particles)
        {
            particle.gameObject.SetActive(false); // Disables the GameObject containing the particle system
            Debug.Log($"Disabled particle system on: {particle.gameObject.name}");
        }
    }
    public void Revive()
    {
        if (CompareTag("Player"))
        {
            ActivateInvincibility(5f);
            currentHealth = maxHealth;
            currentShield = maxShield;
            GetComponent<EquipmentManager>().Revive();
            GetComponent<Ship>().enabled = true;
            GetComponent<JoystickSpaceshipController>().enabled = true;
            // OLD: GetComponent<Collider>().enabled = true;
            // NEW: enable ALL colliders in children (works even if some are inactive)
            SetCollidersEnabled(true);
            GetComponent<EquipmentManager>().enabled = true;
            GetComponent<Radar>().enabled = true;
            GetComponent<Engine>().enabled = true;
            isDead = false;
            Debug.Log($"DamageHandler ({gameObject.name}): Revive() - Player components enabled.");
            
            return;
        }
    }
    public void EquipShield(int maxShield, int regenerationRate)
    {
        this.maxShield = maxShield;
        this.regenerationRate = regenerationRate;
        currentShield = maxShield;
        Debug.Log($"DamageHandler ({gameObject.name}): EquipShield (maxShield={maxShield}, regen={regenerationRate})");
    }
    public void EquipRepairModule(int repairRate)
    {
        this.repairRate = repairRate;
        Debug.Log($"DamageHandler ({gameObject.name}): EquipRepairModule (repairRate={repairRate})");
    }
    public void InitializeHealth(int health)
    {
        this.maxHealth = health;
        this.currentHealth = Mathf.Clamp(health, 0, maxHealth);
        Debug.Log($"DamageHandler ({gameObject.name}): InitializeHealth - Health set to {health}");
    }
    public void ActivateInvincibility(float duration)
    {
        StartCoroutine(BlinkWhileInvincible(5f));
        invincible = true;
        Debug.Log("Player is now invincible!");
        StartCoroutine(DisableInvincibilityAfterTime(duration));
    }
    private IEnumerator DisableInvincibilityAfterTime(float duration)
    {
        yield return new WaitForSeconds(duration); // Wait 'duration' seconds
        invincible = false;
        Debug.Log("Invincibility ended!");
    }
    public float CurrentHealth => currentHealth;
    public void ApplyHeal(int heal)
    {
        if (currentHealth + heal > maxHealth)
        {
            currentHealth = maxHealth;
        }
        else
            currentHealth += heal;
    }
    public void ApplyShield(int shield)
    {
        if (currentShield + shield > maxShield)
            currentShield = maxShield;
        else
            currentShield += shield;
    }

    public Collider[] GetColliders()
    {
        return _colliders;
    }

    private IEnumerator BlinkWhileInvincible(float duration)
    {
        MeshRenderer[] renderers = GetComponentsInChildren<MeshRenderer>(true);

        float endTime = Time.time + duration;
        float blinkInterval = 0.15f;

        while (Time.time < endTime)
        {
            // Toggle visibility
            foreach (var r in renderers)
                r.enabled = !r.enabled;

            yield return new WaitForSeconds(blinkInterval);
        }

        // After invincibility ends → make sure renderers are ON
        foreach (var r in renderers)
            r.enabled = true;
    }
}