using UnityEngine;

public class TurretController : MonoBehaviour
{
    [Header("Cannons")]
    public CannonSlot[] cannons;

    [Header("Detection")]
    public float detectionRange = 1500f;
    public string playerTag = "PlayerCollider";
    public LayerMask obstacleLayers;

    [Header("Rotation")]
    public float rotationSpeed = 30f;
    public bool showGizmos = true;

    private Transform currentTarget;

    void Start()
    {
        if (cannons == null || cannons.Length == 0)
            cannons = GetComponentsInChildren<CannonSlot>();
    }

    void Update()
    {
        // If no target, try to find one
        if (currentTarget == null)
        {
            TryFindTarget();
            if (currentTarget == null) return;
        }

        // Check if current target is still in range
        float distance = Vector3.Distance(transform.position, currentTarget.position);
        if (distance > detectionRange)
        {
            currentTarget = null;
            return;
        }

        // Rotate toward target
        RotateTowardTarget();

        // Check line of sight and shoot if clear
        if (HasLineOfSight())
        {
            foreach (var c in cannons) c?.TryFire();
        }
    }

    void TryFindTarget()
    {
        var go = GameObject.FindGameObjectWithTag(playerTag);
        if (go) currentTarget = go.transform;
    }

    void RotateTowardTarget()
    {
        if (!currentTarget) return;

        Vector3 dir = (currentTarget.position - transform.position).normalized;
        Quaternion targetRotation = Quaternion.LookRotation(dir);
        transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, Time.deltaTime * rotationSpeed);
    }

    bool HasLineOfSight()
    {
        if (!currentTarget) return false;

        Vector3 directionToTarget = currentTarget.position - transform.position;
        float distanceToTarget = directionToTarget.magnitude;

        // Raycast to check for obstacles
        if (Physics.Raycast(transform.position, directionToTarget.normalized, out RaycastHit hit, distanceToTarget, obstacleLayers))
        {
            // Something is blocking the shot
            return false;
        }

        return true;
    }

    void OnDrawGizmosSelected()
    {
        if (!showGizmos) return;

        Vector3 origin = transform.position;

        // Detection range
        Gizmos.color = new Color(1f, 0.6f, 0f, 0.15f);
        Gizmos.DrawSphere(origin, detectionRange);
        Gizmos.color = Color.cyan;
        Gizmos.DrawWireSphere(origin, detectionRange);

        // Current aiming direction
        Gizmos.color = Color.green;
        Gizmos.DrawRay(origin, transform.forward * 16f);

        // Line of sight to target
        if (currentTarget != null)
        {
            bool clearShot = HasLineOfSight();
            Gizmos.color = clearShot ? Color.green : Color.red;
            Gizmos.DrawLine(origin, currentTarget.position);
        }
    }
}