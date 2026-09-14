using UnityEngine;
using TMPro;
using System.Collections;

[RequireComponent(typeof(CanvasGroup))]
public class CombatLogEntry : MonoBehaviour
{
    public TMP_Text label;

    private CanvasGroup _cg;
    private Coroutine _lifeCo;

    void Awake()
    {
        _cg = GetComponent<CanvasGroup>();
        if (label == null)
        {
            // Auto-create label if missing
            var go = new GameObject("Text", typeof(RectTransform), typeof(TMP_Text));
            go.transform.SetParent(transform, false);
            label = go.GetComponent<TMP_Text>();
            label.fontSize = 20;
            label.alignment = TextAlignmentOptions.Left;

#if UNITY_2023_1_OR_NEWER
            label.textWrappingMode = TextWrappingModes.NoWrap; // ✅ New API
#else
            label.enableWordWrapping = false; // Legacy support
#endif

            var rt = label.rectTransform;
            rt.anchorMin = new Vector2(0f, 1f);
            rt.anchorMax = new Vector2(1f, 1f);
            rt.pivot = new Vector2(0f, 1f);
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;
        }
    }

    public void Show(string text, float duration, float fadeOut, System.Action onComplete)
    {
        if (_lifeCo != null) StopCoroutine(_lifeCo);
        label.text = text;
        _cg.alpha = 1f;
        gameObject.SetActive(true);
        _lifeCo = StartCoroutine(LifeRoutine(duration, fadeOut, onComplete));
    }

    private IEnumerator LifeRoutine(float duration, float fadeOut, System.Action onComplete)
    {
        float stayTime = Mathf.Max(0f, duration - Mathf.Max(0f, fadeOut));

        float t = 0f;
        while (t < stayTime)
        {
            t += Time.unscaledDeltaTime;
            yield return null;
        }

        float f = 0f;
        float inv = (fadeOut > 0f) ? (1f / fadeOut) : 0f;
        while (f < fadeOut)
        {
            f += Time.unscaledDeltaTime;
            _cg.alpha = 1f - Mathf.Clamp01(f * inv);
            yield return null;
        }
        _cg.alpha = 0f;

        onComplete?.Invoke();
    }
}
