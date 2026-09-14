
#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.SceneManagement;
#endif
using UnityEngine;
using System.Collections.Generic;
using System.IO;
using UnityEngine.SceneManagement;
using System.Linq;

// NOTE: The classes ChunkSubSceneDataCollection, ChunkSubSceneData, ColliderChunkData, and EnvironmentCollision
// must exist in your project for this script to compile and run.

public class ChunkSubSceneGenerator : MonoBehaviour
{
    [Header("ECS Chunk Settings")]
    public float chunkSize = 1000f;

    [Header("Scene Organization")]
    public GameObject objectsParent;
    public string subScenePrefix = "Chunk_";

    [Header("Data Storage")]
    public string chunkDataFileName = "chunk_data.json";

    [Header("Collider Chunk Settings")]
    public float colliderChunkSize = 500f;
    public LayerMask colliderLayers = -1;
    public string colliderChunkPrefix = "ColliderChunk_";

#if UNITY_EDITOR
    // ---------------------- Helper Methods ----------------------
    GameObject GetMovableRoot(GameObject go)
    {
        // Gets the outermost Prefab instance root, or the GameObject itself if it's not a Prefab instance.
        GameObject prefabRoot = PrefabUtility.GetOutermostPrefabInstanceRoot(go);
        return prefabRoot != null ? prefabRoot : go;
    }

    bool IsChildOfParent(Transform t, Transform parent)
    {
        if (t == null) return false;
        Transform cur = t;
        while (cur != null)
        {
            if (cur == parent) return true;
            cur = cur.parent;
        }
        return false;
    }

    Vector2Int GetChunkCoordinate(Vector3 pos) => new Vector2Int(Mathf.FloorToInt(pos.x / chunkSize), Mathf.FloorToInt(pos.z / chunkSize));
    Vector2Int GetColliderChunkCoordinate(Vector3 pos) => new Vector2Int(Mathf.FloorToInt(pos.x / colliderChunkSize), Mathf.FloorToInt(pos.z / colliderChunkSize));
    bool IsLayerIncluded(GameObject obj, LayerMask mask) => ((1 << obj.layer) & mask) != 0;
    bool HasParticleSystemInChildren(GameObject go) => go.GetComponentInChildren<ParticleSystem>() != null;

    // ---------------------- JSON Saving ----------------------
    void SaveChunkDataToJSON(Dictionary<Vector2Int, (string name, string guid)> ecsChunks,
                             Dictionary<Vector2Int, (string sceneName, string scenePath)> colliderChunks)
    {
        var data = new ChunkSubSceneDataCollection
        {
            chunkSize = chunkSize,
            colliderChunkSize = colliderChunkSize,
            subScenePrefix = subScenePrefix,
            colliderChunkPrefix = colliderChunkPrefix
        };

        foreach (var kvp in ecsChunks)
        {
            data.ecsChunks.Add(new ChunkSubSceneData
            {
                coordinateX = kvp.Key.x,
                coordinateZ = kvp.Key.y,
                subSceneName = kvp.Value.name,
                subSceneGUID = kvp.Value.guid
            });
        }

        foreach (var kvp in colliderChunks)
        {
            data.colliderChunks.Add(new ColliderChunkData
            {
                coordinateX = kvp.Key.x,
                coordinateZ = kvp.Key.y,
                colliderSceneName = kvp.Value.sceneName
            });
        }

        string streamingAssetsPath = Application.streamingAssetsPath;
        if (!Directory.Exists(streamingAssetsPath))
            Directory.CreateDirectory(streamingAssetsPath);

        string fullPath = Path.Combine(streamingAssetsPath, chunkDataFileName);
        File.WriteAllText(fullPath, JsonUtility.ToJson(data, true));
        AssetDatabase.Refresh();
        Debug.Log($"Saved {data.ecsChunks.Count} ECS chunks and {data.colliderChunks.Count} collider chunks to {fullPath}");
    }

    void AddColliderScenesToBuildSettings(Dictionary<Vector2Int, (string sceneName, string scenePath)> colliderChunks)
    {
        var buildScenes = new List<EditorBuildSettingsScene>(EditorBuildSettings.scenes);
        var existingPaths = new HashSet<string>(buildScenes.Select(s => s.path));
        int addedCount = 0;
        foreach (var kvp in colliderChunks)
        {
            string path = kvp.Value.scenePath;
            if (!existingPaths.Contains(path))
            {
                buildScenes.Add(new EditorBuildSettingsScene(path, true));
                existingPaths.Add(path);
                addedCount++;
            }
        }
        EditorBuildSettings.scenes = buildScenes.ToArray();
        Debug.Log($"Added {addedCount} collider scenes to Build Settings (total: {buildScenes.Count})");
    }

