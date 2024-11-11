using System;
using UnityEngine;
using UnityEngine.Events;

public class ApplovinManager : MonoBehaviour
{
    public static ApplovinManager Instance;

    private void Awake()
    {
        if (Instance)
        {
            DestroyImmediate(gameObject);
        }
        else
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
    }

    [SerializeField] private string maxSdkKey = "ENTER_MAX_SDK_KEY_HERE";

#if UNITY_IOS
    [SerializeField] private string InterstitialAdUnitId = "ENTER_IOS_INTERSTITIAL_AD_UNIT_ID_HERE";
    [SerializeField] private string RewardedAdUnitId = "ENTER_IOS_REWARD_AD_UNIT_ID_HERE";
    [SerializeField] private string RewardedInterstitialAdUnitId = "ENTER_IOS_REWARD_INTER_AD_UNIT_ID_HERE";
    [SerializeField] private string BannerAdUnitId = "ENTER_IOS_BANNER_AD_UNIT_ID_HERE";
    // [SerializeField] private string MRecAdUnitId = "ENTER_IOS_MREC_AD_UNIT_ID_HERE";
#else // UNITY_ANDROID
    [SerializeField] private string InterstitialAdUnitId = "ENTER_ANDROID_INTERSTITIAL_AD_UNIT_ID_HERE";
    [SerializeField] private string RewardedAdUnitId = "ENTER_ANDROID_REWARD_AD_UNIT_ID_HERE";
    [SerializeField] private string RewardedInterstitialAdUnitId = "ENTER_ANDROID_REWARD_INTER_AD_UNIT_ID_HERE";
    [SerializeField] private string BannerAdUnitId = "ENTER_ANDROID_BANNER_AD_UNIT_ID_HERE";
    // [SerializeField] private string MRecAdUnitId = "ENTER_ANDROID_MREC_AD_UNIT_ID_HERE";
#endif
    [SerializeField] private MaxSdkBase.BannerPosition _bannerPosition = MaxSdkBase.BannerPosition.BottomCenter;
    
    private bool _isBannerShowing;
    private bool _isMRecShowing;

    private int _interstitialRetryAttempt;
    private int _rewardedRetryAttempt;
    private int _rewardedInterstitialRetryAttempt;

    private UnityAction _onRewardAdShowedEvent;
    private UnityAction _onRewardedInterstitialAdShowedEvent;
    
    private void Start()
    {
        MaxSdkCallbacks.OnSdkInitializedEvent += sdkConfiguration =>
        {
            // AppLovin SDK is initialized, configure and start loading ads.
            Debug.Log("MAX SDK Initialized");

            InitializeInterstitialAds();
            InitializeRewardedAds();
            InitializeRewardedInterstitialAds();
            InitializeBannerAds();
            // InitializeMRecAds();
            MaxSdk.ShowMediationDebugger();
        };

        MaxSdk.SetSdkKey(maxSdkKey);
        MaxSdk.InitializeSdk();
    }
    
    #region Interstitial Ad Methods

    private void InitializeInterstitialAds()
    {
        // Attach callbacks
        MaxSdkCallbacks.Interstitial.OnAdLoadedEvent += OnInterstitialLoadedEvent;
        MaxSdkCallbacks.Interstitial.OnAdLoadFailedEvent += OnInterstitialFailedEvent;
        MaxSdkCallbacks.Interstitial.OnAdDisplayFailedEvent += InterstitialFailedToDisplayEvent;
        MaxSdkCallbacks.Interstitial.OnAdHiddenEvent += OnInterstitialDismissedEvent;
        MaxSdkCallbacks.Interstitial.OnAdRevenuePaidEvent += OnInterstitialRevenuePaidEvent;
        
        // Load the first interstitial
        LoadInterstitial();
    }

    private void LoadInterstitial()
    {
        Debug.Log("Interstitial Loading...");
        MaxSdk.LoadInterstitial(InterstitialAdUnitId);
    }

    public bool IsInterstitialReady()
    {
        return MaxSdk.IsInterstitialReady(InterstitialAdUnitId);
    }

