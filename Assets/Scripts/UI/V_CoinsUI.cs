using TMPro;
using UnityEngine;

public class V_CoinsUI : MonoBehaviour
{
    public TextMeshProUGUI v_CoinsText;
    public void Update()
    {
        v_CoinsText.text = PlayerPrefs.GetInt("V-Coins").ToString();
    }
}