using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;
using UnityEngine;

namespace AStar3D.ECS
{
    [BurstCompile]
    [UpdateInGroup(typeof(SimulationSystemGroup))]
    [UpdateBefore(typeof(DynamicPathfindingSystem))]
    public partial struct PathUpdateSystem : ISystem
    {
        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<DynamicGridConfig>();
          //  Debug.Log("[PathUpdateSystem] OnCreate - Requires DynamicGridConfig");
        }
        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            float currentTime = (float)SystemAPI.Time.ElapsedTime;

            var updateJob = new CheckPathUpdateJob
            {
                CurrentTime = currentTime,
                LocalTransformLookup = SystemAPI.GetComponentLookup<LocalTransform>(true)
            };
            updateJob.ScheduleParallel();
            state.Dependency.Complete();

            // Process entities that need path updates
            var ecb = new EntityCommandBuffer(Allocator.TempJob);

            foreach (var (transform, target, entity) in
                SystemAPI.Query<RefRO<LocalTransform>, RefRO<UnitTarget>>()
                    .WithAll<NeedsPathUpdate>()
                    .WithEntityAccess())
            {
                if (!SystemAPI.HasComponent<LocalTransform>(target.ValueRO.TargetEntity))
                    continue;

                var targetTransform = SystemAPI.GetComponent<LocalTransform>(target.ValueRO.TargetEntity);

                // Add or update pathfinding request
                if (!SystemAPI.HasComponent<PathfindingRequest>(entity))
                {
                    ecb.AddComponent(entity, new PathfindingRequest
                    {
                        Start = transform.ValueRO.Position,
                        End = targetTransform.Position,
                        IsProcessing = false
                    });
                }
                else
                {
                    ecb.SetComponent(entity, new PathfindingRequest
                    {
                        Start = transform.ValueRO.Position,
                        End = targetTransform.Position,
                        IsProcessing = false
                    });
                }

                ecb.SetComponentEnabled<NeedsPathUpdate>(entity, false);
            }

            ecb.Playback(state.EntityManager);
            ecb.Dispose();
        }

        [BurstCompile]
        [WithAll(typeof(UnitTarget))]
        [WithDisabled(typeof(NeedsPathUpdate))]
        partial struct CheckPathUpdateJob : IJobEntity
        {
            public float CurrentTime;
            [ReadOnly] public ComponentLookup<LocalTransform> LocalTransformLookup;

            void Execute(Entity entity, ref UnitTarget target,
                        EnabledRefRW<NeedsPathUpdate> needsUpdate)
            {
                // Check if it's time for an update
                if (CurrentTime < target.NextUpdateTime)
                    return;

                // Check if target exists and has moved significantly
                if (!LocalTransformLookup.HasComponent(target.TargetEntity))
                    return;

                var targetTransform = LocalTransformLookup[target.TargetEntity];
                float sqrDistance = math.distancesq(targetTransform.Position, target.LastTargetPosition);
                float sqrThreshold = target.PathUpdateThreshold * target.PathUpdateThreshold;

                if (sqrDistance > sqrThreshold)
                {
                    needsUpdate.ValueRW = true;
                    target.LastTargetPosition = targetTransform.Position;

                    // Set next update time with randomization to spread load
                    var random = new Unity.Mathematics.Random((uint)(entity.Index + CurrentTime * 1000));
                    float randomDelay = random.NextFloat(target.MinPathUpdateTime, target.MaxPathUpdateTime);
                    target.NextUpdateTime = CurrentTime + randomDelay;
                }
                else
                {
                    // Still schedule next check even if no update needed
                    var random = new Unity.Mathematics.Random((uint)(entity.Index + CurrentTime * 1000));
                    float randomDelay = random.NextFloat(target.MinPathUpdateTime, target.MaxPathUpdateTime);
                    target.NextUpdateTime = CurrentTime + randomDelay;
                }
            }
        }
    }
}