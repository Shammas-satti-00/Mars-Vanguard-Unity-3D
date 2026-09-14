using UnityEngine;
using System.Collections.Generic;
using System;

public class EquipmentManager : MonoBehaviour
{
    public Ship ship;

    public int regenerationRate;

    public Engine equippedEngine;
    public Pilot equippedPilot;
    public Shield equippedShield;
    public RepairModule equippedRepairModule;
    public Radar equippedRadar;

    [SerializeField] public CannonSlot[] cannonSlots;
    [SerializeField] public LauncherSlot[] launcherSlots;

    public void InitializeEquipment()
    {
        ship = GetComponent<Ship>();
        GetAllCannonsFromShip();
        GetAllLauncherFromShips();
        equippedPilot = GetComponent<Pilot>();
        InitializePilot();
        equippedShield = GetComponent<Shield>();
        InitializeShield();
        equippedRepairModule = GetComponent<RepairModule>();
        InitializeRepairModule();
        equippedEngine = GetComponent<Engine>();
        equippedRadar = GetComponent<Radar>();
    }

    public bool InitializeEquipmentAI(EnemyClassData enemyClassData)
    {
        ship = GetComponent<Ship>();
        equippedPilot = GetComponent<Pilot>();
        equippedShield = GetComponent<Shield>();
        equippedRepairModule = GetComponent<RepairModule>();
        equippedEngine = GetComponent<Engine>();
        equippedEngine.moveSpeed = enemyClassData.moveSpeed;
        equippedRadar = GetComponent<Radar>();
        InitializePilot();
        InitializeShield();
        InitializeRepairModule();
        GetAllCannonsFromShip();
        GetAllLauncherFromShips();
        if (enemyClassData == null)
        {
            Debug.LogWarning("EnemyClassData is null in InitializeEquipmentAI on " + gameObject.name);
            return false;
        }

        DamageHandler damageHandler = GetComponent<DamageHandler>();
        if (damageHandler != null)
        {
            damageHandler.maxHealth = enemyClassData.health;
            damageHandler.currentHealth = enemyClassData.health;
            damageHandler.repairRate = Mathf.RoundToInt(enemyClassData.healthRegenPerSecond);
        }
        else
        {
            Debug.LogError("DamageHandler component not found in InitializeEquipmentAI on " + gameObject.name);
        }

        if (equippedShield != null)
        {
            equippedShield.maxShield = enemyClassData.shield;
            equippedShield.regenerationRate = Mathf.RoundToInt(enemyClassData.shieldRegenPerSecond);
            InitializeShield();
            Debug.Log($"Applied Shield Data: MaxShield={enemyClassData.shield}, RegenRate={equippedShield.regenerationRate} on {gameObject.name}");
        }
        else
        {
            Debug.LogWarning("No Shield component found; setting shield directly on DamageHandler on " + gameObject.name);
            if (damageHandler != null)
            {
                damageHandler.maxShield = enemyClassData.shield;
                damageHandler.currentShield = enemyClassData.shield;
                damageHandler.regenerationRate = Mathf.RoundToInt(enemyClassData.shieldRegenPerSecond);
            }
        }

        if (equippedRepairModule != null)
        {
            equippedRepairModule.repairRate = Mathf.RoundToInt(enemyClassData.healthRegenPerSecond);
            InitializeRepairModule();
            Debug.Log($"Applied Repair Data: RepairRate={equippedRepairModule.repairRate} on {gameObject.name}");
        }
        else
        {
            Debug.LogWarning("No RepairModule component found; repair rate already set on DamageHandler on " + gameObject.name);
        }

        CannonSlot[] cannons = GetAllCannonSlots();
        if (cannons != null)
        {
            foreach (var cannon in cannons)
            {
                cannon.projectileDamage = enemyClassData.cannonDamage;
                cannon.targetTag = enemyClassData.targetTag;
                //cannon.projectilePrefab = DataHolder.Instance.projectilePrefabs[cannon.projectilePrefabIndex];
                cannon.muzzleFlashPrefab = DataHolder.Instance.muzzleFlashPrefab[cannon.muzzleFlashPrefabIndex];
                cannon.impactEffectPrefab = DataHolder.Instance.impactEffectPrefab[cannon.impactEffectPrefabIndex];
                cannon.currentAmmo = cannon.magazineCapacity;
            }
        }
        else
        {
            Debug.LogWarning("No cannon slots found in InitializeEquipmentAI on " + gameObject.name);
        }

        LauncherSlot[] launchers = GetAllLauncherSlots();
        if (launchers != null)
        {
            foreach (var launcher in launchers)
            {
                launcher.projectileDamage = enemyClassData.missileDamage;
                launcher.projectileSpeed = enemyClassData.missileSpeed;
                launcher.targetTag = enemyClassData.targetTag;
                launcher.projectilePrefab = DataHolder.Instance.projectilePrefabs[launcher.projectilePrefabIndex];
                launcher.muzzleFlashPrefab = DataHolder.Instance.muzzleFlashPrefab[launcher.muzzleFlashPrefabIndex];
                launcher.impactEffectPrefab = DataHolder.Instance.impactEffectPrefab[launcher.impactEffectPrefabIndex];
                launcher.currentAmmo = launcher.magazineCapacity;
            }
            Debug.Log($"Applied Launcher Data: Damage={enemyClassData.missileDamage}, Speed={enemyClassData.missileSpeed} on {gameObject.name}");
        }
        else
        {
            Debug.LogWarning("No launcher slots found in InitializeEquipmentAI on " + gameObject.name);
        }

        return true;
    }

