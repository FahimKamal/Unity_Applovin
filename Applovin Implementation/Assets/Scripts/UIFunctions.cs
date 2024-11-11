using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class UIFunctions : MonoBehaviour
{
    [SerializeField] private Text rewardText;
    
    [SerializeField] float reward;

    public void GoSecondPage()
    {
        SceneManager.LoadScene("SecondPage");
    }

    public void GoFirstPage()
    {
        SceneManager.LoadScene("FirstPage");
    }
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
