using UnityEngine;
using TMPro;
using System.Collections;

/// <summary>
/// Simple temporary message display script.
/// Assign a TextMeshProUGUI in the inspector, then call ShowMessage("Your text", 3f) from anywhere.
/// The text will appear for the specified duration and then automatically clear itself.
/// Works even if multiple messages are triggered in quick succession (the latest one wins and gets its full duration).
/// </summary>
public class ShowMessage : MonoBehaviour
{
    [Header("Text Reference")]
    [Tooltip("The TextMeshProUGUI that will display the message")]
    [SerializeField] private TextMeshProUGUI messageText;

    [Header("Default Settings")]
    [Tooltip("Default duration if you call ShowMessage(string) without specifying seconds")]
    [SerializeField] private float defaultDuration = 3f;


    private Coroutine currentHideCoroutine = null;


    /// <summary>
    /// Shows a message for a custom duration
    /// </summary>
    public void ShowMessageT(string text, float duration)
    {
        if (messageText == null)
        {
            Debug.LogError("ShowMessage: No TextMeshProUGUI assigned!");
            return;
        }

        // Stop any previous hide coroutine so the new message gets its full time
        if (currentHideCoroutine != null)
            StopCoroutine(currentHideCoroutine);

        messageText.text = text;
        messageText.gameObject.SetActive(true); // ensures it's visible even if it was disabled

        currentHideCoroutine = StartCoroutine(HideAfterSeconds(duration));
    }

    /// <summary>
    /// Shows a message using the default duration set in the inspector
    /// </summary>
    public void ShowMessageDefaultDuration(string text)
    {
        ShowMessageT(text, defaultDuration);
    }

    /// <summary>
    /// Manually hide/clear the message immediately
    /// </summary>
    public void HideMessage()
    {
        if (currentHideCoroutine != null)
        {
            StopCoroutine(currentHideCoroutine);
            currentHideCoroutine = null;
        }

        if (messageText != null)
        {
            messageText.text = "";
            // Optional: disable the GameObject if you prefer it completely hidden
            // messageText.gameObject.SetActive(false);
        }
    }

    private IEnumerator HideAfterSeconds(float seconds)
    {
        yield return new WaitForSeconds(seconds);

        if (messageText != null)
            messageText.text = "";

        currentHideCoroutine = null;
    }

    // ─────────────────────────────────────────────────────────────────────
    // Optional: quick test buttons directly in the inspector (requires Odin or similar, otherwise just use the methods above)
    // ─────────────────────────────────────────────────────────────────────
#if UNITY_EDITOR
    [Header("Inspector Testing")]
    [SerializeField] private string testMessage = "Hello World!";
    [SerializeField] private float testDuration = 4f;

    [ContextMenu("Test Message")]
    private void EditorTest()
    {
        ShowMessageT(testMessage, testDuration);
    }
#endif
}