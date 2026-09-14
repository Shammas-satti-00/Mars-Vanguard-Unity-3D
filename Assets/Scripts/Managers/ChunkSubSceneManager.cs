using UnityEngine;
using Unity.Entities;
using Unity.Scenes;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine.SceneManagement;
using UnityEngine.Networking; // Added for UnityWebRequest

[System.Serializable]
public class ChunkSubSceneData
{
    public int coordinateX;
    public int coordinateZ;
    public string subSceneName;
    public string subSceneGUID;
}

[System.Serializable]
public class ColliderChunkData
{
    public int coordinateX;
    public int coordinateZ;
    public string colliderSceneName;
}

[System.Serializable]
public class ChunkSubSceneDataCollection
{
    public List<ChunkSubSceneData> ecsChunks = new List<ChunkSubSceneData>();
    public List<ColliderChunkData> colliderChunks = new List<ColliderChunkData>();
    public float chunkSize = 1000f;
    public float colliderChunkSize = 500f;
    public string subScenePrefix = "Chunk_";
    public string colliderChunkPrefix = "ColliderChunk_";
}

public class ChunkSubSceneManager : MonoBehaviour
{
    public static bool loadedFirstTime = false;
    [Header("ECS Chunk Settings")]
    public float chunkSize = 1000f;
    public float loadDistance = 100f;
    public Transform trackedTransform;
    [Header("Layer Filtering")]
    public LayerMask includedLayers = -1;
    [Header("Scene Organization")]
    public GameObject objectsParent;
    public string subScenePrefix = "Chunk_";
    [Header("Data Storage")]
    public string chunkDataFileName = "chunk_data.json";
    [Header("Collider Chunk Settings")]
    public float colliderChunkSize = 500f;
    public float colliderLoadDistance = 200f;
    public LayerMask colliderLayers = -1;
    public string colliderChunkPrefix = "ColliderChunk_";
    [Header("Performance")]
    public int maxLoadsPerFrame = 1;
    public float preloadDistance = 300f;
    public float colliderPreloadDistance = 400f;
    private Dictionary<Vector2Int, (string name, Unity.Entities.Hash128 guid)> chunkSubScenes = new();
    private Dictionary<Vector2Int, Entity> loadedChunkEntities = new();
    private HashSet<Vector2Int> loadedChunks = new();
    private Vector2Int currentChunk;
    private EntityManager entityManager;
    private Dictionary<Vector2Int, string> colliderChunkNames = new();
    private HashSet<Vector2Int> loadedColliderChunks = new();
    private HashSet<Vector2Int> preloadingColliderChunks = new();
    private Dictionary<Vector2Int, AsyncOperation> colliderLoadOperations = new();
    private Vector2Int currentColliderChunk;
    private Queue<Vector2Int> chunkLoadQueue = new();
    private Queue<Vector2Int> colliderLoadQueue = new();
    private Queue<Vector2Int> chunkUnloadQueue = new();
    private Queue<Vector2Int> colliderUnloadQueue = new();

    void Start()
    {
        entityManager = World.DefaultGameObjectInjectionWorld.EntityManager;
        if (trackedTransform == null) trackedTransform = transform;

        // --- MODIFIED: Start data loading asynchronously ---
        StartCoroutine(LoadAndInitialize());
    }

    // --- NEW: Coroutine to handle asynchronous initialization ---
    IEnumerator LoadAndInitialize()
    {
        yield return StartCoroutine(LoadChunkDataAsync());

        // Initialization continues only after the data is loaded
        currentChunk = GetChunkCoordinate(trackedTransform.position);
        currentColliderChunk = GetColliderChunkCoordinate(trackedTransform.position);
        UpdateChunkLoading();
        UpdateColliderChunkLoading();
        StartCoroutine(ProcessLoadQueues());
    }

    void Update()
    {
        if (trackedTransform == null) trackedTransform = transform;
        Vector2Int newChunk = GetChunkCoordinate(trackedTransform.position);
        Vector2Int newColliderChunk = GetColliderChunkCoordinate(trackedTransform.position);

        if (newChunk != currentChunk)
        {
            currentChunk = newChunk;
            UpdateChunkLoading();
        }
        if (newColliderChunk != currentColliderChunk)
        {
            currentColliderChunk = newColliderChunk;
            UpdateColliderChunkLoading();
        }
    }

