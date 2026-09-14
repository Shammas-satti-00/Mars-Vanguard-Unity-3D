using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;

[DisallowMultipleComponent]
public class RepeatButton : MonoBehaviour, IPointerDownHandler, IPointerUpHandler, IPointerExitHandler
{
    [Tooltip("How many times per second to invoke OnRepeat.")]
    [Min(0.01f)] public float repeatsPerSecond = 10f;

    [Tooltip("Optional delay before the repeating starts (seconds).")]
    [Min(0f)] public float initialDelay = 0f;

    [Tooltip("Invoke once immediately on press (before the repeat loop).")]
    public bool invokeImmediatelyOnPress = false;

    [Tooltip("Use unscaled time (ignores Time.timeScale).")]
    public bool useUnscaledTime = false;

    [Tooltip("Event fired repeatedly while held.")]
    public UnityEvent OnRepeat;

    bool _isHeld;
    Coroutine _repeatRoutine;

    // Optional: allow non-pointer input to trigger (e.g., from code)
    public void Press()
    {
        if (_isHeld) return;
        _isHeld = true;

        if (invokeImmediatelyOnPress)
            OnRepeat?.Invoke();

        _repeatRoutine = StartCoroutine(RepeatLoop());
    }

    public void Release()
    {
        _isHeld = false;
        if (_repeatRoutine != null)
        {
            StopCoroutine(_repeatRoutine);
            _repeatRoutine = null;
        }
    }

    // UI pointer interfaces
    public void OnPointerDown(PointerEventData eventData) => Press();

    public void OnPointerUp(PointerEventData eventData) => Release();

    // If finger/mouse leaves the element while held, stop repeating
    public void OnPointerExit(PointerEventData eventData) => Release();

    void OnDisable() => Release();

    System.Collections.IEnumerator RepeatLoop()
    {
        float interval = 1f / repeatsPerSecond;

        // initial delay (optional)
        if (initialDelay > 0f)
        {
            if (useUnscaledTime)
                yield return new WaitForSecondsRealtime(initialDelay);
            else
                yield return new WaitForSeconds(initialDelay);
        }

        // main repeat loop
        while (_isHeld)
        {
            OnRepeat?.Invoke();

            if (useUnscaledTime)
                yield return new WaitForSecondsRealtime(interval);
            else
                yield return new WaitForSeconds(interval);
        }
    }
}