    public void InitializePilot()
    {
        // Apply Pilot buffs instead of Core energy
        if (equippedPilot == null) return;

        // Engine buffs
        if (equippedEngine != null)
        {
            equippedEngine.moveSpeed += equippedPilot.moveSpeedBuff;
            equippedEngine.boostMultiplier += equippedPilot.boostMultiplierBuff;
        }

        // Radar lock-on reduction (lockOnBuff is a fraction: 0.10 = 10% faster)
        if (equippedRadar != null)
        {
            equippedRadar.lockOnTime = Mathf.Max(0.1f, equippedRadar.lockOnTime * (1f - equippedPilot.lockOnBuff));
        }

        // Ship survivability buffs
        if (ship?.damageHandler != null)
        {
            ship.damageHandler.maxHealth += equippedPilot.additionalHealthBuff;
            ship.damageHandler.currentHealth = Mathf.Min(
                ship.damageHandler.currentHealth + equippedPilot.additionalHealthBuff,
                ship.damageHandler.maxHealth
            );

            ship.damageHandler.maxShield += equippedPilot.additionalShieldBuff;
            ship.damageHandler.regenerationRate += equippedPilot.additionalShieldRegnBuff; // shield regen
            ship.damageHandler.repairRate += equippedPilot.additionalHealthRegnBuff;       // health regen
        }

        // Weapon damage buffs
        for (int i = 0; i < cannonSlots.Length; i++)
            cannonSlots[i].projectileDamage += equippedPilot.additionalCannonDamage;

        for (int i = 0; i < launcherSlots.Length; i++)
            launcherSlots[i].projectileDamage += equippedPilot.additionalLauncherDamage;
    }

    public void InitializeShield()
    {
        if (ship?.damageHandler != null)
        {
            ship.damageHandler.EquipShield(equippedShield != null ? equippedShield.maxShield : 0, equippedShield != null ? equippedShield.regenerationRate : 0);
        }
    }

    public void InitializeRepairModule()
    {
        if (ship?.damageHandler != null)
        {
            ship.damageHandler.EquipRepairModule(equippedRepairModule != null ? equippedRepairModule.repairRate : 0);
        }
    }

