using Unity.Entities;
using Unity.Mathematics;
using Unity.Collections;
using UnityEngine;

namespace AStar3D.ECS
{
    /// <summary>
    /// Dynamic grid configuration - only stores parameters, not nodes
    /// </summary>
    public struct DynamicGridConfig : IComponentData
    {
        public float NodeRadius;
        public int UnwalkableLayer;
        public int MaxPathLength;
        public float MaxPathDistance;
    }

    /// <summary>
    /// Dynamic pathfinding that generates nodes on-the-fly
    /// No pre-computed grid needed - works for infinite worlds!
    /// </summary>
    [UpdateInGroup(typeof(SimulationSystemGroup))]
    [UpdateBefore(typeof(UnitMovementSystem))]
    public partial class DynamicPathfindingSystem : SystemBase
    {
        protected override void OnCreate()
        {
            RequireForUpdate<DynamicGridConfig>();
            Debug.Log("[DynamicPathfindingSystem] Created");
        }

        protected override void OnUpdate()
        {
            var config = SystemAPI.GetSingleton<DynamicGridConfig>();
            var ecb = new EntityCommandBuffer(Allocator.TempJob);

            foreach (var (request, pathBuffer, entity) in SystemAPI.Query<RefRW<PathfindingRequest>, DynamicBuffer<PathWaypoint>>()
                .WithNone<PathfindingSuccess>()
                .WithEntityAccess())
            {
                if (request.ValueRO.IsProcessing) continue;

                Debug.Log($"[DynamicPathfinding] Processing entity {entity.Index}: {request.ValueRO.Start} -> {request.ValueRO.End}");
                request.ValueRW.IsProcessing = true;

                var job = new DynamicPathfindingJob
                {
                    Config = config,
                    StartPos = request.ValueRO.Start,
                    EndPos = request.ValueRO.End,
                    PathWaypoints = pathBuffer
                };

                bool success = job.Execute();

                if (success && pathBuffer.Length > 0)
                {
                    Debug.Log($"[DynamicPathfinding] SUCCESS! Path found with {pathBuffer.Length} waypoints");

                    // Log waypoints for debugging
                    for (int i = 0; i < pathBuffer.Length; i++)
                    {
                        Debug.Log($"  Waypoint {i}: {pathBuffer[i].Position}");
                    }

                    ecb.SetComponentEnabled<PathfindingSuccess>(entity, true);
                }
                else
                {
                    Debug.LogWarning($"[DynamicPathfinding] FAILED to find path");
                }

                request.ValueRW.IsProcessing = false;
                ecb.RemoveComponent<PathfindingRequest>(entity);
            }

            ecb.Playback(EntityManager);
            ecb.Dispose();
        }

        private struct DynamicPathfindingJob
        {
            public DynamicGridConfig Config;
            public float3 StartPos;
            public float3 EndPos;
            public DynamicBuffer<PathWaypoint> PathWaypoints;

