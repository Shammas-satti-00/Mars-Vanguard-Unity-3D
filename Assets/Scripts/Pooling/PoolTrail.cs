using UnityEngine;

public class PooledTrail : PooledObject
{
    private TrailRenderer _trail;

    void Awake()
    {
        if (!_trail) _trail = GetComponent<TrailRenderer>();
    }

    public override void OnSpawned()
    {
        if (!_trail) _trail = GetComponent<TrailRenderer>();
        // Remove any leftover segments from previous uses and start emitting fresh
        _trail.Clear();
        _trail.emitting = true;
        // Optional: also reset width/gradient if you were animating them
    }

    public override void OnDespawned()
    {
        if (!_trail) _trail = GetComponent<TrailRenderer>();
        // Stop and clear so it comes back clean next time
        _trail.emitting = false;
        _trail.Clear();
    }
}
