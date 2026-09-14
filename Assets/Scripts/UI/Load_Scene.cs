using UnityEngine;
using UnityEngine.SceneManagement;

public class Load_Scene : MonoBehaviour
{
    public string SceneName;


    public void LoadScene()
    {
        PlayerPrefs.Save();
        SceneManager.LoadScene(SceneName);
    }
}