    // ---------------------- Generate Only SubScenes ----------------------
    [ContextMenu("Generate Only SubScenes")]
    public void GenerateOnlySubScenes()
    {
        if (objectsParent == null)
        {
            Debug.LogError("No objects parent assigned!");
            return;
        }

        // Load existing JSON (unchanged)
        string jsonPath = Path.Combine(Application.streamingAssetsPath, chunkDataFileName);
        ChunkSubSceneDataCollection existingData = null;
        if (File.Exists(jsonPath))
        {
            string json = File.ReadAllText(jsonPath);
            existingData = JsonUtility.FromJson<ChunkSubSceneDataCollection>(json);
        }
        if (existingData == null) existingData = new ChunkSubSceneDataCollection();

        // -----------------------------------------------------------------------------------
        // FIX: Collect ONLY direct children of objectsParent to be moved.
        // -----------------------------------------------------------------------------------
        var ecsChunkRoots = new Dictionary<Vector2Int, HashSet<GameObject>>();

        // Iterate over immediate children of objectsParent's transform
        foreach (Transform t in objectsParent.transform)
        {
            // Only consider active GameObjects
            if (t == null || !t.gameObject.activeInHierarchy) continue;

            // Get the movable root (essential for correct Prefab handling)
            GameObject movableRoot = GetMovableRoot(t.gameObject);

            // Safety check: Ensure the movableRoot is the object we are currently iterating over
            // or that its parent is the objectsParent (if it was an indirect child of a prefab that got moved).
            // For direct iteration, 't.gameObject' should equal 'movableRoot'
            if (movableRoot != t.gameObject)
            {
                // This means 't' is a child of a Prefab Instance root that is also a child of objectsParent.
                // We should only process the Prefab Instance root, which is 't'.
                // Since we are iterating direct children, 't' *is* the root we care about in this context.
            }

            // The object 't' is the root we want to move.
            Vector2Int chunk = GetChunkCoordinate(t.position);
            if (!ecsChunkRoots.ContainsKey(chunk)) ecsChunkRoots[chunk] = new HashSet<GameObject>();
            ecsChunkRoots[chunk].Add(t.gameObject);
        }
        // -----------------------------------------------------------------------------------

        string mainScenePath = EditorSceneManager.GetActiveScene().path;
        string scenesFolder = Path.GetDirectoryName(mainScenePath);
        string ecsFolder = $"{scenesFolder}/SubScenes";
        Directory.CreateDirectory(ecsFolder);

        var generatedEcsChunks = new Dictionary<Vector2Int, (string name, string guid)>();

        foreach (var kvp in ecsChunkRoots)
        {
            Vector2Int chunk = kvp.Key;
            HashSet<GameObject> rootObjects = kvp.Value;
            rootObjects.RemoveWhere(go => go == null);
            if (rootObjects.Count == 0) continue;

            string subSceneName = $"{subScenePrefix}{chunk.x}_{chunk.y}";
            string scenePath = $"{ecsFolder}/{subSceneName}.unity";
            Scene newScene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Additive);
            newScene.name = subSceneName;

            // Create the new empty root object in the new scene
            GameObject newSceneRoot = new GameObject($"ChunkRoot_{chunk.x}_{chunk.y}");
            SceneManager.MoveGameObjectToScene(newSceneRoot, newScene);

            foreach (var obj in rootObjects)
            {
                // 1. Unparent the object from objectsParent. 
                obj.transform.SetParent(null, true);

                // 2. Move the now-root object to the new scene.
                SceneManager.MoveGameObjectToScene(obj, newScene);

                // 3. Parent it under the empty root object in the new scene.
                obj.transform.SetParent(newSceneRoot.transform, true);
            }

            EditorSceneManager.SaveScene(newScene, scenePath);
            string sceneGUID = AssetDatabase.AssetPathToGUID(scenePath);
            EditorSceneManager.CloseScene(newScene, true);

            generatedEcsChunks[chunk] = (subSceneName, sceneGUID);
            Debug.Log($"Created ECS chunk {subSceneName} at coord {chunk}");
        }

        // Preserve existing collider chunks (unchanged)
        var colliderChunks = existingData?.colliderChunks ?? new List<ColliderChunkData>();
        SaveChunkDataToJSON(generatedEcsChunks, colliderChunks.ToDictionary(c => new Vector2Int(c.coordinateX, c.coordinateZ), c => (c.colliderSceneName, "")));

