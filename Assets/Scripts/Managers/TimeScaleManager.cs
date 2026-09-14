using UnityEngine;
using UnityEngine.UI;

[DisallowMultipleComponent]
public class TimeScaleManager : MonoBehaviour
{
    [Header("UI")]
    public Slider timeScaleSlider;
    public Text timeScaleLabel;

    [Header("Settings")]
    [Range(0f, 10f)] public float maxTimeScale = 10f;
    [Range(0f, 10f)] public float startTimeScale = 1f;
    public bool scaleAudioPitch = false;

    private float baseFixedDeltaTime;

    void Start()
    {
        baseFixedDeltaTime = Time.fixedDeltaTime;

        if (timeScaleSlider != null)
        {
            timeScaleSlider.minValue = 0f;
            timeScaleSlider.maxValue = maxTimeScale;
            timeScaleSlider.wholeNumbers = false;
            timeScaleSlider.value = startTimeScale;
        }

        ApplyTimeScale(startTimeScale);
    }

    void Update()
    {
        if (timeScaleSlider != null)
        {
            ApplyTimeScale(timeScaleSlider.value);
        }
    }

    public void ApplyTimeScale(float scale)
    {
        // Clamp and prevent total freeze
        scale = Mathf.Clamp(scale, 0.01f, maxTimeScale);

        // Apply time scale
        Time.timeScale = scale;

        // Ensure physics stays in sync with time scale
        Time.fixedDeltaTime = 0.02f * Time.timeScale;

        // Optional: scale audio pitch
        if (scaleAudioPitch)
        {
            foreach (var src in FindObjectsOfType<AudioSource>())
                src.pitch = scale;
        }

        // Update label
        if (timeScaleLabel != null)
            timeScaleLabel.text = $"{scale:0.00}x";
    }

    // Quick UI button methods
    public void Pause() => ApplyTimeScale(0.01f);  // small nonzero to avoid full freeze
    public void NormalSpeed() => ApplyTimeScale(1f);
    public void DoubleSpeed() => ApplyTimeScale(2f);
    public void TripleSpeed() => ApplyTimeScale(3f);
    public void MaxSpeed() => ApplyTimeScale(maxTimeScale);
}
