using UnityEngine;

[CreateAssetMenu(fileName = "EnemyClassData", menuName = "ScriptableObjects/EnemyClassData", order = 1)]
public class EnemyClassData : ScriptableObject
{
    [Header("Class Info")]
    public int classNumber;

    [Header("Vital Stats")]
    public int health;
    public int shield;
    public float healthRegenPerSecond; // Table has floats like 0.5
    public float shieldRegenPerSecond;

    [Header("Weapon Stats")]
    public int cannonDamage;
    //public int cannonBulletSpeed;
    public int missileDamage;
    public int missileSpeed;
    public int lifeTime;

    public string targetTag = "PlayerCollider";


    public int moveSpeed;
    public AIDifficulty difficulty;

    [Header("Rewards")]
    public int rewardCredits;
    public int rewardPerCannon;
   

    
}