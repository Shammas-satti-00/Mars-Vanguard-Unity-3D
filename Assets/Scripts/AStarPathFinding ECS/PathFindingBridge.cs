using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;
using UnityEngine;
using System.Collections.Generic;

namespace AStar3D.ECS
{
    /// <summary>
    /// Bridge between ShipAI and ECS pathfinding system
    /// Handles pathfinding requests and provides navigation waypoints to ShipAI
    /// </summary>
    [RequireComponent(typeof(ShipAI))]
    public class ShipPathfindingBridge : MonoBehaviour
    {
        [Header("Pathfinding Settings")]
        [Tooltip("Enable pathfinding for obstacle avoidance")]
        public bool enablePathfinding = true;

        [Tooltip("Distance ahead to check for obstacles")]
        public float lookAheadDistance = 100f;

        [Tooltip("How often to update path (seconds)")]
        public float pathUpdateInterval = 2f;

        [Tooltip("Blend between direct path and pathfinding (0=direct, 1=full pathfinding)")]
        [Range(0f, 1f)]
        public float pathfindingWeight = 0.7f;

        [Tooltip("Distance to waypoint before moving to next")]
        public float waypointReachedDistance = 20f;

        [Header("Debug")]
        [Tooltip("Show pathfinding debug info")]
        public bool showDebug = false;

        [Tooltip("Show path in scene view")]
        public bool showPath = true;

        [Tooltip("Path color")]
        public Color pathColor = Color.green;

        // ECS components
        private Entity pathfindingEntity;
        private EntityManager entityManager;
        private World world;

        // Path data
        private List<Vector3> currentPath = new List<Vector3>();
        private int currentWaypointIndex = 0;
        private bool hasActivePath = false;
        private bool isRequestingPath = false;

        // Timing
        private float lastPathRequestTime;
        private Vector3 lastPathRequestPosition;

        // Cached reference
        private Transform targetTransform;

        #region Unity Lifecycle

        private void Start()
        {
            InitializeEntity();
            lastPathRequestTime = -pathUpdateInterval; // Allow immediate first request
            lastPathRequestPosition = transform.position;
        }

        private void OnDestroy()
        {
            CleanupEntity();
        }

        #endregion

        #region Entity Management

        private void InitializeEntity()
        {
            world = World.DefaultGameObjectInjectionWorld;
            if (world == null)
            {
                Debug.LogError("[ShipPathfindingBridge] No default ECS world found!");
                return;
            }

            entityManager = world.EntityManager;

            // Create persistent pathfinding entity
            pathfindingEntity = entityManager.CreateEntity();

            // Add required components
            entityManager.AddComponentData(pathfindingEntity, LocalTransform.FromPosition(transform.position));
            entityManager.AddBuffer<PathWaypoint>(pathfindingEntity);

            // Add movement component (needed for pathfinding system)
            entityManager.AddComponentData(pathfindingEntity, new UnitMovement
            {
                Speed = 10f, // Not used for ship movement
                TurnDistance = waypointReachedDistance,
                TurnSpeed = 1f,
                CurrentWaypointIndex = 0,
                IsFollowingPath = false
            });

            // Add enableable component for success tracking
            entityManager.AddComponent<PathfindingSuccess>(pathfindingEntity);
            entityManager.SetComponentEnabled<PathfindingSuccess>(pathfindingEntity, false);

            if (showDebug)
            {
                Debug.Log($"[ShipPathfindingBridge] Entity {pathfindingEntity.Index} created for {gameObject.name}");
            }
        }

        private void CleanupEntity()
        {
            if (world != null && world.IsCreated && entityManager.Exists(pathfindingEntity))
            {
                if (entityManager.HasBuffer<PathWaypoint>(pathfindingEntity))
                {
                    var buffer = entityManager.GetBuffer<PathWaypoint>(pathfindingEntity);
                    buffer.Clear();
                }

                if (entityManager.HasComponent<PathfindingRequest>(pathfindingEntity))
                {
                    entityManager.RemoveComponent<PathfindingRequest>(pathfindingEntity);
                }

                if (showDebug)
                {
                    Debug.Log($"[ShipPathfindingBridge] Cleaned up entity {pathfindingEntity.Index}");
                }
            }
        }

