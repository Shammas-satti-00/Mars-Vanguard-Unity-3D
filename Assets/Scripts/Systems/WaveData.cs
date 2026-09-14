// WaveData.cs - ScriptableObject to store wave configuration
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "WaveData", menuName = "Wave System/Wave Data")]
public class WaveData : ScriptableObject
{
    public List<EnemySession> enemySessions = new List<EnemySession>();
    public int[] startingEnemiesPerSession;
    public int enemiesPerWave = 1;
    public int[] waveChangeCheckpoints;
}
