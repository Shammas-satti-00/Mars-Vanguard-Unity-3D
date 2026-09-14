// Combined helper file: Editor-friendly Asteroid Spawner (instantiates PREFAB instances in the Editor)
// and a utility to duplicate GameObject colliders into "entity mode" duplicates (hybrid fallback + optional Entities conversion).

using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.SceneManagement;
#endif

#if UNITY_ENTITIES
using Unity.Entities;
using Unity.Transforms;
using Unity.Rendering;
using Unity.Physics.Authoring;
using Unity.Physics;
using UnityEditor.Experimental.SceneManagement;
#endif

// -----------------------------
// Editor-friendly AsteroidSpawner
// -----------------------------
[ExecuteAlways]
public class AsteroidSpawner : MonoBehaviour
{
    [Header("Asteroid Settings")]
    [SerializeField] private GameObject[] asteroidPrefabs;
    [SerializeField] private int asteroidCount = 100;

    [Header("Arc Shape")]
    [Tooltip("Inner radius of the asteroid arc (distance from center).")]
    [SerializeField] private float innerRadius = 80f;

    [Tooltip("Outer radius of the asteroid arc (distance from center).")]
    [SerializeField] private float outerRadius = 100f;

    [Tooltip("Vertical thickness of the arc (Y-axis spread).")]
    [SerializeField] private float arcHeight = 10f;

    [Tooltip("Starting angle of the arc in degrees (0° = local +X).")]
    [SerializeField] private float arcStartAngle = -45f;

    [Tooltip("Total span of the arc in degrees (e.g., 90° for a quarter circle).")]
    [SerializeField] private float arcSpanAngle = 90f;

    [Header("Size Settings")]
    [SerializeField] private float minSize = 0.5f;
    [SerializeField] private float maxSize = 3f;

    [Header("Rotation Settings")]
    [Tooltip("Min random spin speed for rigidbodies (deg/sec).")]
    [SerializeField] private float minRotationSpeed = 5f;

    [Tooltip("Max random spin speed for rigidbodies (deg/sec).")]
    [SerializeField] private float maxRotationSpeed = 50f;

    [Header("Parent Object")]
    [Tooltip("Optional parent object for asteroids & dust. If null, this spawner transform is used.")]
    [SerializeField] private GameObject parentObject;

    [Header("Dust / Debris")]
    [Tooltip("Optional dust / fog / debris particle prefab to scatter in the arc.")]
    [SerializeField] private GameObject dustPrefab;

    [Tooltip("How many dust particle instances to spawn in the arc.")]
    [SerializeField] private int dustCount = 30;

    [Tooltip("Vertical thickness for dust – can be smaller than arcHeight for a denser mid-plane.")]
    [SerializeField] private float dustHeight = 6f;

    [Tooltip("Random radius offset so dust isn't perfectly aligned with asteroids.")]
    [SerializeField] private float dustRadiusJitter = 3f;

    private bool hasSpawned = false;

    private Transform ArcRoot => parentObject != null ? parentObject.transform : transform;

    private void Start()
    {
        if (!Application.isPlaying && !hasSpawned)
        {
            // In edit mode we don't auto spawn unless user explicitly calls the context menu.
            return;
        }

        if (!hasSpawned)
        {
            SpawnArc();
            hasSpawned = true;
        }
    }

    [ContextMenu("Spawn Asteroid Arc")]
    private void SpawnArc()
    {
        if (asteroidPrefabs == null || asteroidPrefabs.Length == 0)
        {
            Debug.LogWarning("AsteroidSpawner: No asteroidPrefabs assigned.");
            return;
        }

        if (innerRadius < 0f) innerRadius = 0f;
        if (outerRadius <= innerRadius) outerRadius = innerRadius + 1f;
        if (arcSpanAngle <= 0f)
        {
            Debug.LogWarning("AsteroidSpawner: arcSpanAngle must be > 0.");
            return;
        }

        Transform root = ArcRoot;

        SpawnAsteroidsArc(root, root);
        if (dustPrefab != null && dustCount > 0)
        {
            SpawnDustArc(root, root);
        }

        Debug.Log($"AsteroidSpawner: Spawned asteroid arc with {asteroidCount} asteroids" +
                  (dustPrefab != null ? $" and {dustCount} dust instances." : "."));
    }

