using UnityEngine;

public class RotateAwayOnHit : MonoBehaviour
{
    [Header("Rotation Settings")]
    public float rotationSpeed = 5f;   // Smooth rotation speed
    public bool instant = false;       // Instantly rotate or smoothly

    private void OnCollisionEnter(Collision collision)
    {
        TryRotate(collision.transform);
    }

    private void OnTriggerEnter(Collider other)
    {
        TryRotate(other.transform);
    }

    private void TryRotate(Transform hitTransform)
    {
        if (!hitTransform.CompareTag("PlayerCollider"))
            return;

        // Get the ROOT of the collided object
        Transform root = hitTransform.root;

        if (root == null)
            return;

        // Direction AWAY from this object
        Vector3 directionAway = (root.position - transform.position).normalized;

        Quaternion targetRotation = Quaternion.LookRotation(directionAway);

        if (instant)
        {
            root.rotation = targetRotation;
        }
        else
        {
            StopAllCoroutines();
            StartCoroutine(SmoothRotate(root, targetRotation));
        }
    }

    private System.Collections.IEnumerator SmoothRotate(Transform target, Quaternion desiredRot)
    {
        float t = 0f;

        while (t < 1f)
        {
            t += Time.deltaTime * rotationSpeed;
            target.rotation = Quaternion.Slerp(target.rotation, desiredRot, t);
            yield return null;
        }
    }
}