    public void SetTrackedTransform(Transform newTransform)
    {
        trackedTransform = newTransform;
        currentChunk = GetChunkCoordinate(trackedTransform.position);
        currentColliderChunk = GetColliderChunkCoordinate(trackedTransform.position);
        UpdateChunkLoading();
        UpdateColliderChunkLoading();
    }

    Vector2Int GetChunkCoordinate(Vector3 pos)
    {
        return new Vector2Int(Mathf.FloorToInt(pos.x / chunkSize), Mathf.FloorToInt(pos.z / chunkSize));
    }

    Vector2Int GetColliderChunkCoordinate(Vector3 pos)
    {
        return new Vector2Int(Mathf.FloorToInt(pos.x / colliderChunkSize), Mathf.FloorToInt(pos.z / colliderChunkSize));
    }

    bool IsLayerIncluded(GameObject obj, LayerMask mask) => ((1 << obj.layer) & mask) != 0;
    bool HasParticleSystemInChildren(GameObject go) => go.GetComponentInChildren<ParticleSystem>() != null;

    // --- MODIFIED: The original LoadChunkData is now a coroutine ---
    public IEnumerator LoadChunkDataAsync()
    {
        string fullPath = Path.Combine(Application.streamingAssetsPath, chunkDataFileName);
        string json = null;

        // Check if the path requires UnityWebRequest (Android, WebGL, some iOS cases)
        if (fullPath.Contains("://") || fullPath.Contains(":///"))
        {
            using (UnityWebRequest www = UnityWebRequest.Get(fullPath))
            {
                yield return www.SendWebRequest();

                if (www.result != UnityWebRequest.Result.Success)
                {
                    Debug.LogError($"Chunk data file request failed: {www.error}. Path: {fullPath}");
                    yield break;
                }

                json = www.downloadHandler.text;
            }
        }
        else // Standard file system access (Windows, Mac, Editor)
        {
            if (!File.Exists(fullPath))
            {
                Debug.LogError($"Chunk data file not found at {fullPath}. Make sure to generate chunks first.");
                yield break;
            }
            json = File.ReadAllText(fullPath);
        }

        // --- Extracted logic into a separate method for clarity ---
        ProcessChunkData(json);
    }

    private void ProcessChunkData(string json)
    {
        if (string.IsNullOrEmpty(json))
        {
            Debug.LogError("No JSON data to process.");
            return;
        }

        try
        {
            var data = JsonUtility.FromJson<ChunkSubSceneDataCollection>(json);
            chunkSubScenes.Clear();
            colliderChunkNames.Clear();

            // Safety check for deserialization failure (can happen if JSON is corrupt)
            if (data == null)
            {
                Debug.LogError("Failed to deserialize JSON data into ChunkSubSceneDataCollection.");
                return;
            }

            chunkSize = data.chunkSize;
            colliderChunkSize = data.colliderChunkSize;
            subScenePrefix = data.subScenePrefix;
            colliderChunkPrefix = data.colliderChunkPrefix;

            foreach (var chunk in data.ecsChunks)
            {
                var coord = new Vector2Int(chunk.coordinateX, chunk.coordinateZ);
                chunkSubScenes[coord] = (chunk.subSceneName, new Unity.Entities.Hash128(chunk.subSceneGUID));
            }

            foreach (var chunk in data.colliderChunks)
            {
                var coord = new Vector2Int(chunk.coordinateX, chunk.coordinateZ);
                colliderChunkNames[coord] = chunk.colliderSceneName;
            }

            Debug.Log($"Loaded {chunkSubScenes.Count} ECS chunks and {colliderChunkNames.Count} collider chunks.");
        }
        catch (System.Exception e)
        {
            Debug.LogError($"Failed to process chunk data: {e.Message}\n{e.StackTrace}");
        }
    }

    // --- Rest of the script remains the same ---

