using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Events;
using TMPro;

[RequireComponent(typeof(Canvas))]
public class CannonLockUI : MonoBehaviour
{
    [Header("Required References")]
    public Camera worldCamera;
    public Canvas canvas;
    public RectTransform lockAreaRect;

    [Header("Cannon Crosshair")]
    public RectTransform cannonCrosshairRect;
    public Image cannonCrosshairImage;

    [Header("Lock Animation Elements")]
    [Tooltip("4 arc UI elements that animate inward")]
    public RectTransform[] lockArcs = new RectTransform[4];
    [Tooltip("4 arrow UI elements that animate inward")]
    public RectTransform[] lockArrows = new RectTransform[4];
    [Tooltip("Distance from center when animation starts")]
    public float animStartDistance = 300f;
    [Tooltip("Final distance from center when locked")]
    public float animEndDistance = 50f;
    [Tooltip("Animation duration in seconds")]
    public float animDuration = 0.5f;
    [Tooltip("Animation curve for distance")]
    public AnimationCurve animCurve = AnimationCurve.EaseInOut(0, 0, 1, 1);

    [Header("Colors")]
    public Color unlockedGreen = new Color(0.1f, 1f, 0.1f, 1f);
    public Color lockedRed = new Color(1f, 0.1f, 0.1f, 1f);

    [Header("Behavior")]
    [Tooltip("How quickly the crosshair follows its target (1/s).")]
    public float followSmooth = 15f;
    public bool recenterOnUnlock = true;
    public LayerMask visibleLayers = ~0;

    [Header("Audio")]
    [Tooltip("AudioSource used to play lock sound (created at runtime if missing).")]
    public AudioSource lockSource;
    [Tooltip("Sound played once when target is locked.")]
    public AudioClip lockSound;
    [Range(0f, 1f)] public float lockSoundVolume = 0.8f;

    [Header("Target Health/Shield Display")]
    [Tooltip("Container for health/shield UI - will be shown/hidden on lock")]
    public GameObject targetStatsContainer;
    [Tooltip("Slider for target's health")]
    public Slider targetHealthSlider;
    [Tooltip("Slider for target's shield")]
    public Slider targetShieldSlider;
    [Tooltip("TextGUI fpr target's name")]
    public TextMeshProUGUI targetNameText;
    [Tooltip("Smooth speed for slider value updates")]
    public float sliderUpdateSmooth = 10f;

    [Header("Events")]
    public UnityEvent OnLockOn;
    public UnityEvent OnLockOff;

    // --- runtime ---
    public EquipmentManager _em;
    private Camera _cam;
    private Vector2 _screenCenter;

    // Cannon lock state
    private Targetable _cur;

    // Lock animation state
    private bool _isAnimatingLock = false;
    private float _animTimer = 0f;
    private Vector2[] _arcOriginalPositions = new Vector2[4];
    private Vector2[] _arrowOriginalPositions = new Vector2[4];
    private Vector2[] _arcStartOffsets = new Vector2[4];
    private Vector2[] _arrowStartOffsets = new Vector2[4];
    private CanvasGroup[] _arcCanvasGroups = new CanvasGroup[4];
    private CanvasGroup[] _arrowCanvasGroups = new CanvasGroup[4];

    // Target stats tracking
    private DamageHandler _currentTargetDamageHandler;
    private float _targetHealthSmoothValue;
    private float _targetShieldSmoothValue;
    private void Awake()
    {
        lockSource = GetComponent<AudioSource>();
    }
    void Start()
    {
        EnsureLockSource();
        InitializeLockAnimationElements();
        InitializeTargetStatsDisplay();
    }

    public void Initialize(EquipmentManager em)
    {
        _cam = worldCamera != null ? worldCamera : Camera.main;
        if (canvas == null) canvas = GetComponent<Canvas>();
        _screenCenter = new Vector2(Screen.width * 0.5f, Screen.height * 0.5f);

        if (cannonCrosshairImage) cannonCrosshairImage.color = unlockedGreen;
        EnsureLockSource();
        this._em = em;

        if (recenterOnUnlock && cannonCrosshairRect)
            cannonCrosshairRect.position = _screenCenter;

        InitializeTargetStatsDisplay();
    }

