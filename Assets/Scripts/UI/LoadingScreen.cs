using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class LoadingScreen : MonoBehaviour
{
    [Header("UI References")]
    public GameObject loadingScreen;
    public Slider loadingScreenSlider;
    public TextMeshProUGUI loadedPercent;

    /// <summary>
    /// Called to update the loading progress (expects 0–100 integer).
    /// </summary>
    public void Loaded(int value)
    {
        // Clamp the input value between 0 and 100
        int clampedValue = Mathf.Clamp(value, 0, 100);

        // Convert to 0-1 range for the slider
        float progress = clampedValue / 100f;

        loadingScreenSlider.value = progress;
        loadedPercent.text = $"{clampedValue}%";

        if (clampedValue >= 100)
            loadingScreen.SetActive(false);
    }

    /// <summary>
    /// Reset the loading screen to start again.
    /// </summary>
    public void Reset()
    {
        loadingScreenSlider.value = 0f;
        loadedPercent.text = "0%";
        loadingScreen.SetActive(true);
    }
}