    void UpdateChunkLoading()
    {
        var immediate = GetChunksInRange(trackedTransform.position, chunkSize, loadDistance);
        var preload = GetChunksInRange(trackedTransform.position, chunkSize, preloadDistance);

        foreach (var c in loadedChunks)
            if (!preload.Contains(c) && !chunkUnloadQueue.Contains(c))
                chunkUnloadQueue.Enqueue(c);

        var priorityLoad = new List<Vector2Int>(immediate);
        priorityLoad.Sort((a, b) => Vector2Int.Distance(currentChunk, a).CompareTo(Vector2Int.Distance(currentChunk, b)));

        foreach (var c in priorityLoad)
            if (!loadedChunks.Contains(c) && !chunkLoadQueue.Contains(c))
                chunkLoadQueue.Enqueue(c);

        foreach (var c in preload)
            if (!immediate.Contains(c) && !loadedChunks.Contains(c) && !chunkLoadQueue.Contains(c))
                chunkLoadQueue.Enqueue(c);
    }

    HashSet<Vector2Int> GetChunksInRange(Vector3 pos, float size, float dist)
    {
        var chunks = new HashSet<Vector2Int>();
        var cur = new Vector2Int(Mathf.FloorToInt(pos.x / size), Mathf.FloorToInt(pos.z / size));
        chunks.Add(cur);

        float minX = cur.x * size, maxX = (cur.x + 1) * size;
        float minZ = cur.y * size, maxZ = (cur.y + 1) * size;

        bool nearR = (maxX - pos.x) < dist, nearL = (pos.x - minX) < dist;
        bool nearF = (maxZ - pos.z) < dist, nearB = (pos.z - minZ) < dist;

        if (nearR) chunks.Add(new Vector2Int(cur.x + 1, cur.y));
        if (nearL) chunks.Add(new Vector2Int(cur.x - 1, cur.y));
        if (nearF) chunks.Add(new Vector2Int(cur.x, cur.y + 1));
        if (nearB) chunks.Add(new Vector2Int(cur.x, cur.y - 1));

        if (nearR && nearF) chunks.Add(new Vector2Int(cur.x + 1, cur.y + 1));
        if (nearR && nearB) chunks.Add(new Vector2Int(cur.x + 1, cur.y - 1));
        if (nearL && nearF) chunks.Add(new Vector2Int(cur.x - 1, cur.y + 1));
        if (nearL && nearB) chunks.Add(new Vector2Int(cur.x - 1, cur.y - 1));

        return chunks;
    }

    IEnumerator ProcessLoadQueues()
    {
        while (true)
        {
            int loadsThisFrame = 0;
            while (chunkUnloadQueue.Count > 0 && loadsThisFrame < maxLoadsPerFrame)
            {
                UnloadChunk(chunkUnloadQueue.Dequeue());
                loadsThisFrame++;
            }
            while (colliderUnloadQueue.Count > 0 && loadsThisFrame < maxLoadsPerFrame)
            {
                UnloadColliderChunk(colliderUnloadQueue.Dequeue());
                loadsThisFrame++;
            }
            while (chunkLoadQueue.Count > 0 && loadsThisFrame < maxLoadsPerFrame)
            {
                LoadChunk(chunkLoadQueue.Dequeue());
                loadsThisFrame++;
            }
            while (colliderLoadQueue.Count > 0 && loadsThisFrame < maxLoadsPerFrame)
            {
                LoadColliderChunk(colliderLoadQueue.Dequeue());
                loadsThisFrame++;
            }
            yield return null;
        }
    }

    void LoadChunk(Vector2Int chunk)
    {
        if (!chunkSubScenes.ContainsKey(chunk)) return;
        var (_, guid) = chunkSubScenes[chunk];
        var sceneEntity = SceneSystem.LoadSceneAsync(entityManager.World.Unmanaged, guid);
        loadedChunkEntities[chunk] = sceneEntity;
        loadedChunks.Add(chunk);
    }

    void UnloadChunk(Vector2Int chunk)
    {
        if (loadedChunkEntities.TryGetValue(chunk, out Entity e))
        {
            if (entityManager.Exists(e))
                SceneSystem.UnloadScene(entityManager.World.Unmanaged, e);
            loadedChunkEntities.Remove(chunk);
            loadedChunks.Remove(chunk);
        }
    }

