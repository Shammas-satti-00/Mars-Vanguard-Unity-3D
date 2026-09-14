
using Unity.Entities;
using Unity.Transforms;
using Unity.Mathematics;
using UnityEngine;

namespace AStar3D.ECS
{
    /// <summary>
    /// Debug visualizer for ECS pathfinding
    /// Shows the current path for all units in the scene
    /// </summary>
    public class PathfindingDebugVisualizer : MonoBehaviour
    {
        [Header("Visualization")]
        public bool showPaths = true;
        public bool showWaypoints = true;
        public bool showCurrentWaypoint = true;
        public Color pathColor = Color.green;
        public Color currentWaypointColor = Color.yellow;
        public float waypointSize = 0.5f;

        private World world;
        private EntityManager entityManager;

        private void OnDrawGizmos()
        {
            if (!Application.isPlaying || !showPaths) return;

            if (world == null || !world.IsCreated)
            {
                world = World.DefaultGameObjectInjectionWorld;
                if (world == null) return;
            }

            entityManager = world.EntityManager;

            // Query all entities with paths
            var query = entityManager.CreateEntityQuery(
                typeof(LocalTransform),
                typeof(UnitMovement),
                typeof(PathWaypoint)
            );

            var entities = query.ToEntityArray(Unity.Collections.Allocator.Temp);
            var transforms = query.ToComponentDataArray<LocalTransform>(Unity.Collections.Allocator.Temp);

            for (int i = 0; i < entities.Length; i++)
            {
                var entity = entities[i];
                var transform = transforms[i];

                if (!entityManager.HasBuffer<PathWaypoint>(entity))
                    continue;

                var pathBuffer = entityManager.GetBuffer<PathWaypoint>(entity);
                var movement = entityManager.GetComponentData<UnitMovement>(entity);

                if (pathBuffer.Length == 0) continue;

                // Draw path from unit position to first waypoint
                Gizmos.color = Color.cyan;
                Gizmos.DrawLine(transform.Position, pathBuffer[0].Position);

                // Draw path lines
                Gizmos.color = pathColor;
                for (int j = 0; j < pathBuffer.Length - 1; j++)
                {
                    Gizmos.DrawLine(pathBuffer[j].Position, pathBuffer[j + 1].Position);
                }

                // Draw waypoint spheres
                if (showWaypoints)
                {
                    for (int j = 0; j < pathBuffer.Length; j++)
                    {
                        if (showCurrentWaypoint && j == movement.CurrentWaypointIndex && movement.IsFollowingPath)
                        {
                            Gizmos.color = currentWaypointColor;
                            Gizmos.DrawSphere(pathBuffer[j].Position, waypointSize);
                        }
                        else
                        {
                            Gizmos.color = pathColor;
                            Gizmos.DrawWireSphere(pathBuffer[j].Position, waypointSize * 0.5f);
                        }
                    }
                }

                // Draw unit position
                Gizmos.color = Color.blue;
                Gizmos.DrawWireSphere(transform.Position, waypointSize * 0.8f);

                // Draw forward direction
                var forward = math.mul(transform.Rotation, new Unity.Mathematics.float3(0, 0, 1));
                Gizmos.color = Color.red;
                Gizmos.DrawRay(transform.Position, forward * 2f);

#if UNITY_EDITOR
                // Draw debug info
                if (movement.IsFollowingPath)
                {
                    var label = $"Entity {entity.Index}\nWaypoint: {movement.CurrentWaypointIndex}/{pathBuffer.Length}";
                    UnityEditor.Handles.Label(transform.Position + new Unity.Mathematics.float3(0, 1, 0), label);

                }
#endif
            }

            entities.Dispose();
            transforms.Dispose();
            query.Dispose();
        }
    }
}