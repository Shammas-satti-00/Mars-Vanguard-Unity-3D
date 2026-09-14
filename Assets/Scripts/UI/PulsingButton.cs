using UnityEngine;
using UnityEngine.UI;
using System.Collections;

public class PulsingButton : MonoBehaviour
{
    public float minScale = 1f;  // Minimum scale
    public float maxScale = 1.2f;  // Maximum scale
    public float pulseSpeed = 2f;  // Speed of pulsing effect (higher = faster)

    private RectTransform rectTransform;

    void Start()
    {
        rectTransform = GetComponent<RectTransform>();  // Get the RectTransform component of the UI element
        StartCoroutine(PulseAnimation());  // Start the pulsing effect
    }

    // Coroutine to animate the scaling effect
    private IEnumerator PulseAnimation()
    {
        while (true)
        {
            float lerpTime = Mathf.PingPong(Time.time * pulseSpeed, 1);  // PingPong value between 0 and 1
            float scale = Mathf.Lerp(minScale, maxScale, lerpTime);  // Interpolate between min and max scale
            rectTransform.localScale = new Vector3(scale, scale, 1);  // Apply the scaling effect
            yield return null;  // Wait until next frame
        }
    }
}
