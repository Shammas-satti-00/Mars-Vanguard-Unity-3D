using UnityEngine;
using UnityEngine.UI;

public class RollingCredits : MonoBehaviour
{
    public RectTransform creditsText;
    public float scrollSpeed = 40f;

    private float startY;
    private float endY;

    void Start()
    {
        // force layout update so we get correct height
        LayoutRebuilder.ForceRebuildLayoutImmediate(creditsText);

        float textHeight = creditsText.rect.height;
        float screenHeight = Screen.height;

        // start slightly below screen
        startY = -screenHeight * 0.5f;

        // end when the entire text has scrolled above the screen
        endY = textHeight + screenHeight * 0.5f;

        creditsText.anchoredPosition = new Vector2(0, startY);

        Debug.Log($"[RollingCredits] TextHeight={textHeight}, StartY={startY}, EndY={endY}");
    }

    void Update()
    {
        creditsText.anchoredPosition += new Vector2(0, scrollSpeed * Time.deltaTime);

        if (creditsText.anchoredPosition.y >= endY)
        {
            creditsText.anchoredPosition = new Vector2(0, startY);
        }
    }
}
