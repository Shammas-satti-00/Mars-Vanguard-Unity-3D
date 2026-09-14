using UnityEngine;
using UnityEngine.UI;
using TMPro;

[DisallowMultipleComponent]
public class BarsUI_Bridge : MonoBehaviour
{
    [Header("Data Sources")]
    public DamageHandler damageHandler;
    [SerializeField] public Engine engine; // will be auto-filled from ship if possible

    [Header("Health UI")]
    public Slider healthSlider;
    public Image  healthFill;
    public TextMeshProUGUI healthText;
    [Header("Health Colors")]
    public Color healthLow   = new Color(0.85f, 0.2f, 0.2f, 1f);
    public Color healthMid   = new Color(1.0f, 0.8f, 0.2f, 1f);
    public Color healthHigh  = new Color(0.2f,  0.85f, 0.3f, 1f);

    [Header("Shield UI")]
    public Slider shieldSlider;
    public Image  shieldFill;
    public TextMeshProUGUI shieldText;
    [Header("Shield Colors")]
    public Color shieldLow   = new Color(0.2f, 0.5f, 1.0f, 1f);
    public Color shieldMid   = new Color(0.3f, 0.75f, 1.0f, 1f);
    public Color shieldHigh  = new Color(0.6f, 0.95f, 1.0f, 1f);

    [Header("Boost UI")]
    public Slider boostSlider;          // normalized 0..1
    public Image  boostFill;
    public TextMeshProUGUI boostText;
    [Header("Boost Colors")]
    public Color boostLow   = new Color(1.0f, 0.45f, 0.2f, 1f);
    public Color boostMid   = new Color(1.0f, 0.7f,  0.25f, 1f);
    public Color boostHigh  = new Color(1.0f, 0.9f,  0.4f,  1f);

    [Header("Formatting")]
    public string healthLabel = "HP";
    public string shieldLabel = "Shield";
    public string boostLabel  = "Boost";

    public void Initialize(Engine engine, DamageHandler dh)
    {

        this.engine = engine;
        damageHandler = dh;
        // Initialize slider max values once
        if (damageHandler != null && healthSlider)
        {
            healthSlider.maxValue = Mathf.Max(1, damageHandler.maxHealth);
            healthSlider.value    = Mathf.Clamp(damageHandler.currentHealth, 0, damageHandler.maxHealth);
        }
        if (damageHandler != null && shieldSlider)
        {
            shieldSlider.maxValue = Mathf.Max(0, damageHandler.maxShield);
            shieldSlider.value    = Mathf.Clamp(damageHandler.currentShield, 0, damageHandler.maxShield);
        }
        if (boostSlider)
        {
            boostSlider.minValue = 0f;
            boostSlider.maxValue = 1f; // normalized fuel percent
        }

        UpdateAll();
    }

    void Update()
    {
        UpdateAll();
    }

    void UpdateAll()
    {
        UpdateHealthUI();
        UpdateShieldUI();
        UpdateBoostUI();
    }

    void UpdateHealthUI()
    {
        if (damageHandler == null) return;

        float maxH = Mathf.Max(1, damageHandler.maxHealth);
        float curH = Mathf.Clamp(damageHandler.currentHealth, 0, damageHandler.maxHealth);
        float ratio = curH / maxH;

        if (healthSlider)
        {
            healthSlider.maxValue = maxH; // in case max changed
            healthSlider.value = curH;
        }
        if (healthFill)
        {
            healthFill.color = ThreePointGradient(ratio, healthLow, healthMid, healthHigh);
        }
        if (healthText)
        {
            healthText.text = $"{healthLabel}: {curH}/{maxH}";
        }
    }

    void UpdateShieldUI()
    {
        if (damageHandler == null) return;

        float maxS = Mathf.Max(0, damageHandler.maxShield);
        float curS = Mathf.Clamp(damageHandler.currentShield, 0, damageHandler.maxShield);
        float ratio = (maxS > 0f) ? (curS / maxS) : 0f;

        if (shieldSlider)
        {
            shieldSlider.maxValue = maxS; // in case max changed
            shieldSlider.value = curS;
        }
        if (shieldFill)
        {
            shieldFill.color = ThreePointGradient(ratio, shieldLow, shieldMid, shieldHigh);
        }
        if (shieldText)
        {
            shieldText.text = $"{shieldLabel}: {curS}/{maxS}";
        }
    }

    void UpdateBoostUI()
    {
        // Engine optional; if absent, just clear/skip boost
        if (engine == null)
        {
            if (boostText)   boostText.text = $"{boostLabel}: --";
            if (boostSlider) boostSlider.value = 0f;
            if (boostFill)   boostFill.color = boostLow;
            return;
        }

        float percent = Mathf.Clamp01(engine.FuelPercent); // 0..1
        if (boostSlider)
        {
            boostSlider.value = percent;
        }
        if (boostFill)
        {
            boostFill.color = ThreePointGradient(percent, boostLow, boostMid, boostHigh);
        }
        if (boostText)
        {
            // Show percent; add (CD) when on cooldown, (∞) when unlimited
            if (engine.unlimitedBoost)
            {
                boostText.text = $"{boostLabel}: ∞";
            }
            else
            {
                int pct = Mathf.RoundToInt(percent * 100f);
                boostText.text = $"{boostLabel}: {pct}%";
            }
        }
    }


    /// <summary>
    /// Interpolates color across low->mid->high stops based on 0..1 value.
    /// </summary>
    Color ThreePointGradient(float t, Color low, Color mid, Color high)
    {
        t = Mathf.Clamp01(t);
        if (t < 0.5f)
        {
            float k = t / 0.5f; // 0..1
            return Color.LerpUnclamped(low, mid, k);
        }
        else
        {
            float k = (t - 0.5f) / 0.5f; // 0..1
            return Color.LerpUnclamped(mid, high, k);
        }
    }
}
