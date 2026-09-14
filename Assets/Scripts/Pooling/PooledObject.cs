using UnityEngine;

public class PooledObject : MonoBehaviour
{
    [HideInInspector] public GameObject originPrefab;
    [HideInInspector] public bool isInUse = false;

    public virtual void OnSpawned() { }
    public virtual void OnDespawned() { }
}
