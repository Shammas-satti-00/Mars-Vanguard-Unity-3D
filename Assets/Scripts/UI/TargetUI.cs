using UnityEngine;
using UnityEngine.UI;
using TMPro;


public class TargetUI : MonoBehaviour
{


    public Radar radar = null;

    public Slider healthSlider;

    public Image healthFill;

    public Slider shieldSlider;

    public Image shieldFill;

    public TextMeshProUGUI targetNameText;

    public void Initialize(Radar radar)
    {
        this.radar = radar;
    }
    void Update()
    {

        if (radar.currentTarget != null)
        {
            SetUIActive(true);
            targetNameText.text = radar.currentTarget.name;
            DamageHandler damageHandler = radar.currentTarget.GetComponent<Targetable>().damageHandler;
            float maxH = Mathf.Max(1, damageHandler.maxHealth);
            float curH = Mathf.Clamp(damageHandler.currentHealth, 0, damageHandler.maxHealth);
            healthSlider.maxValue = maxH; // in case max changed
            healthSlider.value = curH;
            float maxS = Mathf.Max(0, damageHandler.maxShield);
            float curS = Mathf.Clamp(damageHandler.currentShield, 0, damageHandler.maxShield);
            shieldSlider.maxValue = maxS; // in case max changed
            shieldSlider.value = curS;
        }
        else SetUIActive(false);
        
    }

    private void SetUIActive(bool active)
    {
        if (healthSlider != null) healthSlider.gameObject.SetActive(active);
        if (healthFill != null) healthFill.gameObject.SetActive(active);
        if (shieldSlider != null) shieldSlider.gameObject.SetActive(active);
        if (shieldFill != null) shieldFill.gameObject.SetActive(active);
        if (targetNameText != null) targetNameText.gameObject.SetActive(active);
    }
}