    public void ShowInterstitial()
    {
        if (MaxSdk.IsInterstitialReady(InterstitialAdUnitId))
        {
            Debug.Log("Interstitial Showing");
            MaxSdk.ShowInterstitial(InterstitialAdUnitId);
        }
        else
        {
            Debug.Log("Interstitial Ad not ready");
        }
    }

    private void OnInterstitialLoadedEvent(string adUnitId, MaxSdkBase.AdInfo adInfo)
    {
        // Interstitial ad is ready to be shown. MaxSdk.IsInterstitialReady(interstitialAdUnitId) will now return 'true'
        Debug.Log("Interstitial loaded");
        
        // Reset retry attempt
        _interstitialRetryAttempt = 0;
    }

    private void OnInterstitialFailedEvent(string adUnitId, MaxSdkBase.ErrorInfo errorInfo)
    {
        // Interstitial ad failed to load. We recommend retrying with exponentially higher delays up to a maximum delay (in this case 64 seconds).
        _interstitialRetryAttempt++;
        double retryDelay = Math.Pow(2, Math.Min(6, _interstitialRetryAttempt));
        
        Debug.Log("Interstitial Load failed: " + errorInfo.Code + "\nRetrying in " + retryDelay + "s...");
        
        Invoke("LoadInterstitial", (float) retryDelay);
    }

    private void InterstitialFailedToDisplayEvent(string adUnitId, MaxSdkBase.ErrorInfo errorInfo, MaxSdkBase.AdInfo adInfo)
    {
        // Interstitial ad failed to display. We recommend loading the next ad
        Debug.Log("Interstitial failed to display with error code: " + errorInfo.Code);
        LoadInterstitial();
    }

    private void OnInterstitialDismissedEvent(string adUnitId, MaxSdkBase.AdInfo adInfo)
    {
        // Interstitial ad is hidden. Pre-load the next ad
        Debug.Log("Interstitial dismissed");
        LoadInterstitial();
    }

    private void OnInterstitialRevenuePaidEvent(string adUnitId, MaxSdkBase.AdInfo adInfo)
    {
        // Interstitial ad revenue paid. Use this callback to track user revenue.
        Debug.Log("Interstitial revenue paid");

        // Ad revenue
        double revenue = adInfo.Revenue;
        
        // Miscellaneous data
        string countryCode = MaxSdk.GetSdkConfiguration().CountryCode; // "US" for the United States, etc - Note: Do not confuse this with currency code which is "USD"!
        string networkName = adInfo.NetworkName; // Display name of the network that showed the ad (e.g. "AdColony")
        string adUnitIdentifier = adInfo.AdUnitIdentifier; // The MAX Ad Unit ID
        string placement = adInfo.Placement; // The placement this ad's postbacks are tied to
    }

    #endregion
    
    #region Rewarded Ad Methods

    private void InitializeRewardedAds()
    {
        // Attach callbacks
        MaxSdkCallbacks.Rewarded.OnAdLoadedEvent += OnRewardedAdLoadedEvent;
        MaxSdkCallbacks.Rewarded.OnAdLoadFailedEvent += OnRewardedAdFailedEvent;
        MaxSdkCallbacks.Rewarded.OnAdDisplayFailedEvent += OnRewardedAdFailedToDisplayEvent;
        MaxSdkCallbacks.Rewarded.OnAdDisplayedEvent += OnRewardedAdDisplayedEvent;
        MaxSdkCallbacks.Rewarded.OnAdClickedEvent += OnRewardedAdClickedEvent;
        MaxSdkCallbacks.Rewarded.OnAdHiddenEvent += OnRewardedAdDismissedEvent;
        MaxSdkCallbacks.Rewarded.OnAdReceivedRewardEvent += OnRewardedAdReceivedRewardEvent;
        MaxSdkCallbacks.Rewarded.OnAdRevenuePaidEvent += OnRewardedAdRevenuePaidEvent;

        // Load the first RewardedAd
        LoadRewardedAd();
    }

    private void LoadRewardedAd()
    {
        Debug.Log("RewardedAd Loading...");
        MaxSdk.LoadRewardedAd(RewardedAdUnitId);
    }

    public bool IsRewardedAdReady()
    {
        return MaxSdk.IsRewardedAdReady(RewardedAdUnitId);
    }

