using UnityEngine;

public class TurnOnOffObject : MonoBehaviour
{
    [Header("Objects controlled by THIS script")]
    [SerializeField] private GameObject[] objectsToActivate;
    [SerializeField] private GameObject[] objectsToDeactivate;

    [Header("Settings")]
    [SerializeField] private bool activateOnStart = false;

    private void Start()
    {
        if (activateOnStart)
            Activate();
    }

    /// <summary>
    /// Activates ONLY this script's assigned objects.
    /// Does NOT deactivate or affect other scripts or arrays.
    /// </summary>
    public void Activate()
    {
        // Activate assigned objects
        foreach (var obj in objectsToActivate)
        {
            if (obj != null)
                obj.SetActive(true);
        }

        // Deactivate assigned objects
        foreach (var obj in objectsToDeactivate)
        {
            if (obj != null)
                obj.SetActive(false);
        }
    }
}
