using System;
using System.Linq;
using TMPro;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.UI;
using static UnityEngine.Rendering.ProbeAdjustmentVolume;

public class HangarManager : MonoBehaviour
{
    [Header("Display")]
    [SerializeField] private Transform displayPosition; // Single position where the current ship will be displayed
    public ShipStatsUI shipStatsUI;

    [Header("Ship UI Controls")]
    public GameObject unlockButton;
    public GameObject selectButton;
    public GameObject selectedLabel;
    public GameObject upgradeButton;
    public TextMeshProUGUI unlockPriceText;
    public TextMeshProUGUI upgradePriceText;
    public GameObject listEntryPrefab;
    public Transform listContent;

    [Header("Selection Keys")]
    public string selectedShipKey = "Selected_Ship_Name"; // saved selected ship name

    [Header("Currency")]
    public string coinsKey = "V-Coins"; // same wallet as modules

    public int currentIndex = 0;
    private int totalShips = 0;

    // runtime cache of the current display instance and components
    private GameObject currentDisplayInstance;
    private Ship currentDisplayShip;           // your Ship component on the instance
    private UpgradeShip currentUpgradeData;    // our upgrade settings on the instance

    private void Start()
    {
        Time.timeScale = 1f;
        currentIndex = PlayerPrefs.GetInt("Hanger_shipIndex", 0);
        Debug.Log("HangarManager: Start() called.");

        if (DataHolder.Instance == null)
        {
            Debug.LogError("HangarManager: DataHolder.Instance is NULL!");
            return;
        }

        totalShips = DataHolder.Instance.shipPrefabs.Length;
        Debug.Log($"HangarManager: Total ships available = {totalShips}");
        PopulateList();
        DisplayCurrentShip();
        HandleAudio();
    }

    // ========= PUBLIC UI HOOKS (Wire these to your buttons) =========
    public void PopulateList()
    {
        for (int i = 0; i < DataHolder.Instance.shipPrefabs.Length; i++)
        {
            GameObject entry = Instantiate(listEntryPrefab, listContent.transform);
            
            TextMeshProUGUI label = entry.GetComponentInChildren<TextMeshProUGUI>();
            if (label != null) label.text = DataHolder.Instance.shipPrefabs[i].GetComponent<Ship>()._name;

            int index = i;  // Capture the correct index for each ship
            Button btn = entry.GetComponentInChildren<Button>();
            Image icon = entry.GetComponentInChildren<Image>();
            if (icon != null)
            {
                Sprite shipSprite = DataHolder.Instance.icons[i];
                if (shipSprite != null)
                {
                    icon.sprite = shipSprite;
                }
            }
            if (btn != null)
            {
                btn.onClick.AddListener(() =>
                {
                    SelectShip(index);  // Use the correct index when selecting the ship
                });
            }
        }

        // Initialize stats view for index 0 (if available)
        if (DataHolder.Instance.shipPrefabs.Length > 0)
        {
            currentIndex = PlayerPrefs.GetInt("Hanger_shipIndex", 0);
            SelectShip(currentIndex);  // Automatically select the ship at start
        }
    }


    public void SelectShip(int index)
    {
        Debug.Log("HangarManager: SelectShip() called.");

        // Validate the selected index
        if (index < 0 || index >= totalShips)
        {
            Debug.LogWarning("HangarManager: Invalid index selected.");
            return;
        }

        currentIndex = index;  // Update currentIndex with the selected index

        DisplayCurrentShip();  // Refresh ship display
    }


    public void OnClickUnlockCurrentShip()
    {
        if (!EnsureUpgradeData()) return;

        string shipName = GetCurrentShipNameSafe();
        int isUnlocked = PlayerPrefs.GetInt($"Ship_{shipName}_isUnlocked", 0);
        if (isUnlocked == 1)
        {
            Debug.Log($"HangarManager: {shipName} already unlocked.");
            return;
        }

        int price = currentUpgradeData.unlockPrice;
        if (!DeductCredits(price))
        {
            Debug.Log("HangarManager: Not enough V-Coins to unlock.");
            return;
        }

        PlayerPrefs.SetInt($"Ship_{shipName}_isUnlocked", 1);
        // Initialize level/health on first unlock
        int savedLevel = PlayerPrefs.GetInt($"Ship_{shipName}_level", 1);
        savedLevel = Mathf.Max(1, savedLevel);
        int newMax = currentUpgradeData.MaxHealthForLevel(savedLevel);
        PlayerPrefs.SetInt($"Ship_{shipName}_level", savedLevel);
        PlayerPrefs.SetInt($"Ship_{shipName}_maxHealth", newMax);
        PlayerPrefs.Save();

        ApplySavedStatsToDisplayShip();    // refresh display instance
        UpdateShipUiState();               // update buttons/prices
        Debug.Log($"HangarManager: Unlocked {shipName}. Level {savedLevel}, MaxHealth {newMax}");
    }