    public void LoadEquipmentData()
    {
        // Log the equipment name being loaded
        string name = PlayerPrefs.GetString("Selected_Ship_Name");
        Debug.Log("Loading equipment data for ship: " + name);

        // Log the equipment for each component
        string engine = PlayerPrefs.GetString("Ship_" + name + "_engine");
        Debug.Log("Loaded engine: " + engine);
        string repair = PlayerPrefs.GetString("Ship_" + name + "_repair");
        Debug.Log("Loaded repair module: " + repair);
        string pilot = PlayerPrefs.GetString("Ship_" + name + "_pilot");
        Debug.Log("Loaded core: " + pilot);
        string shield = PlayerPrefs.GetString("Ship_" + name + "_shield");
        Debug.Log("Loaded shield: " + shield);
        string radar = PlayerPrefs.GetString("Ship_" + name + "_radar");
        Debug.Log("Loaded radar: " + radar);
        string cannon = PlayerPrefs.GetString("Ship_" + name + "_cannon");
        Debug.Log("Loaded cannon: " + cannon);
        string launcher = PlayerPrefs.GetString("Ship_" + name + "_launcher");
        Debug.Log("Loaded launcher: " + launcher);

        ship.maxHealth = PlayerPrefs.GetInt("Ship_" + name + "_maxHealth");
        Debug.Log("Loaded max health: " + ship.maxHealth);

        equippedRepairModule.repairRate = PlayerPrefs.GetInt("RepairModule_" + repair + "_repairRate");
        Debug.Log("Loaded repair rate: " + equippedRepairModule.repairRate);
        ship.damageHandler.InitializeHealth(ship.maxHealth);

        // Pilot: load absolute buff values from PlayerPrefs (no UpgradePilot involved)
        equippedPilot.additionalCannonDamage = PlayerPrefs.GetInt("Pilot_" + pilot + "_additionalCannonDamage");
        Debug.Log("Loaded pilot additional cannon damage: " + equippedPilot.additionalCannonDamage);

        equippedPilot.additionalLauncherDamage = PlayerPrefs.GetInt("Pilot_" + pilot + "_additionalLauncherDamage");
        Debug.Log("Loaded pilot additional launcher damage: " + equippedPilot.additionalLauncherDamage);

        equippedPilot.lockOnBuff = PlayerPrefs.GetFloat("Pilot_" + pilot + "_lockOnBuff");
        Debug.Log("Loaded pilot lock-on buff (fraction): " + equippedPilot.lockOnBuff);

        equippedPilot.moveSpeedBuff = PlayerPrefs.GetInt("Pilot_" + pilot + "_moveSpeedBuff");
        Debug.Log("Loaded pilot move speed buff: " + equippedPilot.moveSpeedBuff);

        equippedPilot.boostMultiplierBuff = PlayerPrefs.GetFloat("Pilot_" + pilot + "_boostMultiplierBuff");
        Debug.Log("Loaded pilot boost multiplier buff (fraction): " + equippedPilot.boostMultiplierBuff);

        equippedPilot.additionalHealthBuff = PlayerPrefs.GetInt("Pilot_" + pilot + "_additionalHealthBuff");
        Debug.Log("Loaded pilot additional health buff: " + equippedPilot.additionalHealthBuff);

        equippedPilot.additionalShieldBuff = PlayerPrefs.GetInt("Pilot_" + pilot + "_additionalShieldBuff");
        Debug.Log("Loaded pilot additional shield buff: " + equippedPilot.additionalShieldBuff);

        equippedPilot.additionalHealthRegnBuff = PlayerPrefs.GetInt("Pilot_" + pilot + "_additionalHealthRegnBuff");
        Debug.Log("Loaded pilot additional health regen buff: " + equippedPilot.additionalHealthRegnBuff);

        equippedPilot.additionalShieldRegnBuff = PlayerPrefs.GetInt("Pilot_" + pilot + "_additionalShieldRegnBuff");
        Debug.Log("Loaded pilot additional shield regen buff: " + equippedPilot.additionalShieldRegnBuff);

        equippedShield.maxShield = PlayerPrefs.GetInt("Shield_" + shield + "_maxShield");
        Debug.Log("Loaded shield max value: " + equippedShield.maxShield);
        equippedShield.regenerationRate = PlayerPrefs.GetInt("Shield_" + shield + "_regenerationRate");
        Debug.Log("Loaded shield regeneration rate: " + equippedShield.regenerationRate);

        InitializeShield();
        InitializeRepairModule();

        equippedEngine.moveSpeed = PlayerPrefs.GetFloat("Engine_" + engine + "_moveSpeed");
        equippedEngine.baseMoveSpeed = equippedEngine.moveSpeed;
        Debug.Log("Loaded engine move speed: " + equippedEngine.moveSpeed);
        equippedEngine.boostMultiplier = PlayerPrefs.GetFloat("Engine_" + engine + "_boostMultiplier");
        Debug.Log("Loaded engine boost multiplier: " + equippedEngine.boostMultiplier);
        equippedEngine.boostDuration = PlayerPrefs.GetFloat("Engine_" + engine + "_boostDuration");
        Debug.Log("Loaded engine boost duration: " + equippedEngine.boostDuration);
        equippedRadar.detectionRange = PlayerPrefs.GetInt("Radar_" + radar + "_detectionRange");
        Debug.Log("Loaded radar detection range: " + equippedRadar.detectionRange);
        equippedRadar.lockOnTime = PlayerPrefs.GetFloat("Radar_" + radar + "_lockOnTime");
        Debug.Log("Loaded radar lock-on time: " + equippedRadar.lockOnTime);

        // Log the loaded values for cannon slots
        for (int i = 0; i < cannonSlots.Length; i++)
        {
            Debug.Log("Loading cannon slot " + i + " data:");
            cannonSlots[i].projectileDamage = PlayerPrefs.GetInt("Cannon_" + cannon + "_projectileDamage");
            Debug.Log("Cannon projectile damage: " + cannonSlots[i].projectileDamage);
            //cannonSlots[i].projectileSpeed = PlayerPrefs.GetInt("Cannon_" + cannon + "_projectileSpeed");
            //Debug.Log("Cannon projectile speed: " + cannonSlots[i].projectileSpeed);
            cannonSlots[i].fireRate = PlayerPrefs.GetInt("Cannon_" + cannon + "_fireRate");
            Debug.Log("Cannon fire rate: " + cannonSlots[i].fireRate);
            cannonSlots[i].lifetime = PlayerPrefs.GetFloat("Cannon_" + cannon + "_lifetime");
            Debug.Log("Cannon lifetime: " + cannonSlots[i].lifetime);
            cannonSlots[i].magazineCapacity = PlayerPrefs.GetInt("Cannon_" + cannon + "_magazineCapacity");
            Debug.Log("Cannon magazine capacity: " + cannonSlots[i].magazineCapacity);
            cannonSlots[i].ammoPerShot = PlayerPrefs.GetInt("Cannon_" + cannon + "_ammoPerShot");
            Debug.Log("Cannon ammo per shot: " + cannonSlots[i].ammoPerShot);
            cannonSlots[i].recoveryRatePerSecond = PlayerPrefs.GetInt("Cannon_" + cannon + "_recoveryRatePerSecond");
            Debug.Log("Cannon recovery rate per second: " + cannonSlots[i].recoveryRatePerSecond);
            cannonSlots[i].recoveryDelay = PlayerPrefs.GetInt("Cannon_" + cannon + "_recoveryDelay");
            Debug.Log("Cannon recovery delay: " + cannonSlots[i].recoveryDelay);
            //cannonSlots[i].projectilePrefab = DataHolder.Instance.projectilePrefabs[PlayerPrefs.GetInt("Cannon_" + cannon + "_projectilePrefabIndex")];
            cannonSlots[i].muzzleFlashPrefab = DataHolder.Instance.muzzleFlashPrefab[PlayerPrefs.GetInt("Cannon_" + cannon + "_muzzleFlashPrefabIndex")];
            cannonSlots[i].impactEffectPrefab = DataHolder.Instance.impactEffectPrefab[PlayerPrefs.GetInt("Cannon_" + cannon + "_impactEffectPrefabIndex")];
            cannonSlots[i].currentAmmo = cannonSlots[i].magazineCapacity;
        }

        // Log the loaded values for launcher slots
        for (int i = 0; i < launcherSlots.Length; i++)
        {
            Debug.Log("Loading launcher slot " + i + " data:");
            launcherSlots[i].projectileDamage = PlayerPrefs.GetInt("Launcher_" + launcher + "_projectileDamage");
            Debug.Log("Launcher projectile damage: " + launcherSlots[i].projectileDamage);
            launcherSlots[i].projectileSpeed = PlayerPrefs.GetInt("Launcher_" + launcher + "_projectileSpeed");
            Debug.Log("Launcher projectile speed: " + launcherSlots[i].projectileSpeed);
            launcherSlots[i].fireRate = PlayerPrefs.GetInt("Launcher_" + launcher + "_fireRate");
            Debug.Log("Launcher fire rate: " + launcherSlots[i].fireRate);
            launcherSlots[i].lifetime = PlayerPrefs.GetFloat("Launcher_" + launcher + "_lifetime");
            Debug.Log("Launcher lifetime: " + launcherSlots[i].lifetime);
            launcherSlots[i].magazineCapacity = PlayerPrefs.GetInt("Launcher_" + launcher + "_magazineCapacity");
            Debug.Log("Launcher magazine capacity: " + launcherSlots[i].magazineCapacity);
            launcherSlots[i].ammoPerShot = PlayerPrefs.GetInt("Launcher_" + launcher + "_ammoPerShot");
            Debug.Log("Launcher ammo per shot: " + launcherSlots[i].ammoPerShot);
            launcherSlots[i].recoveryRatePerSecond = PlayerPrefs.GetInt("Launcher_" + launcher + "_recoveryRatePerSecond");
            Debug.Log("Launcher recovery rate per second: " + launcherSlots[i].recoveryRatePerSecond);
            launcherSlots[i].projectilePrefab = DataHolder.Instance.projectilePrefabs[PlayerPrefs.GetInt("Launcher_" + launcher + "_projectilePrefabIndex")];
            launcherSlots[i].muzzleFlashPrefab = DataHolder.Instance.muzzleFlashPrefab[PlayerPrefs.GetInt("Launcher_" + launcher + "_muzzleFlashPrefabIndex")];
            launcherSlots[i].impactEffectPrefab = DataHolder.Instance.impactEffectPrefab[PlayerPrefs.GetInt("Launcher_" + launcher + "_impactEffectPrefabIndex")];
            launcherSlots[i].currentAmmo = launcherSlots[i].magazineCapacity;
        }
    }

