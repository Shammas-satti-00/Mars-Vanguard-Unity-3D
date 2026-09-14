using UnityEngine;
using UnityEngine.Advertisements;

public class AdsInitializer : MonoBehaviour, IUnityAdsInitializationListener
{
    [Header("Game IDs (from Unity Dashboard)")]
    [SerializeField] private string _androidGameId = "5973625";
    [SerializeField] private string _iOSGameId = "5973624";

    [Header("Settings")]
    [SerializeField] private bool _testMode = true;
    [SerializeField] private RewardedAdsButton rewardedAdsButton;

    private string _gameId;

    void Awake()
    {
        InitializeAds();
    }

    private void InitializeAds()
    {
#if UNITY_IOS
        _gameId = _iOSGameId;
#elif UNITY_ANDROID || UNITY_EDITOR
        // Use Android Game ID when testing in Editor
        _gameId = _androidGameId;
#endif

        if (string.IsNullOrWhiteSpace(_gameId))
        {
            Debug.LogError("[ADS] Game ID is empty or null. Please assign it in the Inspector.");
            return;
        }

        if (!Advertisement.isInitialized && Advertisement.isSupported)
        {
            Debug.Log("[ADS] Initializing Unity Ads...");
            Advertisement.Initialize(_gameId, _testMode, this);
        }
        else
        {
            Debug.Log("[ADS] Ads already initialized.");
            OnInitializationComplete();
        }
    }

    public void OnInitializationComplete()
    {
        Debug.Log("[ADS] Unity Ads initialization complete.");
        if (rewardedAdsButton != null)
        {
            rewardedAdsButton.LoadAd();
        }
        else
        {
            Debug.LogWarning("[ADS] RewardedAdsButton reference missing in AdsInitializer!");
        }
    }

    public void OnInitializationFailed(UnityAdsInitializationError error, string message)
    {
        Debug.LogError($"[ADS] Initialization Failed: {error} - {message}");
    }
}
