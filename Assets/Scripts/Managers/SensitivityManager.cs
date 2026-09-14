using UnityEngine;
using UnityEngine.UI;

public class SensitivityManager : MonoBehaviour
{
    public Slider sensitivitySlider;
    public Slider gyroSensitivitySlider;
    public Button gyroOn;
    public Button gyroOff;
    public GyroController gyroController;
    public GameObject gyroWindow;
    public GameObject hud;

    public void Initialize()
    {

        sensitivitySlider.onValueChanged.AddListener(OnSensitivityChange);
        if (gyroSensitivitySlider)
            gyroSensitivitySlider.onValueChanged.AddListener(OnGyroSensitivityChanged);

        // FIX: correct button assignment
        if (gyroOn)
            gyroOn.onClick.AddListener(GyroOff);
        if (gyroOff)
            gyroOff.onClick.AddListener(GyroOn);

        float savedSensitivity = PlayerPrefs.GetFloat("Sensitivity", 2.5f);
        float gyroSensitivity = PlayerPrefs.GetFloat("GyroSensitivity", 1f);

        sensitivitySlider.value = savedSensitivity / 5f;
        if (gyroSensitivitySlider)
            gyroSensitivitySlider.value = gyroSensitivity / 5f;

        if (DataHolder.Instance != null)
        {
            DataHolder.Instance.sensitivity = savedSensitivity;
            DataHolder.Instance.gyroSensitivity = gyroSensitivity;
        }

        // FIX: Initialize UI correctly AND hide unused button
        bool gyroEnabled = PlayerPrefs.GetInt("GyroEnabled", 0) == 1;

        if (gyroOn != null)
            gyroOn.gameObject.SetActive(gyroEnabled);
        if (gyroOff != null)
            gyroOff.gameObject.SetActive(!gyroEnabled);

        if (gyroEnabled && gyroController != null)
        {
            gyroController.ToggleGyro(true);
            gyroController.InitializeGyro();
        }
    }

    public void OnSensitivityChange(float value)
    {
        float adjusted = value * 5f;
        PlayerPrefs.SetFloat("Sensitivity", adjusted);
        PlayerPrefs.Save();

        if (DataHolder.Instance)
            DataHolder.Instance.sensitivity = adjusted;
    }

    public void OnGyroSensitivityChanged(float value)
    {
        float adjusted = value * 5f;
        PlayerPrefs.SetFloat("GyroSensitivity", adjusted);
        PlayerPrefs.Save();

        if (gyroController != null)
        {
            gyroController.gyroSensitivity = adjusted;

            // FIX: ensure gyro stays initialized
            if (gyroController.gyroEnabled && !gyroController.IsGyroActive())
                gyroController.InitializeGyro();
        }
    }

    public void GyroOn()
    {
        PlayerPrefs.SetInt("GyroEnabled", 1);
        PlayerPrefs.Save();

        if (gyroController)
        {
            gyroController.ToggleGyro(true);
            gyroController.InitializeGyro();
        }

        gyroOn.gameObject.SetActive(true);
        gyroOff.gameObject.SetActive(false);
    }

    public void GyroOff()
    {
        PlayerPrefs.SetInt("GyroEnabled", 0);
        PlayerPrefs.Save();

        if (gyroController)
            gyroController.ToggleGyro(false);

        gyroOn.gameObject.SetActive(false);
        gyroOff.gameObject.SetActive(true);

        gyroSensitivitySlider.value = 0f;
    }

    public void SetGyroController(GyroController gyroController)
    {
        this.gyroController = gyroController;
    }

    public void InititializeWithWindow()
    {
        Time.timeScale = 0f;
        gyroWindow.SetActive(true);
    }

    public void InitializeHelperMethod()
    {
        InitializeGamePlay();
        gyroWindow.SetActive(false);
        Time.timeScale = 1f;
        hud.SetActive(true);
    }
    public void InitializeGamePlay()
    {
        

  
        float savedSensitivity = PlayerPrefs.GetFloat("Sensitivity", 1.2f);
        float gyroSensitivity = PlayerPrefs.GetFloat("GyroSensitivity", 1f);

        if (DataHolder.Instance != null)
        {
            DataHolder.Instance.sensitivity = savedSensitivity;
            DataHolder.Instance.gyroSensitivity = gyroSensitivity;
        }

        // FIX: Initialize UI correctly AND hide unused button
        bool gyroEnabled = PlayerPrefs.GetInt("GyroEnabled", 0) == 1;

     
        if (gyroEnabled && gyroController != null)
        {
            gyroController.ToggleGyro(true);
            gyroController.InitializeGyro();
            gyroController.gyroSensitivity = gyroSensitivity;
        }
    }



}
