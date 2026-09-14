using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;

public class AmmoUI : MonoBehaviour
{
    [Header("References")]
    public TMP_Text cannonText;
    public TMP_Text missileText;
    public TMP_Text cannonStatusText;
    public TMP_Text missileStatusText;

    [Header("Sliders (Optional)")]
    public Slider cannonSlider;
    public Slider missileSlider;

    [Header("Custom Slider Calibration (Cannon)")]
    [Range(0f, 1f)] public float cannonEmptyValue = 0.358f;
    [Range(0f, 1f)] public float cannonFullValue = 0.933f;

    [Header("Custom Slider Calibration (Missile)")]
    [Range(0f, 1f)] public float missileEmptyValue = 0.358f;
    [Range(0f, 1f)] public float missileFullValue = 0.933f;

    [Header("Formatting")]
    public string cannonFormat = "Cannon {0}/{1}";
    public string missileFormat = "Missile {0}/{1}";

    // Runtime references
    private EquipmentManager _em;
    private CannonSlot _cannon;
    private LauncherSlot _launcher;

    // Track last ammo counts to detect changes
    private int lastCannonAmmo = -1;
    private int lastMissileAmmo = -1;

    // Store coroutine handles so we can stop them safely
    private Coroutine cannonStatusRoutine;
    private Coroutine missileStatusRoutine;

    // Called externally to initialize
    public void Initialize(EquipmentManager em)
    {
        _em = em;
        Bind();
        UpdateUI(true); // Force full refresh
    }

    void Update()
    {
        UpdateUI();
    }

    private void Bind()
    {
        _cannon = null;
        _launcher = null;

        if (_em == null) return;

        _cannon = _em.GetPrimaryCannonSlot();
        _launcher = _em.GetPrimaryLauncherSlot();
    }

    private void UpdateUI(bool forceRefresh = false)
    {
        if (_em == null || !isActiveAndEnabled) return;

        // --- Cannon ---
        int cannonCur = _cannon != null ? _cannon.currentAmmo : 0;
        int cannonCap = _cannon != null ? _cannon.magazineCapacity : 0;

        if (cannonText)
            cannonText.text = string.Format(cannonFormat, cannonCur, cannonCap);

        if (cannonSlider)
            cannonSlider.value = MapAmmoToSlider(cannonCur, cannonCap, cannonEmptyValue, cannonFullValue);

        // Update cannon status only when value changes
        if (forceRefresh || cannonCur != lastCannonAmmo)
        {
            lastCannonAmmo = cannonCur;
            if (cannonStatusRoutine != null)
                StopCoroutine(cannonStatusRoutine);
            cannonStatusRoutine = StartCoroutine(UpdateStatusText(cannonStatusText, cannonCur, cannonCap));
        }

        // --- Missiles ---
        int missileCur = _launcher != null ? _launcher.CurrentAmmo : 0;
        int missileCap = _launcher != null ? _launcher.magazineCapacity : 0;

        if (missileText)
            missileText.text = string.Format(missileFormat, missileCur, missileCap);

        if (missileSlider)
            missileSlider.value = MapAmmoToSlider(missileCur, missileCap, missileEmptyValue, missileFullValue);

        // Update missile status only when value changes
        if (forceRefresh || missileCur != lastMissileAmmo)
        {
            lastMissileAmmo = missileCur;
            if (missileStatusRoutine != null)
                StopCoroutine(missileStatusRoutine);
            missileStatusRoutine = StartCoroutine(UpdateStatusText(missileStatusText, missileCur, missileCap));
        }
    }

    private IEnumerator UpdateStatusText(TMP_Text statusText, int current, int capacity)
    {
        if (statusText == null)
            yield break;

        if (current <= 0)
        {
            // Out of ammo
            statusText.text = "EMPTY";
            statusText.color = Color.red;
            yield break;
        }
        else if (current < capacity * 0.2f)
        {
            // Low ammo blinking
            while (true)
            {
                statusText.text = "<b><color=red>LOW</color></b>";
                statusText.color = Color.red;
                yield return new WaitForSeconds(0.5f);
                statusText.text = "";
                yield return new WaitForSeconds(0.5f);
            }
        }
        else if (current == capacity)
        {
            // Full ammo
            statusText.text = "FULL";
            statusText.color = Color.green;
        }
        else
        {
            // Normal
            statusText.text = "";
            statusText.color = Color.white;
        }
    }

    private float MapAmmoToSlider(int current, int capacity, float emptyVal, float fullVal)
    {
        if (!IsValidRange(emptyVal, fullVal))
            return capacity > 0 ? Mathf.Clamp01((float)current / capacity) : 0f;

        float t = capacity > 0 ? Mathf.Clamp01((float)current / capacity) : 0f;
        return Mathf.Lerp(emptyVal, fullVal, t);
    }

    private bool IsValidRange(float a, float b)
    {
        if (Mathf.Approximately(a, b)) return false;
        return a >= 0f && a <= 1f && b >= 0f && b <= 1f;
    }

#if UNITY_EDITOR
    private void OnValidate()
    {
        cannonEmptyValue = Mathf.Clamp01(cannonEmptyValue);
        cannonFullValue = Mathf.Clamp01(cannonFullValue);
        missileEmptyValue = Mathf.Clamp01(missileEmptyValue);
        missileFullValue = Mathf.Clamp01(missileFullValue);
    }
#endif
}
