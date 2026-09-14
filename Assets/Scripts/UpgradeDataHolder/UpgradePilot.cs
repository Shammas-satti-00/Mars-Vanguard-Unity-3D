using System;
using UnityEngine;

public class UpgradePilot : MonoBehaviour
{
    // Cache once, then reuse
    private Pilot pilot => GetComponent<Pilot>();

    [Header("Economy")]
    public int unlockPrice = 300;
    public int level = 1;
    public int maxLevel = 10;
    public int UpgradePrice => GetUpgradePrice(level + 1);

    private int GetUpgradePrice(int targetLevel)
    {
        if (targetLevel <= 1) return 0;
        float baseCost = Mathf.Max(50f, unlockPrice * 0.45f);
        // ~35% per level, rounded to nearest 10
        return Mathf.CeilToInt(baseCost * Mathf.Pow(1.35f, targetLevel - 2) / 10f) * 10;
    }

    //[Header("BASE buffs at Level 1 (tune in Inspector)")]
    //[Tooltip("+ damage per cannon shot (additive)")]
    public int baseAdditionalCannonDamage => pilot.additionalCannonDamage;
    //[Tooltip("+ damage per missile (additive)")]
    public int baseAdditionalLauncherDamage => pilot.additionalLauncherDamage;

    //[Tooltip("Lock-on time reduction fraction (e.g., 0.05 = 5% faster lock)")]
    //[Range(0f, 1f)]
    public float baseLockOnBuff => pilot.lockOnBuff;

    //[Tooltip("+ move speed (additive units)")]
    public int baseMoveSpeedBuff => pilot.moveSpeedBuff;

    //[Tooltip("Boost effectiveness multiplier bonus (e.g., 0.05 = +5%)")]
    public float baseBoostMultiplierBuff => pilot.boostMultiplierBuff;

    //[Tooltip("+ max health (additive)")]
    public int baseAdditionalHealthBuff => pilot.additionalHealthBuff;

    //[Tooltip("+ max shield (additive)")]
    public int baseAdditionalShieldBuff => pilot.additionalShieldBuff;

    //[Tooltip("+ health regen per second (additive)")]
    public int baseAdditionalHealthRegnBuff => pilot.additionalHealthRegnBuff;

    //[Tooltip("+ shield regen per second (additive)")]
    public int baseAdditionalShieldRegnBuff => additionalShieldRegnBuff;

    [Header("Computed buffs (runtime)")]
    [NonSerialized] public int additionalCannonDamage;
    [NonSerialized] public int additionalLauncherDamage;
    [NonSerialized] public float lockOnBuff;              // fraction: 0.10 = 10% faster lock
    [NonSerialized] public int moveSpeedBuff;
    [NonSerialized] public float boostMultiplierBuff;     // fraction: 0.10 = +10% boost power
    [NonSerialized] public int additionalHealthBuff;
    [NonSerialized] public int additionalShieldBuff;
    [NonSerialized] public int additionalHealthRegnBuff;
    [NonSerialized] public int additionalShieldRegnBuff;

    private void Awake()
    {
        level = Mathf.Clamp(level, 1, maxLevel);
        Recompute();
    }

#if UNITY_EDITOR
    private void OnValidate()
    {
        level = Mathf.Clamp(level, 1, maxLevel);
        Recompute();
    }
#endif

    /// <summary>
    /// Recompute current buffs from level.
    /// Scaling is conservative to avoid OP stacking.
    /// </summary>
    public void Recompute()
    {
        int l = Mathf.Clamp(level, 1, maxLevel);
        int k = l - 1;

        // Additive damage buffs: ~+12% per level (rounded), using base as seed
        additionalCannonDamage = Mathf.RoundToInt(baseAdditionalCannonDamage * Mathf.Pow(1.12f, k));
        additionalLauncherDamage = Mathf.RoundToInt(baseAdditionalLauncherDamage * Mathf.Pow(1.10f, k));

        // Lock-on reduction buff: starts at base, +6% of base per level; cap at 60% total reduction
        lockOnBuff = Mathf.Min(0.60f, baseLockOnBuff * (1f + 0.06f * k));

        // Move speed additive: +1 per 2 levels from base
        moveSpeedBuff = baseMoveSpeedBuff + (k / 2);

        // Boost power multiplier bonus: +4% of base per level
        boostMultiplierBuff = baseBoostMultiplierBuff * (1f + 0.04f * k);

        // Survivability (additive): linear with small late-game bump
        additionalHealthBuff = baseAdditionalHealthBuff + k * 5 + (l >= 8 ? 10 : 0);
        additionalShieldBuff = baseAdditionalShieldBuff + k * 5 + (l >= 8 ? 10 : 0);
        additionalHealthRegnBuff = baseAdditionalHealthRegnBuff + (k / 2); // +1 every 2 levels
        additionalShieldRegnBuff = baseAdditionalShieldRegnBuff + (k / 2);
    }


    /// <summary>
    /// Preview next-level deltas for UI.
    /// </summary>
    public (int cDmg, int lDmg, float lockOnFrac, int move, float boostMul, int hp, int sp, int hpR, int spR, int price)
        GetNextLevelPreview()
    {
        int originalLevel = level;
        level = Mathf.Min(level + 1, maxLevel);
        Recompute();

        int n_cDmg = additionalCannonDamage;
        int n_lDmg = additionalLauncherDamage;
        float n_lock = lockOnBuff;
        int n_move = moveSpeedBuff;
        float n_boost = boostMultiplierBuff;
        int n_hp = additionalHealthBuff;
        int n_sp = additionalShieldBuff;
        int n_hpR = additionalHealthRegnBuff;
        int n_spR = additionalShieldRegnBuff;
        int price = UpgradePrice;

        // revert
        level = originalLevel;
        Recompute();

        return (
            n_cDmg - additionalCannonDamage,
            n_lDmg - additionalLauncherDamage,
            n_lock - lockOnBuff,
            n_move - moveSpeedBuff,
            n_boost - boostMultiplierBuff,
            n_hp - additionalHealthBuff,
            n_sp - additionalShieldBuff,
            n_hpR - additionalHealthRegnBuff,
            n_spR - additionalShieldRegnBuff,
            price
        );
    }
}