    public void ShowRewardedAd(UnityAction reward)
    {
        if (MaxSdk.IsRewardedAdReady(RewardedAdUnitId))
        {
            _onRewardAdShowedEvent = reward;
            Debug.Log("RewardedAd Showing");
            MaxSdk.ShowRewardedAd(RewardedAdUnitId);
        }
        else
        {
            Debug.Log("RewardedAd not ready");
        }
    }

    private void OnRewardedAdLoadedEvent(string adUnitId, MaxSdkBase.AdInfo adInfo)
    {
        // Rewarded ad is ready to be shown. MaxSdk.IsRewardedAdReady(rewardedAdUnitId) will now return 'true'
        Debug.Log("Rewarded ad loaded");
        
        // Reset retry attempt
        _rewardedRetryAttempt = 0;
    }

    private void OnRewardedAdFailedEvent(string adUnitId, MaxSdkBase.ErrorInfo errorInfo)
    {
        // Rewarded ad failed to load. We recommend retrying with exponentially higher delays up to a maximum delay (in this case 64 seconds).
        _rewardedRetryAttempt++;
        double retryDelay = Math.Pow(2, Math.Min(6, _rewardedRetryAttempt));
        
        Debug.Log("RewardedAd Load failed: " + errorInfo.Code + "\nRetrying in " + retryDelay + "s...");
        
        Invoke("LoadRewardedAd", (float) retryDelay);
    }

    private void OnRewardedAdFailedToDisplayEvent(string adUnitId, MaxSdkBase.ErrorInfo errorInfo, MaxSdkBase.AdInfo adInfo)
    {
        // Rewarded ad failed to display. We recommend loading the next ad
        Debug.Log("Rewarded ad failed to display with error code: " + errorInfo.Code);
        LoadRewardedAd();
    }

    private void OnRewardedAdDisplayedEvent(string adUnitId, MaxSdkBase.AdInfo adInfo)
    {
        Debug.Log("Rewarded ad displayed");
    }

    private void OnRewardedAdClickedEvent(string adUnitId, MaxSdkBase.AdInfo adInfo)
    {
        Debug.Log("Rewarded ad clicked");
    }

    private void OnRewardedAdDismissedEvent(string adUnitId, MaxSdkBase.AdInfo adInfo)
    {
        // Rewarded ad is hidden. Pre-load the next ad
        Debug.Log("Rewarded ad dismissed");
        LoadRewardedAd();
    }

    private void OnRewardedAdReceivedRewardEvent(string adUnitId, MaxSdk.Reward reward, MaxSdkBase.AdInfo adInfo)
    {
        // Rewarded ad was displayed and user should receive the reward
        _onRewardAdShowedEvent.Invoke();
        Debug.Log("Rewarded ad received reward");
    }

    private void OnRewardedAdRevenuePaidEvent(string adUnitId, MaxSdkBase.AdInfo adInfo)
    {
        // Rewarded ad revenue paid. Use this callback to track user revenue.
        Debug.Log("Rewarded ad revenue paid");

        // Ad revenue
        double revenue = adInfo.Revenue;
        
        // Miscellaneous data
        string countryCode = MaxSdk.GetSdkConfiguration().CountryCode; // "US" for the United States, etc - Note: Do not confuse this with currency code which is "USD"!
        string networkName = adInfo.NetworkName; // Display name of the network that showed the ad (e.g. "AdColony")
        string adUnitIdentifier = adInfo.AdUnitIdentifier; // The MAX Ad Unit ID
        string placement = adInfo.Placement; // The placement this ad's postbacks are tied to
    }

    #endregion
    
    #region Rewarded Interstitial Ad Methods

