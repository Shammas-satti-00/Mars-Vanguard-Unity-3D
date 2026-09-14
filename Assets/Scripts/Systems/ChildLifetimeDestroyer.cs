using UnityEngine;
using System.Collections.Generic;

public class ChildLifetimeDestroyer : MonoBehaviour
{
    public float lifetime = 5f;

    private Dictionary<Transform, float> childSpawnTimes = new Dictionary<Transform, float>();

    void Update()
    {
        TrackNewChildren();
        DestroyExpiredChildren();
    }

    void TrackNewChildren()
    {
        for (int i = 0; i < transform.childCount; i++)
        {
            Transform child = transform.GetChild(i);

            if (!childSpawnTimes.ContainsKey(child))
            {
                childSpawnTimes.Add(child, Time.time);
            }
        }
    }

    void DestroyExpiredChildren()
    {
        List<Transform> toRemove = new List<Transform>();

        foreach (var entry in childSpawnTimes)
        {
            Transform child = entry.Key;

            // ✅ Child was destroyed elsewhere
            if (child == null)
            {
                toRemove.Add(child);
                continue;
            }

            // ✅ Lifetime expired
            if (Time.time >= entry.Value + lifetime)
            {
                Destroy(child.gameObject);
                toRemove.Add(child);
            }
        }

        // ✅ Cleanup safely
        foreach (var child in toRemove)
        {
            childSpawnTimes.Remove(child);
        }
    }
}
