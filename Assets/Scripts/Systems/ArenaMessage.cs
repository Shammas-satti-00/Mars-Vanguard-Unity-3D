using UnityEngine;
using TMPro;
using System.Collections;

[RequireComponent(typeof(Collider))]
public class ArenaMessage : MonoBehaviour
{
    [Header("UI Settings")]
    [Tooltip("Assign the TextMeshProUGUI element that will display the message (usually part of a Canvas).")]
    public TextMeshProUGUI messageText;

    [Header("Message Settings")]
    [TextArea(3, 5)]
    public string message = "Defeat all arena waves to unlock warpgate";

    [Header("Display Settings")]
    [Tooltip("How long the message stays visible after the player enters the zone (in seconds). Set to 0 if you want it to stay forever after first trigger.")]
    public float showDuration = 5f;

    [Header("Player Settings")]
    [Tooltip("Tag of the player object/collider that should trigger the message.")]
    public string playerTag = "Player";

    private Collider messageCollider;
    private Coroutine hideCoroutine;

    private void Awake()
    {
        messageCollider = GetComponent<Collider>();
        messageCollider.isTrigger = true;

        if (messageText != null)
            messageText.gameObject.SetActive(false);
    }



private void OnTriggerEnter(Collider other)
{
    // Exact same detection logic as your Teleporter script
    if (!other.CompareTag(playerTag))
        return;

    if (messageText != null)
    {
        messageText.text = message;
        messageText.gameObject.SetActive(true);

        // Restart the timer every time the player (re)enters so they always get the full duration
        if (hideCoroutine != null)
            StopCoroutine(hideCoroutine);

        if (showDuration > 0f)
            hideCoroutine = StartCoroutine(HideAfterDelay(showDuration));
        // if showDuration <= 0 → message stays on screen forever once triggered
    }
}

private IEnumerator HideAfterDelay(float delay)
{
    yield return new WaitForSeconds(delay);

    if (messageText != null)
        messageText.gameObject.SetActive(false);

    hideCoroutine = null;
}
}