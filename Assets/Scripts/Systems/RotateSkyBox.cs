using UnityEngine;

public class RotateSkybox : MonoBehaviour
{
    [Header("Skybox Rotation Settings")]
    public float rotationSpeed = 5f; // Rotation speed in degrees per second

    void Update()
    {
        // Rotate the skybox around the Y-axis
        float rotationAmount = rotationSpeed * Time.deltaTime;
        RenderSettings.skybox.SetFloat("_Rotation", RenderSettings.skybox.GetFloat("_Rotation") + rotationAmount);
    }
}
