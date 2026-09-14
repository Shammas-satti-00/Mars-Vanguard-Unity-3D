using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;   // ← Added for UnityEvent
using TMPro;

public class EnemyWaveSpawner : MonoBehaviour
{
    // Arrays kept with original names
    public List<EnemySession> enemySessions = new List<EnemySession>();
    public int[] startingEnemiesPerSession;
    public int enemiesPerWave = 1;
    public int[] waveChangeCheckpoints;
    private int currentWave = 0;
    private int waveInSession = 0;
    private int sessionIndex = 0;
    private GameObject[] currentEnemyArray;
    public float delayBeforeSpawn = 2f;
    public TextMeshProUGUI waveText;
    public float displayTime = 5f;
    [Header("Spawner Reference")]
    public EnemySpawner enemySpawner;
    [Header("ScriptableObject")]
    public WaveData waveDataAsset;
    [Header("Player Detection")]
    [Tooltip("Radius used to validate distance from this spawner to the player root object.")]
    public float detectionRadius = 10f;
    [Tooltip("Extra distance beyond detectionRadius after which enemies are force-despawned.")]
    public float extraDespawnDistance = 2000f;
    private Transform player;
    private bool isPlayerCurrentlyInRange = false;
    private bool hasShownBaseNameThisVisit = false;
    [Header("Base Info")]
    [Tooltip("Name of this base, shown in the combat log when the player enters detection range.")]
    public string baseName;
    [Tooltip("Optional Combat Log UI text (like map name pop-up).")]
    public TextMeshProUGUI combatLogText;
    private Coroutine combatLogClearCoroutine;
    private List<GameObject> spawnedEnemies = new List<GameObject>();

    [Header("Events")]                                    // ← New header
    public UnityEvent onAllWavesCompleted;                 // ← The new event you can hook in the Inspector

    // ----------------------- ScriptableObject Helpers -----------------------
    [ContextMenu("Save to ScriptableObject")]
    private void SaveToScriptableObject()
    {
        if (waveDataAsset == null)
        {
            Debug.LogError("No WaveData asset assigned! Create one from Assets > Create > Wave System > Wave Data");
            return;
        }
        waveDataAsset.enemySessions = new List<EnemySession>(enemySessions);
        waveDataAsset.startingEnemiesPerSession = (int[])startingEnemiesPerSession.Clone();
        waveDataAsset.enemiesPerWave = enemiesPerWave;
        waveDataAsset.waveChangeCheckpoints = (int[])waveChangeCheckpoints.Clone();
#if UNITY_EDITOR
        UnityEditor.EditorUtility.SetDirty(waveDataAsset);
        UnityEditor.AssetDatabase.SaveAssets();
#endif
        Debug.Log("Wave data saved to ScriptableObject!");
    }

    [ContextMenu("Load from ScriptableObject")]
    private void LoadFromScriptableObject()
    {
        if (waveDataAsset == null)
        {
            Debug.LogError("No WaveData asset assigned!");
            return;
        }
        enemySessions = new List<EnemySession>(waveDataAsset.enemySessions);
        startingEnemiesPerSession = (int[])waveDataAsset.startingEnemiesPerSession.Clone();
        enemiesPerWave = waveDataAsset.enemiesPerWave;
        waveChangeCheckpoints = (int[])waveDataAsset.waveChangeCheckpoints.Clone();
        Debug.Log("Wave data loaded from ScriptableObject!");
    }

    // ----------------------- Unity Lifecycle -----------------------
    private void Start()
    {
        if (enemySpawner == null)
        {
            Debug.LogError("EnemySpawner not assigned!");
            return;
        }
        if (enemySessions.Count > 0)
        {
            currentEnemyArray = enemySessions[0].enemies;
        }
        StartCoroutine(SpawnWave());
    }

    private void Update()
    {
        // Always try to grab player from DataHolder
        Transform dataHolderPlayer = null;
        if (DataHolder.Instance != null)
        {
            dataHolderPlayer = DataHolder.Instance.playerTrasnfrom;
        }
        if (dataHolderPlayer == null)
        {
            player = null;
            isPlayerCurrentlyInRange = false;
            hasShownBaseNameThisVisit = false;
            return;
        }
        player = dataHolderPlayer;

        bool inRangeNow = IsPlayerWithinRadius();

        if (inRangeNow && !isPlayerCurrentlyInRange)
        {
            ShowBaseNameOncePerVisit();
        }
        else if (!inRangeNow && isPlayerCurrentlyInRange)
        {
            hasShownBaseNameThisVisit = false;
        }
        isPlayerCurrentlyInRange = inRangeNow;

        if (player != null)
        {
            float maxDespawnRadius = detectionRadius + extraDespawnDistance;
            float sqrDistToPlayer = (player.position - transform.position).sqrMagnitude;
            float sqrDespawnRadius = maxDespawnRadius * maxDespawnRadius;
            if (sqrDistToPlayer > sqrDespawnRadius)
            {
                DespawnAllEnemies();
            }
        }
    }

