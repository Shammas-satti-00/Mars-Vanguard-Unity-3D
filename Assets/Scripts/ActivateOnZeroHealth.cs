using UnityEngine;

public class ActivateOnZeroHealth : MonoBehaviour
{
    [Header("References")]
    public DamageHandler damageHandler;      // Reference to your ship / main health handler
    public GameObject objectToActivate;      // Optional: object to activate when health = 0
    public GameObject objectToDeactivate;    // Optional: object to deactivate when health = 0

    [Header("Objects to Disable on Death")]
    public GameObject[] objectsToDisable;    // Array of GameObjects to disable when health is zero

    private bool hasDeactivated = false;

    void Start()
    {
        if (damageHandler == null)
        {
            damageHandler = GetComponent<DamageHandler>();
        }

        if (objectToActivate != null)
        {
            objectToActivate.SetActive(false);
        }
    }

    void Update()
    {
        if (hasDeactivated) return;

        if (damageHandler != null && damageHandler.currentHealth <= 0)
        {
            // Mark as done so this runs only once
            hasDeactivated = true;

            // Activate / Deactivate optional objects
            if (objectToActivate != null) objectToActivate.SetActive(true);
            if (objectToDeactivate != null) objectToDeactivate.SetActive(false);

            // Disable all specified objects
            foreach (GameObject go in objectsToDisable)
            {
                if (go != null)
                {
                    go.SetActive(false);
                }
            }
        }
    }
}
