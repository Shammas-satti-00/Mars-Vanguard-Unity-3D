using System.Collections;
using UnityEngine;

public class CameraShake : MonoBehaviour
{
    [Tooltip("Base duration of the shake effect in seconds")]
    public float shakeDuration = 0.5f;

    [Tooltip("Maximum shake angle (in degrees, scaled by damage ratio)")]
    public float maxShakeMagnitude = 5f;

    public void Inititialize(DamageHandler damageHandler)
    {
        damageHandler.OnDamageTaken += HandleDamageTaken;
    }

    private void OnDestroy()
    {
        // Unsubscribe to avoid memory leaks
        GameObject player = GameObject.FindWithTag("Player");
        if (player != null)
        {
            DamageHandler damageHandler = player.GetComponent<DamageHandler>();
            if (damageHandler != null)
            {
                damageHandler.OnDamageTaken -= HandleDamageTaken;
            }
        }
    }

    private void HandleDamageTaken(float damageRatio)
    {
        // Clamp the ratio to 0-1 for safety
        damageRatio = Mathf.Clamp(damageRatio, 0f, 1f);
        StartCoroutine(Shake(damageRatio));
    }

    private IEnumerator Shake(float intensity)
    {
        Quaternion originalRotation = transform.localRotation;
        float elapsed = 0f;

        while (elapsed < shakeDuration)
        {
            // Compute a random roll angle around Z
            float zAngle = Random.Range(-maxShakeMagnitude, maxShakeMagnitude) * intensity;

            // Apply rotation only around the Z axis
            transform.localRotation = originalRotation * Quaternion.Euler(0f, 0f, zAngle);

            elapsed += Time.deltaTime;
            yield return null;
        }

        // Restore original rotation
        transform.localRotation = originalRotation;
    }
}
