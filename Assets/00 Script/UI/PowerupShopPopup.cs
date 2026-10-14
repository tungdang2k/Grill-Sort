using System;
using DG.Tweening;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class PowerupShopPopup : MonoBehaviour
{
   
    [SerializeField] private GameObject m_panel;
    [SerializeField] private TextMeshProUGUI titleTxt;
    [SerializeField] private RectTransform content;
    [SerializeField] private CanvasGroup canvasGroup;
    [SerializeField] private int goldCost = 250;

    private string currentPowerUpId;
    private System.Action onSuccess;

    // show popup
    public void Show(string id, System.Action onSuccessCallback = null)
    {
        if(m_panel.activeSelf) return; 

        currentPowerUpId = id;
        onSuccess = onSuccessCallback;

        titleTxt.text = LocalizationManager.Get("Storage");

        m_panel.SetActive(true);

        content.localScale = Vector3.zero;
        canvasGroup.alpha = 0;


        content.DOKill();
        canvasGroup.DOKill();

        canvasGroup.DOFade(1, 0.2f);

        content.DOScale(1f, 0.35f)
            .SetEase(Ease.OutBack);
    }

    // hide popup
    public void Hide()
    {
        content.DOKill();
        canvasGroup.DOKill();

        Sequence seq = DOTween.Sequence();

        seq.Join(
            content.DOScale(0f, 0.2f).SetEase(Ease.InBack)
        );

        seq.Join(
            canvasGroup.DOFade(0, 0.2f)
        );

        seq.OnComplete(() =>
        {
            m_panel.SetActive(false);
        });

    }
   

    public void OnClickBuy()
    {
        if (GoldManager.Instance.SpendGold(goldCost))
        {
            PowerUpUsesManager.AddUses(currentPowerUpId, 1);

            onSuccess?.Invoke(); 
            Hide();
        }
    }


    public void OnClickWatchAd()
    {
        ShowRewardAd(() =>
        {
            PowerUpUsesManager.AddUses(currentPowerUpId, 1);

            onSuccess?.Invoke();
            Hide();
        });
    }


    void ShowRewardAd(System.Action onDone)
    {
        AdsManager.Instance.ShowRewarded(() =>
        {
            onDone?.Invoke();
        });
    }

}
