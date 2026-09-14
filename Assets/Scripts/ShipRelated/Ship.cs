using System.Collections.Generic;
using UnityEngine;



public class Ship : MonoBehaviour
{
    public string _name;
    public int spriteIndex;
    public int maxHealth;
    public int totalCannons;
    public int totalLaunchers;
    public EquipmentManager equipmentManager;

    public DamageHandler damageHandler;







    public void Initalize()
    {
        damageHandler = GetComponent<DamageHandler>();
        equipmentManager = GetComponent<EquipmentManager>();
    }

    public void LoadData()
    {
        maxHealth = PlayerPrefs.GetInt("ship_maxHealth", maxHealth);
    }

    public EquipmentManager GetEquipmentManager()
    {
        return equipmentManager;
    }
    
    void Update()
    {
        equipmentManager?.equippedEngine?.Tick(Time.deltaTime);
    }
}

