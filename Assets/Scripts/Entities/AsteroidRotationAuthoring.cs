using Unity.Entities;
using UnityEngine;

// Authoring component for baking asteroid rotation
public class AsteroidRotationAuthoring : MonoBehaviour
{
    public float rotationSpeed = 25f; // degrees per second
    public Vector3 rotationAxis = new Vector3(0.5f, 1f, 0.3f); // random asteroid-like spin

    // Baker to convert to ECS data
    class Baker : Baker<AsteroidRotationAuthoring>
    {
        public override void Bake(AsteroidRotationAuthoring authoring)
        {
            var entity = GetEntity(TransformUsageFlags.Dynamic);
            AddComponent(entity, new AsteroidRotation
            {
                RotationSpeed = authoring.rotationSpeed,
                Axis = authoring.rotationAxis.normalized
            });

            // Optional: Add a tag to identify this entity as an asteroid
            AddComponent<AsteroidTag>(entity);

        }
    }
}


