using UnityEngine;

public class DistanceActivator : MonoBehaviour
{
    [Header("Target and Distance")]
    [Tooltip("The object to measure distance to. If null, script attempts to find 'PlayerCollider' tag.")]
    public Transform targetTransform;

    [Tooltip("The distance at which the activation/deactivation occurs.")]
    public float activationDistance = 5.0f;

    [Header("Objects to Toggle")]
    [Tooltip("Object to ACTIVATE when the distance is LESS than the activation distance.")]
    public GameObject activateObject;

    [Tooltip("Object to DEACTIVATE when the distance is LESS than the activation distance.")]
    public GameObject deactivateObject;

    private bool isCurrentlyActive = false;

    // Define the tag you want to find
    private const string PlayerTag = "PlayerCollider";

    void Start()
    {
        // 1. If the target is NOT set in the Inspector, attempt to find it by tag.
        if (targetTransform == null)
        {
            GameObject player = GameObject.FindGameObjectWithTag(PlayerTag);

            if (player != null)
            {
                // Found the player! Assign its transform.
                targetTransform = player.transform;
            }
            else
            {
                // Fallback 1: If player tag search failed, try the Main Camera (as in original script)
                if (Camera.main != null)
                {
                    targetTransform = Camera.main.transform;
                }

                // Fallback 2: If all searches fail, log an error and disable.
                if (targetTransform == null)
                {
                    Debug.LogError($"DistanceActivator failed to find a target. Ensure an object is tagged '{PlayerTag}' or assign one manually.");
                    enabled = false;
                    return;
                }
            }
        }

        // Ensure initial states are correct based on the starting distance
        CheckDistanceAndToggle();
    }

    void Update()
    {
        // We only check if targetTransform is null here in case the target is destroyed mid-game.
        if (targetTransform == null)
        {
            // If the target was destroyed, try to find it again (if that's the desired behavior).
            // NOTE: Repeated searching in Update/CheckDistance is generally inefficient.
            // It's better to search once in Start, or use events/colliders for activation.
            // For now, we'll stick to the original structure but keep the logic efficient:

            // To fulfill the request to "find again and again until u find him":
            GameObject player = GameObject.FindGameObjectWithTag(PlayerTag);
            if (player != null)
            {
                targetTransform = player.transform;
            }
            else
            {
                // If the target is still gone, exit the check.
                return;
            }
        }

        CheckDistanceAndToggle();
    }

    private void CheckDistanceAndToggle()
    {
        // targetTransform is guaranteed to be non-null when called from Update/Start 
        // unless it was destroyed later. The check in Update handles destruction.
        if (targetTransform == null) return;

        float distance = Vector3.Distance(transform.position, targetTransform.position);
        bool shouldActivate = distance <= activationDistance;

        // Check if the state needs to change
        if (shouldActivate != isCurrentlyActive)
        {
            isCurrentlyActive = shouldActivate;

            if (isCurrentlyActive)
            {
                // Action when close
                if (activateObject != null) activateObject.SetActive(true);
                if (deactivateObject != null) deactivateObject.SetActive(false);
            }
            else
            {
                // Action when far
                if (activateObject != null) activateObject.SetActive(false);
                if (deactivateObject != null) deactivateObject.SetActive(true);
            }
        }
    }
}