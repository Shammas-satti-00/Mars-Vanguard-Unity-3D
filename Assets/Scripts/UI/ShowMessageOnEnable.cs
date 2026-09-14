using UnityEngine;
using TMPro;
using System.Collections;

public class ShowMessageOnEnable : MonoBehaviour
{
    [Header("UI")]
    public TMP_Text messageText;     // Assign TMP text here

    [Header("Message Settings")]
    [TextArea]
    public string message = "Object Activated!";

    [Space(5)]
    public bool useDelay = false;
    public float displayDelay = 0.5f;

    public bool useDuration = false;
    public float messageDuration = 2f;

    [Header("After Message Behavior")]
    public bool clearTextAfterDuration = false;
    public bool disableObjectAfterDuration = false;

    private void OnEnable()
    {
        if (messageText == null)
        {
            Debug.LogWarning($"ShowMessageOnEnable: No TMP Text assigned on {gameObject.name}");
            return;
        }

        StopAllCoroutines();
        StartCoroutine(ShowMessageRoutine());
    }

    private IEnumerator ShowMessageRoutine()
    {
        if (useDelay)
            yield return new WaitForSeconds(displayDelay);

        // Show the message
        messageText.text = message;

        if (useDuration)
        {
            yield return new WaitForSeconds(messageDuration);

            if (clearTextAfterDuration)
                messageText.text = "";

            if (disableObjectAfterDuration)
                gameObject.SetActive(false);
        }
    }
}