        #endregion

        #region Public API

        /// <summary>
        /// Get a navigation target that avoids obstacles
        /// Call this from ShipAI's CalculateTargetPosition method
        /// </summary>
        /// <param name="desiredTarget">The direct target position (player position + prediction)</param>
        /// <param name="currentTarget">The player transform for path updates</param>
        /// <returns>Adjusted target position that avoids obstacles</returns>
        public Vector3 GetNavigationTarget(Vector3 desiredTarget, Transform currentTarget)
        {
            if (!enablePathfinding)
                return desiredTarget;

            // Store target for path updates
            targetTransform = currentTarget;

            // Check if we need to request a new path
            UpdatePathRequest(desiredTarget);

            // Check for completed pathfinding
            CheckPathfindingStatus();

            // If we have a valid path, use it
            if (hasActivePath && currentPath.Count > 0)
            {
                return GetPathTarget(desiredTarget);
            }

            // No path available, return direct target
            return desiredTarget;
        }

        /// <summary>
        /// Force a path request (useful when AI state changes)
        /// </summary>
        public void RequestNewPath(Vector3 targetPosition)
        {
            if (!enablePathfinding || isRequestingPath)
                return;

            RequestPath(transform.position, targetPosition);
        }

        /// <summary>
        /// Clear the current path
        /// </summary>
        public void ClearPath()
        {
            currentPath.Clear();
            hasActivePath = false;
            currentWaypointIndex = 0;
            isRequestingPath = false;
        }

        /// <summary>
        /// Check if currently following a path
        /// </summary>
        public bool IsFollowingPath()
        {
            return hasActivePath && currentPath.Count > 0;
        }

        #endregion

        #region Internal Methods

        private void UpdatePathRequest(Vector3 targetPosition)
        {
            // Don't spam path requests
            if (isRequestingPath)
                return;

            float timeSinceLastRequest = Time.time - lastPathRequestTime;
            float distanceMoved = Vector3.Distance(transform.position, lastPathRequestPosition);

            // Request new path if:
            // 1. Enough time has passed
            // 2. We've moved significantly
            // 3. Target has moved significantly (if we have a path)
            bool shouldUpdate = timeSinceLastRequest >= pathUpdateInterval;

            if (shouldUpdate || distanceMoved > 50f)
            {
                // Check if there's an obstacle in the direct path
                Vector3 direction = (targetPosition - transform.position).normalized;
                float distance = Vector3.Distance(transform.position, targetPosition);
                float checkDistance = Mathf.Min(distance, lookAheadDistance);

                if (CheckForObstacles(transform.position, direction, checkDistance))
                {
                    RequestPath(transform.position, targetPosition);
                }
                else if (hasActivePath)
                {
                    // Clear path if direct line is clear
                    ClearPath();
                }

                lastPathRequestTime = Time.time;
                lastPathRequestPosition = transform.position;
            }
        }

        private bool CheckForObstacles(Vector3 origin, Vector3 direction, float distance)
        {
            // Get the unwalkable layer from DynamicGridConfig
            var query = entityManager.CreateEntityQuery(typeof(DynamicGridConfig));
            if (query.IsEmpty)
            {
                query.Dispose();
                return false;
            }

            var config = query.GetSingleton<DynamicGridConfig>();
            query.Dispose();

            // Raycast to check for obstacles
            return Physics.Raycast(origin, direction, distance, config.UnwalkableLayer);
        }

