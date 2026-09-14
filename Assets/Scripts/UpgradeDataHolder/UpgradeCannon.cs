using System;
using UnityEngine;
public class UpgradeCannon : MonoBehaviour
{
    CannonSlot slot=>GetComponent<CannonSlot>();
    public int unlockPrice = 300;
    [NonSerialized] public int level = 1;
    public int maxLevel = 10;
    /// <summary>
    /// Cost to go from current level -> next level.
    /// Example curve: starts near 45% of unlock and grows ~35% per level.
    /// </summary>
    public int UpgradePrice => GetUpgradePrice(level + 1);
    public int GetUpgradePrice(int targetLevel)
    {
        // Level 1 is the base (unlocked). Upgrades start at level 2.
        if (targetLevel <= 1) return 0;
        targetLevel = Mathf.Clamp(targetLevel, 2, maxLevel);

        float baseCost = Mathf.Max(50f, unlockPrice * 0.45f);  // floor + scale with unlock
        float growth = 1.35f;                                 // price growth per level
        // Example: L2 ~= 0.45*unlock; L3 ~= +35%; etc.
        float cost = baseCost * Mathf.Pow(growth, targetLevel - 2);
        // Round to nearest 10 for nicer numbers
        return Mathf.CeilToInt(cost / 10f) * 10;
    }
    public int baseProjectileDamage => slot.projectileDamage;  // damage per projectile
    public int baseFireRate => slot.fireRate;   // shots/sec (int in your CannonSlot)
    public int baseMagazineCapacity => slot.magazineCapacity;   // shells
    public int baseRecoveryRatePerSecond => slot.recoveryRatePerSecond;   // shells/sec
    //computed
    [NonSerialized] public int projectileDamage;
    [NonSerialized] public int fireRate;
    [NonSerialized] public int magazineCapacity;
    [NonSerialized] public int recoveryRatePerSecond;
    void OnValidate()
    {
        level = Mathf.Clamp(level, 1, maxLevel);
        Recompute();
    }

    void Awake()
    {
        level = Mathf.Clamp(level, 1, maxLevel);
        Recompute();
    }
    public void Recompute()
    {
        int l = Mathf.Clamp(level, 1, maxLevel);

        // --- Damage: soft exponential (≈ +12%/lvl) -> ~2.8x at L10 (not crazy-OP)
        projectileDamage = Mathf.RoundToInt(baseProjectileDamage * Mathf.Pow(1.12f, l - 1));

        // --- Fire Rate: discrete bumps (cap 3x if base=1)
        // +1 at L4, +1 at L8 (keeps ROF in check vs damage)
        fireRate = baseFireRate
                 + (l >= 4 ? 1 : 0)
                 + (l >= 8 ? 1 : 0);
        fireRate = Mathf.Max(1, fireRate);

        // --- Magazine: +2 every 3 levels (L3/6/9)
        magazineCapacity = baseMagazineCapacity + 2 * ((l - 1) / 3);

        // --- Recovery: +1 at L5 and L9 (keeps uptime improving, but not runaway)
        recoveryRatePerSecond = baseRecoveryRatePerSecond
                              + (l >= 5 ? 1 : 0)
                              + (l >= 9 ? 1 : 0);
    }

    /// <summary>
    /// Attempt to level up (currency deduction handled externally).
    /// Call Recompute then ApplyTo(slot) to push new stats.
    /// </summary>
    public bool LevelUp(CannonSlot slot)
    {
        if (level >= maxLevel) return false;
        level++;
        Recompute();
        ApplyTo(slot);
        return true;
    }

    /// <summary>
    /// Push the computed stats into a CannonSlot.
    /// </summary>
    public void ApplyTo(CannonSlot slot)
    {
        if (!slot) return;

        slot.projectileDamage = projectileDamage;
        slot.fireRate = fireRate;
        slot.magazineCapacity = magazineCapacity;
        slot.recoveryRatePerSecond = recoveryRatePerSecond; // your slot expects "shells/sec"
        // Leave projectileSpeed/lifetime/etc. to the slot or other modules
    }

    /// <summary>
    /// Preview deltas for UI (e.g., green/red arrows).
    /// </summary>
    public (int dmg, int rof, int mag, int rec, int price) GetNextLevelPreview()
    {
        int next = Mathf.Clamp(level + 1, 1, maxLevel);

        int nextDamage = Mathf.RoundToInt(baseProjectileDamage * Mathf.Pow(1.12f, next - 1));
        int nextROF = baseFireRate + (next >= 4 ? 1 : 0) + (next >= 8 ? 1 : 0);
        int nextMag = baseMagazineCapacity + 2 * ((next - 1) / 3);
        int nextRec = baseRecoveryRatePerSecond + (next >= 5 ? 1 : 0) + (next >= 9 ? 1 : 0);

        return (
            dmg: nextDamage - projectileDamage,
            rof: nextROF - fireRate,
            mag: nextMag - magazineCapacity,
            rec: nextRec - recoveryRatePerSecond,
            price: GetUpgradePrice(next)
        );
    }
}