    public void OnClickUpgradeCurrentShip()
    {
        if (!EnsureUpgradeData()) return;

        string shipName = GetCurrentShipNameSafe();

        int isUnlocked = PlayerPrefs.GetInt($"Ship_{shipName}_isUnlocked", 0);
        if (isUnlocked == 0)
        {
            Debug.Log($"HangarManager: {shipName} is locked. Cannot upgrade.");
            return;
        }

        int currentLevel = PlayerPrefs.GetInt($"Ship_{shipName}_level", 1);
        if (currentLevel >= currentUpgradeData.maxLevel)
        {
            Debug.Log($"HangarManager: {shipName} already at max level.");
            return;
        }

        int price = currentUpgradeData.UpgradePriceForLevel(currentLevel);
        if (!DeductCredits(price))
        {
            Debug.Log("HangarManager: Not enough V-Coins to upgrade.");
            return;
        }

        // Level up
        int newLevel = currentLevel + 1;
        int newMaxHealth = currentUpgradeData.MaxHealthForLevel(newLevel);

        PlayerPrefs.SetInt($"Ship_{shipName}_level", newLevel);
        PlayerPrefs.SetInt($"Ship_{shipName}_maxHealth", newMaxHealth);
        PlayerPrefs.Save();

        // Apply to live display + refresh UI
        ApplySavedStatsToDisplayShip();
        UpdateShipUiState();

        Debug.Log($"HangarManager: Upgraded {shipName} to Level {newLevel} (MaxHealth {newMaxHealth}).");
    }

    public void OnClickSelectCurrentShip()
    {
        string shipName = GetCurrentShipNameSafe();
        if (PlayerPrefs.GetInt($"Ship_{shipName}_isUnlocked", 0) == 0)
        {
            Debug.Log($"HangarManager: {shipName} is locked. Cannot select.");
            return;
        }

        PlayerPrefs.SetString(selectedShipKey, shipName);
        PlayerPrefs.SetInt("Hanger_shipIndex", currentIndex);
        PlayerPrefs.Save();

        // Refresh UI to show "Selected" label instead of "Select" button
        UpdateShipUiState();

        Debug.Log($"HangarManager: Selected ship -> {shipName}");
    }

    public void SelectNextShip()
    {
        Debug.Log("HangarManager: SelectNextShip() called.");

        if (totalShips <= 0)
        {
            Debug.LogWarning("HangarManager: No ships available to select next.");
            return;
        }

        if (currentIndex >= totalShips - 1)
        {
            Debug.Log("HangarManager: Already at last ship. Not switching.");
            return;
        }

        currentIndex++;
        Debug.Log($"HangarManager: Current index after next = {currentIndex}");
        DisplayCurrentShip();
    }

    public void SelectPreviousShip()
    {
        Debug.Log("HangarManager: SelectPreviousShip() called.");

        if (totalShips <= 0)
        {
            Debug.LogWarning("HangarManager: No ships available to select previous.");
            return;
        }

        if (currentIndex <= 0)
        {
            Debug.Log("HangarManager: Already at first ship. Not switching.");
            return;
        }

        currentIndex--;
        Debug.Log($"HangarManager: Current index after previous = {currentIndex}");
        DisplayCurrentShip();
    }

    // =================== CORE DISPLAY / STATS ===================