            public bool Execute()
            {
                float distance = math.distance(StartPos, EndPos);
                if (distance > Config.MaxPathDistance)
                {
                    Debug.LogWarning($"[DynamicPathfinding] Distance {distance:F1} exceeds max {Config.MaxPathDistance}");
                    return false;
                }

                // Convert world positions to grid coordinates
                int3 startGrid = WorldToGrid(StartPos, Config.NodeRadius);
                int3 endGrid = WorldToGrid(EndPos, Config.NodeRadius);

                Debug.Log($"[DynamicPathfinding] Grid coords: Start={startGrid}, End={endGrid}");

                // Check if start/end are walkable
                if (!IsWalkable(StartPos, Config))
                {
                    Debug.LogWarning("[DynamicPathfinding] Start position is not walkable!");
                    return false;
                }

                if (!IsWalkable(EndPos, Config))
                {
                    Debug.LogWarning("[DynamicPathfinding] End position is not walkable!");
                    return false;
                }

                // A* search through virtual grid
                var openSet = new NativeList<DynamicNode>(256, Allocator.Temp);
                var openSetHash = new NativeHashMap<int3, int>(256, Allocator.Temp);
                var closedSet = new NativeHashSet<int3>(512, Allocator.Temp);
                var cameFrom = new NativeHashMap<int3, int3>(512, Allocator.Temp);
                var gCosts = new NativeHashMap<int3, int>(512, Allocator.Temp);

                var startNode = new DynamicNode
                {
                    GridPos = startGrid,
                    WorldPos = StartPos,
                    GCost = 0,
                    HCost = GetDistance(startGrid, endGrid)
                };

                openSet.Add(startNode);
                openSetHash.Add(startGrid, 0);
                gCosts.Add(startGrid, 0);

                bool pathFound = false;
                int iterations = 0;
                int maxIterations = Config.MaxPathLength * 5;

                while (openSet.Length > 0 && iterations < maxIterations)
                {
                    iterations++;

                    // Find node with lowest F cost
                    int lowestIdx = 0;
                    for (int i = 1; i < openSet.Length; i++)
                    {
                        if (openSet[i].FCost < openSet[lowestIdx].FCost ||
                            (openSet[i].FCost == openSet[lowestIdx].FCost && openSet[i].HCost < openSet[lowestIdx].HCost))
                        {
                            lowestIdx = i;
                        }
                    }

                    var current = openSet[lowestIdx];
                    openSet.RemoveAtSwapBack(lowestIdx);

                    // Update hash map indices after swap
                    if (lowestIdx < openSet.Length)
                    {
                        openSetHash[openSet[lowestIdx].GridPos] = lowestIdx;
                    }
                    openSetHash.Remove(current.GridPos);

                    closedSet.Add(current.GridPos);

                    // Check if we reached the goal
                    if (math.all(current.GridPos == endGrid))
                    {
                        pathFound = true;
                        Debug.Log($"[DynamicPathfinding] Path found after {iterations} iterations");
                        ReconstructPath(startGrid, endGrid, cameFrom, Config.NodeRadius, PathWaypoints);
                        break;
                    }

                    // Check all 26 neighbors in 3D
                    for (int x = -1; x <= 1; x++)
                    {
                        for (int y = -1; y <= 1; y++)
                        {
                            for (int z = -1; z <= 1; z++)
                            {
                                if (x == 0 && y == 0 && z == 0) continue;

                                int3 neighborGrid = current.GridPos + new int3(x, y, z);

                                if (closedSet.Contains(neighborGrid))
                                    continue;

                                float3 neighborWorld = GridToWorld(neighborGrid, Config.NodeRadius);

                                // Check if this position is walkable (no obstacles)
                                if (!IsWalkable(neighborWorld, Config))
                                    continue;

                                // Calculate movement cost
                                int movementCost = GetDistance(current.GridPos, neighborGrid);
                                int tentativeGCost = current.GCost + movementCost;

                                bool inOpenSet = gCosts.ContainsKey(neighborGrid);
                                int currentGCost = inOpenSet ? gCosts[neighborGrid] : int.MaxValue;

                                if (tentativeGCost < currentGCost)
                                {
                                    // This is a better path to this neighbor
                                    var neighborNode = new DynamicNode
                                    {
                                        GridPos = neighborGrid,
                                        WorldPos = neighborWorld,
                                        GCost = tentativeGCost,
                                        HCost = GetDistance(neighborGrid, endGrid)
                                    };

                                    cameFrom[neighborGrid] = current.GridPos;
                                    gCosts[neighborGrid] = tentativeGCost;

                                    if (!inOpenSet)
                                    {
                                        openSet.Add(neighborNode);
                                        openSetHash.Add(neighborGrid, openSet.Length - 1);
                                    }
                                    else
                                    {
                                        // Update existing node in open set
                                        int idx = openSetHash[neighborGrid];
                                        if (idx < openSet.Length && math.all(openSet[idx].GridPos == neighborGrid))
                                        {
                                            openSet[idx] = neighborNode;
                                        }
                                    }
                                }
                            }
                        }
                    }
                }

                if (!pathFound)
                {
                    Debug.LogWarning($"[DynamicPathfinding] No path found after {iterations} iterations");
                }

                openSet.Dispose();
                openSetHash.Dispose();
                closedSet.Dispose();
                cameFrom.Dispose();
                gCosts.Dispose();

                return pathFound;
            }

