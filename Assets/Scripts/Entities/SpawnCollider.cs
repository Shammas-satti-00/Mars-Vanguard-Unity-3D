using Unity.Entities;
using Unity.Transforms; // Required for LocalTransform
using UnityEngine;
using Unity.Collections;
using System.Collections;

public class SpawnCollider : MonoBehaviour
{
    [Header("Prefab Assignment")]
    public GameObject asteroidPrefab; // Prefab to spawn at the position of each asteroid entity.
    public float waitTime = 10f; // Time to wait before checking again (in seconds)

    private EntityManager entityManager;

    void Start()
    {
        entityManager = World.DefaultGameObjectInjectionWorld.EntityManager;
        StartCoroutine(CheckAndSpawnAsteroids());
    }

    [ContextMenu("Spawn Collider")]
    public void SpawnTheColliders()
    {
        entityManager = World.DefaultGameObjectInjectionWorld.EntityManager;
        StartCoroutine(CheckAndSpawnAsteroids());
    }

    private IEnumerator CheckAndSpawnAsteroids()
    {
        while (true)
        {
            // Query for all entities with the AsteroidTag component and LocalTransform component
            EntityQuery asteroidQuery = entityManager.CreateEntityQuery(typeof(AsteroidTag), typeof(LocalTransform));

            // Get all entities with the AsteroidTag
            NativeArray<Entity> asteroidEntities = asteroidQuery.ToEntityArray(Unity.Collections.Allocator.TempJob);

            if (asteroidEntities.Length > 0)
            {
                // Loop through each entity and spawn the prefab at its position
                foreach (var entity in asteroidEntities)
                {
                    // Get the position of the asteroid entity from the LocalTransform component
                    var asteroidPosition = entityManager.GetComponentData<LocalTransform>(entity).Position;

                    // Spawn the prefab at the asteroid's position
                    Instantiate(asteroidPrefab, asteroidPosition, Quaternion.identity);
                }
            }
            else
            {
                Debug.Log("No Asteroids found, waiting for 10 seconds...");
            }

            // Dispose the NativeArray after use
            asteroidEntities.Dispose();

            // Wait for the specified time before checking again
            yield return new WaitForSeconds(waitTime);
        }
    }
}