    public void FireAllCannons()
    {
        Debug.Log($"EquipmentManager ({ship?.name}): Firing all cannons. Total slots: {cannonSlots?.Length}");
        int firedCount = 0;
        foreach (var slot in cannonSlots)
        {
            Debug.Log($"EquipmentManager ({ship?.name}): Firing cannon in slot '{slot.name}'");
            slot.FireCannon();
            firedCount++;
        }
        Debug.Log($"EquipmentManager ({ship?.name}): Fired {firedCount} cannon(s).");
    }

    public void FireAllLaunchers()
    {
        foreach (var slot in launcherSlots)
        {

            slot.FireMissile();

        }
    }
    public void FireAllLaunchers(Transform target)
    {
        foreach (var slot in launcherSlots)
        {
            slot.FireMissile(target);
        }
    }

    public void ActivateBoost()
    {
        if (equippedEngine != null)
        {
            equippedEngine.ActivateBoost();
            Debug.Log($"EquipmentManager ({ship?.name}): Boost activated on engine '{equippedEngine?.label}'");
        }
        else Debug.LogWarning($"EquipmentManager ({ship?.name}): No engine equipped - cannot activate boost.");
    }

    public void DeactivateBoost()
    {
        if (equippedEngine != null)
        {
            equippedEngine.DeactivateBoost();
            Debug.Log($"EquipmentManager ({ship?.name}): Boost deactivated on engine '{equippedEngine?.label}'");
        }
        else Debug.LogWarning($"EquipmentManager ({ship?.name}): No engine equipped - cannot deactivate boost.");
    }

