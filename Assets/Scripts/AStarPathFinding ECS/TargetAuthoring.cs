using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;

namespace AStar3D.ECS
{
    public class TargetAuthoring : MonoBehaviour
    {
    }

    public class TargetBaker : Baker<TargetAuthoring>
    {
        public override void Bake(TargetAuthoring authoring)
        {
            var entity = GetEntity(TransformUsageFlags.Dynamic);
            // Target entities just need a transform, nothing special
        }
    }
}