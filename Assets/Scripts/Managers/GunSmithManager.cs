using System;
using System.Text;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class GunSmithManager : MonoBehaviour
{
    public HangarManager _hm;
    public GameObject shipDisplayPanel;
    public GameObject gunsmithPanel;
    public GameObject modulePanel;
    public Image moduleIcon;
    public GameObject moduleListContent;
    public GameObject moduleEntry;
    public TextMeshProUGUI moduleStatsText;
    public TextMeshProUGUI moduleName;
    public int currentIndex = 0;
    public GameObject unlockButton;
    public GameObject selectButton;
    public GameObject selected;
    public GameObject upgradeButton;
    public TextMeshProUGUI unlockPriceText;
    public TextMeshProUGUI upgradePriceText;
    public CurrentModule currentModule = CurrentModule.None;

    [Header("CameraSettings")]
    public Camera cam;
    public Vector3 storedPosition = Vector3.zero;
    public Quaternion storedRotation = Quaternion.identity;
    public Vector3 storedPosition2 = Vector3.zero;
    public Quaternion storedRotation2 = Quaternion.identity;

    private string shipName;
    private int currentShipIndex => _hm.currentIndex;

    // Module configuration data
    private class ModuleConfig
    {
        public string PlayerPrefKey;
        public string UnlockPrefix;
        public System.Func<int> GetModuleCount;
        public System.Func<int, string> GetModuleLabel;
        public System.Func<int, Component> GetUpgradeComponent;
        public System.Action<Component> UpgradeAction;
        public System.Action<string> UpdateStatsAction;
    }

    private ModuleConfig GetModuleConfig(CurrentModule moduleType)
    {
        var data = DataHolder.Instance;
        switch (moduleType)
        {
            case CurrentModule.Cannon:
                return new ModuleConfig
                {
                    PlayerPrefKey = "cannon",
                    UnlockPrefix = "Cannon_",
                    GetModuleCount = () => data.avaliableCannons.Length,
                    GetModuleLabel = (i) => data.avaliableCannons[i].label,
                    GetUpgradeComponent = (i) => data.avaliableCannons[i].GetComponent<UpgradeCannon>(),
                    UpgradeAction = (u) => UpgradeCannon((UpgradeCannon)u),
                    UpdateStatsAction = UpdateCannonStatsText
                };
            case CurrentModule.Launcher:
                return new ModuleConfig
                {
                    PlayerPrefKey = "launcher",
                    UnlockPrefix = "Launcher_",
                    GetModuleCount = () => data.avaliableLaunchers.Length,
                    GetModuleLabel = (i) => data.avaliableLaunchers[i].label,
                    GetUpgradeComponent = (i) => data.avaliableLaunchers[i].GetComponent<UpgradeLauncher>(),
                    UpgradeAction = (u) => UpgradeLauncher((UpgradeLauncher)u),
                    UpdateStatsAction = UpdateLauncherStatsText
                };
            case CurrentModule.Engine:
                return new ModuleConfig
                {
                    PlayerPrefKey = "engine",
                    UnlockPrefix = "Engine_",
                    GetModuleCount = () => data.avaliableEngines.Length,
                    GetModuleLabel = (i) => data.avaliableEngines[i].label,
                    GetUpgradeComponent = (i) => data.avaliableEngines[i].GetComponent<UpgradeEngine>(),
                    UpgradeAction = (u) => UpgradeEngine((UpgradeEngine)u),
                    UpdateStatsAction = UpdateEngineStatsText
                };
            case CurrentModule.Pilot:
                return new ModuleConfig
                {
                    PlayerPrefKey = "pilot",
                    UnlockPrefix = "Pilot_",
                    GetModuleCount = () => data.avaliablePilots.Length,
                    GetModuleLabel = (i) => data.avaliablePilots[i].label,
                    GetUpgradeComponent = (i) => data.avaliablePilots[i].GetComponent<UpgradePilot>(),
                    UpgradeAction = (u) => UpgradePilot((UpgradePilot)u),
                    UpdateStatsAction = UpdatePilotStatsText
                };
            case CurrentModule.Shield:
                return new ModuleConfig
                {
                    PlayerPrefKey = "shield",
                    UnlockPrefix = "Shield_",
                    GetModuleCount = () => data.avaliableShields.Length,
                    GetModuleLabel = (i) => data.avaliableShields[i].label,
                    GetUpgradeComponent = (i) => data.avaliableShields[i].GetComponent<UpgradeShield>(),
                    UpgradeAction = (u) => UpgradeShield((UpgradeShield)u),
                    UpdateStatsAction = UpdateShieldStatsText
                };
            case CurrentModule.Radar:
                return new ModuleConfig
                {
                    PlayerPrefKey = "radar",
                    UnlockPrefix = "Radar_",
                    GetModuleCount = () => data.avaliableRadars.Length,
                    GetModuleLabel = (i) => data.avaliableRadars[i].label,
                    GetUpgradeComponent = (i) => data.avaliableRadars[i].GetComponent<UpgradeRadar>(),
                    UpgradeAction = (u) => UpgradeRadar((UpgradeRadar)u),
                    UpdateStatsAction = UpdateRadarStatsText
                };
            case CurrentModule.RepairModule:
                return new ModuleConfig
                {
                    PlayerPrefKey = "repair",
                    UnlockPrefix = "RepairModule_",
                    GetModuleCount = () => data.repairModules.Length,
                    GetModuleLabel = (i) => data.repairModules[i].label,
                    GetUpgradeComponent = (i) => data.repairModules[i].GetComponent<UpgradeRepair>(),
                    UpgradeAction = (u) => UpgradeRepairModule((UpgradeRepair)u),
                    UpdateStatsAction = UpdateRepairModuleStatsText
                };
            default:
                return null;
        }
    }

    // UI Navigation
    public void OpenGunSmith()
    {
        shipDisplayPanel.SetActive(false);
        gunsmithPanel.SetActive(true);
    }

    public void CloseGunSmith()
    {
        gunsmithPanel.SetActive(false);
        shipDisplayPanel.SetActive(true);
    }

    public void CloseModule()
    {
        modulePanel.SetActive(false);
        gunsmithPanel.SetActive(true);
        ChangeCamPos1();
    }

    public void ChangeCamPos1()
    {
        cam.transform.position = storedPosition;
        cam.transform.rotation = storedRotation;
    }

    public void ChangeCamPos2()
    {
        cam.transform.position = storedPosition2;
        cam.transform.rotation = storedRotation2;
    }

    // Generalized Load Method
    public void LoadModule(CurrentModule moduleType)
    {
        ChangeCamPos2();
        shipName = DataHolder.Instance.avaliableShips[currentShipIndex]._name;
        currentModule = moduleType;
        gunsmithPanel.SetActive(false);
        modulePanel.SetActive(true);
        currentIndex = 0;

        var config = GetModuleConfig(moduleType);
        if (config == null) return;

        // Clear old entries
        foreach (Transform c in moduleListContent.transform)
            Destroy(c.gameObject);

        int moduleCount = config.GetModuleCount();

        // Build fresh entries
        for (int i = 0; i < moduleCount; i++)
        {
            GameObject entry = Instantiate(moduleEntry, moduleListContent.transform);
            var label = entry.GetComponentInChildren<TextMeshProUGUI>();
            if (label) label.text = config.GetModuleLabel(i);

            int idx = i;
            var btn = entry.GetComponentInChildren<Button>();
            if (btn) btn.onClick.AddListener(() => SelectModule(idx));
        }

        if (moduleCount > 0)
        {
            currentIndex = 0;
            SelectModule(currentIndex);
        }
    }

    // Public methods for each module type (calls LoadModule)
    public void LoadWithRespectToCannon() => LoadModule(CurrentModule.Cannon);
    public void LoadWithRespectToLauncher() => LoadModule(CurrentModule.Launcher);
    public void LoadWithRespectToEngine() => LoadModule(CurrentModule.Engine);
    public void LoadWithRespectToPilot() => LoadModule(CurrentModule.Pilot);
    public void LoadWithRespectToShield() => LoadModule(CurrentModule.Shield);
    public void LoadWithRespectToRadar() => LoadModule(CurrentModule.Radar);
    public void LoadWithRespectToRepairModule() => LoadModule(CurrentModule.RepairModule);

    // Generalized Select Method - FIXED
    private void SelectModule(int index)
    {
        currentIndex = index;
        var config = GetModuleConfig(currentModule);
        if (config == null) return;

        int moduleCount = config.GetModuleCount();
        if (currentIndex < 0 || currentIndex >= moduleCount) return;

        string moduleLabel = config.GetModuleLabel(currentIndex);
        string selectedModule = PlayerPrefs.GetString($"Ship_{shipName}_{config.PlayerPrefKey}", "");
        bool isSelected = (moduleLabel == selectedModule);

        var upgradeComp = config.GetUpgradeComponent(currentIndex);
        if (upgradeComp == null) return;

        // Store original level to restore after reading price
        var levelField = upgradeComp.GetType().GetField("level");
        int originalLevel = (int)levelField?.GetValue(upgradeComp);

        // Sync the component's level with saved data before displaying price
        int savedLevel = PlayerPrefs.GetInt($"{config.UnlockPrefix}{moduleLabel}_level", 1);
        levelField?.SetValue(upgradeComp, savedLevel);

        // Call Recompute to ensure price calculation uses correct level
        var recomputeMethod = upgradeComp.GetType().GetMethod("Recompute");
        recomputeMethod?.Invoke(upgradeComp, null);

        bool isUnlocked = PlayerPrefs.GetInt($"{config.UnlockPrefix}{moduleLabel}_isUnlocked", 0) == 1;

        if (isUnlocked)
        {
            unlockButton.SetActive(false);
            upgradeButton.SetActive(true);
            selectButton.SetActive(!isSelected);
            selected.SetActive(isSelected);

            var maxLevel = upgradeComp.GetType().GetField("maxLevel")?.GetValue(upgradeComp);
            if (maxLevel != null && savedLevel >= (int)maxLevel)
            {
                // Hide upgrade button if at max level
                upgradeButton.SetActive(false);
                upgradePriceText.text = ""; // CLEAR THE TEXT
            }
            else
            {
                var upgradePrice = upgradeComp.GetType().GetProperty("UpgradePrice")?.GetValue(upgradeComp);
                if (upgradePrice != null)
                    upgradePriceText.text = upgradePrice.ToString();
                else
                    upgradePriceText.text = ""; // CLEAR IF NULL
            }
        }
        else
        {
            unlockButton.SetActive(true);
            upgradeButton.SetActive(false);
            selectButton.SetActive(false);
            selected.SetActive(false);
            upgradePriceText.text = ""; // CLEAR UPGRADE PRICE FOR LOCKED MODULES

            var unlockPrice = upgradeComp.GetType().GetField("unlockPrice")?.GetValue(upgradeComp);
            if (unlockPrice != null)
                unlockPriceText.text = unlockPrice.ToString();
            else
                unlockPriceText.text = ""; // CLEAR IF NULL
        }

        // Restore original level to prevent contamination
        levelField?.SetValue(upgradeComp, originalLevel);
        if (originalLevel != savedLevel)
        {
            recomputeMethod?.Invoke(upgradeComp, null);
        }

        config.UpdateStatsAction(moduleLabel);
        Debug.Log($"Selected {currentModule}: {moduleLabel} (index {currentIndex})");
    }

    // Generalized Select for Save
    public void SelectModuleForSave()
    {
        var config = GetModuleConfig(currentModule);
        if (config == null) return;

        shipName = DataHolder.Instance.avaliableShips[currentShipIndex]._name;
        string moduleLabel = config.GetModuleLabel(currentIndex);

        // Check if module is unlocked before allowing selection
        bool isUnlocked = PlayerPrefs.GetInt($"{config.UnlockPrefix}{moduleLabel}_isUnlocked", 0) == 1;

        if (!isUnlocked)
        {
            Debug.LogWarning($"Cannot equip {moduleLabel} - module is locked!");
            return;
        }

        PlayerPrefs.SetString($"Ship_{shipName}_{config.PlayerPrefKey}", moduleLabel);

        // CRITICAL FOR ANDROID: Force save immediately
        PlayerPrefs.Save();

        // Add a small delay before UI update to ensure save completes
#if UNITY_ANDROID
        System.Threading.Thread.Sleep(50); // 50ms delay on Android
#endif

        UponSelect();

        Debug.Log($"Equipped {moduleLabel} for {shipName} - PlayerPrefs saved");
    }

    // Generalized Unlock
    // Generalized Unlock - FIXED
    public void Unlock()
    {
        var config = GetModuleConfig(currentModule);
        if (config == null) return;

        string moduleLabel = config.GetModuleLabel(currentIndex);
        var upgradeComp = config.GetUpgradeComponent(currentIndex);
        if (upgradeComp == null) return;

        var unlockPrice = (int)upgradeComp.GetType().GetField("unlockPrice")?.GetValue(upgradeComp);

        if (DeductCredits(unlockPrice))
        {
            PlayerPrefs.SetInt($"{config.UnlockPrefix}{moduleLabel}_isUnlocked", 1);
            PlayerPrefs.Save();

            // CRITICAL FIX: Set the upgrade price after unlocking
            var levelField = upgradeComp.GetType().GetField("level");
            var maxLevelField = upgradeComp.GetType().GetField("maxLevel");
            var recomputeMethod = upgradeComp.GetType().GetMethod("Recompute");

            // Get the saved level (should be 1 for newly unlocked)
            int savedLevel = PlayerPrefs.GetInt($"{config.UnlockPrefix}{moduleLabel}_level", 1);
            int maxLevel = (int)maxLevelField?.GetValue(upgradeComp);

            // Sync component level and recompute
            levelField?.SetValue(upgradeComp, savedLevel);
            recomputeMethod?.Invoke(upgradeComp, null);

            // Get and display the upgrade price
            if (savedLevel < maxLevel)
            {
                var upgradePrice = upgradeComp.GetType().GetProperty("UpgradePrice")?.GetValue(upgradeComp);
                if (upgradePrice != null)
                {
                    upgradePriceText.text = upgradePrice.ToString();
                }
            }

            UponUnlock();
        }
    }

    // Helper Methods
    void UponUnlock()
    {
        unlockButton.SetActive(false);
        selectButton.SetActive(true);
        upgradeButton.SetActive(true);
        // Don't call PlayerPrefs.Save() here - already saved in Unlock()
    }

    // Generalized Upgrade - FIXED
    public void Upgrade()
    {
        var config = GetModuleConfig(currentModule);
        if (config == null) return;

        string moduleLabel = config.GetModuleLabel(currentIndex);
        var upgradeComp = config.GetUpgradeComponent(currentIndex);
        if (upgradeComp == null) return;

        var maxLevel = (int)upgradeComp.GetType().GetField("maxLevel")?.GetValue(upgradeComp);
        int currentLevel = PlayerPrefs.GetInt($"{config.UnlockPrefix}{moduleLabel}_level", 1);

        if (currentLevel >= maxLevel)
        {
            Debug.Log($"{currentModule} is already at max level! No coins deducted.");
            return;
        }

        // CRITICAL FIX: Sync component level with PlayerPrefs before getting price
        upgradeComp.GetType().GetField("level")?.SetValue(upgradeComp, currentLevel);
        var recomputeMethod = upgradeComp.GetType().GetMethod("Recompute");
        recomputeMethod?.Invoke(upgradeComp, null);

        var upgradePrice = (int)upgradeComp.GetType().GetProperty("UpgradePrice")?.GetValue(upgradeComp);

        if (DeductCredits(upgradePrice))
        {
            config.UpgradeAction(upgradeComp);
            PlayerPrefs.Save();
            config.UpdateStatsAction(moduleLabel);

            // Refresh the UI to show updated price
            var updatedComp = config.GetUpgradeComponent(currentIndex);
            if (updatedComp != null)
            {
                int newLevel = PlayerPrefs.GetInt($"{config.UnlockPrefix}{moduleLabel}_level", 1);
                updatedComp.GetType().GetField("level")?.SetValue(updatedComp, newLevel);
                recomputeMethod?.Invoke(updatedComp, null);

                var newUpgradePrice = updatedComp.GetType().GetProperty("UpgradePrice")?.GetValue(updatedComp);
                if (newUpgradePrice != null)
                {
                    upgradePriceText.text = newUpgradePrice.ToString();
                }

                // Check if we've reached max level after this upgrade
                if (newLevel >= maxLevel)
                {
                    upgradeButton.SetActive(false);
                    Debug.Log($"{currentModule} has reached max level!");
                }
            }
        }
    }

    // Movement Methods
    public void MoveRight()
    {
        var config = GetModuleConfig(currentModule);
        if (config == null) return;

        int moduleCount = config.GetModuleCount();
        if (moduleCount == 0) return;

        if (currentIndex < moduleCount - 1)
        {
            currentIndex++;
            SelectModule(currentIndex);
            Debug.Log($"Moved Right -> {config.GetModuleLabel(currentIndex)}");
        }
        else
        {
            Debug.Log($"No next {currentModule} available.");
        }
    }

    public void MoveLeft()
    {
        var config = GetModuleConfig(currentModule);
        if (config == null) return;

        int moduleCount = config.GetModuleCount();
        if (moduleCount == 0) return;

        if (currentIndex > 0)
        {
            currentIndex--;
            SelectModule(currentIndex);
            Debug.Log($"Moved Left -> {config.GetModuleLabel(currentIndex)}");
        }
        else
        {
            Debug.Log($"No previous {currentModule} available.");
        }
    }


    void UponSelect()
    {
        selectButton.SetActive(false);
        selected.SetActive(true);
    }

    bool DeductCredits(int price)
    {
        int vCoins = PlayerPrefs.GetInt("V-Coins");
        if (price <= vCoins)
        {
            PlayerPrefs.SetInt("V-Coins", vCoins - price);
            PlayerPrefs.Save();
            return true;
        }
        return false;
    }

    // Specific Upgrade Methods - ALL FIXED
    private void UpgradeCannon(UpgradeCannon u)
    {
        string moduleLabel = DataHolder.Instance.avaliableCannons[currentIndex].label;
        int currentLevel = PlayerPrefs.GetInt($"Cannon_{moduleLabel}_level", 1);

        // FIX: Sync level with PlayerPrefs first
        u.level = currentLevel;
        u.Recompute();

        // Now increment and recompute
        u.level = currentLevel + 1;
        u.Recompute();

        PlayerPrefs.SetInt($"Cannon_{moduleLabel}_level", u.level);
        PlayerPrefs.SetInt($"Cannon_{moduleLabel}_projectileDamage", u.projectileDamage);
        PlayerPrefs.SetInt($"Cannon_{moduleLabel}_fireRate", u.fireRate);
        PlayerPrefs.SetInt($"Cannon_{moduleLabel}_magazineCapacity", u.magazineCapacity);
        PlayerPrefs.SetInt($"Cannon_{moduleLabel}_recoveryRatePerSecond", u.recoveryRatePerSecond);
        Debug.Log($"Upgraded {moduleLabel} to Level {u.level}");
    }

    private void UpgradeLauncher(UpgradeLauncher u)
    {
        string moduleLabel = DataHolder.Instance.avaliableLaunchers[currentIndex].label;
        string key = $"Launcher_{moduleLabel}_";
        int currentLevel = PlayerPrefs.GetInt(key + "level", 1);

        // FIX: Sync level with PlayerPrefs first
        u.level = currentLevel;
        u.Recompute();

        // Now increment and recompute
        u.level = currentLevel + 1;
        u.Recompute();
        PlayerPrefs.SetInt(key + "level", u.level);
        PlayerPrefs.SetInt(key + "projectileDamage", u.projectileDamage);
        PlayerPrefs.SetInt(key + "projectileSpeed", u.projectileSpeed);
        PlayerPrefs.SetInt(key + "recoveryRatePerSecond", u.recoveryRatePerSecond);
        Debug.Log($"Upgraded Launcher {moduleLabel} to Level {u.level}");
    }

    private void UpgradeEngine(UpgradeEngine u)
    {
        string moduleLabel = DataHolder.Instance.avaliableEngines[currentIndex].label;
        string key = $"Engine_{moduleLabel}_";
        int currentLevel = PlayerPrefs.GetInt(key + "level", 1);
        // FIX: Sync level with PlayerPrefs first
        u.level = currentLevel;
        u.Recompute();
        // Now increment and recompute
        u.level = currentLevel + 1;
        u.Recompute();
        PlayerPrefs.SetInt(key + "level", u.level);
        PlayerPrefs.SetFloat(key + "moveSpeed", u.moveSpeed);
        PlayerPrefs.SetFloat(key + "boostDuration", u.boostDuration);
        PlayerPrefs.SetFloat(key + "boostCooldown", u.boostCooldown);
        Debug.Log($"Upgraded Engine {moduleLabel} to Level {u.level}");
    }
    private void UpgradePilot(UpgradePilot u)
    {
        string moduleLabel = DataHolder.Instance.avaliablePilots[currentIndex].label;
        string key = $"Pilot_{moduleLabel}_";
        int currentLevel = PlayerPrefs.GetInt(key + "level", 1);
        // FIX: Sync level with PlayerPrefs first
        u.level = currentLevel;
        u.Recompute();
        // Now increment and recompute
        u.level = currentLevel + 1;
        u.Recompute();
        PlayerPrefs.SetInt(key + "level", u.level);
        PlayerPrefs.SetInt(key + "isUnlocked", 1);
        Debug.Log($"Upgraded Pilot {moduleLabel} to Level {u.level}");
    }

    private void UpgradeShield(UpgradeShield u)
    {
        string moduleLabel = DataHolder.Instance.avaliableShields[currentIndex].label;
        string key = $"Shield_{moduleLabel}_";
        int currentLevel = PlayerPrefs.GetInt(key + "level", 1);
        // FIX: Sync level with PlayerPrefs first
        u.level = currentLevel;
        u.Recompute();
        // Now increment and recompute
        u.level = currentLevel + 1;
        u.Recompute();
        PlayerPrefs.SetInt(key + "level", u.level);
        PlayerPrefs.SetInt(key + "maxShield", u.maxShield);
        PlayerPrefs.SetInt(key + "regenerationRate", u.regenerationRate);
        Debug.Log($"Upgraded Shield {moduleLabel} to Level {u.level}");
    }

    private void UpgradeRadar(UpgradeRadar u)
    {
        string moduleLabel = DataHolder.Instance.avaliableRadars[currentIndex].label;
        string key = $"Radar_{moduleLabel}_";
        int currentLevel = PlayerPrefs.GetInt(key + "level", 1);
        // FIX: Sync level with PlayerPrefs first
        u.level = currentLevel;
        u.Recompute();
        // Now increment and recompute
        u.level = currentLevel + 1;
        u.Recompute();
        PlayerPrefs.SetInt(key + "level", u.level);
        PlayerPrefs.SetInt(key + "detectionRange", u.detectionRange);
        PlayerPrefs.SetFloat(key + "lockOnTime", u.lockOnTime);
        Debug.Log($"Upgraded Radar {moduleLabel} to Level {u.level}");
    }

    private void UpgradeRepairModule(UpgradeRepair u)
    {
        string moduleLabel = DataHolder.Instance.repairModules[currentIndex].label;
        string key = $"RepairModule_{moduleLabel}_";
        int currentLevel = PlayerPrefs.GetInt(key + "level", 1);
        // FIX: Sync level with PlayerPrefs first
        u.level = currentLevel;
        u.Recompute();
        // Now increment and recompute
        u.level = currentLevel + 1;
        u.Recompute();
        PlayerPrefs.SetInt(key + "level", u.level);
        PlayerPrefs.SetInt(key + "repairRate", u.repairRate);
        Debug.Log($"Upgraded Repair Module {moduleLabel} to Level {u.level}");
    }
    // Stats Update Methods
    private void UpdateCannonStatsText(string label)
    {
        moduleName.text = label;
        int level = PlayerPrefs.GetInt($"Cannon_{label}_level", 1);
        int projectileDamage = PlayerPrefs.GetInt($"Cannon_{label}_projectileDamage", 0);
        int fireRate = PlayerPrefs.GetInt($"Cannon_{label}_fireRate", 0);
        int magazineCapacity = PlayerPrefs.GetInt($"Cannon_{label}_magazineCapacity", 0);
        int recoveryRate = PlayerPrefs.GetInt($"Cannon_{label}_recoveryRatePerSecond", 0);
        var sb = new StringBuilder(256);
        sb.AppendLine($"Level : {level}");
        sb.AppendLine($"Projectile Damage : {projectileDamage}");
        sb.AppendLine($"Fire Rate : {fireRate}");
        sb.AppendLine($"Magazine Capacity : {magazineCapacity}");
        sb.AppendLine($"Recovery Rate / s : {recoveryRate}");
        moduleStatsText.text = sb.ToString();
    }
    private void UpdateLauncherStatsText(string label)
    {
        moduleName.text = label;
        int level = PlayerPrefs.GetInt($"Launcher_{label}_level", 1);
        int projectileDamage = PlayerPrefs.GetInt($"Launcher_{label}_projectileDamage", 0);
        int projectileSpeed = PlayerPrefs.GetInt($"Launcher_{label}_projectileSpeed", 0);
        int fireRate = PlayerPrefs.GetInt($"Launcher_{label}_fireRate", 0);
        int magazineCapacity = PlayerPrefs.GetInt($"Launcher_{label}_magazineCapacity", 0);
        int ammoPerShot = PlayerPrefs.GetInt($"Launcher_{label}_ammoPerShot", 1);
        int recoveryRate = PlayerPrefs.GetInt($"Launcher_{label}_recoveryRatePerSecond", 0);
        var sb = new StringBuilder(256);
        sb.AppendLine($"Level : {level}");
        sb.AppendLine($"Projectile Damage : {projectileDamage}");
        sb.AppendLine($"Projectile Speed : {projectileSpeed}");
        sb.AppendLine($"Fire Rate : {fireRate}");
        sb.AppendLine($"Magazine Capacity : {magazineCapacity}");
        sb.AppendLine($"Ammo Per Shot : {ammoPerShot}");
        sb.AppendLine($"Recovery Rate / s : {recoveryRate}");
        moduleStatsText.text = sb.ToString();
    }

    private void UpdateEngineStatsText(string label)
    {
        moduleName.text = label;
        int level = PlayerPrefs.GetInt($"Engine_{label}_level", 1);
        float moveSpeed = PlayerPrefs.GetFloat($"Engine_{label}_moveSpeed", 0f);
        float boostDuration = PlayerPrefs.GetFloat($"Engine_{label}_boostDuration", 0f);
        float boostCooldown = PlayerPrefs.GetFloat($"Engine_{label}_boostCooldown", 0f);
        var sb = new StringBuilder(256);
        sb.AppendLine($"Level : {level}");
        sb.AppendLine($"Move Speed : {moveSpeed}");
        sb.AppendLine($"Boost Duration : {boostDuration}");
        sb.AppendLine($"Boost Cooldown : {boostCooldown}");
        moduleStatsText.text = sb.ToString();
    }

    private void UpdatePilotStatsText(string label)
    {
        var pilot = DataHolder.Instance.avaliablePilots[currentIndex];
        moduleName.text = label;
        int level = PlayerPrefs.GetInt($"Pilot_{label}_level", 1);
        var up = pilot.GetComponent<UpgradePilot>();
        if (up != null)
        {
            int oldLevel = up.level;
            up.level = Mathf.Clamp(level, 1, up.maxLevel);
            up.Recompute();
            int lockOnPct = Mathf.RoundToInt(up.lockOnBuff * 100f);
            int boostPct = Mathf.RoundToInt(up.boostMultiplierBuff * 100f);
            var sb = new StringBuilder(256);
            sb.AppendLine($"Level : {up.level}");
            sb.AppendLine($"Cannon Damage + : {up.additionalCannonDamage}");
            sb.AppendLine($"Launcher Damage + : {up.additionalLauncherDamage}");
            sb.AppendLine($"Lock-On Faster : {lockOnPct}%");
            sb.AppendLine($"Move Speed + : {up.moveSpeedBuff}");
            sb.AppendLine($"Boost Power + : {boostPct}%");
            sb.AppendLine($"Max Health + : {up.additionalHealthBuff}");
            sb.AppendLine($"Max Shield + : {up.additionalShieldBuff}");
            sb.AppendLine($"Health Regen + : {up.additionalHealthRegnBuff}/s");
            sb.AppendLine($"Shield Regen + : {up.additionalShieldRegnBuff}/s");
            moduleStatsText.text = sb.ToString();

            up.level = oldLevel;
            if (oldLevel != level)
            {
                up.level = level;
                up.Recompute();
            }
        }
        else
        {
            moduleStatsText.text = $"Level : {level}\n(UpgradePilot missing)";
        }
    }

    private void UpdateShieldStatsText(string label)
    {
        moduleName.text = label;
        int level = PlayerPrefs.GetInt($"Shield_{label}_level", 1);
        int maxShield = PlayerPrefs.GetInt($"Shield_{label}_maxShield", 0);
        int regenerationRate = PlayerPrefs.GetInt($"Shield_{label}_regenerationRate", 0);
        var sb = new StringBuilder(256);
        sb.AppendLine($"Level : {level}");
        sb.AppendLine($"Max Shield : {maxShield}");
        sb.AppendLine($"Regen Rate : {regenerationRate}");
        moduleStatsText.text = sb.ToString();
    }

    private void UpdateRadarStatsText(string label)
    {
        moduleName.text = label;
        int level = PlayerPrefs.GetInt($"Radar_{label}_level", 1);
        int detectionRange = PlayerPrefs.GetInt($"Radar_{label}_detectionRange", 0);
        float lockOnTime = PlayerPrefs.GetFloat($"Radar_{label}_lockOnTime", 0f);
        var sb = new StringBuilder(256);
        sb.AppendLine($"Level : {level}");
        sb.AppendLine($"Detection Range : {detectionRange}");
        sb.AppendLine($"Lock-On Time : {lockOnTime}");
        moduleStatsText.text = sb.ToString();
    }

    private void UpdateRepairModuleStatsText(string label)
    {
        moduleName.text = label;
        int level = PlayerPrefs.GetInt($"RepairModule_{label}_level", 1);
        int repairRate = PlayerPrefs.GetInt($"RepairModule_{label}_repairRate", 0);
        var sb = new StringBuilder(256);
        sb.AppendLine($"Level : {level}");
        sb.AppendLine($"Repair Rate : {repairRate}");
        moduleStatsText.text = sb.ToString();
    }
}
public enum CurrentModule
{
    None,
    Cannon,
    Launcher,
    Radar,
    Shield,
    Engine,
    Pilot,
    RepairModule
}