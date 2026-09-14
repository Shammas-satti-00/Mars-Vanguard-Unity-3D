using UnityEngine;
using System;
// ---------------- RADAR ----------------
public class UpgradeRadar : MonoBehaviour
{
    Radar radar => GetComponent<Radar>();
    public int unlockPrice = 250;
    [NonSerialized] public int level = 1;
    public int maxLevel = 10;
    public int UpgradePrice => GetUpgradePrice(level + 1);
    int GetUpgradePrice(int targetLevel)
    {
        if (targetLevel <= 1) return 0;
        float baseCost = Mathf.Max(40f, unlockPrice * 0.45f);
        return Mathf.CeilToInt(baseCost * Mathf.Pow(1.35f, targetLevel - 2) / 10f) * 10;
    }


    public int baseDetectionRange => radar.detectionRange;
    public float baseLockOnTime => radar.lockOnTime;


    [NonSerialized] public int detectionRange;
    [NonSerialized] public float lockOnTime;

    public void Recompute()
    {
        int l = Mathf.Clamp(level, 1, maxLevel);

        detectionRange = Mathf.RoundToInt(baseDetectionRange * (1f + 0.08f * (l - 1))); // +8%/lvl
        lockOnTime = Mathf.Max(0.3f, baseLockOnTime * Mathf.Pow(0.93f, l - 1)); // -7%/lvl, floor
    }
}
