using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;
using Unity.Burst;
using UnityEngine;

namespace AStar3D.ECS
{
    [UpdateInGroup(typeof(SimulationSystemGroup))]
    //[UpdateAfter(typeof(PathfindingSystem))]
    [BurstCompile]
    public partial struct UnitMovementSystem : ISystem
    {
        private int updateCount;

        public void OnCreate(ref SystemState state)
        {
            updateCount = 0;
         //   Debug.Log("[UnitMovementSystem] OnCreate called.");
        }
        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            updateCount++;
            if (updateCount % 60 == 0) // Log every 60 frames to avoid spam
            {
           //     Debug.Log($"[UnitMovementSystem] OnUpdate #{updateCount} - Frame: {Time.frameCount}");
            }

            float deltaTime = SystemAPI.Time.DeltaTime;

            // Count entities with PathfindingSuccess
            int successEntityCount = 0;
            foreach (var _ in SystemAPI.Query<RefRO<PathfindingSuccess>>().WithAll<PathfindingSuccess>())
            {
                successEntityCount++;
            }
            if (successEntityCount > 0)
            {
             //   Debug.Log($"[UnitMovementSystem] Found {successEntityCount} entities with PathfindingSuccess");
            }

            // Start following path when pathfinding succeeds
            int successCount = 0;
            foreach (var (movement, pathBuffer, entity) in
                SystemAPI.Query<RefRW<UnitMovement>, DynamicBuffer<PathWaypoint>>()
                    .WithAll<PathfindingSuccess>()
                    .WithEntityAccess())
            {
               // Debug.Log($"[UnitMovementSystem] Pathfinding success for entity {entity.Index}, path length: {pathBuffer.Length}");

                if (pathBuffer.Length > 0)
                {
                    movement.ValueRW.IsFollowingPath = true;
                    movement.ValueRW.CurrentWaypointIndex = 0;
                 //   Debug.Log($"[UnitMovementSystem] Started following path for entity {entity.Index}");

                    // Log all waypoints for debugging
                    for (int i = 0; i < pathBuffer.Length; i++)
                    {
                   //     Debug.Log($"  Waypoint {i}: {pathBuffer[i].Position}");
                    }
                }
                else
                {
                    //Debug.LogWarning($"[UnitMovementSystem] Path buffer is empty for entity {entity.Index}!");
                }

                state.EntityManager.SetComponentEnabled<PathfindingSuccess>(entity, false);
                successCount++;
            }

            if (successCount > 0)
            {
            //    Debug.Log($"[UnitMovementSystem] Processed {successCount} successful pathfinding results");
            }

            // Count units currently following paths
            int followingPathCount = 0;
            foreach (var (movement, _) in SystemAPI.Query<RefRO<UnitMovement>, DynamicBuffer<PathWaypoint>>())
            {
                if (movement.ValueRO.IsFollowingPath)
                {
                    followingPathCount++;
                }
            }
            if (updateCount % 60 == 0 && followingPathCount > 0)
            {
             //   Debug.Log($"[UnitMovementSystem] {followingPathCount} units currently following paths");
            }

            // Move units along their paths
            var moveJob = new MoveAlongPathJob
            {
                DeltaTime = deltaTime
            };
            state.Dependency = moveJob.ScheduleParallel(state.Dependency);
        }

        [BurstCompile]
        [WithAll(typeof(PathWaypoint))]
        partial struct MoveAlongPathJob : IJobEntity
        {
            public float DeltaTime;

            void Execute(ref LocalTransform transform, ref UnitMovement movement,
                        in DynamicBuffer<PathWaypoint> pathBuffer)
            {
                if (!movement.IsFollowingPath || pathBuffer.Length == 0) return;

                // Check if we've reached the end of the path
                if (movement.CurrentWaypointIndex >= pathBuffer.Length)
                {
                    movement.IsFollowingPath = false;
                    return;
                }

                float3 currentPos = transform.Position;
                float3 targetPos = pathBuffer[movement.CurrentWaypointIndex].Position;
                float distanceToWaypoint = math.distance(currentPos, targetPos);

                // Use a slightly larger turn distance to prevent overshooting
                float effectiveTurnDistance = movement.TurnDistance * 1.5f;

                // Check if we should move to next waypoint
                if (distanceToWaypoint <= effectiveTurnDistance)
                {
                    movement.CurrentWaypointIndex++;

                    // Check if this was the last waypoint
                    if (movement.CurrentWaypointIndex >= pathBuffer.Length)
                    {
                        movement.IsFollowingPath = false;
                        return;
                    }

                    targetPos = pathBuffer[movement.CurrentWaypointIndex].Position;
                    distanceToWaypoint = math.distance(currentPos, targetPos);
                }

                // If we're very close to the final waypoint, just move directly to it
                if (movement.CurrentWaypointIndex == pathBuffer.Length - 1 &&
                    distanceToWaypoint < movement.TurnDistance * 2f)
                {
                    float3 direction = math.normalize(targetPos - currentPos);
                    float moveAmount = math.min(distanceToWaypoint, movement.Speed * DeltaTime);
                    transform.Position += direction * moveAmount;

                    // Rotate to face target
                    quaternion targetRotation = quaternion.LookRotationSafe(direction, math.up());
                    transform.Rotation = math.slerp(transform.Rotation, targetRotation, DeltaTime * movement.TurnSpeed);

                    return;
                }

                // Calculate direction to target
                float3 direction2 = math.normalize(targetPos - currentPos);

                // Smooth rotation towards target (faster turn speed for tighter turns)
                quaternion targetRotation2 = quaternion.LookRotationSafe(direction2, math.up());
                float turnSpeedMultiplier = 1.0f;

                // If we're far from aligned, turn faster
                float3 currentForward = math.mul(transform.Rotation, new float3(0, 0, 1));
                float alignment = math.dot(currentForward, direction2);
                if (alignment < 0.7f) // Not well aligned
                {
                    turnSpeedMultiplier = 2.0f;
                }

                transform.Rotation = math.slerp(transform.Rotation, targetRotation2,
                                                DeltaTime * movement.TurnSpeed * turnSpeedMultiplier);

                // Only move forward if we're reasonably aligned with target
                if (alignment > 0.3f) // Allow some movement even during turns
                {
                    float3 forward = math.mul(transform.Rotation, new float3(0, 0, 1));
                    float speedMultiplier = math.max(0.3f, alignment); // Slow down during sharp turns
                    transform.Position += forward * movement.Speed * DeltaTime * speedMultiplier;
                }
            }
        }
    }
}