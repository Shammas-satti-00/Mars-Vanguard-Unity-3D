using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Events;
using System.Collections.Generic;

[RequireComponent(typeof(Canvas))]
public class MissileLockUI : MonoBehaviour
{
    [Header("Required References")]
    public Camera worldCamera;
    public Canvas canvas;
    public RectTransform lockAreaRect;

    [Header("Missile Crosshair")]
    public RectTransform missileCrosshairRect;
    public Image missileCrosshairImage;

    [Header("Colors")]
    public Color unlockedGreen = new Color(0.1f, 1f, 0.1f, 1f);
    public Color lockedRed     = new Color(1f, 0.1f, 0.1f, 1f);
    public Color lockingOrange = new Color(1f, 0.55f, 0f, 1f);

    [Header("Behavior")]
    [Tooltip("How quickly the crosshair follows its target (1/s).")]
    public float followSmooth = 15f;
    public bool recenterOnUnlock = true;
    public LayerMask visibleLayers = ~0;

    [Header("Locking FX")]
    public float lockingBlinkHz = 3.0f;
    [Range(0f, 1f)] public float lockBlinkAlphaMin = 0.25f;
    [Range(0f, 1f)] public float lockBlinkAlphaMax = 1.0f;

    [Header("Audio")]
    [Tooltip("AudioSource used to play lock beeps (created at runtime if missing).")]
    public AudioSource beepSource;

    [Tooltip("Beep played while LOCKING.")]
    public AudioClip lockingBeep;
    [Tooltip("Beep (or long tone) played while LOCKED.")]
    public AudioClip lockedBeep;

    [Range(0f,1f)] public float lockingBeepVolume = 0.8f;
    [Range(0f,1f)] public float lockedBeepVolume  = 0.9f;

    [Tooltip("Beep rate (Hz) WHILE LOCKING. (e.g., 2 Hz = two beeps per second)")]
    public float lockingBeepHz = 2.0f;

    [Tooltip("Beep rate (Hz) WHILE LOCKED. Set to 0 for a continuous looped tone using 'lockedBeep'.")]
    public float lockedBeepHz = 0f;

    [Header("Quality of Life")]
    [Tooltip("If true, the locked beep/tone will auto-silence after a duration.")]
    public bool silenceAfterLock = true;
    [Tooltip("How long to keep beeping after achieving lock (seconds).")]
    public float lockedBeepMaxDuration = 1.5f;

    [Header("Events")]
    public UnityEvent OnLockOn;
    public UnityEvent OnLockOff;

    // runtime
    public EquipmentManager _em;
    private Radar _radar;
    private Camera _cam;
    private Vector2 _screenCenter;

    // Missile lock state
    private Targetable _cur;
    private Targetable _hover;
    private Targetable _timer;
    private float _lockTimer;

    // Audio timers/state
    private float _lockingBeepTimer;
    private float _lockedBeepTimer;
    private bool  _lockedLoopPlaying;
    private float _lockedBeepElapsed = 0f;
    private bool  _wasLockedLastTick = false;

    void Awake()
    {
        beepSource = GetComponent<AudioSource>();
    }


    public void Initialize(EquipmentManager em)
    {
        _cam = worldCamera != null ? worldCamera : Camera.main;
        if (canvas == null) canvas = GetComponent<Canvas>();
        _screenCenter = new Vector2(Screen.width * 0.5f, Screen.height * 0.5f);

        if (missileCrosshairImage) missileCrosshairImage.color = unlockedGreen;
        EnsureBeepSource();
        this._em = em;
        this._radar = em != null ? em.equippedRadar : null;
        if (recenterOnUnlock && missileCrosshairRect)
            missileCrosshairRect.position = _screenCenter;
    }

    void OnDisable()
    {
        StopLockedLoopIfNeeded();
        _lockedBeepElapsed = 0f;
        _wasLockedLastTick = false;
    }