    void UpdateColliderChunkLoading()
    {
        var immediate = GetChunksInRange(trackedTransform.position, colliderChunkSize, colliderLoadDistance);
        var preload = GetChunksInRange(trackedTransform.position, colliderChunkSize, colliderPreloadDistance);

        foreach (var c in loadedColliderChunks)
            if (!preload.Contains(c) && !colliderUnloadQueue.Contains(c))
                colliderUnloadQueue.Enqueue(c);

        var priorityLoad = new List<Vector2Int>(immediate);
        priorityLoad.Sort((a, b) => Vector2Int.Distance(currentColliderChunk, a).CompareTo(Vector2Int.Distance(currentColliderChunk, b)));

        foreach (var c in priorityLoad)
            if (!loadedColliderChunks.Contains(c) && !preloadingColliderChunks.Contains(c) && !colliderLoadQueue.Contains(c))
                colliderLoadQueue.Enqueue(c);

        foreach (var c in preload)
            if (!immediate.Contains(c) && !loadedColliderChunks.Contains(c) && !preloadingColliderChunks.Contains(c) && !colliderLoadQueue.Contains(c))
                colliderLoadQueue.Enqueue(c);
    }

    void LoadColliderChunk(Vector2Int chunk)
    {
        if (!colliderChunkNames.TryGetValue(chunk, out string sceneName)) return;

        Scene s = SceneManager.GetSceneByName(sceneName);
        if (s.IsValid() && s.isLoaded)
        {
            loadedColliderChunks.Add(chunk);
            return;
        }

        if (preloadingColliderChunks.Contains(chunk)) return;

        preloadingColliderChunks.Add(chunk);
        StartCoroutine(LoadColliderChunkAsync(chunk, sceneName));
    }

    IEnumerator LoadColliderChunkAsync(Vector2Int chunk, string sceneName)
    {
        var op = SceneManager.LoadSceneAsync(sceneName, LoadSceneMode.Additive);
        if (op == null)
        {
            preloadingColliderChunks.Remove(chunk);
            yield break;
        }

        op.allowSceneActivation = false;
        colliderLoadOperations[chunk] = op;

        while (op.progress < 0.9f)
            yield return null;

        yield return null; // Wait one more frame after progress hits 0.9

        op.allowSceneActivation = true;

        while (!op.isDone)
            yield return null;

        loadedColliderChunks.Add(chunk);
        preloadingColliderChunks.Remove(chunk);
        colliderLoadOperations.Remove(chunk);
    }

    void UnloadColliderChunk(Vector2Int chunk)
    {
        if (colliderLoadOperations.ContainsKey(chunk))
        {
            // If it's still loading, we can stop tracking it
            colliderLoadOperations.Remove(chunk);
            preloadingColliderChunks.Remove(chunk);
        }

        if (!colliderChunkNames.TryGetValue(chunk, out string sceneName)) return;

        Scene s = SceneManager.GetSceneByName(sceneName);
        if (s.IsValid() && s.isLoaded)
            SceneManager.UnloadSceneAsync(s);

        loadedColliderChunks.Remove(chunk);
    }

    public bool AreChunksLoaded()
    {
        if (trackedTransform == null) return false;

        // Get chunks that should be loaded based on current position
        var requiredECSChunks = GetChunksInRange(trackedTransform.position, chunkSize, loadDistance);
        var requiredColliderChunks = GetChunksInRange(trackedTransform.position, colliderChunkSize, colliderLoadDistance);

        // Check if there are any pending loads in queues
        bool hasQueuedLoads = chunkLoadQueue.Count > 0 || colliderLoadQueue.Count > 0;

        // Check if there are any collider chunks still preloading
        bool hasPreloadingChunks = preloadingColliderChunks.Count > 0;

        // Check if all required ECS chunks are loaded
        bool allECSLoaded = true;
        foreach (var chunk in requiredECSChunks)
        {
            if (chunkSubScenes.ContainsKey(chunk) && !loadedChunks.Contains(chunk))
            {
                allECSLoaded = false;
                break;
            }
        }

        // Check if all required collider chunks are loaded
        bool allCollidersLoaded = true;
        foreach (var chunk in requiredColliderChunks)
        {
            if (colliderChunkNames.ContainsKey(chunk) && !loadedColliderChunks.Contains(chunk))
            {
                allCollidersLoaded = false;
                break;
            }
        }

        return allECSLoaded && allCollidersLoaded && !hasQueuedLoads && !hasPreloadingChunks;
    }
}