using UnityEngine;
using UnityEngine.UI;

public class UIFunctions : MonoBehaviour
{
    [SerializeField] private Text rewardText;
    
    [SerializeField] float reward;
    
    public void ShowBanner()
    {
        ApplovinManager.Instance.ShowBannerAd();
    }

    public void HideBanner()
    {
        ApplovinManager.Instance.HideBannerAd();
    }

    public void ShowInterstitial()
    {
        ApplovinManager.Instance.ShowInterstitial();
    }

    public void ShowRewardedVideo()
    {
        ApplovinManager.Instance.ShowRewardedAd(() =>
        {
            reward += 10;
            rewardText.text = reward.ToString();
        });
    }
}
