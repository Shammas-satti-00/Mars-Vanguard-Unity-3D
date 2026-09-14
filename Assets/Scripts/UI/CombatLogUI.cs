using UnityEngine;
using TMPro;
using UnityEngine.UI;
using System.Collections.Generic;

public class CombatLogUI : MonoBehaviour
{
    public static CombatLogUI Instance;

    [Header("Hierarchy")]
    [Tooltip("Parent RectTransform to hold log lines (e.g., a Vertical Layout Group).")]
    public RectTransform listRoot;

    [Tooltip("Prefab with a TextMeshProUGUI + CanvasGroup + CombatLogEntry.")]
    public CombatLogEntry entryPrefab;

    [Header("Behavior")]
    [Tooltip("Seconds each entry stays on screen (including fade).")]
    public float entryDuration = 2.5f;

    [Tooltip("Seconds used for fade-out at the end of duration.")]
    public float fadeOutTime = 0.5f;

    [Tooltip("Max concurrent entries on screen (oldest will be removed if exceeded).")]
    public int maxEntries = 6;

    [Header("Layout")]
    public float entrySpacingY = 30f; // distance between stacked entries

    private readonly Queue<CombatLogEntry> _active = new Queue<CombatLogEntry>();

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;

        if (listRoot == null)
        {
            // Create a simple vertical root if missing
            var go = new GameObject("CombatLogRoot", typeof(RectTransform), typeof(VerticalLayoutGroup), typeof(ContentSizeFitter));
            go.transform.SetParent(transform, false);
            listRoot = go.GetComponent<RectTransform>();
            var v = go.GetComponent<VerticalLayoutGroup>();
            v.childAlignment = TextAnchor.UpperLeft;
            v.childControlWidth = true;
            v.childControlHeight = true;
            v.childForceExpandWidth = true;
            v.childForceExpandHeight = false;
            v.spacing = 4f;

            var fitter = go.GetComponent<ContentSizeFitter>();
            fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
            fitter.horizontalFit = ContentSizeFitter.FitMode.Unconstrained;

            // Anchor to top-left by default
            listRoot.anchorMin = new Vector2(0f, 1f);
            listRoot.anchorMax = new Vector2(0f, 1f);
            listRoot.pivot = new Vector2(0f, 1f);
            listRoot.anchoredPosition = new Vector2(20f, -20f);
        }
    }

    /// <summary>
    /// Add a new "X destroyed +Y credits" entry. New entry appears LOWER than existing ones.
    /// </summary>
    public void LogDestroy(string targetName, int credits)
    {
        if (entryPrefab == null || listRoot == null) return;

        // Remove oldest if too many
        while (_active.Count >= maxEntries)
        {
            var oldest = _active.Dequeue();
            if (oldest != null) Destroy(oldest.gameObject);
        }

        // Spawn new entry at the top
        var entry = Instantiate(entryPrefab, listRoot);
        entry.transform.SetAsFirstSibling();

        // Stack all existing entries downward
        int i = 0;
        foreach (var e in _active)
        {
            if (e != null)
            {
                var rt = (RectTransform)e.transform;
                rt.anchoredPosition = new Vector2(0f, -(i + 1) * entrySpacingY);
            }
            i++;
        }

     // Position the new entry at the top (y=0)
     ((RectTransform)entry.transform).anchoredPosition = Vector2.zero;

        // Animate + destroy correctly
        entry.Show($"{targetName} destroyed   +{credits} credits",
            entryDuration,
            fadeOutTime,
            onComplete: () =>
            {
                // Safe removal
                var list = new List<CombatLogEntry>(_active);
                list.Remove(entry);
                _active.Clear();
                foreach (var x in list) _active.Enqueue(x);

                if (entry != null)
                    Destroy(entry.gameObject);
            });

        // Add to queue AFTER spawning
        _active.Enqueue(entry);
    }


}