            private void ReconstructPath(int3 start, int3 end, NativeHashMap<int3, int3> cameFrom,
                                        float nodeRadius, DynamicBuffer<PathWaypoint> waypoints)
            {
                var path = new NativeList<int3>(128, Allocator.Temp);
                int3 current = end;
                int safety = 0;

                // Build path from end to start
                while (!math.all(current == start) && safety < 1000)
                {
                    path.Add(current);
                    if (!cameFrom.TryGetValue(current, out current))
                        break;
                    safety++;
                }

                Debug.Log($"[DynamicPathfinding] Raw path length: {path.Length}");

                // Simplify path by removing redundant waypoints
                waypoints.Clear();

                if (path.Length > 0)
                {
                    // Path is built backwards (end to start), so we process it in reverse
                    // to go from start to end

                    int3 dirOld = int3.zero;

                    // Start from the end of the path list (which is near the start position)
                    // and work towards the beginning (which is the end position)
                    for (int i = path.Length - 1; i > 0; i--)
                    {
                        int3 dirNew = path[i - 1] - path[i];

                        // Add waypoint when direction changes
                        if (!math.all(dirNew == dirOld))
                        {
                            waypoints.Add(new PathWaypoint { Position = GridToWorld(path[i], nodeRadius) });
                        }
                        dirOld = dirNew;
                    }

                    // Always add the final destination
                    waypoints.Add(new PathWaypoint { Position = GridToWorld(path[0], nodeRadius) });
                }

                Debug.Log($"[DynamicPathfinding] Simplified to {waypoints.Length} waypoints");

                path.Dispose();
            }

            private bool IsWalkable(float3 worldPos, DynamicGridConfig config)
            {
                // Check if there's an obstacle at this position
                bool hasObstacle = Physics.CheckSphere(worldPos, config.NodeRadius * 0.9f, config.UnwalkableLayer);
                return !hasObstacle;
            }

            private int3 WorldToGrid(float3 worldPos, float nodeRadius)
            {
                float nodeDiameter = nodeRadius * 2f;
                return new int3(
                    (int)math.round(worldPos.x / nodeDiameter),
                    (int)math.round(worldPos.y / nodeDiameter),
                    (int)math.round(worldPos.z / nodeDiameter)
                );
            }

            private float3 GridToWorld(int3 gridPos, float nodeRadius)
            {
                float nodeDiameter = nodeRadius * 2f;
                return new float3(gridPos.x, gridPos.y, gridPos.z) * nodeDiameter;
            }

            private int GetDistance(int3 a, int3 b)
            {
                // 3D Octile distance heuristic
                int3 dst = math.abs(a - b);

                // Sort the distances
                int min = math.min(math.min(dst.x, dst.y), dst.z);
                int max = math.max(math.max(dst.x, dst.y), dst.z);
                int mid = dst.x + dst.y + dst.z - min - max;

                // Diagonal moves cost more
                // 17 = sqrt(3) * 10 (3D diagonal)
                // 14 = sqrt(2) * 10 (2D diagonal)  
                // 10 = straight move
                return (17 * min) + (14 * (mid - min)) + (10 * (max - mid));
            }
        }

        private struct DynamicNode
        {
            public int3 GridPos;
            public float3 WorldPos;
            public int GCost;
            public int HCost;
            public int FCost => GCost + HCost;
        }
    }
}