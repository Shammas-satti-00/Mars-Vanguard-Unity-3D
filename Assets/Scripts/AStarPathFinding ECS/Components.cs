using Unity.Entities;
using Unity.Mathematics;
using Unity.Collections;
using System;

namespace AStar3D.ECS
{
    // Grid configuration component (singleton)
    public struct GridConfig : IComponentData
    {
        public float3 GridWorldSize;
        public float NodeRadius;
        public float NodeDiameter;
        public int3 GridSize;
        public float3 WorldBottomLeft;
        public int UnwalkableLayer;
        public int WalkableLayer;
        public int WalkableMaskPenalty;
        public int ObstacleProximityPenalty;
        public bool WeightBlur;
        public int WeightBlurSize;
    }

    // Grid data (singleton) - stored in a blob asset for performance
    public struct GridData : IComponentData
    {
        public BlobAssetReference<GridBlob> GridBlob;
    }

    public struct GridBlob
    {
        public BlobArray<NodeData> Nodes;
        public int GridSizeX;
        public int GridSizeY;
        public int GridSizeZ;
    }

    public struct NodeData
    {
        public bool Walkable;
        public float3 WorldPosition;
        public int3 GridPosition;
        public int MovementPenalty;
    }

    // Pathfinding request component
    public struct PathfindingRequest : IComponentData
    {
        public float3 Start;
        public float3 End;
        public bool IsProcessing;
    }

    // Pathfinding result buffer
    public struct PathWaypoint : IBufferElementData
    {
        public float3 Position;
    }

    // Unit movement component
    public struct UnitMovement : IComponentData
    {
        public float Speed;
        public float TurnDistance;
        public float TurnSpeed;
        public int CurrentWaypointIndex;
        public bool IsFollowingPath;
    }

    // Unit target component
    public struct UnitTarget : IComponentData
    {
        public Entity TargetEntity;
        public float3 LastTargetPosition;
        public float PathUpdateThreshold;
        public float MinPathUpdateTime;
        public float MaxPathUpdateTime;
        public float NextUpdateTime;
    }

    // Tag component for units that need path updates
    public struct NeedsPathUpdate : IComponentData, IEnableableComponent { }

    // Tag component for successful pathfinding
    public struct PathfindingSuccess : IComponentData, IEnableableComponent { }

    // Stuck detection component
    public struct StuckDetection : IComponentData
    {
        public float3 LastPosition;
        public float CheckTime;
        public float StuckThreshold;
    }

    // Node heap item for A* algorithm (used in native collections)
    public struct HeapNode : IComparable<HeapNode>
    {
        public int Index;
        public int GCost;
        public int HCost;
        public int FCost => GCost + HCost;
        public int ParentIndex;

        public int CompareTo(HeapNode other)
        {
            int compare = FCost.CompareTo(other.FCost);
            if (compare == 0)
            {
                compare = HCost.CompareTo(other.HCost);
            }
            return -compare;
        }
    }

    // Helper struct for pathfinding job
    public struct PathfindingJobData
    {
        public int3 StartGrid;
        public int3 EndGrid;
        public int GridSizeX;
        public int GridSizeY;
        public int GridSizeZ;
    }
}
