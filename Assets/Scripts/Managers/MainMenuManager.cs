using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections;

public class MainMenuManager : MonoBehaviour
{
    public string gamePlayeSceneName = "GamePlayScene";
    public string hangerSceneName = "HangerScene";

    public GameObject mainMenuUI;
    public GameObject settingsUI;
    public GameObject chunkSettingsUI;
    public GameObject musicOn;
    public GameObject musicOff;
    public GameObject soundsOn;
    public GameObject soundsOff;
    public LoadingScreen loadingScreen;
    public MenuStartAnimation menuStartAnimation;
    public SensitivityManager sensitivityManager;

    bool isMusicEnabled;
    bool isSoundsEnabled;

    public void Start()
    {
        Time.timeScale = 1.0f;
        if(DataHolder.Instance != null)
        {
            DataHolder.Instance.sensitivity = PlayerPrefs.GetFloat("Sensitivity");
        }
        if (BGMusicManager.Instance != null)
        {
            BGMusicManager.Instance.volume = PlayerPrefs.GetFloat("MusicVolume");
        }
        if (sensitivityManager != null)
        {
            sensitivityManager.Initialize();
        }
    }
    public void PlayGame()
    {

        menuStartAnimation.StartAnimation();
        StartCoroutine(DelayRoutine(2));

        
    }

    private IEnumerator SceneDelayRoutine(string sceneName)
    {    
        yield return new WaitForSeconds(0.7f);
        SceneManager.LoadScene(sceneName);
    }

    private IEnumerator DelayRoutine(float seconds)
    {
        yield return new WaitForSeconds(seconds);
        loadingScreen.gameObject.SetActive(true);
        loadingScreen.Loaded(25);
        StartCoroutine(SceneDelayRoutine(gamePlayeSceneName));
    }

    public void GoToHanger()
    {
        UnityEngine.SceneManagement.SceneManager.LoadScene(hangerSceneName);
    }

    public void Exit()
    {
        PlayerPrefs.Save();
        Application.Quit();
    }

    public void ToggleSettings()
    {
        bool isActive = settingsUI.activeSelf;
        settingsUI.SetActive(!isActive);
        if(settingsUI.activeSelf)
        {
        musicOff.SetActive(!isMusicEnabled);
            musicOn.SetActive(isMusicEnabled);
        soundsOff.SetActive(!isSoundsEnabled);
            soundsOn.SetActive(isSoundsEnabled);
        }
        mainMenuUI.SetActive(isActive);
    }

    public void ToggleChunkSettings()
    {
        bool isActive = chunkSettingsUI.activeSelf;
        chunkSettingsUI.SetActive(!isActive);
        settingsUI.SetActive(isActive);
    }

    public void ToggleMusic()
    {
        // Toggle the saved bool (1 ↔ 0)
        bool isCurrentlyOn = PlayerPrefs.GetInt("MusicEnabled", 1) == 1;
        bool newState = !isCurrentlyOn;
        // Save the new state
        PlayerPrefs.SetInt("MusicEnabled", newState ? 1 : 0);
        PlayerPrefs.Save();
        isMusicEnabled = newState;
        musicOff.SetActive(!isMusicEnabled);
        musicOn.SetActive(isMusicEnabled);
    }

    public void ToggleSounds()
    {
        // Toggle the saved bool (1 ↔ 0)
        bool isCurrentlyOn = PlayerPrefs.GetInt("SoundsEnabled", 1) == 1;
        bool newState = !isCurrentlyOn;
        // Save the new state
        PlayerPrefs.SetInt("SoundsEnabled", newState ? 1 : 0);
        PlayerPrefs.Save();
        isSoundsEnabled = newState;
        soundsOff.SetActive(!isSoundsEnabled);
        soundsOn.SetActive(isSoundsEnabled);
    }



    
}