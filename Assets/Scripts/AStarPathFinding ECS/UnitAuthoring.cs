using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;

namespace AStar3D.ECS
{
    /// <summary>
    /// MonoBehaviour authoring component for units
    /// Attach this to a GameObject to convert it to an ECS unit
    /// </summary>
    public class UnitAuthoring : MonoBehaviour
    {
        [Header("Movement Settings")]
        [Tooltip("Speed of the unit")]
        public float speed = 7f;
        
        [Tooltip("Distance from turn point to start turning. Higher values = smoother curves")]
        public float turnDistance = 1f;
        
        [Tooltip("How quickly the unit rotates. Higher values = sharper turns")]
        public float turnSpeed = 5f;

        [Header("Path Update Settings")]
        [Tooltip("Target to path towards")]
        public GameObject target;
        
        [Tooltip("How far the target must move before recalculating path")]
        public float pathUpdateThreshold = 0.5f;
        
        [Tooltip("Minimum time between path updates (random range)")]
        public float minPathUpdateTime = 0.3f;
        
        [Tooltip("Maximum time between path updates (random range)")]
        public float maxPathUpdateTime = 0.7f;

        [Header("Debug")]
        [Tooltip("Show the path in scene view")]
        public bool showPath = false;
    }

    /// <summary>
    /// Baker to convert UnitAuthoring to ECS components
    /// </summary>
    public class UnitBaker : Baker<UnitAuthoring>
    {
        public override void Bake(UnitAuthoring authoring)
        {
            var entity = GetEntity(TransformUsageFlags.Dynamic);

            // Add movement component
            AddComponent(entity, new UnitMovement
            {
                Speed = authoring.speed,
                TurnDistance = authoring.turnDistance,
                TurnSpeed = authoring.turnSpeed,
                CurrentWaypointIndex = 0,
                IsFollowingPath = false
            });

            // Add target tracking component
            if (authoring.target != null)
            {
                var targetEntity = GetEntity(authoring.target, TransformUsageFlags.Dynamic);
                AddComponent(entity, new UnitTarget
                {
                    TargetEntity = targetEntity,
                    LastTargetPosition = float3.zero,
                    PathUpdateThreshold = authoring.pathUpdateThreshold,
                    MinPathUpdateTime = authoring.minPathUpdateTime,
                    MaxPathUpdateTime = authoring.maxPathUpdateTime,
                    NextUpdateTime = 0f
                });
            }

            // Add path waypoint buffer
            AddBuffer<PathWaypoint>(entity);

            // Add enableable components (disabled by default)
            AddComponent(entity, new NeedsPathUpdate());
            SetComponentEnabled<NeedsPathUpdate>(entity, false);

            AddComponent(entity, new PathfindingSuccess());
            SetComponentEnabled<PathfindingSuccess>(entity, false);

            // Add stuck detection component
            AddComponent(entity, new StuckDetection
            {
                LastPosition = authoring.transform.position,
                CheckTime = 2.0f,
                StuckThreshold = 0.1f
            });
        }
    }

    /// <summary>
    /// Simple target authoring - just marks an entity as a target
    /// </summary>
    
}