        private void RequestPath(Vector3 start, Vector3 end)
        {
            if (world == null || !world.IsCreated || !entityManager.Exists(pathfindingEntity))
                return;

            isRequestingPath = true;

            // Update entity position
            entityManager.SetComponentData(pathfindingEntity, LocalTransform.FromPosition(start));

            // Add or update pathfinding request
            if (entityManager.HasComponent<PathfindingRequest>(pathfindingEntity))
            {
                entityManager.SetComponentData(pathfindingEntity, new PathfindingRequest
                {
                    Start = start,
                    End = end,
                    IsProcessing = false
                });
            }
            else
            {
                entityManager.AddComponentData(pathfindingEntity, new PathfindingRequest
                {
                    Start = start,
                    End = end,
                    IsProcessing = false
                });
            }

            if (showDebug)
            {
                Debug.Log($"[ShipPathfindingBridge] Requested path: {start} -> {end}");
            }
        }

        private void CheckPathfindingStatus()
        {
            if (!isRequestingPath || world == null || !world.IsCreated || !entityManager.Exists(pathfindingEntity))
                return;

            // Check if pathfinding succeeded
            if (entityManager.IsComponentEnabled<PathfindingSuccess>(pathfindingEntity))
            {
                var pathBuffer = entityManager.GetBuffer<PathWaypoint>(pathfindingEntity);

                currentPath.Clear();
                for (int i = 0; i < pathBuffer.Length; i++)
                {
                    currentPath.Add(pathBuffer[i].Position);
                }

                hasActivePath = currentPath.Count > 0;
                currentWaypointIndex = 0;
                isRequestingPath = false;

                entityManager.SetComponentEnabled<PathfindingSuccess>(pathfindingEntity, false);

                if (showDebug)
                {
                    Debug.Log($"[ShipPathfindingBridge] Path received: {currentPath.Count} waypoints");
                }
            }
            // Check if request was removed without success (failed)
            else if (!entityManager.HasComponent<PathfindingRequest>(pathfindingEntity))
            {
                isRequestingPath = false;
                hasActivePath = false;

                if (showDebug)
                {
                    Debug.LogWarning($"[ShipPathfindingBridge] Path request failed");
                }
            }
        }

        private Vector3 GetPathTarget(Vector3 directTarget)
        {
            if (currentPath.Count == 0)
                return directTarget;

            // Update current waypoint
            while (currentWaypointIndex < currentPath.Count)
            {
                float distance = Vector3.Distance(transform.position, currentPath[currentWaypointIndex]);
                if (distance <= waypointReachedDistance)
                {
                    currentWaypointIndex++;
                }
                else
                {
                    break;
                }
            }

            // If we've reached all waypoints, clear path
            if (currentWaypointIndex >= currentPath.Count)
            {
                hasActivePath = false;
                return directTarget;
            }

            // Get current waypoint
            Vector3 pathTarget = currentPath[currentWaypointIndex];

            // Blend between path target and direct target
            return Vector3.Lerp(directTarget, pathTarget, pathfindingWeight);
        }

        #endregion

        #region Debug Visualization

        private void OnDrawGizmos()
        {
            if (!showPath || !Application.isPlaying || currentPath.Count == 0)
                return;

            // Draw path lines
            Gizmos.color = pathColor;

            // Line from ship to first waypoint
            Gizmos.DrawLine(transform.position, currentPath[0]);

            // Lines between waypoints
            for (int i = 0; i < currentPath.Count - 1; i++)
            {
                Gizmos.DrawLine(currentPath[i], currentPath[i + 1]);
            }

            // Draw waypoint spheres
            for (int i = 0; i < currentPath.Count; i++)
            {
                if (i == currentWaypointIndex)
                {
                    Gizmos.color = Color.yellow; // Current waypoint
                    Gizmos.DrawSphere(currentPath[i], 5f);
                }
                else
                {
                    Gizmos.color = pathColor;
                    Gizmos.DrawWireSphere(currentPath[i], 3f);
                }
            }

#if UNITY_EDITOR
            if (hasActivePath)
            {
                string label = $"Path: {currentWaypointIndex + 1}/{currentPath.Count}";
                UnityEditor.Handles.Label(transform.position + Vector3.up * 30f, label);
            }
#endif
        }

        #endregion
    }
}