    private void DisplayCurrentShip()
    {
        Debug.Log("HangarManager: DisplayCurrentShip() called.");

        // Destroy existing display ship if any
        for (int i = displayPosition.childCount - 1; i >= 0; i--)
        {
            Transform child = displayPosition.GetChild(i);
            if (child.name.EndsWith("_Display"))
            {
                Debug.Log($"HangarManager: Destroying previous display ship: {child.name}");
                Destroy(child.gameObject);
            }
        }
        currentDisplayInstance = null;
        currentDisplayShip = null;
        currentUpgradeData = null;

        if (totalShips == 0)
        {
            Debug.LogWarning("HangarManager: totalShips == 0, aborting display.");
            return;
        }

        var prefab = DataHolder.Instance.shipPrefabs[currentIndex];
        if (prefab == null)
        {
            Debug.LogError($"HangarManager: shipPrefabs[{currentIndex}] is NULL!");
            return;
        }

        GameObject shipInst = Instantiate(prefab, displayPosition.position, displayPosition.rotation);
        currentDisplayInstance = shipInst;

        Debug.Log($"HangarManager: Instantiated ship prefab {shipInst.name} at index {currentIndex}");

        shipInst.transform.SetParent(displayPosition);
        Debug.Log($"HangarManager: Ship parent set to displayPosition ({displayPosition.name})");

        Ship shipComponent = shipInst.GetComponent<Ship>();
        if (shipComponent == null)
        {
            Debug.LogError($"HangarManager: Instantiated ship {shipInst.name} has no Ship component!");
        }
        else
        {
            currentDisplayShip = shipComponent;
            shipInst.name = currentDisplayShip._name + "_Display";
            Debug.Log($"HangarManager: Ship renamed to {shipInst.name}");
        }

        // Disable any scripts that might make it move or interact
        foreach (var script in shipInst.GetComponentsInChildren<MonoBehaviour>())
        {
            if (script == null) continue;

            script.enabled = false;
        }

        foreach (ParticleSystem particleSystem in shipInst.GetComponentsInChildren<ParticleSystem>())
            particleSystem.gameObject.SetActive(false);

        shipInst.SetActive(true);


        // Cache/ensure upgrade data
        currentUpgradeData = DataHolder.Instance.avaliableShips[currentIndex].GetComponent<UpgradeShip>();

        // Apply saved stats to this display instance + update UI
        ApplySavedStatsToDisplayShip();
        UpdateShipUiState();

        // Push to ShipStatsUI
        if (shipStatsUI == null)
        {
            Debug.LogError("HangarManager: shipStatsUI reference is NULL!");
        }
        else
        {
            shipStatsUI.DisplayValuesOnUI(currentDisplayShip);
        }

        Debug.Log("HangarManager: DisplayCurrentShip() finished.");
    }

    /// <summary>
    /// Reads PlayerPrefs for current ship (level/maxHealth) and applies to the live display Ship component.
    /// </summary>
    private void ApplySavedStatsToDisplayShip()
    {
        if (currentDisplayShip == null || currentUpgradeData == null) return;

        string shipName = GetCurrentShipNameSafe();

        // Default: level 1. If unlocked, use saved. If locked, show level 1 baseline.
        int isUnlocked = PlayerPrefs.GetInt($"Ship_{shipName}_isUnlocked", 0);
        int level = (isUnlocked == 1)
            ? PlayerPrefs.GetInt($"Ship_{shipName}_level", 1)
            : 1;

        level = Mathf.Clamp(level, 1, Mathf.Max(1, currentUpgradeData.maxLevel));

        // Compute and persist current max health (so you can show it in other places too)
        int maxHealth = currentUpgradeData.MaxHealthForLevel(level);
        PlayerPrefs.SetInt($"Ship_{shipName}_level", level);
        PlayerPrefs.SetInt($"Ship_{shipName}_maxHealth", maxHealth);
        PlayerPrefs.Save();

        // Apply to Ship component.
        // Assumes your Ship has a public int maxHealth (common pattern).
        // If you also track currentHealth, you can clamp/refresh here.
        try
        {
            currentDisplayShip.maxHealth = maxHealth;
            // If your Ship has currentHealth and you want full on display:
            var curHealthField = currentDisplayShip.GetType().GetField("currentHealth");
            if (curHealthField != null)
            {
                int cur = (int)curHealthField.GetValue(currentDisplayShip);
                cur = Mathf.Min(cur, maxHealth);
                curHealthField.SetValue(currentDisplayShip, cur);
            }
        }
        catch
        {
            Debug.LogWarning("HangarManager: Could not assign Ship.maxHealth or currentHealth. Ensure your Ship has a public int maxHealth.");
        }

        // Also reflect the runtime level on UpgradeShip for consistency
        currentUpgradeData.level = level;

        // Refresh stats UI panel if present
        if (shipStatsUI != null)
            shipStatsUI.DisplayValuesOnUI(currentDisplayShip);
    }

