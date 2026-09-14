using UnityEngine;

public class Radar: Equipment
{
    public int detectionRange;
    public float lockOnTime;

    public Transform currentTarget;
    public Vector3 cannonTarget;


    public void LoadData()
    {
        detectionRange = PlayerPrefs.GetInt("equippedRadar_detectionRange", detectionRange);
        lockOnTime = PlayerPrefs.GetFloat("equippedRadar_lockOnTime", lockOnTime);
    }
    
    
}