    void Update()
    {
        if (_cam == null || canvas == null || lockAreaRect == null || missileCrosshairRect == null)
            return;


        float requiredTime = Mathf.Max(0.05f, _radar != null ? _radar.lockOnTime : 0.5f);
        UpdateMissileLock(requiredTime);

        // feed radar with missile lock
        if (_radar != null) _radar.currentTarget = _cur ? _cur.transform : null;

        // AUDIO
        bool locked  = (_cur != null);
        bool locking = !locked && (_hover != null);

        TickBeep(locking, locked, Time.unscaledDeltaTime);
    }

    // ---------------- Missile lock ----------------
    private void UpdateMissileLock(float requiredTime)
    {
        if (_cur != null)
        {
            if (!IsValidTarget(_cur) || !TryW2S(_cur.AimTransform.position, out var sp) || IsOffscreen(sp))
            {
                ClearMissileLock();
                return;
            }

            MoveCrosshairTowards(missileCrosshairRect, sp, Time.deltaTime);
            if (missileCrosshairImage) missileCrosshairImage.color = lockedRed;
            return;
        }

        _hover = FindClosestInsideLockArea(missileCrosshairRect);
        if (_hover != null)
        {
            if (_timer == _hover) _lockTimer += Time.deltaTime; else { _timer = _hover; _lockTimer = 0f; }

            if (TryW2S(_hover.AimTransform.position, out var spLock))
            {
                MoveCrosshairTowards(missileCrosshairRect, spLock, Time.deltaTime);
                BlinkLocking(missileCrosshairImage);
            }

            if (_lockTimer >= requiredTime) SetMissileLock(_hover);
        }
        else
        {
            _timer = null; _lockTimer = 0f;
            if (missileCrosshairImage) missileCrosshairImage.color = unlockedGreen;
            if (recenterOnUnlock) MoveCrosshairTowards(missileCrosshairRect, _screenCenter, Time.deltaTime);
        }
    }

    private void SetMissileLock(Targetable t)
    {
        _cur = t; _timer = null; _lockTimer = 0f;
        if (missileCrosshairImage) missileCrosshairImage.color = lockedRed;
        if (TryW2S(t.AimTransform.position, out var sp)) missileCrosshairRect.position = sp;
        OnLockOn?.Invoke();
    }

    private void ClearMissileLock()
    {
        bool hadLock = _cur != null;
        _cur = null; _timer = null; _lockTimer = 0f;
        if (missileCrosshairImage) missileCrosshairImage.color = unlockedGreen;
        if (recenterOnUnlock && missileCrosshairRect) MoveCrosshairTowards(missileCrosshairRect, _screenCenter, Time.deltaTime);
        if (hadLock) OnLockOff?.Invoke();
    }

    // ---------------- Public API ----------------
    public Transform GetLockedTargetTransform() => _cur ? _cur.transform : null;

    // ---------------- Helpers ----------------
    private void BlinkLocking(Image img)
    {
        if (!img) return;
        float a = (Mathf.Sin(Time.time * Mathf.PI * 2f * lockingBlinkHz) + 1f) * 0.5f;
        float alpha = Mathf.Lerp(lockBlinkAlphaMin, lockBlinkAlphaMax, a);
        var c = lockingOrange; c.a = alpha; img.color = c;
    }

    private void MoveCrosshairTowards(RectTransform rect, Vector2 screenPos, float dt)
    {
        Vector3 cur = rect.position;
        Vector3 tgt = new Vector3(screenPos.x, screenPos.y, cur.z);
        rect.position = Vector3.Lerp(cur, tgt, 1f - Mathf.Exp(-followSmooth * dt));
    }

    private bool TryW2S(Vector3 worldPos, out Vector2 screen)
    {
        var sp = _cam.WorldToScreenPoint(worldPos);
        if (sp.z <= 0f) { screen = default; return false; }
        screen = new Vector2(sp.x, sp.y);
        return true;
    }

    private bool IsOffscreen(Vector2 s) =>
        (s.x < 0f || s.x > Screen.width || s.y < 0f || s.y > Screen.height);