    private void InitializeRewardedInterstitialAds()
    {
        // Attach callbacks
        MaxSdkCallbacks.RewardedInterstitial.OnAdLoadedEvent += OnRewardedInterstitialAdLoadedEvent;
        MaxSdkCallbacks.RewardedInterstitial.OnAdLoadFailedEvent += OnRewardedInterstitialAdFailedEvent;
        MaxSdkCallbacks.RewardedInterstitial.OnAdDisplayFailedEvent += OnRewardedInterstitialAdFailedToDisplayEvent;
        MaxSdkCallbacks.RewardedInterstitial.OnAdDisplayedEvent += OnRewardedInterstitialAdDisplayedEvent;
        MaxSdkCallbacks.RewardedInterstitial.OnAdClickedEvent += OnRewardedInterstitialAdClickedEvent;
        MaxSdkCallbacks.RewardedInterstitial.OnAdHiddenEvent += OnRewardedInterstitialAdDismissedEvent;
        MaxSdkCallbacks.RewardedInterstitial.OnAdReceivedRewardEvent += OnRewardedInterstitialAdReceivedRewardEvent;
        MaxSdkCallbacks.RewardedInterstitial.OnAdRevenuePaidEvent += OnRewardedInterstitialAdRevenuePaidEvent;

        // Load the first RewardedInterstitialAd
        LoadRewardedInterstitialAd();
    }

    private void LoadRewardedInterstitialAd()
    {
        Debug.Log("Rewarded Interstitial Loading...");
        MaxSdk.LoadRewardedInterstitialAd(RewardedInterstitialAdUnitId);
    }

    public bool IsRewardedInterstitialAdReady()
    {
        return MaxSdk.IsRewardedInterstitialAdReady(RewardedInterstitialAdUnitId);
    }
    
    
    public void ShowRewardedInterstitialAd(UnityAction reward)
    {
        if (MaxSdk.IsRewardedInterstitialAdReady(RewardedInterstitialAdUnitId))
        {
            _onRewardedInterstitialAdShowedEvent = reward;
            Debug.Log("Rewarded Interstitial Showing");
            MaxSdk.ShowRewardedInterstitialAd(RewardedInterstitialAdUnitId);
        }
        else
        {
            Debug.Log("Rewarded Interstitial not ready");
        }
    }

    private void OnRewardedInterstitialAdLoadedEvent(string adUnitId, MaxSdkBase.AdInfo adInfo)
    {
        // Rewarded interstitial ad is ready to be shown. MaxSdk.IsRewardedInterstitialAdReady(rewardedInterstitialAdUnitId) will now return 'true'
        Debug.Log("Rewarded interstitial ad loaded");
        
        // Reset retry attempt
        _rewardedInterstitialRetryAttempt = 0;
    }

    private void OnRewardedInterstitialAdFailedEvent(string adUnitId, MaxSdkBase.ErrorInfo errorInfo)
    {
        // Rewarded interstitial ad failed to load. We recommend retrying with exponentially higher delays up to a maximum delay (in this case 64 seconds).
        _rewardedInterstitialRetryAttempt++;
        double retryDelay = Math.Pow(2, Math.Min(6, _rewardedInterstitialRetryAttempt));
        
        Debug.Log("Rewarded Interstitial Load failed: " + errorInfo.Code + "\nRetrying in " + retryDelay + "s...");
        
        Invoke("LoadRewardedInterstitialAd", (float) retryDelay);
    }

    private void OnRewardedInterstitialAdFailedToDisplayEvent(string adUnitId, MaxSdkBase.ErrorInfo errorInfo, MaxSdkBase.AdInfo adInfo)
    {
        // Rewarded interstitial ad failed to display. We recommend loading the next ad
        Debug.Log("Rewarded interstitial ad failed to display with error code: " + errorInfo.Code);
        LoadRewardedInterstitialAd();
    }

    private void OnRewardedInterstitialAdDisplayedEvent(string adUnitId, MaxSdkBase.AdInfo adInfo)
    {
        Debug.Log("Rewarded interstitial ad displayed");
    }

    private void OnRewardedInterstitialAdClickedEvent(string adUnitId, MaxSdkBase.AdInfo adInfo)
    {
        Debug.Log("Rewarded interstitial ad clicked");
    }

    private void OnRewardedInterstitialAdDismissedEvent(string adUnitId, MaxSdkBase.AdInfo adInfo)
    {
        // Rewarded interstitial ad is hidden. Pre-load the next ad
        Debug.Log("Rewarded interstitial ad dismissed");
        LoadRewardedInterstitialAd();
    }

    private void OnRewardedInterstitialAdReceivedRewardEvent(string adUnitId, MaxSdk.Reward reward, MaxSdkBase.AdInfo adInfo)
    {
        // Rewarded interstitial ad was displayed and user should receive the reward
        _onRewardedInterstitialAdShowedEvent.Invoke();
        Debug.Log("Rewarded interstitial ad received reward");
    }

