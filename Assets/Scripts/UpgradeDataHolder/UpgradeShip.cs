using UnityEngine;

[DisallowMultipleComponent]
public class UpgradeShip : MonoBehaviour
{
    [Header("Pricing")]
    public int unlockPrice = 1000;

    [Tooltip("Upgrade price grows per level using priceMultiplier.")]
    public int baseUpgradePrice = 750;

    [Range(1.05f, 2.5f)]
    public float priceMultiplier = 1.35f;

    [Header("Health Progression")]
    public int baseMaxHealth = 100;
    public int healthPerLevel = 25;

    [Header("Max Level")]
    public int maxLevel = 10;

    [HideInInspector] public int level = 1;

    public int UpgradePriceForLevel(int currentLevel)
    {
        // currentLevel is the level you have *before* buying the next
        // e.g., upgrading from 1 -> 2 uses currentLevel = 1
        if (currentLevel <= 1) return baseUpgradePrice;
        float price = baseUpgradePrice * Mathf.Pow(priceMultiplier, currentLevel - 1);
        return Mathf.RoundToInt(price);
    }

    public int MaxHealthForLevel(int targetLevel)
    {
        targetLevel = Mathf.Max(1, targetLevel);
        return baseMaxHealth + (targetLevel - 1) * healthPerLevel;
    }
}
