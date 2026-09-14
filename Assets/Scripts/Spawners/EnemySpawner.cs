// EnemySpawner.cs - Handles only spawning logic
using UnityEngine;

public class EnemySpawner : MonoBehaviour
{
    [Header("Spawn Settings")]
    public Transform spawnPoint;
    public float initialSpawnRadius = 5f;
    public float radiusGrowthFactor = 0.1f;
    public int maxSpawnAttempts = 30; // Maximum attempts to find a valid spawn position

    // We now use the same player reference source as EnemyWaveSpawner:
    // DataHolder.Instance.playerTrasnfrom
    private Transform player;

    private void Awake()
    {
        // Fallback: if no spawnPoint assigned, use this object's transform
        if (spawnPoint == null)
            spawnPoint = transform;
    }

    private void Update()
    {
        // Always try to grab player from DataHolder
        // If DataHolder or playerTrasnfrom is null: no biggie, we'll simply not spawn near player
        if (DataHolder.Instance != null)
        {
            player = DataHolder.Instance.playerTrasnfrom;
        }
        else
        {
            player = null;
        }
    }

    public GameObject SpawnEnemy(GameObject[] enemyArray, int totalEnemiesInWave)
    {
        if (enemyArray == null || enemyArray.Length == 0)
        {
            Debug.LogError("Enemy array is empty or null!");
            return null;
        }

        if (spawnPoint == null)
        {
            Debug.LogError("EnemySpawner: spawnPoint is not assigned!");
            return null;
        }

        float dynamicRadius = initialSpawnRadius + (totalEnemiesInWave * radiusGrowthFactor);
        Vector3 spawnPosition = Vector3.zero;
        bool validPositionFound = false;

        // Try to find a spawn position, preferably not too close to the player (if we have one)
        for (int attempt = 0; attempt < maxSpawnAttempts; attempt++)
        {
            Vector3 randomPosition = spawnPoint.position + Random.insideUnitSphere * dynamicRadius;
            randomPosition.y = spawnPoint.position.y;

            if (player != null)
            {
                float distanceToPlayer = Vector3.Distance(randomPosition, player.position);

                // Only accept if outside the dynamicRadius from the player
                if (distanceToPlayer > dynamicRadius)
                {
                    spawnPosition = randomPosition;
                    validPositionFound = true;
                    break;
                }
            }
            else
            {
                // No player transform available, just accept the first random position
                spawnPosition = randomPosition;
                validPositionFound = true;
                break;
            }
        }

        // If no valid position found after max attempts, spawn anyway (fallback)
        if (!validPositionFound)
        {
            spawnPosition = spawnPoint.position + Random.insideUnitSphere * dynamicRadius;
            spawnPosition.y = spawnPoint.position.y;
            Debug.LogWarning("EnemySpawner: Could not find ideal spawn position. Spawning anyway.");
        }

        int enemyIndex = Random.Range(0, enemyArray.Length);
        GameObject enemy = Instantiate(enemyArray[enemyIndex], spawnPosition, spawnPoint.rotation);

        return enemy;
    }
}