    // ----------------------- Core Logic -----------------------
    IEnumerator SpawnWave()
    {
        while (true)
        {
            yield return new WaitUntil(PlayerCanTriggerSpawns);
            yield return new WaitForSeconds(delayBeforeSpawn);

            bool switchedSession = false; // renamed for clarity

            // Safer check - prevents index-out-of-range if arrays are misconfigured
            if (sessionIndex < waveChangeCheckpoints.Length && waveInSession >= waveChangeCheckpoints[sessionIndex])
            {
                sessionIndex++;
                waveInSession = 0;
                switchedSession = true;

                if (sessionIndex < enemySessions.Count)
                {
                    currentEnemyArray = enemySessions[sessionIndex].enemies;
                }
            }

            // === ALL WAVES COMPLETED CHECK ===
            if (sessionIndex >= enemySessions.Count)
            {
                onAllWavesCompleted.Invoke(); // ← This is the new event

                if (waveText != null)
                {
                    waveText.text = "<b>All Waves Cleared!</b>";
                    StartCoroutine(HideTextAfterDelay());
                }

                yield break; // Stop the spawner - no more waves to spawn
            }

            // Show session custom message only when we actually switched to a valid session
            if (switchedSession && waveText != null)
            {
                string sessionMsg = null;
                if (sessionIndex < enemySessions.Count)
                    sessionMsg = enemySessions[sessionIndex].sessionMessage;

                if (!string.IsNullOrEmpty(sessionMsg))
                {
                    waveText.text = sessionMsg;
                }
                else
                {
                    waveText.text = "<b>Stronger Enemies</b>\nClass " + (sessionIndex + 1);
                }

                StartCoroutine(HideTextAfterDelay());
                yield return new WaitForSeconds(displayTime);
            }

            int startingEnemies = startingEnemiesPerSession[sessionIndex];
            int enemiesToSpawn = startingEnemies + (waveInSession * enemiesPerWave);

            if (waveText != null)
            {
                // Show in the requested format: Wave [digit] (next line) [enemy count]
                waveText.text = $"Wave {currentWave + 1}\n{enemiesToSpawn}";
                StartCoroutine(HideTextAfterDelay());
            }

            spawnedEnemies.Clear();
            for (int i = 0; i < enemiesToSpawn; i++)
            {
                GameObject enemy = enemySpawner.SpawnEnemy(currentEnemyArray, enemiesToSpawn);
                if (enemy != null)
                {
                    spawnedEnemies.Add(enemy);
                    StartCoroutine(FixEngineSpeedAfterDelay(enemy));
                }
                yield return new WaitForSeconds(1f);
            }

            yield return new WaitUntil(AreAllEnemiesDead);

            currentWave++;
            waveInSession++;
            yield return new WaitForSeconds(2f);
        }
    }

    IEnumerator HideTextAfterDelay()
    {
        yield return new WaitForSeconds(displayTime);
        if (waveText != null)
        {
            waveText.text = "";
        }
    }

    IEnumerator ClearCombatLogAfterDelay()
    {
        yield return new WaitForSeconds(displayTime);
        if (combatLogText != null)
        {
            if (combatLogText.text == baseName)
                combatLogText.text = "";
        }
    }

    bool AreAllEnemiesDead()
    {
        foreach (GameObject enemy in spawnedEnemies)
        {
            if (enemy != null && enemy.activeInHierarchy)
            {
                return false;
            }
        }
        return true;
    }

    bool IsPlayerWithinRadius()
    {
        if (player == null) return false;
        float sqrDist = (player.position - transform.position).sqrMagnitude;
        return sqrDist <= detectionRadius * detectionRadius;
    }

    bool PlayerCanTriggerSpawns()
    {
        if (DataHolder.Instance == null || DataHolder.Instance.playerTrasnfrom == null)
            return false;

        player = DataHolder.Instance.playerTrasnfrom;
        if (player == null) return false;

        return IsPlayerWithinRadius();
    }

    private void DespawnAllEnemies()
    {
        if (spawnedEnemies.Count == 0) return;

        for (int i = 0; i < spawnedEnemies.Count; i++)
        {
            if (spawnedEnemies[i] != null)
            {
                Destroy(spawnedEnemies[i]);
            }
        }
        spawnedEnemies.Clear();
    }

    private void ShowBaseNameOncePerVisit()
    {
        if (hasShownBaseNameThisVisit) return;
        hasShownBaseNameThisVisit = true;

        if (string.IsNullOrEmpty(baseName)) return;

        if (combatLogText != null)
        {
            combatLogText.text = baseName;
            if (combatLogClearCoroutine != null)
                StopCoroutine(combatLogClearCoroutine);
            combatLogClearCoroutine = StartCoroutine(ClearCombatLogAfterDelay());
        }
        else if (waveText != null)
        {
            waveText.text = baseName;
            StartCoroutine(HideTextAfterDelay());
        }

        Debug.Log($"EnemyWaveSpawner: Entered base '{baseName}'.");
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, detectionRadius);

        Gizmos.color = Color.yellow;
        float maxDespawnRadius = detectionRadius + extraDespawnDistance;
        Gizmos.DrawWireSphere(transform.position, maxDespawnRadius);
    }

    private IEnumerator FixEngineSpeedAfterDelay(GameObject enemy)
    {
        yield return new WaitForSeconds(2f);

        if (enemy == null) yield break;

        Engine eng = enemy.GetComponent<Engine>();
        if (eng != null)
        {
            if (eng.baseMoveSpeed < 50)
                eng.baseMoveSpeed = 50;

            if (eng.moveSpeed < 50)
                eng.moveSpeed = 50;
        }
    }
}

[System.Serializable]
public class EnemySession
{
    public GameObject[] enemies;

    [TextArea(2, 6)]
    [Tooltip("Optional message shown when this session (class) becomes active. Leave empty for default text.")]
    public string sessionMessage;
}