using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections;
using UnityEngine.UI;

public class GameManager : MonoBehaviour
{   
    public static GameManager Instance;

    public int currentCredits;
    public int currentScores;
    public int noOfShipsDestroyed;
    public int reviveWindowDuration;
    public DamageHandler damageHandler;
    public bool rewarded;
    public TurnOnOffObject station;
    public TurnOnOffObject arena;



    public int highScore => PlayerPrefs.GetInt("High-Score");

    private void Awake()
    {
        Instance = this;
        currentScores = 0;
        currentCredits = 0;
    }

    void Start()
    {
        int a = PlayerPrefs.GetInt("StationCleared");
        int b = PlayerPrefs.GetInt("ArenaCleared");
        if (a == 1)
            HasClearedStation();
        if(b == 1)
            HasClearedArena();
    }

    public string currentSceneName;
    public string mainMenuSceneName = "MainMenu";


    public void LoadMainMenu()
    {

        SceneManager.LoadScene(mainMenuSceneName);
    }


    public void ClearedStation()
    {
        PlayerPrefs.SetInt("StationCleared", 1);
    }

    public void ClearedArena()
    {
        PlayerPrefs.SetInt("ArenaCleared", 1);
    }

    public void ClearedShip()
    {
        PlayerPrefs.SetInt("ShipCleared", 1);
    }

    

    public void HasClearedStation()
    {
        station.Activate();
    }

    public void HasClearedArena()
    {
        arena.Activate();
    }

    public void HasClearedShip()
    {

    }



    public void RestartCurrentScene()
    {
        SceneManager.LoadScene(currentSceneName);
    }

    public void UpdateValues(int credits, int scores)
    {
        currentCredits += credits;
        currentScores += scores;
        noOfShipsDestroyed += 1;
    }


    public void ShowReviveWindow()
    {
        Time.timeScale = 0; // Pause the game
        StartCoroutine(ReviveWindowTimer(reviveWindowDuration));
    }

    public void Revive()
    {
        damageHandler.Revive();
        Time.timeScale = 1; // Resume the game when revived
    }

    private IEnumerator ReviveWindowTimer(float duration)
    {
        float timeLeft = duration;

        UIManager.Instance.ShowReviveWindow(true); // Show the revive window
        Text reviveWindowTimerText = UIManager.Instance.reviveWindowTimerText;

        // Use unscaled time so the timer continues even when the game is paused
        while (timeLeft > 0)
        {
            reviveWindowTimerText.text = Mathf.Ceil(timeLeft).ToString() + "s";
            timeLeft -= Time.unscaledDeltaTime; // 👈 Unscaled time, ignores Time.timeScale
            yield return null;
        }

        // Timer finished — hide revive window and show game over screen
        UIManager.Instance.ShowReviveWindow(false);
        if(!rewarded)
        UIManager.Instance.ShowGameOverMenu(highScore, currentScores, currentCredits, noOfShipsDestroyed);
        reviveWindowTimerText.text = "";
        rewarded = false;
      
    }


    public void FinializeScoresAndCredits()
    {
        PlayerPrefs.SetInt("V-Coins", PlayerPrefs.GetInt("V-Coins") + currentCredits);
        PlayerPrefs.Save();

        int highScore = PlayerPrefs.GetInt("High-Score", 0);
        if (highScore < currentScores)
        {
            PlayerPrefs.SetInt("High-Score", currentScores);
        }

        int maxNoOfShips = PlayerPrefs.GetInt("MaxShips-Destroyed", 0);
        if (maxNoOfShips < noOfShipsDestroyed)
        {
            PlayerPrefs.SetInt("MaxShips-Destroyed", noOfShipsDestroyed); // <-- fix
        }

        PlayerPrefs.Save();
    }

    public void GameOverViaHanger()
    {

        FinializeScoresAndCredits();
        UIManager.Instance.ShowGameFinishMenu(highScore, currentScores, currentCredits, noOfShipsDestroyed);

    }

    public void GameOver()
    {

        FinializeScoresAndCredits();
        UIManager.Instance.ShowGameFinishMenu(highScore, currentScores, currentCredits, noOfShipsDestroyed);

    }


    public void Reward()
    {
        rewarded = true;
        UIManager.Instance.ShowReviveWindow(false);
        UIManager.Instance.hud.SetActive(true);
        UIManager.Instance.gameOverMenu.SetActive(false);
        Revive(); // Reward logic
    }

    public void AddCoins(int amount)
    {
        int prev = PlayerPrefs.GetInt("V-Coins");
        PlayerPrefs.SetInt("V-Coins", prev + amount);
    }
}