    void Update()
    {
        if (_cam == null || canvas == null || lockAreaRect == null || cannonCrosshairRect == null)
            return;

        UpdateCannonLock();
        UpdateLockAnimation();
        UpdateTargetStatsDisplay();
    }

    // ---------------- Cannon lock ----------------

    private void UpdateCannonLock()
    {
        // Find closest target inside lock area
        Targetable closestTarget = FindClosestInsideLockArea();

        if (closestTarget != null)
        {
            // Lock on immediately
            bool wasNewLock = (_cur != closestTarget);

            if (wasNewLock)
            {
                Debug.Log($"Locking onto target: {closestTarget.name}");
            }

            _cur = closestTarget;

            // Move crosshair to target
            if (TryW2S(_cur.AimTransform.position, out var sp))
            {
                MoveCrosshairTowards(cannonCrosshairRect, sp, Time.deltaTime);
                if (cannonCrosshairImage) cannonCrosshairImage.color = lockedRed;
            }

            // Rotate cannons to aim at target
            RotateCannonsTowardsTarget(_cur);

            // Play lock sound once when newly locked
            if (wasNewLock)
            {
                PlayLockSound();
                StartLockAnimation();
                OnLockOn?.Invoke();
                UpdateTargetDamageHandler();
                ShowTargetStats();
            }
        }
        else
        {
            // No target - clear lock
            bool hadLock = _cur != null;

            if (hadLock)
            {
                Debug.Log("Lost lock - no targets in area");
            }

            _cur = null;

            if (cannonCrosshairImage) cannonCrosshairImage.color = unlockedGreen;
            if (recenterOnUnlock) MoveCrosshairTowards(cannonCrosshairRect, _screenCenter, Time.deltaTime);

            RotateCannonsToDefault();

            if (hadLock)
            {
                ResetLockElements();
                OnLockOff?.Invoke();
                HideTargetStats();
            }
        }
    }

    private void RotateCannonsTowardsTarget(Targetable tgt)
    {
        if (_em == null || tgt == null || tgt.AimTransform == null) return;

        Vector3 targetPos = tgt.AimTransform.position;

        foreach (var slot in _em.cannonSlots)
        {
            if (slot.FirePoint == null || slot.transform == null) continue;
            slot.transform.LookAt(targetPos);
        }
    }

    private void RotateCannonsToDefault()
    {
        if (_em == null) return;

        foreach (var slot in _em.cannonSlots)
        {
            if (slot.transform == null) continue;

            Vector3 forwardPos = _em.transform.position + _em.transform.forward * 100f;
            slot.transform.LookAt(forwardPos);
        }
    }

    // ---------------- Public API for weapons ----------------
    public Transform GetLockedTargetTransform() => _cur ? _cur.transform : null;

    /// Ray from camera through the cannon crosshair.
    public bool TryGetCannonAimRay(out Ray ray)
    {
        ray = default;
        if (_cam == null || cannonCrosshairRect == null) return false;

        Vector2 screen = cannonCrosshairRect.position;
        ray = _cam.ScreenPointToRay(screen);
        return true;
    }

    /// World aim point for cannons:
    /// - If cannon-locked: target AimTransform position.
    /// - Else: raycast from the cannon crosshair; fallback = far point along ray.
    public bool TryGetCannonAimPoint(out Vector3 worldPoint, float raycastMaxDist = 10000f)
    {
        worldPoint = default;

        if (_cur != null && _cur.AimTransform != null)
        {
            worldPoint = _cur.AimTransform.position;
            return true;
        }

        if (TryGetCannonAimRay(out var ray))
        {
            if (Physics.Raycast(ray, out var hit, raycastMaxDist, visibleLayers, QueryTriggerInteraction.Ignore))
            {
                worldPoint = hit.point;
                return true;
            }
            worldPoint = ray.origin + ray.direction * raycastMaxDist;
            return true;
        }
        return false;
    }

    // ---------------- Helpers ----------------
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

