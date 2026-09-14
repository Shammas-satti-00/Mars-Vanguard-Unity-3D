using UnityEngine;
using System;

// ---------------- LAUNCHER ----------------
public class UpgradeLauncher : MonoBehaviour
{
    LauncherSlot launcher => GetComponent<LauncherSlot>();

    public int unlockPrice = 400;
    [NonSerialized] public int level = 1;
    public int maxLevel = 10;
    public int UpgradePrice => GetUpgradePrice(level + 1);
    int GetUpgradePrice(int targetLevel)
    {
        if (targetLevel <= 1) return 0;
        float baseCost = Mathf.Max(60f, unlockPrice * 0.45f);
        return Mathf.CeilToInt(baseCost * Mathf.Pow(1.35f, targetLevel - 2) / 10f) * 10;
    }

    [Header("Base stats (L1)")]
    public int baseDamage => launcher.projectileDamage;
    public int baseSpeed => launcher.projectileSpeed;
    // NOTE: In your LauncherSlot, "recoveryRatePerSecond" is used as *seconds per missile*.
    public int baseSecondsPerMissile => launcher.recoveryRatePerSecond; // lower = faster recovery

    //computed
    [NonSerialized] public int projectileDamage;
    [NonSerialized] public int projectileSpeed;
    [NonSerialized] public int recoveryRatePerSecond; // seconds per missile

    public void Recompute()
    {
        int l = Mathf.Clamp(level, 1, maxLevel);

        // Damage: small exponential (~6%/lvl)
        projectileDamage = Mathf.RoundToInt(baseDamage * Mathf.Pow(1.06f, l - 1));

        // Speed: +2 every 2 levels
        projectileSpeed = baseSpeed + 2 * ((l - 1) / 2);

        // Seconds per missile: improves multiplicatively (down to a floor)
        float secs = baseSecondsPerMissile * Mathf.Pow(0.92f, l - 1);
        recoveryRatePerSecond = Mathf.Max(2, Mathf.RoundToInt(secs));
    }
}
