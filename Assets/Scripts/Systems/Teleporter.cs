using UnityEngine;

[RequireComponent(typeof(Collider))]
public class Teleporter : MonoBehaviour
{
    [Header("Teleport Settings")]
    [Tooltip("Where the player will be teleported to.")]
    public Transform teleportTo;

    [Header("Player Settings")]
    [Tooltip("Tag of the player object/collider that should trigger teleport.")]
    public string playerTag = "Player";

    private Collider teleportCollider;

    private void Awake()
    {
        teleportCollider = GetComponent<Collider>();
        teleportCollider.isTrigger = true;

        if (teleportTo == null)
            teleportTo = transform;
    }

    private void OnTriggerEnter(Collider other)
    {
        if (!other.CompareTag(playerTag))
            return;

        // Find the top-most parent of the object
        Transform rootTransform = other.transform.root;
        if (rootTransform == null)
            return;

        // Teleport and match rotation
        rootTransform.SetPositionAndRotation(
            teleportTo.position,
            teleportTo.rotation
        );
    }
}