    public CannonSlot[] GetAllCannonSlots()
    {
        return cannonSlots;
    }

    public LauncherSlot[] GetAllLauncherSlots()
    {
        return launcherSlots;
    }

    public CannonSlot GetPrimaryCannonSlot()
    {
        var list = GetAllCannonSlots();
        return (list != null && list.Length > 0) ? list[0] : null;
    }

    public LauncherSlot GetPrimaryLauncherSlot()
    {
        var list = GetAllLauncherSlots();
        return (list != null && list.Length > 0) ? list[0] : null;
    }

    void GetAllCannonsFromShip()
    {
        // Get only active GameObjects’ components
        CannonSlot[] foundCannons = GetComponentsInChildren<CannonSlot>();

        // Filter out disabled CannonSlots
        cannonSlots = System.Array.FindAll(foundCannons, c => c.enabled);

        // Determine target tag
        string t = (tag == "Player") ? "EnemyCollider" :
                   (tag == "EnemyCollider") ? "PlayerCollider" : "";

        // Assign target tag
        foreach (var cannon in cannonSlots)
            cannon.targetTag = t;
    }

    void GetAllLauncherFromShips()
    {
        // Get only active GameObjects’ components
        LauncherSlot[] foundLaunchers = GetComponentsInChildren<LauncherSlot>();

        // Filter out disabled Launchers
        launcherSlots = System.Array.FindAll(foundLaunchers, l => l.enabled);

        // Determine target tag
        string t = (tag == "Player") ? "EnemyCollider" :
                   (tag == "Enemy") ? "PlayerCollider" : "";
        var radar = GetComponent<Radar>();
        // Assign target tag
        foreach (var launcher in launcherSlots)
        {
            launcher.targetTag = t;
            launcher.radar = radar;
        }

    }

