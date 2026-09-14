using UnityEngine;
using UnityEngine.UI;

public class ButtonSound : MonoBehaviour
{
    public PlaySoundOnClick playSoundOnClick;

    public void Start()
    {
        playSoundOnClick = FindFirstObjectByType<PlaySoundOnClick>();
        if(playSoundOnClick != null )
        GetComponent<Button>().onClick.AddListener(playSoundOnClick.PlaySound);
    }
}