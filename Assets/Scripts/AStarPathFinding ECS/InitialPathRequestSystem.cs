using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;
using UnityEngine;

namespace AStar3D.ECS
{
    [BurstCompile]
    // Initial path request system (runs once at start)
    [UpdateInGroup(typeof(InitializationSystemGroup))]
    public partial struct InitialPathRequestSystem : ISystem
    {
        private bool hasRun;

        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<DynamicGridConfig>();
            hasRun = false;
           // Debug.Log("[InitialPathRequestSystem] OnCreate called.");
        }
        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            if (hasRun)
            {
                state.Enabled = false;
                return;
            }

    //            Debug.Log("[InitialPathRequestSystem] OnUpdate called.");

            var ecb = new EntityCommandBuffer(Allocator.Temp);
            float currentTime = (float)SystemAPI.Time.ElapsedTime;
            int requestCount = 0;

            // Only process units that don't have a path request yet
            foreach (var (transform, target, entity) in
                SystemAPI.Query<RefRO<LocalTransform>, RefRW<UnitTarget>>()
                    .WithNone<PathfindingRequest>()
                    .WithEntityAccess())
            {
                if (!SystemAPI.HasComponent<LocalTransform>(target.ValueRO.TargetEntity))
                {
      //              Debug.LogWarning($"[InitialPathRequestSystem] Unit {entity.Index} target entity {target.ValueRO.TargetEntity.Index} has no LocalTransform!");
                    continue;
                }

                var targetTransform = SystemAPI.GetComponent<LocalTransform>(target.ValueRO.TargetEntity);

        //        Debug.Log($"[InitialPathRequestSystem] Creating initial path request for entity {entity.Index}");
          //      Debug.Log($"  Start: {transform.ValueRO.Position}");
            //    Debug.Log($"  End: {targetTransform.Position}");

                ecb.AddComponent(entity, new PathfindingRequest
                {
                    Start = transform.ValueRO.Position,
                    End = targetTransform.Position,
                    IsProcessing = false
                });

                // Initialize update timing
                var random = new Unity.Mathematics.Random((uint)(entity.Index + currentTime * 1000));
                float randomDelay = random.NextFloat(target.ValueRO.MinPathUpdateTime,
                                                     target.ValueRO.MaxPathUpdateTime);
                target.ValueRW.NextUpdateTime = currentTime + randomDelay + 0.5f; // Initial delay
                target.ValueRW.LastTargetPosition = targetTransform.Position;

                requestCount++;
            }

            //Debug.Log($"[InitialPathRequestSystem] Created {requestCount} initial path requests");

            ecb.Playback(state.EntityManager);
            ecb.Dispose();

            // Mark as run
            hasRun = true;
            //Debug.Log("[InitialPathRequestSystem] Disabling system after first run.");
        }
    }
}