    public void Revive()
    {
        for (int i = 0; i < cannonSlots.Length; i++)
        {
            cannonSlots[i].Refill();
        }
        for (int i = 0; i < launcherSlots.Length; i++)
        {
            launcherSlots[i].currentAmmo = launcherSlots[i].magazineCapacity;
        }

    }

    public void AddAmmoToAllCannons(int amount)
    {
        foreach (var cannon in cannonSlots)
        {
            if (cannon.currentAmmo + amount <= cannon.magazineCapacity)  // Assuming 'maxAmmo' is the maximum ammo the launcher can hold
            {
                cannon.currentAmmo += amount;  // Add the amount to the current ammo
            }
            else
            {
                cannon.currentAmmo = cannon.magazineCapacity;  // If adding the amount exceeds max ammo, set currentAmmo to maxAmmo
            }
        }
    }

    public void AddAmmoToAllLaunchers(int amount)
    {
        foreach (var launcher in launcherSlots)
        {
            if (launcher.currentAmmo + amount <= launcher.magazineCapacity)  // Assuming 'maxAmmo' is the maximum ammo the launcher can hold
            {
                launcher.currentAmmo += amount;  // Add the amount to the current ammo
            }
            else
            {
                launcher.currentAmmo = launcher.magazineCapacity;  // If adding the amount exceeds max ammo, set currentAmmo to maxAmmo
            }
        }
    }

    // public void ToggleLockOnAllCannons()
    // {
    //     foreach (var slot in cannonSlots)
    //     {
    //         slot.lockOn = !slot.lockOn;
    //         Debug.Log($"ToggleLockOnAllCannons: Slot '{slot.name}' lockOn set to {slot.lockOn}");
    //     }
    // }
}