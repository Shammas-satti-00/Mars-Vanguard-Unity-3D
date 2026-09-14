using System;
using System.Reflection.Emit;
using TMPro;
using UnityEngine;
using UnityEngine.Rendering;

public class PlayerPrefsManager : MonoBehaviour
{

    public static PlayerPrefsManager Instance;
    private const string InitKey = "__GameInitialized__";
    public void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        DontDestroyOnLoad(gameObject);
    }
    public void Start()
    {
        Initalize();
    }
    bool firstTime = false;
    public void Initalize()
    {
        firstTime = PlayerPrefs.GetInt(InitKey, 0) == 0;
        //if (firstTime)
        //{
        //    Debug.LogError("It is FirstTime");
        //}
        Debug.Log($"PlayerPrefsManager: firstTime = {firstTime}");

        if (firstTime)
        {
            PlayerPrefs.SetInt("V-Coins", 9000);
            PlayerPrefs.Save();
            Debug.Log("PlayerPrefsManager: First time setup detected. Saving all data...");
            SaveAllShipData();
            SaveAllEngineData();
            SaveAllShieldData();
            SaveAllPilotData();
            SaveAllRepairModuleData();
            SaveAllRadarData();
            SaveAllCannonSlotData();
            SaveAllLauncherSlotData();
            UnlockInitialEquipment();
            PlayerPrefs.SetInt(InitKey, 1);
            PlayerPrefs.SetString("Selected_Ship_Name", DataHolder.Instance.avaliableShips[0]._name);
            PlayerPrefs.SetFloat("Sensitivity", 1.2f);
            PlayerPrefs.SetFloat("MusicVolume", 0.2f);
            PlayerPrefs.SetFloat("SoundVolume", 1f);
            PlayerPrefs.Save();
            Debug.Log("PlayerPrefsManager: Initial save complete.");
        }

        PlayerPrefs.Save();
    }

    public void LoadData()
    {
        Debug.Log("PlayerPrefsManager: LoadData() called.");
    }

    public void SaveData()
    {
        Debug.Log("PlayerPrefsManager: SaveData() called.");
    }

    void UnlockInitialEquipment()
    {
        Debug.Log("PlayerPrefsManager: Unlocking initial equipment...");

        PlayerPrefs.SetInt("Ship_" + DataHolder.Instance.avaliableShips[0]._name + "_isUnlocked", 1);
        PlayerPrefs.SetString("Selected_Ship_Name", DataHolder.Instance.avaliableShips[0]._name);
        PlayerPrefs.SetInt("Engine_" + DataHolder.Instance.avaliableEngines[0].label + "_isUnlocked", 1);
        PlayerPrefs.SetInt("Shield_" + DataHolder.Instance.avaliableShields[0].label + "_isUnlocked", 1);
        PlayerPrefs.SetInt("Piolt_" + DataHolder.Instance.avaliablePilots[0].label + "_isUnlocked", 1);
        PlayerPrefs.SetInt("RepairModule_" + DataHolder.Instance.repairModules[0].label + "_isUnlocked", 1);
        PlayerPrefs.SetInt("Radar_" + DataHolder.Instance.avaliableRadars[0].label + "_isUnlocked", 1);
        PlayerPrefs.SetInt("Cannon_" + DataHolder.Instance.avaliableCannons[0].label + "_isUnlocked", 1);
        PlayerPrefs.SetInt("Launcher_" + DataHolder.Instance.avaliableLaunchers[0].label + "_isUnlocked", 1);
        PlayerPrefs.Save();
        Debug.Log("PlayerPrefsManager: Initial equipment unlocked successfully.");
    }
    void SaveAllShipData()
    {
        Debug.Log("PlayerPrefsManager: Saving all Ship data...");
        for (int i = 0; i < DataHolder.Instance.avaliableShips.Length; i++)
        {
            string name = DataHolder.Instance.avaliableShips[i]._name;
            Debug.Log($"Saving Ship {name}");

            PlayerPrefs.SetInt("Ship_" + name + "_isUnlocked", 0);
            PlayerPrefs.SetInt("Ship_" + name + "_totalCannons", DataHolder.Instance.avaliableShips[i].totalCannons);
            PlayerPrefs.SetInt("Ship_" + name + "_totalLaunchers", DataHolder.Instance.avaliableShips[i].totalLaunchers);
            PlayerPrefs.SetInt("Ship_" + name + "_maxHealth", DataHolder.Instance.avaliableShips[i].maxHealth);
            PlayerPrefs.SetInt("Ship_" + name + "_spriteIndex", DataHolder.Instance.avaliableShips[i].spriteIndex);
            

            EquipmentManager em = DataHolder.Instance.avaliableShips[i].GetComponent<EquipmentManager>();
            if (em == null)
            {
                Debug.LogWarning($"Ship {name} has no EquipmentManager attached!");
                continue;
            }

            // Set engine, log the ship name and engine name
            string engineLabel = DataHolder.Instance.avaliableEngines[0].label;
            PlayerPrefs.SetString("Ship_" + name + "_engine", engineLabel);
            Debug.Log("Equipped Engine to Ship: " + name + " - Engine: " + engineLabel);

            // Set core, log the ship name and core name
            string coreLabel = DataHolder.Instance.avaliablePilots[0].label;
            PlayerPrefs.SetString("Ship_" + name + "_pilot", coreLabel);
            Debug.Log("Equipped Core to Ship: " + name + " - Core: " + coreLabel);

            // Set shield, log the ship name and shield name
            string shieldLabel = DataHolder.Instance.avaliableShields[0].label;
            PlayerPrefs.SetString("Ship_" + name + "_shield", shieldLabel);
            Debug.Log("Equipped Shield to Ship: " + name + " - Shield: " + shieldLabel);

            // Set radar, log the ship name and radar name
            string radarLabel = DataHolder.Instance.avaliableRadars[0].label;
            PlayerPrefs.SetString("Ship_" + name + "_radar", radarLabel);
            Debug.Log("Equipped Radar to Ship: " + name + " - Radar: " + radarLabel);

            // Set repair module, log the ship name and repair module name
            string repairLabel = DataHolder.Instance.repairModules[0].label;
            PlayerPrefs.SetString("Ship_" + name + "_repair", repairLabel);
            Debug.Log("Equipped Repair Module to Ship: " + name + " - Repair Module: " + repairLabel);

            // Set cannon, log the ship name and cannon name
            string cannonLabel = DataHolder.Instance.avaliableCannons[0].label;
            PlayerPrefs.SetString("Ship_" + name + "_cannon", cannonLabel);
            Debug.Log("Equipped Cannon to Ship: " + name + " - Cannon: " + cannonLabel);

            // Set launcher, log the ship name and launcher name
            string launcherLabel = DataHolder.Instance.avaliableLaunchers[0].label;
            PlayerPrefs.SetString("Ship_" + name + "_launcher", launcherLabel);
            Debug.Log("Equipped Launcher to Ship: " + name + " - Launcher: " + launcherLabel);

            // Save all preferences
            PlayerPrefs.Save();
            Debug.Log("PlayerPrefs saved.");

        }
    }

    void SaveAllEngineData()
    {
        Debug.Log("PlayerPrefsManager: Saving all Engine data...");
        PlayerPrefs.SetInt("TotalEngines", DataHolder.Instance.avaliableEngines.Length);
        for (int i = 0; i < DataHolder.Instance.avaliableEngines.Length; i++)
        {
            string name = DataHolder.Instance.avaliableEngines[i].label;
            Debug.Log($"Saving Engine {name}");

            PlayerPrefs.SetInt("Engine_" + name + "_isUnlocked", 0);
            PlayerPrefs.SetFloat("Engine_" + name + "_moveSpeed", DataHolder.Instance.avaliableEngines[i].moveSpeed);
            PlayerPrefs.SetFloat("Engine_" + name + "_boostMultiplier", DataHolder.Instance.avaliableEngines[i].boostMultiplier);
            PlayerPrefs.SetFloat("Engine_" + name + "_boostDuration", DataHolder.Instance.avaliableEngines[i].boostDuration);
            PlayerPrefs.SetInt("Engine_" + name + "_unlimitedBoost", DataHolder.Instance.avaliableEngines[i].unlimitedBoost ? 1 : 0);
            PlayerPrefs.Save();
        }
    }

    void SaveAllShieldData()
    {
        Debug.Log("PlayerPrefsManager: Saving all Shield data...");
        PlayerPrefs.SetInt("TotalShields", DataHolder.Instance.avaliableShields.Length);
        for (int i = 0; i < DataHolder.Instance.avaliableShields.Length; i++)
        {
            string name = DataHolder.Instance.avaliableShields[i].label;
            Debug.Log($"Saving Shield {name}");

            PlayerPrefs.SetInt("Shield_" + name + "_isUnlocked", 0);
            PlayerPrefs.SetInt("Shield_" + name + "_maxShield", DataHolder.Instance.avaliableShields[i].maxShield);
            PlayerPrefs.SetInt("Shield_" + name + "_regenerationRate", DataHolder.Instance.avaliableShields[i].regenerationRate);
            PlayerPrefs.Save();
        }
    }

    void SaveAllPilotData()
    {
        Debug.Log("PlayerPrefsManager: Saving all Pilot data...");

        var pilots = DataHolder.Instance.avaliablePilots;
        if (pilots == null)
        {
            Debug.LogWarning("No pilots array found on DataHolder.");
            return;
        }

        PlayerPrefs.SetInt("TotalPilots", pilots.Length);

        for (int i = 0; i < pilots.Length; i++)
        {
            var p = pilots[i];
            string name = p != null ? p.label : $"Pilot_{i}";  // Safe label fallback

            Debug.Log($"Saving Pilot {name}");

            // Save basic unlock status (default unlocked is 0, locked = 1)
            PlayerPrefs.SetInt("Pilot_" + name + "_isUnlocked", 0); // Default locked
            PlayerPrefs.SetInt("Pilot_" + name + "_level", 1); // Default starting level

            // Save Pilot stats (buffs, damage, etc.)
            PlayerPrefs.SetInt("Pilot_" + name + "_additionalCannonDamage", p.additionalCannonDamage);
            PlayerPrefs.SetInt("Pilot_" + name + "_additionalLauncherDamage", p.additionalLauncherDamage);
            PlayerPrefs.SetFloat("Pilot_" + name + "_lockOnBuff", p.lockOnBuff);  // Save as fraction (float)
            PlayerPrefs.SetInt("Pilot_" + name + "_moveSpeedBuff", p.moveSpeedBuff);
            PlayerPrefs.SetFloat("Pilot_" + name + "_boostMultiplierBuff", p.boostMultiplierBuff);  // Save as fraction (float)
            PlayerPrefs.SetInt("Pilot_" + name + "_additionalHealthBuff", p.additionalHealthBuff);
            PlayerPrefs.SetInt("Pilot_" + name + "_additionalShieldBuff", p.additionalShieldBuff);
            PlayerPrefs.SetInt("Pilot_" + name + "_additionalHealthRegnBuff", p.additionalHealthRegnBuff);
            PlayerPrefs.SetInt("Pilot_" + name + "_additionalShieldRegnBuff", p.additionalShieldRegnBuff);

            // Ensure we save
            PlayerPrefs.Save();
        }
    }


    void SaveAllRepairModuleData()
    {
        Debug.Log("PlayerPrefsManager: Saving all RepairModule data...");
        PlayerPrefs.SetInt("TotalRepairModules", DataHolder.Instance.repairModules.Length);
        for (int i = 0; i < DataHolder.Instance.repairModules.Length; i++)
        {
            string name = DataHolder.Instance.repairModules[i].label;
            Debug.Log($"Saving RepairModule {name}");

            PlayerPrefs.SetInt("RepairModule_" + name + "_isUnlocked", 0);
            PlayerPrefs.SetInt("RepairModule_" + name + "_repairRate", DataHolder.Instance.repairModules[i].repairRate);
            PlayerPrefs.Save();
        }
    }

    void SaveAllRadarData()
    {
        Debug.Log("PlayerPrefsManager: Saving all Radar data...");
        PlayerPrefs.SetInt("TotalRadars", DataHolder.Instance.avaliableRadars.Length);
        for (int i = 0; i < DataHolder.Instance.avaliableRadars.Length; i++)
        {
            string name = DataHolder.Instance.avaliableRadars[i].label;
            Debug.Log($"Saving Radar {name}");

            PlayerPrefs.SetInt("Radar_" + name + "_isUnlocked", 0);
            PlayerPrefs.SetInt("Radar_" + name + "_detectionRange", DataHolder.Instance.avaliableRadars[i].detectionRange);
            PlayerPrefs.SetFloat("Radar_" + name + "_lockOnTime", DataHolder.Instance.avaliableRadars[i].lockOnTime);
            PlayerPrefs.Save();
        }
    }

    void SaveAllCannonSlotData()
    {
        Debug.Log("PlayerPrefsManager: Saving all Cannon data...");
        PlayerPrefs.SetInt("TotalCannons", DataHolder.Instance.avaliableCannons.Length);
        for (int i = 0; i < DataHolder.Instance.avaliableCannons.Length; i++)
        {
            string name = DataHolder.Instance.avaliableCannons[i].label;
            Debug.Log($"Saving Cannon {name}");

            PlayerPrefs.SetInt("Cannon_" + name + "_isUnlocked", 0);
            PlayerPrefs.SetInt("Cannon_" + name + "_projectileDamage", DataHolder.Instance.avaliableCannons[i].projectileDamage);
            //PlayerPrefs.SetInt("Cannon_" + name + "_projectileSpeed", DataHolder.Instance.avaliableCannons[i].projectileSpeed);
            PlayerPrefs.SetInt("Cannon_" + name + "_fireRate", DataHolder.Instance.avaliableCannons[i].fireRate);
            PlayerPrefs.SetFloat("Cannon_" + name + "_lifetime", DataHolder.Instance.avaliableCannons[i].lifetime);
            PlayerPrefs.SetInt("Cannon_" + name + "_magazineCapacity", DataHolder.Instance.avaliableCannons[i].magazineCapacity);
            PlayerPrefs.SetInt("Cannon_" + name + "_ammoPerShot", DataHolder.Instance.avaliableCannons[i].ammoPerShot);
            PlayerPrefs.SetInt("Cannon_" + name + "_recoveryRatePerSecond", DataHolder.Instance.avaliableCannons[i].recoveryRatePerSecond);
            PlayerPrefs.SetFloat("Cannon_" + name + "_recoveryDelay", DataHolder.Instance.avaliableCannons[i].recoveryDelay);
            PlayerPrefs.SetInt("Cannon_" + name + "_projectilePrefabIndex", DataHolder.Instance.avaliableCannons[i].projectilePrefabIndex);
            PlayerPrefs.SetInt("Cannon_" + name + "_muzzleFlashPrefabIndex", DataHolder.Instance.avaliableCannons[i].muzzleFlashPrefabIndex);
            PlayerPrefs.SetInt("Cannon_" + name + "_impactEffectPrefabIndex", DataHolder.Instance.avaliableCannons[i].impactEffectPrefabIndex);
            PlayerPrefs.Save();
        }
    }

    void SaveAllLauncherSlotData()
    {
        Debug.Log("PlayerPrefsManager: Saving all Launcher data...");
        PlayerPrefs.SetInt("TotalLaunchers", DataHolder.Instance.avaliableLaunchers.Length);
        for (int i = 0; i < DataHolder.Instance.avaliableLaunchers.Length; i++)
        {
            string name = DataHolder.Instance.avaliableLaunchers[i].label;
            Debug.Log($"Saving Launcher {name}");

            PlayerPrefs.SetInt("Launcher_" + name + "_isUnlocked", 0);
            PlayerPrefs.SetInt("Launcher_" + name + "_projectileDamage", DataHolder.Instance.avaliableLaunchers[i].projectileDamage);
            PlayerPrefs.SetInt("Launcher_" + name + "_projectileSpeed", DataHolder.Instance.avaliableLaunchers[i].projectileSpeed);
            PlayerPrefs.SetInt("Launcher_" + name + "_fireRate", DataHolder.Instance.avaliableLaunchers[i].fireRate);
            PlayerPrefs.SetFloat("Launcher_" + name + "_lifetime", DataHolder.Instance.avaliableLaunchers[i].lifetime);
            PlayerPrefs.SetInt("Launcher_" + name + "_magazineCapacity", DataHolder.Instance.avaliableLaunchers[i].magazineCapacity);
            PlayerPrefs.SetInt("Launcher_" + name + "_ammoPerShot", DataHolder.Instance.avaliableLaunchers[i].ammoPerShot);
            PlayerPrefs.SetInt("Launcher_" + name + "_recoveryRatePerSecond", DataHolder.Instance.avaliableLaunchers[i].recoveryRatePerSecond);
            PlayerPrefs.SetInt("Launcher_" + name + "_pauseRecoveryDuringCooldown", DataHolder.Instance.avaliableLaunchers[i].pauseRecoveryDuringCooldown ? 1 : 0);
            PlayerPrefs.SetInt("Launcher_" + name + "_startFull", DataHolder.Instance.avaliableLaunchers[i].startFull ? 1 : 0);
            PlayerPrefs.SetInt("Launcher_" + name + "_projectilePrefabIndex", DataHolder.Instance.avaliableLaunchers[i].projectilePrefabIndex);
            PlayerPrefs.SetInt("Launcher_" + name + "_muzzleFlashPrefabIndex", DataHolder.Instance.avaliableLaunchers[i].muzzleFlashPrefabIndex);
            PlayerPrefs.SetInt("Launcher_" + name + "_impactEffectPrefabIndex", DataHolder.Instance.avaliableLaunchers[i].impactEffectPrefabIndex);
            PlayerPrefs.Save();
        }
    }
    void InitializeAllShipsEquippedEquipment()
    {
        Debug.Log("PlayerPrefsManager: InitializeAllShipsEquippedEquipment() called.");
    }

}

