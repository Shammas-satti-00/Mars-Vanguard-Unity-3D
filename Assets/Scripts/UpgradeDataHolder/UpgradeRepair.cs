using UnityEngine;
using System;

// ---------------- REPAIR MODULE ----------------
public class UpgradeRepair : MonoBehaviour
{
    RepairModule module => GetComponent<RepairModule>();  
    public int unlockPrice = 200;
    [NonSerialized] public int level = 1;
    public int maxLevel = 10;
    public int UpgradePrice => GetUpgradePrice(level + 1);
    int GetUpgradePrice(int targetLevel)
    {
        if (targetLevel <= 1) return 0;
        float baseCost = Mathf.Max(35f, unlockPrice * 0.45f);
        return Mathf.CeilToInt(baseCost * Mathf.Pow(1.35f, targetLevel - 2) / 10f) * 10;
    }


    public int baseRepairRate => module.repairRate; // HP per second


    [NonSerialized] public int repairRate;

    public void Recompute()
    {
        int l = Mathf.Clamp(level, 1, maxLevel);

        // +1 per level, with a soft extra at high levels
        repairRate = baseRepairRate + (l - 1);
        if (l >= 8) repairRate += 1; // tiny late-game bump
    }
}
