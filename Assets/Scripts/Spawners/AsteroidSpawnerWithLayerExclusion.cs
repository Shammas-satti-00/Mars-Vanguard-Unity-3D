using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.SceneManagement;
#endif

[ExecuteAlways]
public class AsteroidSpawnerWithLayerExclusion : MonoBehaviour
{
    [Header("==== NORMAL ASTEROIDS ====")]
    [SerializeField] private GameObject[] normalAsteroidPrefabs;
    [SerializeField] private int normalAsteroidCount = 100;

    [Header("==== SPHERE SETTINGS ====")]
    [SerializeField] private float innerRadius = 0f;
    [SerializeField] private float outerRadius = 80f;
    [SerializeField] private bool filledSphere = true;

    [Header("==== NORMAL ASTEROID SIZE ====")]
    [SerializeField] private float normalMinSize = 0.5f;
    [SerializeField] private float normalMaxSize = 3f;

    [Header("==== CLUSTERS ====")]
    [Tooltip("Asteroids that belong to clusters (different from normal asteroids).")]
    [SerializeField] private GameObject[] clusterAsteroidPrefabs;

    [Tooltip("Dust particles that spawn inside each cluster.")]
    [SerializeField] private GameObject clusterDustPrefab;

    [Tooltip("How many clusters to spawn.")]
    [SerializeField] private int clusterCount = 10;

    [Tooltip("How many asteroids inside each cluster.")]
    [SerializeField] private int asteroidsPerCluster = 6;

    [Tooltip("Distance between asteroids within the cluster.")]
    [SerializeField] private float clusterSpacing = 3f;

    [Tooltip("Size range of cluster asteroids.")]
    [SerializeField] private float clusterMinSize = 1f;
    [SerializeField] private float clusterMaxSize = 4f;

    [Tooltip("Offset radius around center of cluster where asteroids scatter.")]
    [SerializeField] private float clusterRadius = 5f;

    [Header("==== FORBIDDEN LAYERS ====")]
    [SerializeField] private LayerMask forbiddenLayers;
    [SerializeField] private float spawnCheckRadius = 1.5f;

    [Header("==== PARENT & ORIGIN ====")]
    [SerializeField] private GameObject parentObject;

    [Tooltip("If enabled, uses custom world position as spawn origin instead of this GameObject's position.")]
    [SerializeField] private bool useCustomOrigin = false;

    [Tooltip("Custom world position to use as the center of the spawn sphere.")]
    [SerializeField] private Vector3 customSpawnOrigin = Vector3.zero;

    private bool hasSpawned = false;
    private Transform Root => parentObject ? parentObject.transform : transform;
    private Vector3 SpawnOrigin => useCustomOrigin ? customSpawnOrigin : Root.position;

    private void Start()
    {
        if (!Application.isPlaying && !hasSpawned)
            return;

        if (!hasSpawned)
        {
            SpawnAll();
            hasSpawned = true;
        }
    }

    [ContextMenu("Spawn Everything")]
    private void SpawnAll()
    {
        if (outerRadius <= innerRadius)
            outerRadius = innerRadius + 1f;

        Transform root = Root;

        SpawnNormalAsteroids(root);
        SpawnClusters(root);

        Debug.Log("Spawn OK: Normal asteroids + clusters + per-cluster dust.");
    }

    // ============================================================
    // ★ NORMAL ASTEROIDS SCATTER
    // ============================================================
    private void SpawnNormalAsteroids(Transform parent)
    {
        if (normalAsteroidPrefabs == null || normalAsteroidPrefabs.Length == 0)
            return;

        for (int i = 0; i < normalAsteroidCount; i++)
        {
            Vector3 pos = FindValidPosition();
            Quaternion rot = Random.rotation;

            GameObject prefab = normalAsteroidPrefabs[Random.Range(0, normalAsteroidPrefabs.Length)];

            SpawnPrefab(prefab, pos, rot, parent, Random.Range(normalMinSize, normalMaxSize));
        }
    }


