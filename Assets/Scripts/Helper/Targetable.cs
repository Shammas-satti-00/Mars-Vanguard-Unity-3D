using UnityEngine;
using System.Collections.Generic;

public class Targetable : MonoBehaviour
{
    public static readonly HashSet<Targetable> Registry = new HashSet<Targetable>();

    [Tooltip("Optional: a specific point to aim (e.g., a child transform). If null, uses transform.")]
    public Transform aimPoint;

    [Tooltip("If you have health, set this to false when destroyed.")]
    public bool isTargetable = true;

    public DamageHandler damageHandler;

    public Transform AimTransform => aimPoint != null ? aimPoint : transform;

    private void OnEnable()  => Registry.Add(this);
    private void OnDisable() => Registry.Remove(this);
    private void Start()
    {
        damageHandler = GetComponentInParent<DamageHandler>();
    }
}