        AssetDatabase.Refresh();
        Debug.Log("SubScenes generation complete.");
    }

    // ---------------------- Generate Only Collider Scenes ----------------------
    [ContextMenu("Generate Only Collider Scenes")]
    public void GenerateOnlyColliderScenes()
    {
        if (objectsParent == null)
        {
            Debug.LogError("No objects parent assigned!");
            return;
        }

        // Load existing JSON
        string jsonPath = Path.Combine(Application.streamingAssetsPath, chunkDataFileName);
        ChunkSubSceneDataCollection existingData = null;
        if (File.Exists(jsonPath))
        {
            string json = File.ReadAllText(jsonPath);
            existingData = JsonUtility.FromJson<ChunkSubSceneDataCollection>(json);
        }
        if (existingData == null) existingData = new ChunkSubSceneDataCollection();

        var allTransforms = objectsParent.GetComponentsInChildren<Transform>(true);
        var colliderChunkObjects = new Dictionary<Vector2Int, List<GameObject>>();

        // This collection logic is fine for collider baking as it just collects references to the source GameObjects
        foreach (var t in allTransforms)
        {
            if (t == objectsParent.transform) continue;
            if (!IsChildOfParent(t, objectsParent.transform)) continue;

            bool hasColliderLayer = IsLayerIncluded(t.gameObject, colliderLayers);
            bool hasCollider = t.gameObject.GetComponent<Collider>() != null;
            bool hasParticle = HasParticleSystemInChildren(t.gameObject);

            if ((hasColliderLayer && hasCollider) || hasParticle)
            {
                Vector2Int collChunk = GetColliderChunkCoordinate(t.position);
                if (!colliderChunkObjects.ContainsKey(collChunk)) colliderChunkObjects[collChunk] = new List<GameObject>();
                colliderChunkObjects[collChunk].Add(t.gameObject);
            }
        }

        string mainScenePath = EditorSceneManager.GetActiveScene().path;
        string scenesFolder = Path.GetDirectoryName(mainScenePath);
        string colliderFolder = $"{scenesFolder}/ColliderChunks";
        Directory.CreateDirectory(colliderFolder);

        var generatedColliderChunks = new Dictionary<Vector2Int, (string sceneName, string scenePath)>();

        foreach (var kvp in colliderChunkObjects)
        {
            Vector2Int chunk = kvp.Key;
            List<GameObject> objects = kvp.Value;
            if (objects.Count == 0) continue;

            string sceneName = $"{colliderChunkPrefix}{chunk.x}_{chunk.y}";
            string scenePath = $"{colliderFolder}/{sceneName}.unity";
            Scene newScene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Additive);
            newScene.name = sceneName;

            GameObject root = new GameObject($"ColliderChunkRoot_{chunk.x}_{chunk.y}");
            SceneManager.MoveGameObjectToScene(root, newScene);

            foreach (var source in objects)
            {
                Collider col = source.GetComponent<Collider>();
                // Check if we should bake the collider or if it's a particle system root
                bool bakeCollider = col != null && IsLayerIncluded(source, colliderLayers);
                bool bakeParticle = HasParticleSystemInChildren(source);

                if (bakeCollider || bakeParticle)
                {
                    GameObject empty = new GameObject(bakeCollider ? $"Collider_{source.name}" : $"Particle_{source.name}");
                    empty.transform.position = source.transform.position;
                    empty.transform.rotation = source.transform.rotation;
                    empty.transform.localScale = source.transform.lossyScale;
                    SceneManager.MoveGameObjectToScene(empty, newScene);
                    empty.transform.SetParent(root.transform, true);

                    if (bakeCollider)
                    {
                        empty.AddComponent<EnvironmentCollision>();

                        if (col is BoxCollider box)
                        {
                            var c = empty.AddComponent<BoxCollider>();
                            c.center = box.center; c.size = box.size; c.isTrigger = box.isTrigger;
                        }
                        else if (col is SphereCollider sphere)
                        {
                            var c = empty.AddComponent<SphereCollider>();
                            c.center = sphere.center; c.radius = sphere.radius; c.isTrigger = sphere.isTrigger;
                        }
                        else if (col is CapsuleCollider capsule)
                        {
                            var c = empty.AddComponent<CapsuleCollider>();
                            c.center = capsule.center; c.radius = capsule.radius; c.height = capsule.height;
                            c.direction = capsule.direction; c.isTrigger = capsule.isTrigger;
                        }
                        else if (col is MeshCollider meshCol)
                        {
                            var c = empty.AddComponent<MeshCollider>();
                            c.sharedMesh = meshCol.sharedMesh; c.convex = meshCol.convex; c.isTrigger = meshCol.isTrigger;
                        }

                        empty.layer = 3;
                    }
                }
            }

            EditorSceneManager.SaveScene(newScene, scenePath);
            EditorSceneManager.CloseScene(newScene, true);
            generatedColliderChunks[chunk] = (sceneName, scenePath);
            Debug.Log($"Created collider chunk {sceneName} at coord {chunk}");
        }

        // Preserve existing ECS chunks
        var ecsChunks = existingData?.ecsChunks ?? new List<ChunkSubSceneData>();
        SaveChunkDataToJSON(ecsChunks.ToDictionary(c => new Vector2Int(c.coordinateX, c.coordinateZ), c => (c.subSceneName, c.subSceneGUID)),
                             generatedColliderChunks);

        AddColliderScenesToBuildSettings(generatedColliderChunks);
        AssetDatabase.Refresh();
        Debug.Log("Collider scenes generation complete.");
    }

    [ContextMenu("Generate All Chunks")]
    public void GenerateAllChunks()
    {
        if (objectsParent == null)
        {
            Debug.LogError("No objects parent assigned!");
            return;
        }

        GenerateOnlyColliderScenes();
        // First, generate ECS SubScenes
        GenerateOnlySubScenes();

        // Then, generate Collider Scenes


        Debug.Log("All chunks generation complete (SubScenes + Collider Scenes).");
    }

    [ContextMenu("Maintenance/Change Collider Layers to Environment")]
    public void ChangeColliderLayersToEnvironment()
    {
        // 1. Get the target layer index
        int environmentLayer = LayerMask.NameToLayer("Enviroment");

        if (environmentLayer == -1)
        {
            Debug.LogError("Layer 'Environment' not found. Please create it in Project Settings -> Tags and Layers.");
            return;
        }

        // 2. Load the JSON data to get scene names and paths (essential step)
        string jsonPath = Path.Combine(Application.streamingAssetsPath, chunkDataFileName);
        ChunkSubSceneDataCollection data = null;
        if (File.Exists(jsonPath))
        {
            try
            {
                string json = File.ReadAllText(jsonPath);
                data = JsonUtility.FromJson<ChunkSubSceneDataCollection>(json);
            }
            catch (System.Exception e)
            {
                Debug.LogError($"Failed to load chunk data from JSON: {e.Message}");
                return;
            }
        }

        if (data == null || data.colliderChunks.Count == 0)
        {
            Debug.LogError("No collider chunk data found in JSON. Run 'Generate Only Collider Scenes' first.");
            return;
        }

        // Save the currently active scene to ensure work isn't lost before unloading
        Scene currentScene = EditorSceneManager.GetActiveScene();
        EditorSceneManager.SaveScene(currentScene);

        // 3. Process each Collider Chunk Scene
        int totalScenesProcessed = 0;
        int totalObjectsChanged = 0;

        // Determine the scenes folder path based on the main scene
        string mainScenePath = EditorSceneManager.GetActiveScene().path;
        string scenesFolder = Path.GetDirectoryName(mainScenePath);
        string colliderFolder = $"{scenesFolder}/ColliderChunks";

        foreach (var chunk in data.colliderChunks)
        {
            string sceneName = chunk.colliderSceneName;
            // Reconstruct the full path based on generation logic
            string scenePath = $"{colliderFolder}/{sceneName}.unity";

            if (File.Exists(scenePath))
            {
                // Open the scene additively
                Scene sceneToModify = EditorSceneManager.OpenScene(scenePath, OpenSceneMode.Additive);

                int objectsChangedInScene = 0;

                // Iterate over ALL root GameObjects in the scene
                foreach (GameObject rootObj in sceneToModify.GetRootGameObjects())
                {
                    // Set the layer of the root object
                    if (rootObj.layer != environmentLayer)
                    {
                        rootObj.layer = environmentLayer;
                        objectsChangedInScene++;
                    }

                    // Set the layer of all children using GetComponentsInChildren
                    foreach (Transform t in rootObj.GetComponentsInChildren<Transform>(true))
                    {
                        if (t.gameObject.layer != environmentLayer)
                        {
                            t.gameObject.layer = environmentLayer;
                            objectsChangedInScene++;
                        }
                    }
                }

                // Save and close the scene
                if (objectsChangedInScene > 0)
                {
                    EditorSceneManager.SaveScene(sceneToModify);
                    totalObjectsChanged += objectsChangedInScene;
                }

                EditorSceneManager.CloseScene(sceneToModify, true);
                totalScenesProcessed++;
            }
            else
            {
                Debug.LogWarning($"Collider scene file not found: {scenePath}. Skipping.");
            }
        }

        Debug.Log($"✅ Layer change complete. Processed {totalScenesProcessed} scenes and changed layer on {totalObjectsChanged} GameObjects/Transforms to 'Environment' ({environmentLayer}).");
    }

#endif


}
