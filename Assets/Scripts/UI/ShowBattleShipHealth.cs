using UnityEngine;
using TMPro;
using UnityEngine.UI;

public class ShowBattleShipHealth : MonoBehaviour
{
    [Header("Setup")]
    public GameObject uiRoot;           // The UI panel to show/hide
    public Slider healthSlider;         // Slider

    [Header("Settings")]
    public float showDistance = 30f;    // How close the player needs to be

    private Transform player;
    private DamageHandler damageHandler;

    private void Start()
    {
        if (uiRoot != null)
            uiRoot.SetActive(false);

        // Try to find player repeatedly until successful
        InvokeRepeating(nameof(TryFindPlayer), 0f, 1f);
    }

    private void TryFindPlayer()
    {
        if (player != null) return;

        GameObject found = GameObject.FindGameObjectWithTag("PlayerCollider");
        if (found != null)
        {
            player = found.transform;
            damageHandler = GetComponent<DamageHandler>();

            if (damageHandler == null)
                Debug.LogWarning("DamageHandler not found on this object!");
        }
    }

    private void Update()
    {
        if (player == null || damageHandler == null) return;

        float distance = Vector3.Distance(transform.position, player.position);

        bool shouldShow = distance <= showDistance;
        if (uiRoot != null && uiRoot.activeSelf != shouldShow)
            uiRoot.SetActive(shouldShow);

        if (!shouldShow) return;

        // Update TMP and Slider
        float current = damageHandler.currentHealth;
        float max = damageHandler.maxHealth;


        if (healthSlider != null)
        {
            healthSlider.maxValue = max;
            healthSlider.value = current;
        }
    }
}