    /// <summary>
    /// Toggles Unlock/Select/Upgrade buttons and price labels based on saved state.
    /// </summary>
    public void UpdateShipUiState()
    {
        Debug.Log("UpdateShipUiState() called...");

        if (!EnsureUpgradeData())
        {
            Debug.LogWarning("UpdateShipUiState: EnsureUpgradeData() failed — aborting.");
            return;
        }
        
        string shipName = GetCurrentShipNameSafe();
        Debug.Log($"UpdateShipUiState: Current ship = {shipName}");


        int isUnlocked = PlayerPrefs.GetInt($"Ship_{shipName}_isUnlocked", 0);
        Debug.Log($"UpdateShipUiState: isUnlocked = {isUnlocked}");

        int level = PlayerPrefs.GetInt($"Ship_{shipName}_level", 1);
        Debug.Log($"UpdateShipUiState: Saved level = {level}");

        // Check if this ship is the currently selected one
        string selectedShip = PlayerPrefs.GetString(selectedShipKey, "");
        bool isSelected = (shipName == selectedShip);

        Debug.Log($"UpdateShipUiState: selectedShip from PlayerPrefs = {selectedShip}");
        Debug.Log($"UpdateShipUiState: isSelected = {isSelected}");

        // Unlock vs Upgrade/Select visibility
        if (unlockButton)
        {
            unlockButton.SetActive(isUnlocked == 0);
            Debug.Log($"UpdateShipUiState: unlockButton -> {(isUnlocked == 0 ? "VISIBLE" : "HIDDEN")}");
        }

        if (selectButton)
        {
            selectButton.SetActive(isUnlocked == 1 && !isSelected);
            Debug.Log($"UpdateShipUiState: selectButton -> {(isUnlocked == 1 && !isSelected ? "VISIBLE" : "HIDDEN")}");
        }

        if (selectedLabel)
        {
            selectedLabel.SetActive(isSelected);
            Debug.Log($"UpdateShipUiState: selectedLabel -> {(isSelected ? "VISIBLE" : "HIDDEN")}");
        }

        if (upgradeButton)
        {
            upgradeButton.SetActive(isUnlocked == 1);
            Debug.Log($"UpdateShipUiState: upgradeButton base -> {(isUnlocked == 1 ? "VISIBLE" : "HIDDEN")}");
        }

        // Prices
        if (isUnlocked == 0)
        {
            Debug.Log("UpdateShipUiState: Ship is LOCKED — showing unlock price only.");

            if (unlockPriceText)
            {
                unlockPriceText.text = currentUpgradeData.unlockPrice.ToString();
                Debug.Log($"UpdateShipUiState: unlock price set to {currentUpgradeData.unlockPrice}");
            }

            if (upgradePriceText)
            {
                upgradePriceText.text = "-";
            }
        }
        else
        {
            Debug.Log("UpdateShipUiState: Ship is UNLOCKED — checking upgrade availability...");

            bool canUpgrade = level < currentUpgradeData.maxLevel;
            Debug.Log($"UpdateShipUiState: canUpgrade = {canUpgrade} (level {level}/{currentUpgradeData.maxLevel})");

            if (upgradeButton)
            {
                upgradeButton.SetActive(canUpgrade);
                Debug.Log($"UpdateShipUiState: upgradeButton -> {(canUpgrade ? "VISIBLE" : "HIDDEN (MAX LEVEL)")}");
            }

            if (upgradePriceText)
            {
                if (canUpgrade)
                {
                    int price = currentUpgradeData.UpgradePriceForLevel(level);
                    upgradePriceText.text = price.ToString();
                    Debug.Log($"UpdateShipUiState: upgrade price set to {price}");
                }
                else
                {
                    upgradePriceText.text = "-";
                    Debug.Log("UpdateShipUiState: Upgrade disabled — max level reached.");
                }
            }
        }

        Debug.Log("UpdateShipUiState() finished.");
    }


    // =================== HELPERS ===================

    private bool EnsureUpgradeData()
    {
        if (currentUpgradeData == null)
        {
            Debug.LogWarning("HangarManager: currentUpgradeData is NULL (did display build?).");
            return false;
        }
        return true;
    }

    private string GetCurrentShipNameSafe()
    {
        // Try Ship component's _name; otherwise use prefab name fallback
        if (currentDisplayShip != null && !string.IsNullOrEmpty(currentDisplayShip._name))
            return currentDisplayShip._name;

        var prefab = DataHolder.Instance.shipPrefabs[currentIndex];
        return prefab != null ? prefab.name : $"Ship_{currentIndex}";
    }

    private bool DeductCredits(int price)
    {
        int vCoins = PlayerPrefs.GetInt(coinsKey, 0);
        if (price <= vCoins)
        {
            PlayerPrefs.SetInt(coinsKey, vCoins - price);
            PlayerPrefs.Save();
            return true;
        }
        return false;
    }

    [SerializeField]PlaySoundOnClick p;
    void HandleAudio()
    {
        float volume = PlayerPrefs.GetFloat("SoundVolume");
        p.gameObject.GetComponent<AudioSource>().volume = volume;
    }
}