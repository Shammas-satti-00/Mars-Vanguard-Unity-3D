using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;

public class SoundManager : MonoBehaviour
{
    public Slider volumeSlider;
    public Slider musicSlider;
    public Slider sensitivitySlider;
    public Slider gyroSensitivitySlider;
    public Button musicOnButton;
    public Button musicOffButton;
    public Button soundOnButton;
    public Button soundOffButton;
    public List<AudioSource> allAudioSources;


    public void Start()
    {
        float musicVolume = PlayerPrefs.GetFloat("MusicVolume", 1f); // Default value of 1f
        float soundVolume = PlayerPrefs.GetFloat("SoundVolume", 1f); // Default value of 1f

        // Set music button state and volume
        if (musicVolume <= 0)
        {
            ToggleMusicOff();
        }
        else
        {
            ToggleMusicOn();
        }

        // Set sound button state and volume
        if (soundVolume <= 0)
        {
            ToggleSoundOff();
        }
        else
        {
            ToggleSoundOn();
        }

        // Add listeners to sliders
        musicSlider.onValueChanged.AddListener(SetMusic);
        volumeSlider.onValueChanged.AddListener(SetSound);

        // Add listeners to buttons
        musicOnButton.onClick.AddListener(ToggleMusicOff);
        musicOffButton.onClick.AddListener(ToggleMusicOn);
        soundOffButton.onClick.AddListener(ToggleSoundOn);
        soundOnButton.onClick.AddListener(ToggleSoundOff);

         
    }

    //public void AddSource(AudioSource)
    //{

    //}

    public void SetMusic(float newVolume)
    {
        BGMusicManager.Instance.volume = Mathf.Clamp01(newVolume);
        PlayerPrefs.SetFloat("MusicVolume", BGMusicManager.Instance.volume);
        PlayerPrefs.Save();

        if (BGMusicManager.Instance.currentSource != null)
            BGMusicManager.Instance.currentSource.volume = BGMusicManager.Instance.volume;

        if (BGMusicManager.Instance.nextSource != null)
            BGMusicManager.Instance.nextSource.volume = BGMusicManager.Instance.volume;
    }

    public void SetSound(float newVolume)
    {
        float volume = Mathf.Clamp01(newVolume);
        PlayerPrefs.SetFloat("SoundVolume", volume);
        PlayerPrefs.Save();

        foreach (var m in DataHolder.Instance.muzzleFlashPrefab)
        {
            AudioSource source = m.GetComponent<AudioSource>();
            source.volume = volume;
        }

        foreach (var i in DataHolder.Instance.impactEffectPrefab)
        {
            AudioSource source = i.GetComponent<AudioSource>();
            source.volume = volume;
        }

        foreach(var s in allAudioSources)
        {
            s.volume = volume;
        }

        PoolManager.SetVolumeForAllAudioSources(volume);
    }

    public void ToggleMusicOn()
    {
        musicOffButton.gameObject.SetActive(false);
        musicOnButton.gameObject.SetActive(true);
        SetMusic(PlayerPrefs.GetFloat("MusicVolume"));
        musicSlider.value = PlayerPrefs.GetFloat("MusicVolume");
    }

    public void ToggleMusicOff()
    {
        musicOffButton.gameObject.SetActive(true);
        musicOnButton.gameObject.SetActive(false);
        SetMusic(0f);
        musicSlider.value = 0f; // Sync the slider value with the volume
    }

    public void ToggleSoundOn()
    {
        soundOffButton.gameObject.SetActive(false);
        soundOnButton.gameObject.SetActive(true);
        SetSound(PlayerPrefs.GetFloat("SoundVolume"));
        volumeSlider.value = PlayerPrefs.GetFloat("SoundVolume");
    }

    public void ToggleSoundOff()
    {
        soundOffButton.gameObject.SetActive(true);
        soundOnButton.gameObject.SetActive(false);
        SetSound(0f);
        volumeSlider.value = 0f; // Sync the slider value with the volume
    }

    public void AddAudioSource(AudioSource a)
    {
        allAudioSources.Add(a);
    }
}
