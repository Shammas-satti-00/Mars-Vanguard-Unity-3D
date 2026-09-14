using Unity.Burst;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;

// Burst-compiled for performance
[BurstCompile]
public partial struct AsteroidRotationSystem : ISystem
{
    [BurstCompile]
    public void OnUpdate(ref SystemState state)
    {
        float deltaTime = SystemAPI.Time.DeltaTime;

        foreach (var (transform, rotationData) in SystemAPI.Query<RefRW<LocalTransform>, RefRO<AsteroidRotation>>().WithAll<AsteroidTag>())
        {
            float3 axis = math.normalize(rotationData.ValueRO.Axis);
            float angle = rotationData.ValueRO.RotationSpeed * deltaTime;
            quaternion spin = quaternion.AxisAngle(axis, math.radians(angle));

            // Apply rotation
            transform.ValueRW.Rotation = math.mul(transform.ValueRO.Rotation, spin);
        }
    }
}
