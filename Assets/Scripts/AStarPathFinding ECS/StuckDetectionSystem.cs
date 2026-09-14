using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;
using Unity.Burst;
using Unity.Collections;
using UnityEngine;

namespace AStar3D.ECS
{
    [BurstCompile]
    [UpdateInGroup(typeof(SimulationSystemGroup))]
    [UpdateAfter(typeof(UnitMovementSystem))]
    public partial struct StuckDetectionSystem : ISystem
    {
        private int updateCount;

        [BurstCompile]
        public void OnCreate(ref SystemState state)
        {
            updateCount = 0;
            //UnityEngine.Debug.Log("[StuckDetectionSystem] OnCreate called");
        }

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            updateCount++;
            if (updateCount % 120 == 0) // Log every 2 seconds at 60fps
            {
                //UnityEngine.Debug.Log($"[StuckDetectionSystem] OnUpdate #{updateCount}");
            }

            float currentTime = (float)SystemAPI.Time.ElapsedTime;

            var stuckCheckJob = new StuckCheckJob
            {
                CurrentTime = currentTime
            };
            stuckCheckJob.ScheduleParallel();
        }

        [BurstCompile]
        partial struct StuckCheckJob : IJobEntity
        {
            public float CurrentTime;

            void Execute(Entity entity, ref StuckDetection stuckDetection, in LocalTransform transform,
                        ref UnitMovement movement)
            {
                // Check if it's time to evaluate
                if (CurrentTime < stuckDetection.CheckTime)
                    return;

                // Calculate distance moved since last check
                float distanceMoved = math.distance(transform.Position, stuckDetection.LastPosition);

                // If unit hasn't moved much and is supposed to be following a path, consider it stuck
                if (distanceMoved < stuckDetection.StuckThreshold && movement.IsFollowingPath)
                {
                    //UnityEngine.Debug.LogWarning($"[StuckDetection] Entity {entity.Index} is STUCK! Distance moved: {distanceMoved:F3}");

                    // Stop current path - PathfindingBridge will handle requesting new one
                    movement.IsFollowingPath = false;
                    movement.CurrentWaypointIndex = 0;
                }

                // Always update for next check
                stuckDetection.LastPosition = transform.Position;
                stuckDetection.CheckTime = CurrentTime + 2.0f; // Check every 2 seconds
            }
        }
    }

    // System to initialize stuck detection for new units
    [UpdateInGroup(typeof(InitializationSystemGroup))]
    public partial struct StuckDetectionInitSystem : ISystem
    {
        private bool hasRun;

        public void OnCreate(ref SystemState state)
        {
            hasRun = false;
//            UnityEngine.Debug.Log("[StuckDetectionInitSystem] OnCreate called");
        }

        public void OnUpdate(ref SystemState state)
        {
            // Only run once per entity, then disable
            if (hasRun)
            {
                state.Enabled = false;
                return;
            }

  //          UnityEngine.Debug.Log($"[StuckDetectionInitSystem] Running initialization");

            float currentTime = (float)SystemAPI.Time.ElapsedTime;
            var ecb = new EntityCommandBuffer(Allocator.Temp);

            int addedCount = 0;

            // Add stuck detection to units that don't have it
            foreach (var (transform, entity) in
                SystemAPI.Query<RefRO<LocalTransform>>()
                    .WithAll<UnitMovement>()
                    .WithNone<StuckDetection>()
                    .WithEntityAccess())
            {
                ecb.AddComponent(entity, new StuckDetection
                {
                    LastPosition = transform.ValueRO.Position,
                    CheckTime = currentTime + 2.0f,
                    StuckThreshold = 0.5f // Increased threshold - 0.1 was too sensitive
                });
                addedCount++;
            }

            if (addedCount > 0)
            {
    //            UnityEngine.Debug.Log($"[StuckDetectionInitSystem] Added StuckDetection to {addedCount} units");
            }

            ecb.Playback(state.EntityManager);
            ecb.Dispose();

            hasRun = true;
      //      UnityEngine.Debug.Log("[StuckDetectionInitSystem] Disabling system after first run");
        }
    }
}