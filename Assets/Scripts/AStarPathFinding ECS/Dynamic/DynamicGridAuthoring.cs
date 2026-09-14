using Unity.Entities;
using UnityEngine;

namespace AStar3D.ECS
{
    /// <summary>
    /// Authoring component for dynamic grid configuration
    /// Place this on an empty GameObject in your scene
    /// </summary>
    public class DynamicGridAuthoring : MonoBehaviour
    {
        [Header("Grid Settings")]
        [Tooltip("Size of each grid cell (smaller = more precise, slower)")]
        public float nodeRadius = 0.5f;

        [Tooltip("Maximum distance to pathfind (prevents infinite searches)")]
        public float maxPathDistance = 100f;

        [Tooltip("Maximum waypoints in a path (safety limit)")]
        public int maxPathLength = 200;

        [Header("Collision Detection")]
        [Tooltip("Layer mask for obstacles (walls, terrain, etc)")]
        public LayerMask unwalkableMask;

        [Header("Debug")]
        [Tooltip("Show debug info in console")]
        public bool debugMode = true;
    }

    /// <summary>
    /// Baker to convert authoring to ECS component
    /// </summary>
    public class DynamicGridBaker : Baker<DynamicGridAuthoring>
    {
        public override void Bake(DynamicGridAuthoring authoring)
        {
            var entity = GetEntity(TransformUsageFlags.None);

            AddComponent(entity, new DynamicGridConfig
            {
                NodeRadius = authoring.nodeRadius,
                UnwalkableLayer = authoring.unwalkableMask,
                MaxPathDistance = authoring.maxPathDistance,
                MaxPathLength = authoring.maxPathLength
            });

            Debug.Log($"[DynamicGridAuthoring] Baked config: NodeRadius={authoring.nodeRadius}, MaxDist={authoring.maxPathDistance}");
        }
    }
}