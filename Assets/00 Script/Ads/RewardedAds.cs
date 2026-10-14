using System;
using UnityEngine;
using UnityEngine.Advertisements;

public class RewardedAds : MonoBehaviour, IUnityAdsLoadListener, IUnityAdsShowListener
{
    [SerializeField] private string m_adUnitId = "Rewarded-ads";

    bool isReady = false;
    bool showWhenLoaded;
    Action onReward;

    public void Init()
    {
        Load();

    }

    public void Load()
    {
        Advertisement.Load(m_adUnitId, this);
    }

    public void Show(Action rewardCallback)
    {
        if (!isReady)
        {
            if (showWhenLoaded) return;

            onReward = rewardCallback;
            showWhenLoaded = true;
            Load();
            return;
        }

        onReward = rewardCallback;
        isReady = false;
        Advertisement.Show(m_adUnitId, this);
    }

    public void OnUnityAdsAdLoaded(string id)
    {
        if (id != m_adUnitId) return;

        isReady = true;
        if (!showWhenLoaded) return;

        showWhenLoaded = false;
        Action rewardCallback = onReward;
        Show(rewardCallback);
    }

    public void OnUnityAdsFailedToLoad(string id, UnityAdsLoadError error, string msg)
    {
        if (id == m_adUnitId)
        {
            isReady = false;
            showWhenLoaded = false;
            onReward = null;
        }
        Debug.LogError("Rewarded Load Fail: " + msg);
    }

    public void OnUnityAdsShowComplete(string id, UnityAdsShowCompletionState state)
    {
        if (id == m_adUnitId)
        {
            Action rewardCallback = onReward;
            onReward = null;
            if (state == UnityAdsShowCompletionState.COMPLETED)
            {
                rewardCallback?.Invoke();
            }

            isReady = false;
            Load();
        }
    }

    public void OnUnityAdsShowFailure(string id, UnityAdsShowError error, string msg)
    {
        if (id != m_adUnitId) return;

        onReward = null;
        showWhenLoaded = false;
        isReady = false;
        Debug.LogError("Rewarded Show Fail: " + msg);
        Load();
    }
    public void OnUnityAdsShowStart(string id) { }
    public void OnUnityAdsShowClick(string id) { }
}
