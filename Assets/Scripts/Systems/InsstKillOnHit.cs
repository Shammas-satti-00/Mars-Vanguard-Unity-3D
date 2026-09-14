using UnityEngine;

public class InstantKillOnHit : MonoBehaviour
{
    // Optional: for debugging or configurable damage
    public float damage = 1000f;

    void OnCollisionEnter(Collision collision)
    {
        CheckAndDamage(collision.gameObject);
    }

    void OnTriggerEnter(Collider other)
    {
        CheckAndDamage(other.gameObject);
    }

    void CheckAndDamage(GameObject obj)
    {
        // Only proceed if tag matches
        if (obj.CompareTag("PlayerCollider"))
        {
            // Get the root parent of the collided object
            Transform root = obj.transform.root;

            // Check if root has a DamageHandler
            DamageHandler dh = root.GetComponent<DamageHandler>();
            if (dh != null)
            {
                // Deal full damage (kill)
                dh.TakeDamage(dh.maxHealth);

                // Optional: Debug log
               
            }
        }
    }
}
