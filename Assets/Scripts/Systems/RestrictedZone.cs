using UnityEngine;

public class RestrictedZone : MonoBehaviour
{
    [Header("Push Back Settings")]
    [Tooltip("Tag of the player GameObject")]
    public string playerTag = "Player";

    [Tooltip("Distance to push player away from this collider")]
    public float pushDistance = 2f;

    [Tooltip("Should the push be instant or smooth?")]
    public bool instantPush = true;

    [Tooltip("Speed of smooth push (only used if instantPush is false)")]
    public float pushSpeed = 10f;

    [Header("Rotation Settings")]
    [Tooltip("Should the spaceship rotate to face away from the zone?")]
    public bool rotateAway = true;

    [Tooltip("Rotation speed (higher = faster rotation)")]
    public float rotationSpeed = 5f;

    void OnTriggerEnter(Collider other)
    {
        // Check if the colliding object is the player
        if (other.CompareTag(playerTag))
        {
            // Get the root transform (parent spaceship object)
            Transform rootPlayer = other.transform.root;
            PushPlayerAway(rootPlayer);
        }
    }

    void OnTriggerStay(Collider other)
    {
        // Continuously push if player is still inside
        if (other.CompareTag(playerTag))
        {
            // Get the root transform (parent spaceship object)
            Transform rootPlayer = other.transform.root;
            PushPlayerAway(rootPlayer);
        }
    }

    void PushPlayerAway(Transform player)
    {
        // Get this collider component
        Collider thisCollider = GetComponent<Collider>();

        // Calculate direction from this collider center to player
        Vector3 colliderCenter = thisCollider.bounds.center;
        Vector3 directionAway = (player.position - colliderCenter).normalized;

        // If direction is zero (player exactly at center), push in a default direction
        if (directionAway == Vector3.zero)
        {
            directionAway = Vector3.forward;
        }

        // Calculate the push position
        Vector3 pushPosition = colliderCenter + directionAway * (thisCollider.bounds.extents.magnitude + pushDistance);

        if (instantPush)
        {
            // Instant snap
            player.position = pushPosition;
        }
        else
        {
            // Smooth push
            player.position = Vector3.Lerp(player.position, pushPosition, pushSpeed * Time.deltaTime);
        }
    }

    // Alternative method for collision instead of trigger
    void OnCollisionEnter(Collision collision)
    {
        if (collision.collider.CompareTag(playerTag))
        {
            // Get the root transform (parent spaceship object)
            Transform rootPlayer = collision.transform.root;
            PushPlayerAway(rootPlayer);
        }
    }

    void OnCollisionStay(Collision collision)
    {
        if (collision.collider.CompareTag(playerTag))
        {
            // Get the root transform (parent spaceship object)
            Transform rootPlayer = collision.transform.root;
            PushPlayerAway(rootPlayer);
        }
    }
}