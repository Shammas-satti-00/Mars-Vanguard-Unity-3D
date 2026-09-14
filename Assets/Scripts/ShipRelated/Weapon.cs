using UnityEngine;
using System;

[Serializable]
public abstract class Weapon : Equipment
{

    public int projectileSpeed;
    public int projectileDamage;
    public int fireRate;
    public float lifetime;
    public int magazineCapacity;
    public int recoveryRatePerSecond;
    

    public GameObject projectilePrefab;
    public GameObject muzzleFlashPrefab;
    public GameObject impactEffectPrefab;


}