    private bool IsValidTarget(Targetable t)
    {
        if (t == null || !t.isActiveAndEnabled || !t.isTargetable || t.AimTransform == null) return false;
        if (((1 << t.gameObject.layer) & visibleLayers.value) == 0) return false;
        var sp = _cam.WorldToScreenPoint(t.AimTransform.position);
        return sp.z > 0f;
    }

    private Targetable FindClosestInsideLockArea(RectTransform crosshairRect)
    {
        Targetable best = null;
        float bestDist = float.MaxValue;
        Vector2 crosshairScreen = new Vector2(crosshairRect.position.x, crosshairRect.position.y);

        foreach (var t in Targetable.Registry)
        {
            if (!IsValidTarget(t)) continue;
            if (!TryW2S(t.AimTransform.position, out var sp)) continue;
            if (!ScreenPointInRect(lockAreaRect, sp)) continue;

            float d = (sp - crosshairScreen).sqrMagnitude;
            if (d < bestDist) { bestDist = d; best = t; }
        }
        return best;
    }

    private bool ScreenPointInRect(RectTransform rect, Vector2 screenPoint)
    {
        RectTransformUtility.ScreenPointToLocalPointInRectangle(
            rect,
            screenPoint,
            canvas.renderMode == RenderMode.ScreenSpaceOverlay ? null : _cam,
            out var local);

        Rect r = rect.rect;
        return (local.x >= r.xMin && local.x <= r.xMax &&
                local.y >= r.yMin && local.y <= r.yMax);
    }

    // ---------------- AUDIO ----------------
    private void EnsureBeepSource()
    {
        if (beepSource == null)
        {
            beepSource = gameObject.AddComponent<AudioSource>();
            beepSource.playOnAwake = false;
            beepSource.spatialBlend = 0f; // UI 2D sound
        }
    }

    private void TickBeep(bool locking, bool locked, float dtUnscaled)
    {
        // transitions
        if (locked && !_wasLockedLastTick) _lockedBeepElapsed = 0f;

        if (locked)
        {
            if (silenceAfterLock && _lockedBeepElapsed >= lockedBeepMaxDuration)
            {
                StopLockedLoopIfNeeded();
            }
            else
            {
                if (lockedBeepHz <= 0f)
                {
                    if (!_lockedLoopPlaying && lockedBeep != null && beepSource != null)
                    {
                        beepSource.Stop();
                        beepSource.clip = lockedBeep;
                        beepSource.loop = true;
                        beepSource.Play();
                        _lockedLoopPlaying = true;
                    }
                }
                else
                {
                    StopLockedLoopIfNeeded();
                    _lockedBeepTimer += dtUnscaled;
                    float period = 1f / Mathf.Max(0.01f, lockedBeepHz);
                    if (_lockedBeepTimer >= period)
                    {
                        _lockedBeepTimer = 0f;
                        if (lockedBeep != null && beepSource != null)
                            beepSource.PlayOneShot(lockedBeep);
                    }
                }
                _lockedBeepElapsed += dtUnscaled;
            }

            _lockingBeepTimer = 0f;
            _wasLockedLastTick = true;
            return;
        }

        // not locked
        StopLockedLoopIfNeeded();

        if (locking)
        {
            _lockingBeepTimer += dtUnscaled;
            float period = 1f / Mathf.Max(0.01f, lockingBeepHz);
            if (_lockingBeepTimer >= period)
            {
                _lockingBeepTimer = 0f;
                if (lockingBeep != null && beepSource != null)
                    beepSource.PlayOneShot(lockingBeep, lockingBeepVolume);
            }
            _lockedBeepTimer = 0f;
            _lockedBeepElapsed = 0f;
        }
        else
        {
            _lockingBeepTimer = 0f;
            _lockedBeepTimer = 0f;
            _lockedBeepElapsed = 0f;
        }

        _wasLockedLastTick = false;
    }

    private void StopLockedLoopIfNeeded()
    {
        if (_lockedLoopPlaying && beepSource != null)
        {
            beepSource.Stop();
            beepSource.clip = null;
            beepSource.loop = false;
            _lockedLoopPlaying = false;
        }
    }
}
