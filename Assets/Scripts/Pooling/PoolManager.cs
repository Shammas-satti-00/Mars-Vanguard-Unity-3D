// PoolManager.cs
using System.Collections.Generic;
using UnityEngine;

public class PoolManager : MonoBehaviour
{
    private static PoolManager _instance;
    public static PoolManager Instance
    {
        get
        {
            if (_instance == null)
            {
                var go = new GameObject("[PoolManager]");
                _instance = go.AddComponent<PoolManager>();
                DontDestroyOnLoad(go);
            }
            return _instance;
        }
    }

    // All inactive objects waiting in queues
    private readonly Dictionary<GameObject, Queue<GameObject>> _pools = new();

    // Objects currently being spawned (not yet safe to reuse)
    private readonly HashSet<GameObject> _pending = new();

    /// <summary>
    /// Safe spawn with pending-system (no more duplicate grabs)
    /// </summary>
    public static GameObject Spawn(GameObject prefab, Vector3 pos, Quaternion rot, Transform parent = null)
    {
        if (prefab == null) return null;

        GameObject obj = null;
        var pools = Instance._pools;

        // Get or create pool for this prefab
        if (!pools.TryGetValue(prefab, out var q))
        {
            q = new Queue<GameObject>(8);
            pools[prefab] = q;
        }

        // Find an object that is NOT pending
        while (q.Count > 0)
        {
            var candidate = q.Peek();
            if (!Instance._pending.Contains(candidate))
            {
                obj = q.Dequeue();
                break;
            }
            else
            {
                // Skip pending objects (never reuse them)
                q.Dequeue();
            }
        }

        // If none available → instantiate new
        if (obj == null)
        {
            obj = Instantiate(prefab);
            var po = obj.GetComponent<PooledObject>();
            if (po == null) po = obj.AddComponent<PooledObject>();
            po.originPrefab = prefab;
        }

        // Mark as pending so no other spawner can claim it
        Instance._pending.Add(obj);

        var pooled = obj.GetComponent<PooledObject>();
        pooled.isInUse = true;

        // Set up transform
        obj.transform.SetPositionAndRotation(pos, rot);
        if (parent) obj.transform.SetParent(parent, false);

        obj.SetActive(true);
        pooled.OnSpawned();

        // Now safe to use
        pooled.isInUse = false;
        Instance._pending.Remove(obj);

        return obj;
    }


    /// <summary>
    /// Return an instance to its pool
    /// </summary>
    public static void Release(GameObject obj)
    {
        if (obj == null) return;

        if (!obj.TryGetComponent<PooledObject>(out var po) || po.originPrefab == null)
        {
            Destroy(obj);
            return;
        }

        // Cannot release while pending/spawning
        if (po.isInUse || Instance._pending.Contains(obj))
            return;

        po.isInUse = true;
        po.OnDespawned();

        if (!Instance._pools.TryGetValue(po.originPrefab, out var q))
        {
            q = new Queue<GameObject>(8);
            Instance._pools[po.originPrefab] = q;
        }

        obj.SetActive(false);
        obj.transform.SetParent(Instance.transform, false);

        q.Enqueue(obj);
        po.isInUse = false;
    }


    /// <summary>
    /// Release after delay
    /// </summary>
    public static void Release(GameObject obj, float delaySeconds)
    {
        Instance.StartCoroutine(Instance.ReleaseAfter(obj, delaySeconds));
    }

    private System.Collections.IEnumerator ReleaseAfter(GameObject obj, float delay)
    {
        yield return new WaitForSeconds(delay);
        Release(obj);
    }


    /// <summary>
    /// Example helper to change volume of all pooled audio objects.
    /// </summary>
    public static void SetVolumeForAllAudioSources(float newVolume)
    {
        foreach (var pool in Instance._pools.Values)
        {
            foreach (var pooledObject in pool)
            {
                var audio = pooledObject.GetComponent<AudioSource>();
                if (audio != null)
                    audio.volume = newVolume;
            }
        }
    }
}
