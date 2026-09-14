//using UnityEngine;
//using TMPro;

//public class ChunkSettingsManager : MonoBehaviour
//{
//    [Header("Input Fields")]
//    public TMP_InputField ecsChunkSizeInput;
//    public TMP_InputField ecsLoadDistanceInput;
//    public TMP_InputField ecsPreloadDistanceInput;
//    public TMP_InputField colliderChunkSizeInput;
//    public TMP_InputField colliderLoadDistanceInput;
//    public TMP_InputField colliderPreloadDistanceInput;
//    public TMP_InputField maxLoadsPerFrameInput;

//    private const string KEY_ECS_CHUNK_SIZE = "ECSChunkSize";
//    private const string KEY_ECS_LOAD_DIST = "ECSLoadDistance";
//    private const string KEY_ECS_PRELOAD_DIST = "ECSPreloadDistance";
//    private const string KEY_COLLIDER_CHUNK_SIZE = "ColliderChunkSize";
//    private const string KEY_COLLIDER_LOAD_DIST = "ColliderLoadDistance";
//    private const string KEY_COLLIDER_PRELOAD_DIST = "ColliderPreloadDistance";
//    private const string KEY_MAX_LOADS = "MaxLoadsPerFrame";

//    void Start()
//    {
//        LoadSettings();
//    }

//    public void ApplySave()
//    {
//        float ecsChunkSize = ParseFloat(ecsChunkSizeInput, 0f);
//        float ecsLoadDist = ParseFloat(ecsLoadDistanceInput, 0f);
//        float ecsPreloadDist = ParseFloat(ecsPreloadDistanceInput, 0f);
//        float colliderChunkSize = ParseFloat(colliderChunkSizeInput, 0f);
//        float colliderLoadDist = ParseFloat(colliderLoadDistanceInput, 0f);
//        float colliderPreloadDist = ParseFloat(colliderPreloadDistanceInput, 0f);
//        int maxLoads = ParseInt(maxLoadsPerFrameInput, 0);

//        PlayerPrefs.SetFloat(KEY_ECS_CHUNK_SIZE, ecsChunkSize);
//        PlayerPrefs.SetFloat(KEY_ECS_LOAD_DIST, ecsLoadDist);
//        PlayerPrefs.SetFloat(KEY_ECS_PRELOAD_DIST, ecsPreloadDist);
//        PlayerPrefs.SetFloat(KEY_COLLIDER_CHUNK_SIZE, colliderChunkSize);
//        PlayerPrefs.SetFloat(KEY_COLLIDER_LOAD_DIST, colliderLoadDist);
//        PlayerPrefs.SetFloat(KEY_COLLIDER_PRELOAD_DIST, colliderPreloadDist);
//        PlayerPrefs.SetInt(KEY_MAX_LOADS, maxLoads);

//        PlayerPrefs.Save();

//        Debug.Log("Settings saved!");
//    }

//    public void LoadSettings()
//    {
//        if (ecsChunkSizeInput) ecsChunkSizeInput.text = PlayerPrefs.GetFloat(KEY_ECS_CHUNK_SIZE, 0f).ToString();
//        if (ecsLoadDistanceInput) ecsLoadDistanceInput.text = PlayerPrefs.GetFloat(KEY_ECS_LOAD_DIST, 0f).ToString();
//        if (ecsPreloadDistanceInput) ecsPreloadDistanceInput.text = PlayerPrefs.GetFloat(KEY_ECS_PRELOAD_DIST, 0f).ToString();
//        if (colliderChunkSizeInput) colliderChunkSizeInput.text = PlayerPrefs.GetFloat(KEY_COLLIDER_CHUNK_SIZE, 0f).ToString();
//        if (colliderLoadDistanceInput) colliderLoadDistanceInput.text = PlayerPrefs.GetFloat(KEY_COLLIDER_LOAD_DIST, 0f).ToString();
//        if (colliderPreloadDistanceInput) colliderPreloadDistanceInput.text = PlayerPrefs.GetFloat(KEY_COLLIDER_PRELOAD_DIST, 0f).ToString();
//        if (maxLoadsPerFrameInput) maxLoadsPerFrameInput.text = PlayerPrefs.GetInt(KEY_MAX_LOADS, 0).ToString();
//    }

//    float ParseFloat(TMP_InputField input, float defaultValue)
//    {
//        if (input == null || string.IsNullOrEmpty(input.text)) return defaultValue;
//        return float.TryParse(input.text, out float result) ? result : defaultValue;
//    }

//    int ParseInt(TMP_InputField input, int defaultValue)
//    {
//        if (input == null || string.IsNullOrEmpty(input.text)) return defaultValue;
//        return int.TryParse(input.text, out int result) ? result : defaultValue;
//    }

//    public void ResetToDefaults()
//    {
//        PlayerPrefs.DeleteKey(KEY_ECS_CHUNK_SIZE);
//        PlayerPrefs.DeleteKey(KEY_ECS_LOAD_DIST);
//        PlayerPrefs.DeleteKey(KEY_ECS_PRELOAD_DIST);
//        PlayerPrefs.DeleteKey(KEY_COLLIDER_CHUNK_SIZE);
//        PlayerPrefs.DeleteKey(KEY_COLLIDER_LOAD_DIST);
//        PlayerPrefs.DeleteKey(KEY_COLLIDER_PRELOAD_DIST);
//        PlayerPrefs.DeleteKey(KEY_MAX_LOADS);

//        PlayerPrefs.Save();

//        LoadSettings();

//        Debug.Log("Settings reset!");
//    }
//}
