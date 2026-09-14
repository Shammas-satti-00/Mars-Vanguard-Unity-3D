
using UnityEngine;
using TMPro;
using UnityEngine.UI;



public class ShipStatsUI : MonoBehaviour
{
    [Header("Refernces")]
    public Image icon;
    public Text shipName;
    public Text level;
    public Text health;
    public Text speed;
    public Text boostSpeed;
    public Text shield;
    public Text shieldRegn;
    public Text healthRegn;
    public Text detectionRange;
    public Text lockOnTime;
    public Text totalCannons;
    public Text cannonsDamage;
    public Text cannonMagzine;
    public Text bulletSpeed;
    public Text cannonReloadRate;
    public Text totalLaunchers;
    public Text launchersDamage;
    public Text launcherCapacity;
    public Text missileSpeed;
    public Text launcherReloadTime;

    private Ship _currentShip;



    public void DisplayValuesOnUI(Ship currentShip)
    {
        _currentShip = currentShip;
        string ship = currentShip._name;
        int sriteIndex = PlayerPrefs.GetInt("Ship_" + ship + "_spriteIndex");
        string engine = PlayerPrefs.GetString("Ship_" + ship + "_engine");
        string core = PlayerPrefs.GetString("Ship_" + ship + "_pilot");
        string shield = PlayerPrefs.GetString("Ship_" + ship + "_shield");
        string radar = PlayerPrefs.GetString("Ship_" + ship + "_radar");
        string repair = PlayerPrefs.GetString("Ship_" + ship + "_repair");
        string cannon = PlayerPrefs.GetString("Ship_" + ship + "_cannon");
        string launcher = PlayerPrefs.GetString("Ship_" + ship + "_launcher");

        icon.sprite = DataHolder.Instance.icons[sriteIndex];
        shipName.text = ship;
        level.text = PlayerPrefs.GetInt("Ship_" + ship + "_level").ToString();
        health.text = PlayerPrefs.GetInt("Ship_" + ship + "_maxHealth").ToString();
        float speed = PlayerPrefs.GetFloat("Engine_" + engine + "_moveSpeed") + 230;
        this.speed.text = speed.ToString();
        boostSpeed.text = (PlayerPrefs.GetFloat("Engine_" + engine + "_boostMultiplier") * speed).ToString();
        this.shield.text = PlayerPrefs.GetInt("Shield_" + shield + "_maxShield").ToString();
        shieldRegn.text = PlayerPrefs.GetInt("Shield_" + shield + "_regenerationRate").ToString();
        healthRegn.text = PlayerPrefs.GetInt("RepairModule_" + repair + "_repairRate").ToString();
        detectionRange.text = PlayerPrefs.GetInt("Radar_" + radar + "_detectionRange").ToString();
        lockOnTime.text = PlayerPrefs.GetFloat("Radar_" + radar + "_lockOnTime").ToString();

        int totalCannons = PlayerPrefs.GetInt("Ship_" + ship + "_totalCannons");
        this.totalCannons.text = totalCannons.ToString();
        int totalCdamage = totalCannons * PlayerPrefs.GetInt("Cannon_" + cannon + "_projectileDamage");
        cannonsDamage.text = totalCdamage.ToString();
        bulletSpeed.text = PlayerPrefs.GetInt("Cannon_" + cannon + "_projectileSpeed").ToString();
        cannonMagzine.text = PlayerPrefs.GetInt("Cannon_" + cannon + "_magazineCapacity").ToString();
        cannonReloadRate.text = PlayerPrefs.GetInt("Cannon_" + cannon + "_recoveryRatePerSecond").ToString();


        int totalLaunchers = PlayerPrefs.GetInt("Ship_" + ship + "_totalLaunchers");
        this.totalLaunchers.text = totalLaunchers.ToString();
        int totalLdamage = totalLaunchers * PlayerPrefs.GetInt("Launcher_" + launcher + "_projectileDamage");
        launchersDamage.text = totalLdamage.ToString();
        missileSpeed.text = PlayerPrefs.GetInt("Launcher_" + launcher + "_projectileSpeed").ToString();
        launcherCapacity.text = PlayerPrefs.GetInt("Launcher_" + launcher + "_magazineCapacity").ToString();
        launcherReloadTime.text = PlayerPrefs.GetInt("Launcher_" + launcher + "_recoveryRatePerSecond").ToString();


    }

    



}
