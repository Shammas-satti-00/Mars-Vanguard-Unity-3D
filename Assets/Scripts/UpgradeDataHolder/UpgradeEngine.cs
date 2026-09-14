using System;
using UnityEngine;

// ---------------- ENGINE ----------------
public class UpgradeEngine : MonoBehaviour
{
    Engine engine=>GetComponent<Engine>();


    public int unlockPrice = 350;
    [NonSerialized] public int level = 1;
    public int maxLevel = 10;
    public int UpgradePrice => GetUpgradePrice(level + 1);
    int GetUpgradePrice(int targetLevel)
    {
        if (targetLevel <= 1) return 0;
        float baseCost = Mathf.Max(50f, unlockPrice * 0.45f);
        return Mathf.CeilToInt(baseCost * Mathf.Pow(1.35f, targetLevel - 2) / 10f) * 10;
    }

    public float baseMoveSpeed => engine.moveSpeed;
    public float baseBoostDuration => engine.boostDuration;

    //computed
    [NonSerialized] public float moveSpeed;
    [NonSerialized] public float boostDuration;
    [NonSerialized] public float boostCooldown;

    public void Recompute()
    {
        int l = Mathf.Clamp(level, 1, maxLevel);

        // Speed: mild growth
        moveSpeed = baseMoveSpeed * (1f + 0.05f * (l - 1)); // +5%/lvl

        // Boost duration: +0.08s/lvl
        boostDuration = baseBoostDuration + 0.08f * (l - 1);

        // Cooldown: reduces with diminishing returns
    }




}
