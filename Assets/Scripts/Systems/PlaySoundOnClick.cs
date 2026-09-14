using UnityEngine;

public class PlaySoundOnClick : MonoBehaviour
{
    public AudioClip uiSound;
    private AudioSource audioSource;

    public void Awake()
    {
        audioSource = GetComponent<AudioSource>();
        audioSource.clip = uiSound;
    }

    public void PlaySound()
    {
        audioSource.Play();
    }
}