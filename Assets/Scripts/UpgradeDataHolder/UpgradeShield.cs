using UnityEngine;
using System;

// ---------------- SHIELD ----------------
public class UpgradeShield : MonoBehaviour
{
    Shield shield => GetComponent<Shield>();
    public int unlockPrice = 320;
    [NonSerialized] public int level = 1;
    public int maxLevel = 10;
    public int UpgradePrice => GetUpgradePrice(level + 1);
    int GetUpgradePrice(int targetLevel)
    {
        if (targetLevel <= 1) return 0;
        float baseCost = Mathf.Max(55f, unlockPrice * 0.45f);
        return Mathf.CeilToInt(baseCost * Mathf.Pow(1.35f, targetLevel - 2) / 10f) * 10;
    }

    public int baseMaxShield => shield.maxShield;
    public int baseRegen => shield.regenerationRate;

    [NonSerialized] public int maxShield;
    [NonSerialized] public int regenerationRate;

    public void Recompute()
    {
        int l = Mathf.Clamp(level, 1, maxLevel);

        maxShield = Mathf.RoundToInt(baseMaxShield * (1f + 0.1f * (l - 1))); // +10%/lvl
        regenerationRate = baseRegen + (l - 1); // +1 per level
    }
}