    private void SpawnAsteroidsArc(Transform asteroidContainer, Transform centerRef)
    {
        for (int i = 0; i < asteroidCount; i++)
        {
            GameObject prefab = asteroidPrefabs[Random.Range(0, asteroidPrefabs.Length)];

            // Random angle within the arc segment
            float angleDeg = Random.Range(arcStartAngle, arcStartAngle + arcSpanAngle);
            float angleRad = angleDeg * Mathf.Deg2Rad;

            // Random radius between inner and outer radius
            float radius = Random.Range(innerRadius, outerRadius);

            // Vertical offset within arc height (thickness)
            float yOffset = Random.Range(-arcHeight * 0.5f, arcHeight * 0.5f);

            // Local position around the center in XZ plane (before parent rotation)
            Vector3 localPos = new Vector3(
                Mathf.Cos(angleRad) * radius,
                yOffset,
                Mathf.Sin(angleRad) * radius
            );

            // Respect the orientation of the parent / spawner
            Vector3 worldPos = centerRef.position + centerRef.TransformDirection(localPos);

            Quaternion rotation = Random.rotation;

#if UNITY_EDITOR
            // In the editor (not playing) instantiate as a PREFAB INSTANCE so the created objects remain linked to the source prefab
            if (!Application.isPlaying)
            {
                GameObject instance = (GameObject)PrefabUtility.InstantiatePrefab(prefab, asteroidContainer);
                instance.transform.position = worldPos;
                instance.transform.rotation = rotation;

                float scale = Random.Range(minSize, maxSize);
                instance.transform.localScale = Vector3.one * scale;

                EnsureRigidbodyForEditor(instance);

                Undo.RegisterCreatedObjectUndo(instance, "Spawn Asteroid Prefab Instance");
                EditorUtility.SetDirty(instance);
                EditorSceneManager.MarkSceneDirty(instance.scene);

                continue;
            }
#endif

            // Runtime fallback
            GameObject asteroid = Instantiate(prefab, worldPos, rotation, asteroidContainer);

            float runtimeScale = Random.Range(minSize, maxSize);
            asteroid.transform.localScale = Vector3.one * runtimeScale;

            Rigidbody rb = asteroid.GetComponent<Rigidbody>();
            if (rb == null)
            {
                rb = asteroid.AddComponent<Rigidbody>();
            }

            rb.useGravity = false;
            rb.isKinematic = false;

            float spinSpeed = Random.Range(minRotationSpeed, maxRotationSpeed);
            Vector3 spinAxis = Random.onUnitSphere;
            rb.angularVelocity = spinAxis * spinSpeed * Mathf.Deg2Rad;
        }
    }

#if UNITY_EDITOR
    private void EnsureRigidbodyForEditor(GameObject instance)
    {
        Rigidbody rb = instance.GetComponent<Rigidbody>();
        if (rb == null)
        {
            rb = Undo.AddComponent<Rigidbody>(instance);
        }

        rb.useGravity = false;
        rb.isKinematic = false;
    }
#endif

    private void SpawnDustArc(Transform dustContainer, Transform centerRef)
    {
        for (int i = 0; i < dustCount; i++)
        {
            float angleDeg = Random.Range(arcStartAngle, arcStartAngle + arcSpanAngle);
            float angleRad = angleDeg * Mathf.Deg2Rad;

            float baseRadius = Random.Range(innerRadius, outerRadius);
            float radius = baseRadius + Random.Range(-dustRadiusJitter, dustRadiusJitter);

            float yOffset = Random.Range(-dustHeight * 0.5f, dustHeight * 0.5f);

            Vector3 localPos = new Vector3(
                Mathf.Cos(angleRad) * radius,
                yOffset,
                Mathf.Sin(angleRad) * radius
            );

            Vector3 worldPos = centerRef.position + centerRef.TransformDirection(localPos);

            Quaternion rotation = Quaternion.LookRotation(
                (centerRef.position - worldPos).normalized,
                centerRef.up
            );

#if UNITY_EDITOR
            if (!Application.isPlaying)
            {
                GameObject dust = (GameObject)PrefabUtility.InstantiatePrefab(dustPrefab, dustContainer);
                dust.transform.position = worldPos;
                dust.transform.rotation = rotation;
                float scale = Random.Range(0.7f, 1.3f);
                dust.transform.localScale *= scale;
                Undo.RegisterCreatedObjectUndo(dust, "Spawn Dust Prefab Instance");
                EditorUtility.SetDirty(dust);
                EditorSceneManager.MarkSceneDirty(dust.scene);
                continue;
            }
#endif

            GameObject dustObj = Instantiate(dustPrefab, worldPos, rotation, dustContainer);
            float runtimeScale = Random.Range(0.7f, 1.3f);
            dustObj.transform.localScale *= runtimeScale;
        }
    }

#if UNITY_EDITOR
    [ContextMenu("Clear Previously Spawned Children (Editor)")]
    private void ClearSpawnedChildren()
    {
        Transform root = ArcRoot;
        for (int i = root.childCount - 1; i >= 0; i--)
        {
            var child = root.GetChild(i).gameObject;
            Undo.DestroyObjectImmediate(child);
        }

        EditorSceneManager.MarkAllScenesDirty();
    }
#endif
}