    private void OnRewardedInterstitialAdRevenuePaidEvent(string adUnitId, MaxSdkBase.AdInfo adInfo)
    {
        // Rewarded interstitial ad revenue paid. Use this callback to track user revenue.
        Debug.Log("Rewarded interstitial ad revenue paid");

        // Ad revenue
        double revenue = adInfo.Revenue;
        
        // Miscellaneous data
        string countryCode = MaxSdk.GetSdkConfiguration().CountryCode; // "US" for the United States, etc - Note: Do not confuse this with currency code which is "USD"!
        string networkName = adInfo.NetworkName; // Display name of the network that showed the ad (e.g. "AdColony")
        string adUnitIdentifier = adInfo.AdUnitIdentifier; // The MAX Ad Unit ID
        string placement = adInfo.Placement; // The placement this ad's postbacks are tied to
    }

    #endregion
    
    #region Banner Ad Methods

    
    private void InitializeBannerAds()
    {
        // Attach Callbacks
        MaxSdkCallbacks.Banner.OnAdLoadedEvent += OnBannerAdLoadedEvent;
        MaxSdkCallbacks.Banner.OnAdLoadFailedEvent += OnBannerAdFailedEvent;
        MaxSdkCallbacks.Banner.OnAdClickedEvent += OnBannerAdClickedEvent;
        MaxSdkCallbacks.Banner.OnAdRevenuePaidEvent += OnBannerAdRevenuePaidEvent;

        // Banners are automatically sized to 320x50 on phones and 728x90 on tablets.
        // You may use the utility method `MaxSdkUtils.isTablet()` to help with view sizing adjustments.
        MaxSdk.CreateBanner(BannerAdUnitId, _bannerPosition);

        // Set background or background color for banners to be fully functional.
        MaxSdk.SetBannerBackgroundColor(BannerAdUnitId, Color.black);
    }

    public void ToggleBannerVisibility()
    {
        if (!_isBannerShowing)
        {
            MaxSdk.ShowBanner(BannerAdUnitId);
        }
        else
        {
            MaxSdk.HideBanner(BannerAdUnitId);
        }

        _isBannerShowing = !_isBannerShowing;
    }

    public void ShowBannerAd()
    {
        if (_isBannerShowing) return;
        MaxSdk.ShowBanner(BannerAdUnitId);
        _isBannerShowing = true;
    }

    public void HideBannerAd()
    {
        if (!_isBannerShowing) return;
        MaxSdk.HideBanner(BannerAdUnitId);
        _isBannerShowing = false;
    }

    private void OnBannerAdLoadedEvent(string adUnitId, MaxSdkBase.AdInfo adInfo)
    {
        // Banner ad is ready to be shown.
        // If you have already called MaxSdk.ShowBanner(BannerAdUnitId) it will automatically be shown on the next ad refresh.
        Debug.Log("Banner ad loaded");
    }

    private void OnBannerAdFailedEvent(string adUnitId, MaxSdkBase.ErrorInfo errorInfo)
    {
        // Banner ad failed to load. MAX will automatically try loading a new ad internally.
        Debug.Log("Banner ad failed to load with error code: " + errorInfo.Code);
    }

    private void OnBannerAdClickedEvent(string adUnitId, MaxSdkBase.AdInfo adInfo)
    {
        Debug.Log("Banner ad clicked");
    }

    private void OnBannerAdRevenuePaidEvent(string adUnitId, MaxSdkBase.AdInfo adInfo)
    {
        // Banner ad revenue paid. Use this callback to track user revenue.
        Debug.Log("Banner ad revenue paid");

        // Ad revenue
        double revenue = adInfo.Revenue;
        
        // Miscellaneous data
        string countryCode = MaxSdk.GetSdkConfiguration().CountryCode; // "US" for the United States, etc - Note: Do not confuse this with currency code which is "USD"!
        string networkName = adInfo.NetworkName; // Display name of the network that showed the ad (e.g. "AdColony")
        string adUnitIdentifier = adInfo.AdUnitIdentifier; // The MAX Ad Unit ID
        string placement = adInfo.Placement; // The placement this ad's postbacks are tied to
    }

    #endregion
}