    // ============================================================
    // ★ CLUSTERS (ASTEROIDS + PER-CLUSTER DUST)
    // ============================================================
    private void SpawnClusters(Transform parent)
    {
        if (clusterAsteroidPrefabs == null || clusterAsteroidPrefabs.Length == 0)
            return;

        for (int c = 0; c < clusterCount; c++)
        {
            // Pick a valid cluster center
            Vector3 clusterCenter = FindValidPosition();

            // Spawn dust at the exact cluster center
            if (clusterDustPrefab != null)
            {
                SpawnPrefab(clusterDustPrefab, clusterCenter, Quaternion.identity, parent,
                    Random.Range(0.8f, 1.4f));
            }

            // Spawn asteroids around cluster center
            for (int i = 0; i < asteroidsPerCluster; i++)
            {
                Vector3 offset = Random.insideUnitSphere * clusterRadius;
                offset = offset.normalized * (clusterSpacing * Random.Range(0.8f, 1.2f));

                Vector3 asteroidPos = clusterCenter + offset;

                // ensure cluster asteroid avoids forbidden layers
                if (Physics.CheckSphere(asteroidPos, spawnCheckRadius, forbiddenLayers))
                    continue;

                GameObject prefab = clusterAsteroidPrefabs[Random.Range(0, clusterAsteroidPrefabs.Length)];

                SpawnPrefab(prefab, asteroidPos, Random.rotation, parent,
                    Random.Range(clusterMinSize, clusterMaxSize));
            }
        }
    }


    // ============================================================
    // ★ RANDOM SPHERE POSITION (avoiding forbidden layers)
    // ============================================================
    private Vector3 FindValidPosition()
    {
        int tries = 40;
        while (tries-- > 0)
        {
            Vector3 pos = RandomSpherePoint();
            if (!Physics.CheckSphere(pos, spawnCheckRadius, forbiddenLayers))
                return pos;
        }
        return RandomSpherePoint(); // fallback
    }

    private Vector3 RandomSpherePoint()
    {
        Vector3 dir = Random.onUnitSphere;

        float r;
        if (filledSphere)
            r = Mathf.Lerp(innerRadius, outerRadius, Mathf.Pow(Random.value, 1f / 3f));
        else
            r = Random.Range(innerRadius, outerRadius);

        return SpawnOrigin + dir * r;
    }


    // ============================================================
    // ★ SPAWN HELPER
    // ============================================================
    private void SpawnPrefab(GameObject prefab, Vector3 pos, Quaternion rot, Transform parent, float scale)
    {
#if UNITY_EDITOR
        if (!Application.isPlaying)
        {
            GameObject inst = (GameObject)PrefabUtility.InstantiatePrefab(prefab, parent);
            inst.transform.position = pos;
            inst.transform.rotation = rot;
            inst.transform.localScale = Vector3.one * scale;

            Undo.RegisterCreatedObjectUndo(inst, "Spawn Object");
            EditorSceneManager.MarkSceneDirty(inst.scene);
            return;
        }
#endif

        GameObject obj = Instantiate(prefab, pos, rot, parent);
        obj.transform.localScale = Vector3.one * scale;

    }

#if UNITY_EDITOR


    [ContextMenu("Clear Spawned")]
    private void ClearSpawned()
    {
        Transform root = Root;
        for (int i = root.childCount - 1; i >= 0; i--)
            Undo.DestroyObjectImmediate(root.GetChild(i).gameObject);

        EditorSceneManager.MarkAllScenesDirty();
    }

    private void OnDrawGizmosSelected()
    {
        // Draw the spawn sphere in the editor
        Gizmos.color = Color.cyan;
        Gizmos.DrawWireSphere(SpawnOrigin, outerRadius);

        if (innerRadius > 0)
        {
            Gizmos.color = Color.red;
            Gizmos.DrawWireSphere(SpawnOrigin, innerRadius);
        }
    }
#endif
}