using UnityEngine;

public class EnvironmentCollision : MonoBehaviour
{
    private void Start()
    {
        var col = GetComponent<Collider>();
        if (col != null)
        {
            col.isTrigger = true; // Using trigger-based damage
            Debug.Log($"[EnvironmentCollision] Collider found on {gameObject.name}, set as Trigger.");
        }
        else
        {
            Debug.LogWarning($"[EnvironmentCollision] No collider found on {gameObject.name}!");
        }

        var rb = gameObject.AddComponent<Rigidbody>();
        if (rb != null)
        { 
        rb.isKinematic = true;
        rb.useGravity = false;
        rb.constraints = RigidbodyConstraints.FreezeAll;
        }

        Debug.Log($"[EnvironmentCollision] Rigidbody added to {gameObject.name} (Kinematic, No Gravity).");
    }

    private void OnTriggerEnter(Collider other)
    {
        Debug.Log($"[EnvironmentCollision] Trigger entered by: {other.name}");
        HandleHit(other.transform);
    }

    private void OnCollisionEnter(Collision collision)
    {
        Debug.Log($"[EnvironmentCollision] Collision with: {collision.transform.name}");
        HandleHit(collision.transform);
    }

    private void HandleHit(Transform hitTransform)
    {
        Debug.Log($"[EnvironmentCollision] HandleHit called for: {hitTransform.name}");

        if (!hitTransform.CompareTag("PlayerCollider"))
        {
            Debug.Log($"[EnvironmentCollision] Ignored — '{hitTransform.name}' does not have PlayerCollider tag.");
            return;
        }

        // Step 1: get the top-most parent GameObject
        Transform t = hitTransform;
        while (t.parent != null)
            t = t.parent;

        GameObject rootObject = t.gameObject;

        Debug.Log($"[EnvironmentCollision] Root object detected: {rootObject.name}");

        // Step 2: get the DamageHandler on that root
        DamageHandler damageHandler = rootObject.GetComponent<DamageHandler>();

        if (damageHandler != null)
        {
            Debug.Log($"[EnvironmentCollision] DamageHandler found on {rootObject.name}. Invincible: {damageHandler.Invincible}");

            if (!damageHandler.Invincible)
            {
                Debug.Log($"[EnvironmentCollision] Applying full damage ({damageHandler.maxHealth}) to {rootObject.name}.");
                damageHandler.TakeDamage(damageHandler.maxHealth);
            }
            else
            {
                Debug.Log($"[EnvironmentCollision] No damage applied — player is invincible.");
            }
        }
        else
        {
            Debug.LogWarning($"[EnvironmentCollision] No DamageHandler found on {rootObject.name}!");
        }
    }
}
