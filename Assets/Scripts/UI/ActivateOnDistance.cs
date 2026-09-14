using UnityEngine;

public class ActivateOnDistance : MonoBehaviour
{
    [Header("Detection Settings")]
    public string targetTag = "Player";   // The tag to search for
    public float activationDistance = 10f;

    [Header("Activation Target")]
    public GameObject objectToActivate;

    [Header("Options")]
    public bool deactivateWhenOutOfRange = true;

    private Transform target;

    void Start()
    {
        if (objectToActivate != null)
            objectToActivate.SetActive(false);

        // Find the first object with the tag
        GameObject found = GameObject.FindGameObjectWithTag(targetTag);
        if (found != null)
        {
            target = found.transform;
        }
        else
        {
            Debug.LogWarning($"{name}: No object with tag '{targetTag}' found.");
        }
    }

    void Update()
    {
        if (target == null)
            FindPlayer();
        
            
        if (objectToActivate == null)
            return;
        if (target == null)
            return;

        float distance = Vector3.Distance(transform.position, target.position);

        if (distance <= activationDistance)
        {
            objectToActivate.SetActive(true);
        }
        else if (deactivateWhenOutOfRange)
        {
            objectToActivate.SetActive(false);
        }
    }

    void FindPlayer()
    {
        GameObject found = GameObject.FindGameObjectWithTag(targetTag);
        if (found != null)
        {
            target = found.transform;
        }
        else
        {
            Debug.LogWarning($"{name}: No object with tag '{targetTag}' found.");
        }
    }
}
