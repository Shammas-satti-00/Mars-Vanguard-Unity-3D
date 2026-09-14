using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Attach to the Player. Spawns enemies in waves around the player:
/// 1, then 2, then 3, ...
/// </summary>
public class PlayerWaveSpawner : MonoBehaviour
{
    [Header("Target")]
    [Tooltip("If null, will use this GameObject's transform.")]
    public Transform player;

    [Header("Enemy")]
    public GameObject enemyPrefab;

    [Header("Spawn Shape")]
    [Tooltip("Radius of the spawn ring around the player.")]
    public float spawnRadius = 200f;
    [Tooltip("Random vertical offset applied to spawn position (± value).")]
    public float verticalJitter = 10f;

    [Header("Wave Timing")]
    [Tooltip("Seconds to wait after a wave is cleared before spawning the next one.")]
    public float interWaveDelay = 1.0f;

    [Header("Limits")]
    [Tooltip("Optional cap on waves. 0 or less = unlimited.")]
    public int maxWaves = 0;

    [Header("Debug")]
    public bool drawGizmos = true;

    // runtime
    private readonly List<EnemyDeathRelay> _alive = new List<EnemyDeathRelay>(32);
    private int _nextWaveSize = 1;
    private float _waveCooldown = 0f;
    private bool _spawning = false;

    void Awake()
    {
        if (player == null) player = transform;
        if (enemyPrefab == null)
        {
            Debug.LogError("[PlayerWaveSpawner] Enemy Prefab not assigned.");
            enabled = false;
            return;
        }
    }

    void Start()
    {
        TryStartNextWave(); // kick off wave 1 immediately
    }

    void Update()
    {
        // Clean nulls (in case enemies were destroyed without notifying)
        for (int i = _alive.Count - 1; i >= 0; i--)
        {
            if (_alive[i] == null) _alive.RemoveAt(i);
        }

        // If current wave is alive, do nothing
        if (_alive.Count > 0 || _spawning) return;

        // No enemies alive → run cooldown then spawn next wave
        if (_waveCooldown > 0f)
        {
            _waveCooldown -= Time.deltaTime;
            return;
        }

        TryStartNextWave();
    }

    private void TryStartNextWave()
    {
        if (maxWaves > 0 && _nextWaveSize > maxWaves) return;
        StartCoroutine(SpawnWaveRoutine(_nextWaveSize));
        _nextWaveSize++;
    }

    private System.Collections.IEnumerator SpawnWaveRoutine(int count)
    {
        _spawning = true;

        // Precompute ring positions (evenly spaced)
        Vector3 center = player.position;
        float degreesStep = 360f / Mathf.Max(1, count);

        for (int i = 0; i < count; i++)
        {
            float angle = degreesStep * i * Mathf.Deg2Rad;
            Vector3 onRing = new Vector3(Mathf.Cos(angle), 0f, Mathf.Sin(angle));
            Vector3 pos = center + onRing * spawnRadius;

            // vertical jitter
            pos.y += UnityEngine.Random.Range(-verticalJitter, verticalJitter);

            // Face toward player by default
            Quaternion rot = Quaternion.LookRotation((center - pos).normalized, Vector3.up);

            SpawnOne(pos, rot);

            // Optional tiny stagger to avoid frame spikes (comment if not wanted)
            // yield return null;
        }

        _spawning = false;
        _waveCooldown = interWaveDelay;
        yield break;
    }

    private void SpawnOne(Vector3 position, Quaternion rotation)
    {
        GameObject go = Instantiate(enemyPrefab, position, rotation);

        // Ensure a relay exists to notify this spawner on death
        var relay = go.GetComponent<EnemyDeathRelay>();
        if (relay == null) relay = go.AddComponent<EnemyDeathRelay>();

        relay.Initialize(this);
        _alive.Add(relay);
    }

    internal void NotifyEnemyDied(EnemyDeathRelay relay)
    {
        // Remove from list when an enemy dies
        int idx = _alive.IndexOf(relay);
        if (idx >= 0) _alive.RemoveAt(idx);
    }

    void OnDrawGizmosSelected()
    {
        if (!drawGizmos) return;
        Transform t = player != null ? player : transform;

        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(t.position, spawnRadius);

        Gizmos.color = new Color(1f, 1f, 0f, 0.2f);
        Gizmos.DrawCube(t.position + Vector3.up * verticalJitter, new Vector3(1, 0.1f, 1));
        Gizmos.DrawCube(t.position - Vector3.up * verticalJitter, new Vector3(1, 0.1f, 1));
    }
}

/// <summary>
/// Tiny helper that lets the spawner know when this enemy is gone.
/// Add this to your enemy prefab or it will be auto-added at spawn.
/// Prefer calling OnDied() from your health script; otherwise OnDestroy is used as a fallback.
/// </summary>
public class EnemyDeathRelay : MonoBehaviour
{
    private PlayerWaveSpawner _owner;
    private bool _notified = false;

    // If your enemy has a health script with an OnDeath event,
    // call this method from there for precise timing.
    public void OnDied()
    {
        if (_notified) return;
        _notified = true;
        _owner?.NotifyEnemyDied(this);
    }

    public void Initialize(PlayerWaveSpawner owner)
    {
        _owner = owner;
        _notified = false;
    }

    void OnDestroy()
    {
        // Fallback in case no explicit death event was invoked
        OnDied();
    }
}
