
using UnityEngine.UI;
using UnityEngine;
using TMPro;

public class UIManager : MonoBehaviour
{
    public static UIManager Instance;

    public AudioSource uiAudioSource;
    public AudioClip menuOpenSound;
    public AudioClip menuCloseSound;
    public AudioClip buttonClickSound;
    public GameObject pauseButton;
    public GameObject pauseMenu;
    public GameObject settingsMenu;
    public GameObject gameOverMenu;
    public GameObject reviveWindow;
    public GameObject quitWarningWindow;
    public Text reviveWindowTimerText;
    public Text creditsText;
    public Text shipsDestroyedText;
    public Text currentScoreText;
    public Text highScoreText;
    public Text gameOverLabel;
    public TextMeshProUGUI exitTimerText;
    public GameObject hud;
    // Start is called once before the first execution of Update after the MonoBehaviour is created

    private void Awake()
    {
        Instance = this;
    }

    public void ShowWarningWindow()
    {
        pauseMenu.SetActive(false);
        quitWarningWindow.SetActive(true);
    }

    public void CloseWarningWindow()
    {
        pauseMenu.SetActive(true);
        quitWarningWindow.SetActive(false);
    }

    public void TogglePauseMenu()
    {
        if (pauseMenu.activeSelf)
        {
            pauseMenu.SetActive(false);
            hud.SetActive(true);
            Time.timeScale = 1f; // Resume game
            uiAudioSource.PlayOneShot(menuCloseSound);
        }
        else
        {
            pauseMenu.SetActive(true);
            hud.SetActive(false);
            Time.timeScale = 0f; // Pause game
            uiAudioSource.PlayOneShot(menuOpenSound);
            
            
        }
    }

    public void ShowGameOverMenu(int highScore, int score, int credits, int ships)
    {
        gameOverMenu.SetActive(true);
        hud.SetActive(false);
        highScoreText.text = highScore.ToString();
        currentScoreText.text = score.ToString();
        creditsText.text = credits.ToString();
        shipsDestroyedText.text = ships.ToString();

        Time.timeScale = 0f; // Pause game
    }

    public void ShowGameFinishMenu(int highScore, int score, int credits, int ships)
    {

        gameOverMenu.SetActive(true);
        gameOverLabel.text = "Returned To Base";
        hud.SetActive(false);
        highScoreText.text = highScore.ToString();
        currentScoreText.text = score.ToString();
        creditsText.text = credits.ToString();
        shipsDestroyedText.text = ships.ToString();

        Time.timeScale = 0f; // Pause game
    }




    public void PlayButtonClickSound()
    {
        uiAudioSource.PlayOneShot(buttonClickSound);
    }

    public void ShowReviveWindow(bool a)
    {
        reviveWindow.SetActive(a);
    }

    public void ButtonCheck()
    {
        Debug.Log("Button Is Working");
    }

    public void OpenSettings()
    {
        pauseMenu.SetActive(false);
        settingsMenu.SetActive(true);
    }

    public void CloseSettings()
    {
        settingsMenu.SetActive(false);
        pauseMenu.SetActive(true);
    }

    // UpdateTimer is used to update the timer text
    public void UpdateTimer(float time)
    {
        // Display the remaining time in seconds (formatted as "00")
        float seconds = Mathf.FloorToInt(time % 60);
        exitTimerText.text = "Time: " + seconds.ToString("00");
    }

    public void SetTimerTextActive(bool isActive)
    {
        exitTimerText.gameObject.SetActive(isActive);
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
