// ECS component for rotation
using Unity.Entities;
using UnityEngine;

public struct AsteroidRotation : IComponentData
{
    public float RotationSpeed;
    public Vector3 Axis;
}