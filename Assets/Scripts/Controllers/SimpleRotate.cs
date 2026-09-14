using UnityEngine;

public class SimpleRotate : MonoBehaviour
{
    [Header("Rotation Speed (degrees per second)")]
    public Vector3 rotationSpeed = new Vector3(0f, 50f, 0f);

    void Update()
    {
        // Rotate around each axis by the specified speed
        transform.Rotate(rotationSpeed * Time.deltaTime);
    }
}