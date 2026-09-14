using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Advertisements;

public class RewardedAdsButton : MonoBehaviour, IUnityAdsLoadListener, IUnityAdsShowListener
{
    [Header("Button Reference")]
    [Tooltip("Assign the inactive Button GameObject here.")]
    [SerializeField] private Button _rewardButton;

    [Header("Ad Unit IDs (from Unity Dashboard)")]
    [SerializeField] private string _androidAdUnitId = "Rewarded_Android";
    [SerializeField] private string _iOSAdUnitId = "Rewarded_iOS";

    [Header("Resolved (read-only)")]
    [SerializeField] private string _adUnitId; // shows current selection for sanity

    // ---------------- helpers ----------------
    private void ResolveAdUnitId()
    {
#if UNITY_IOS
        _adUnitId = _iOSAdUnitId;
#elif UNITY_ANDROID || UNITY_EDITOR
        _adUnitId = _androidAdUnitId; // Editor uses Android ID by default
#else
        _adUnitId = string.Empty;
#endif
    }

    private void WarnIfEmpty(string where)
    {
        if (string.IsNullOrEmpty(_adUnitId))
        {
            Debug.LogError($"[ADS] Ad Unit ID is empty in {where}. " +
                           $"(Android='{_androidAdUnitId}', iOS='{_iOSAdUnitId}'). " +
                           $"Fill them in the Inspector with IDs from the Unity Dashboard.");
        }
    }
    // -----------------------------------------

    private void OnValidate()
    {
        // Runs in Editor when values change / script reloads
        ResolveAdUnitId();
        // Only warn in Editor to catch Inspector overrides early
#if UNITY_EDITOR
        WarnIfEmpty("OnValidate");
#endif
    }

    void Awake()
    {
        ResolveAdUnitId();

        if (_rewardButton != null)
        {
            _rewardButton.interactable = false;
            _rewardButton.gameObject.SetActive(false);
        }
        else
        {
            Debug.LogWarning("[ADS] Reward Button not assigned in Inspector!");
        }

        // If some other script calls LoadAd() in its Awake, they'll still
        // hit our guard in LoadAd() with a clear error.
    }

    public void LoadAd()
    {
        // Resolve again in case symbols / inspector changed before this call
        if (string.IsNullOrEmpty(_adUnitId)) ResolveAdUnitId();

        if (string.IsNullOrEmpty(_adUnitId))
        {
            Debug.LogError("[ADS] Ad Unit ID is empty or null! " +
                           "Check RewardedAdsButton inspector fields and platform target.");
            return;
        }

        if (!Advertisement.isInitialized)
        {
            Debug.LogWarning("[ADS] Attempted to load before Ads initialization!");
            return;
        }

        Debug.Log("[ADS] Loading Ad: " + _adUnitId);
        Advertisement.Load(_adUnitId, this);
    }

    public void OnUnityAdsAdLoaded(string adUnitId)
    {
        Debug.Log("[ADS] Ad Loaded: " + adUnitId);

        if (!adUnitId.Equals(_adUnitId)) return;

        if (_rewardButton != null)
        {
            _rewardButton.onClick.RemoveAllListeners();
            _rewardButton.onClick.AddListener(ShowAd);
            _rewardButton.interactable = true;
            _rewardButton.gameObject.SetActive(true);
        }
    }

    public void ShowAd()
    {
        if (string.IsNullOrEmpty(_adUnitId))
        {
            Debug.LogError("[ADS] Cannot show Ad: Ad Unit ID is empty!");
            return;
        }

        if (_rewardButton != null)
            _rewardButton.interactable = false;

        Debug.Log("[ADS] Showing Ad: " + _adUnitId);
        Advertisement.Show(_adUnitId, this);
    }

    public void OnUnityAdsShowComplete(string adUnitId, UnityAdsShowCompletionState showCompletionState)
    {
        if (adUnitId.Equals(_adUnitId) && showCompletionState == UnityAdsShowCompletionState.COMPLETED)
        {
            Debug.Log("[ADS] Rewarded Ad Completed!");
            GameManager.Instance.Reward();
        }

        if (_rewardButton != null)
            _rewardButton.gameObject.SetActive(false);

        LoadAd();
    }

    public void OnUnityAdsFailedToLoad(string adUnitId, UnityAdsLoadError error, string message)
    {
        Debug.LogError($"[ADS] Failed to load Ad Unit {adUnitId}: {error} - {message}");
    }

    public void OnUnityAdsShowFailure(string adUnitId, UnityAdsShowError error, string message)
    {
        Debug.LogError($"[ADS] Failed to show Ad Unit {adUnitId}: {error} - {message}");
        LoadAd();
    }

    public void OnUnityAdsShowStart(string adUnitId) { }
    public void OnUnityAdsShowClick(string adUnitId) { }
}