    private bool IsValidTarget(Targetable t)
    {
        if (t == null || !t.isActiveAndEnabled || !t.isTargetable || t.AimTransform == null) return false;
        if (((1 << t.gameObject.layer) & visibleLayers.value) == 0) return false;
        var sp = _cam.WorldToScreenPoint(t.AimTransform.position);
        return sp.z > 0f;
    }

    private Targetable FindClosestInsideLockArea()
    {
        Targetable best = null;
        float bestDist = float.MaxValue;
        Vector2 crosshairScreen = new Vector2(cannonCrosshairRect.position.x, cannonCrosshairRect.position.y);

        int targetCount = 0;
        int validTargets = 0;
        int insideAreaTargets = 0;

        foreach (var t in Targetable.Registry)
        {
            targetCount++;

            if (!IsValidTarget(t)) continue;
            validTargets++;

            if (!TryW2S(t.AimTransform.position, out var sp)) continue;

            if (!ScreenPointInRect(lockAreaRect, sp)) continue;
            insideAreaTargets++;

            float d = (sp - crosshairScreen).sqrMagnitude;
            if (d < bestDist) { bestDist = d; best = t; }
        }

        if (targetCount > 0 && best == null)
        {
            Debug.Log($"Targets: {targetCount}, Valid: {validTargets}, Inside Area: {insideAreaTargets}");
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
    private void EnsureLockSource()
    {
        if (lockSource == null)
        {
            lockSource = gameObject.AddComponent<AudioSource>();
            lockSource.playOnAwake = false;
            lockSource.spatialBlend = 0f; // UI 2D sound
        }
    }

    private void PlayLockSound()
    {
        if (lockSound != null && lockSource != null)
        {
            lockSource.PlayOneShot(lockSound);
        }
    }

    // ---------------- LOCK ANIMATION ----------------

    /// <summary>
    /// Initialize canvas groups and calculate starting offsets for lock elements
    /// </summary>
    private void InitializeLockAnimationElements()
    {
        for (int i = 0; i < 4; i++)
        {
            // Setup arcs - store their original local positions
            if (i < lockArcs.Length && lockArcs[i] != null)
            {
                _arcCanvasGroups[i] = GetOrAddCanvasGroup(lockArcs[i]);
                // Store the INITIAL local position as the target
                _arcOriginalPositions[i] = lockArcs[i].anchoredPosition;

                // Calculate start offset by extending outward from center through the original position
                Vector2 direction = _arcOriginalPositions[i].normalized;
                if (direction.magnitude < 0.01f) direction = GetDefaultDirection(i);
                _arcStartOffsets[i] = direction * animStartDistance;

                _arcCanvasGroups[i].alpha = 0f;
            }

            // Setup arrows - store their original local positions
            if (i < lockArrows.Length && lockArrows[i] != null)
            {
                _arrowCanvasGroups[i] = GetOrAddCanvasGroup(lockArrows[i]);
                // Store the INITIAL local position as the target
                _arrowOriginalPositions[i] = lockArrows[i].anchoredPosition;

                // Calculate start offset by extending outward from center through the original position
                Vector2 direction = _arrowOriginalPositions[i].normalized;
                if (direction.magnitude < 0.01f) direction = GetDefaultDirection(i);
                _arrowStartOffsets[i] = direction * animStartDistance;

                _arrowCanvasGroups[i].alpha = 0f;
            }
        }
    }

    /// <summary>
    /// Get default direction for corner placement
    /// </summary>
    private Vector2 GetDefaultDirection(int index)
    {
        Vector2[] defaultDirections = new Vector2[]
        {
            new Vector2(1, 1),      // Top-right
            new Vector2(-1, 1),     // Top-left
            new Vector2(-1, -1),    // Bottom-left
            new Vector2(1, -1)      // Bottom-right
        };
        return index < defaultDirections.Length ? defaultDirections[index].normalized : Vector2.one.normalized;
    }

    /// <summary>
    /// Get or add a CanvasGroup component to a RectTransform
    /// </summary>
    private CanvasGroup GetOrAddCanvasGroup(RectTransform rect)
    {
        var cg = rect.GetComponent<CanvasGroup>();
        if (cg == null)
            cg = rect.gameObject.AddComponent<CanvasGroup>();
        return cg;
    }

    /// <summary>
    /// Start the lock-on animation
    /// </summary>
    private void StartLockAnimation()
    {
        _isAnimatingLock = true;
        _animTimer = 0f;

        // Set elements to their far starting positions
        for (int i = 0; i < 4; i++)
        {
            if (i < lockArcs.Length && lockArcs[i] != null)
            {
                lockArcs[i].anchoredPosition = _arcStartOffsets[i];
            }

            if (i < lockArrows.Length && lockArrows[i] != null)
            {
                lockArrows[i].anchoredPosition = _arrowStartOffsets[i];
            }
        }
    }

    /// <summary>
    /// Update the lock animation each frame
    /// </summary>
    private void UpdateLockAnimation()
    {
        if (!_isAnimatingLock)
        {
            // When not locked, ensure elements are invisible
            if (_cur == null)
            {
                HideLockElements();
            }
            return;
        }

        _animTimer += Time.deltaTime;
        float t = Mathf.Clamp01(_animTimer / animDuration);
        float curveValue = animCurve.Evaluate(t);

        // Animate each arc
        for (int i = 0; i < 4; i++)
        {
            if (i < lockArcs.Length && lockArcs[i] != null && _arcCanvasGroups[i] != null)
            {
                AnimateLockElement(lockArcs[i], _arcCanvasGroups[i], _arcStartOffsets[i], _arcOriginalPositions[i], curveValue, t);
            }

            if (i < lockArrows.Length && lockArrows[i] != null && _arrowCanvasGroups[i] != null)
            {
                AnimateLockElement(lockArrows[i], _arrowCanvasGroups[i], _arrowStartOffsets[i], _arrowOriginalPositions[i], curveValue, t);
            }
        }

        // Animation complete
        if (t >= 1f)
        {
            _isAnimatingLock = false;
        }
    }

    /// <summary>
    /// Animate a single lock element (arc or arrow) from start position to original position
    /// </summary>
    private void AnimateLockElement(RectTransform element, CanvasGroup canvasGroup, Vector2 startOffset, Vector2 originalPosition, float distanceProgress, float alphaProgress)
    {
        // Lerp from start offset to original position
        Vector2 currentPos = Vector2.Lerp(startOffset, originalPosition, distanceProgress);

        // Position relative to parent
        element.anchoredPosition = currentPos;

        // Fade in alpha
        canvasGroup.alpha = alphaProgress;
    }

    /// <summary>
    /// Hide all lock elements
    /// </summary>
    private void HideLockElements()
    {
        for (int i = 0; i < 4; i++)
        {
            if (_arcCanvasGroups[i] != null)
                _arcCanvasGroups[i].alpha = 0f;

            if (_arrowCanvasGroups[i] != null)
                _arrowCanvasGroups[i].alpha = 0f;
        }
    }

    /// <summary>
    /// Reset lock elements to their starting positions (invisible and far away)
    /// </summary>
    private void ResetLockElements()
    {
        for (int i = 0; i < 4; i++)
        {
            if (i < lockArcs.Length && lockArcs[i] != null)
            {
                lockArcs[i].anchoredPosition = _arcOriginalPositions[i];
                if (_arcCanvasGroups[i] != null)
                    _arcCanvasGroups[i].alpha = 0f;
            }

            if (i < lockArrows.Length && lockArrows[i] != null)
            {
                lockArrows[i].anchoredPosition = _arrowOriginalPositions[i];
                if (_arrowCanvasGroups[i] != null)
                    _arrowCanvasGroups[i].alpha = 0f;
            }
        }
    }

    // ---------------- TARGET HEALTH/SHIELD DISPLAY ----------------

    /// <summary>
    /// Initialize the target stats display elements
    /// </summary>
    private void InitializeTargetStatsDisplay()
    {
        // Hide stats initially
        if (targetStatsContainer != null)
        {
            targetStatsContainer.SetActive(false);
        }

        // Initialize slider values
        if (targetHealthSlider != null)
        {
            targetHealthSlider.value = 0f;
        }

        if (targetShieldSlider != null)
        {
            targetShieldSlider.value = 0f;
        }

        _targetHealthSmoothValue = 0f;
        _targetShieldSmoothValue = 0f;

    }

    /// <summary>
    /// Update reference to current target's DamageHandler
    /// </summary>
    private void UpdateTargetDamageHandler()
    {
        _currentTargetDamageHandler = null;

        if (_cur != null)
        {
            // Try to get DamageHandler from the target or its parents
            _currentTargetDamageHandler = _cur.GetComponentInParent<DamageHandler>();

            if (_currentTargetDamageHandler == null)
            {
                Debug.LogWarning($"Target {_cur.name} does not have a DamageHandler component in itself or parents!");
            }
            else
            {
                Debug.Log($"Found DamageHandler on {_currentTargetDamageHandler.gameObject.name} - Health: {_currentTargetDamageHandler.currentHealth}/{_currentTargetDamageHandler.maxHealth}, Shield: {_currentTargetDamageHandler.currentShield}/{_currentTargetDamageHandler.maxShield}");
                // Initialize smooth values to current values for immediate display
                _targetHealthSmoothValue = GetNormalizedHealth();
                _targetShieldSmoothValue = GetNormalizedShield();
                targetNameText.text = _currentTargetDamageHandler.displayName;
            }
        }
    }

    /// <summary>
    /// Show the target stats UI
    /// </summary>
    private void ShowTargetStats()
    {
        if (targetStatsContainer != null)
        {
            targetStatsContainer.SetActive(true);
        }
    }

    /// <summary>
    /// Hide the target stats UI
    /// </summary>
    private void HideTargetStats()
    {
        if (targetStatsContainer != null)
        {
            targetStatsContainer.SetActive(false);
        }

        _currentTargetDamageHandler = null;
    }

    /// <summary>
    /// Update the target health and shield sliders each frame
    /// </summary>
    private void UpdateTargetStatsDisplay()
    {
        if (_cur == null || _currentTargetDamageHandler == null)
        {
            HideTargetStats();
            return;
        }

        // Check if target is still valid
        if (!_cur.isActiveAndEnabled || !_cur.gameObject.activeInHierarchy)
        {
            HideTargetStats();
            _cur = null;
            _currentTargetDamageHandler = null;
            return;
        }

        // Get normalized health and shield values (0-1)
        float targetHealth = GetNormalizedHealth();
        float targetShield = GetNormalizedShield();

        // Smooth the slider values
        _targetHealthSmoothValue = Mathf.Lerp(_targetHealthSmoothValue, targetHealth, 1f - Mathf.Exp(-sliderUpdateSmooth * Time.deltaTime));
        _targetShieldSmoothValue = Mathf.Lerp(_targetShieldSmoothValue, targetShield, 1f - Mathf.Exp(-sliderUpdateSmooth * Time.deltaTime));

        // Update slider UI
        if (targetHealthSlider != null)
        {
            targetHealthSlider.value = _targetHealthSmoothValue;
        }

        if (targetShieldSlider != null)
        {
            targetShieldSlider.value = _targetShieldSmoothValue;
        }
    }

    /// <summary>
    /// Get normalized health value (0-1) from current target
    /// </summary>
    private float GetNormalizedHealth()
    {
        if (_currentTargetDamageHandler == null)
            return 0f;

        float maxHealth = _currentTargetDamageHandler.maxHealth;
        if (maxHealth <= 0f)
            return 0f;

        return Mathf.Clamp01(_currentTargetDamageHandler.currentHealth / maxHealth);
    }

    /// <summary>
    /// Get normalized shield value (0-1) from current target
    /// </summary>
    private float GetNormalizedShield()
    {
        if (_currentTargetDamageHandler == null)
            return 0f;

        float maxShield = _currentTargetDamageHandler.maxShield;
        if (maxShield <= 0f)
            return 0f;

        return Mathf.Clamp01(_currentTargetDamageHandler.currentShield / maxShield);
    }
}