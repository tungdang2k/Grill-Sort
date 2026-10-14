using System.Collections;
using System.Collections.Generic;
using System.Linq;
using DG.Tweening;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class Powerups : MonoBehaviour
{
    [SerializeField] private CountDowntimer m_timer;
    [SerializeField] private  Transform m_magnetTarget;
    [SerializeField] private TextMeshProUGUI m_txtAddTimePrefab;
    [SerializeField] private Transform m_popupRoot;
    [SerializeField] private float m_TimeBonus = 45;
    [SerializeField] private PowerupShopPopup m_storagePopup;
    [SerializeField] private TextMeshProUGUI m_magnetCountTxt;
    [SerializeField] private TextMeshProUGUI m_shuffleCountTxt;
    [SerializeField] private TextMeshProUGUI m_timeCountTxt;
    [SerializeField] private Image m_magnetPlusIcon;
    [SerializeField] private Image m_shufflePlusIcon;
    [SerializeField] private Image m_timePlusIcon;
    [SerializeField] private PlateAnimation m_plateAnimation;

    private bool m_isUsingPowerup = false;
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Awake()
    {
        if(m_storagePopup == null)
        {   
            m_storagePopup = FindFirstObjectByType<PowerupShopPopup>();
        }
        if(m_plateAnimation == null)
        {
            m_plateAnimation = FindFirstObjectByType<PlateAnimation>();
        }

        UpdatePowerUpUI();
    }

    void UpdatePowerUpUI()
    {
        UpdateSingle(CONSTANTS.MAGNET, m_magnetCountTxt, m_magnetPlusIcon);
        UpdateSingle(CONSTANTS.SHUFFLE, m_shuffleCountTxt, m_shufflePlusIcon);
        UpdateSingle(CONSTANTS.ADDTIME, m_timeCountTxt, m_timePlusIcon);
    }

    void UpdateSingle(string id, TextMeshProUGUI txt, Image plusIcon)
    {
        int uses = PowerUpUsesManager.GetUses(id);

        txt.text = uses.ToString();

        if (uses > 0)
        {
            txt.text = uses.ToString();
            txt.transform.parent.gameObject.SetActive(true);
            plusIcon.gameObject.SetActive(false);
        }
        else
        {
            txt.transform.parent.gameObject.SetActive(false);
            plusIcon.gameObject.SetActive(true);
        }
    }
    public void OnClickMagnet()
    {
        HandlePowerUp(CONSTANTS.MAGNET, OnMagnet);
    }

    public void OnClickShuffle()
    {
        HandlePowerUp(CONSTANTS.SHUFFLE, OnShuffle);
    }

    public void OnClickAddTime()
    {
        HandlePowerUp(CONSTANTS.ADDTIME, OnAddTime);
    }

    void HandlePowerUp(string id, System.Action action)
    {
        if (m_isUsingPowerup) return;
        int uses = PowerUpUsesManager.GetUses(id);

        if (uses <= 0)
        {
            m_storagePopup.Show(id, UpdatePowerUpUI);
            return;
        }
        if (id == CONSTANTS.MAGNET && !CanUseMagnet())
        {

            return;
        }
        m_isUsingPowerup = true;
        PowerUpUsesManager.AddUses(id, -1);
        UpdatePowerUpUI();
        action?.Invoke();
    }

    private bool CanUseMagnet()
    {
        var allFood = GetAllActiveFood();
        var groups = new Dictionary<string, int>();
        foreach (var (img, slot, tray) in allFood)
        {
            string name = img.sprite.name;
            if (!groups.ContainsKey(name)) groups[name] = 0;
            groups[name]++;
        }
        return groups.Any(kvp => kvp.Value >= 3);
    }

    private void OnMagnet()
    {

        AudioManager.Instance.PlaySFX(SFXType.Merge);

        var allFood = GetAllActiveFood();
        var groups = new Dictionary<string, List<Image>>();

        foreach (var (img, slot, tray) in allFood)
        {
            string name = img.sprite.name;
            if (!groups.ContainsKey(name))
                groups[name] = new List<Image>();
            groups[name].Add(img);
        }

        foreach (var kvp in groups)
        {
            if (kvp.Value.Count >= 3)
            {
                MagnetGroup(kvp.Value);
                break;
            }
        }

    }

    private void MagnetGroup(List<Image> items)
    {
        if (items == null || items.Count < 3)
        {
            m_isUsingPowerup = false;
            return;
        }

        List<Image> foods = items.Take(3).ToList();

        m_plateAnimation.PlayPlateAnimation(foods, () =>
        {

            foreach (var img in foods)
            {

                FoodSlot slot = img.GetComponentInParent<FoodSlot>();
                if (slot != null)
                {
                    slot.OnHideFood();
                    slot.ImgFood.sprite = null;
                    continue;
                }

                TrayItem tray = img.GetComponentInParent<TrayItem>();
                if (tray != null)
                {
                    img.gameObject.SetActive(false);
                    img.sprite = null;
                    tray.OnFoodRemoved();
                }
            }

            foreach (var img in foods)
            {

                FoodSlot slot = img.GetComponentInParent<FoodSlot>();
                slot?.OnCheckPrepareTray();
            }

            GameManager.Instance.OnMinusFood();
            m_isUsingPowerup = false;

        });
    }

    private List<(Image img, FoodSlot slot, TrayItem tray)> GetAllActiveFood()
    {
        var result = new List<(Image, FoodSlot, TrayItem)>();

        foreach (var grill in GameManager.Instance.ListGrill)
        {
            if (!grill.gameObject.activeInHierarchy) continue;

            foreach (var slot in grill.totalSlot)
            {
                if (slot.HasVisibleFood)
                    result.Add((slot.ImgFood, slot, null));
            }


            foreach (var tray in grill.totalTrays) 
            {
                if (!tray.gameObject.activeInHierarchy) continue;
                foreach (var img in tray.FoodList)
                {
                    if (img.gameObject.activeInHierarchy)
                        result.Add((img, null, tray));
                }
            }
        }
        return result;
    }

    private void OnShuffle()
    {
        AudioManager.Instance.PlaySFX(SFXType.Shuffle);
        StartCoroutine(IEShuffle());

    }

    private IEnumerator IEShuffle()
    {
        List<Image> result = new List<Image>();
        foreach (var grill in GameManager.Instance.ListGrill)
        {
            if (grill.gameObject.activeInHierarchy)
                result.AddRange(grill.ListFoodActive());
        }

        if (result.Count <= 1)
        {
            m_isUsingPowerup = false;
            yield break;
        }

        yield return new WaitForSeconds(0.1f);
        float jumpTime = 0.25f;
        float fallTime = 0.35f;

        foreach (var img in result)
            img.transform.DOKill();


        foreach (var img in result)
        {
            img.transform
                .DOScale(1.15f, jumpTime)
                .SetEase(Ease.OutQuad);
        }

        yield return new WaitForSeconds(jumpTime * 0.6f);


        Shuffle(result);

        foreach (var img in result)
            img.transform.DOKill();


        foreach (var img in result)
        {
            img.transform
                .DOScale(1f, fallTime) 
                .SetEase(Ease.OutBack);
        }

        yield return new WaitForSeconds(fallTime);

        m_isUsingPowerup = false;
    }


    private void Shuffle(List<Image> result)
    {

        List<Sprite> sprites = result
            .Select(img => img.sprite)
            .ToList();

        for (int i = 0; i < sprites.Count; i++)
        {
            int randomIndex = Random.Range(i, sprites.Count);

            (sprites[i], sprites[randomIndex]) =
                (sprites[randomIndex], sprites[i]);
        }

        for (int i = 0; i < result.Count; i++)
        {
            result[i].sprite = sprites[i];
        }
    }


    public void OnAddTime()
    {
        m_timer.AddTime(m_TimeBonus);
        ShowAddTimePopup(m_TimeBonus);
        AudioManager.Instance.PlaySFX(SFXType.TimeBonus);
       
    }

    private void ShowAddTimePopup(float seconds)
    {
        TextMeshProUGUI txt = Instantiate(m_txtAddTimePrefab, m_popupRoot);
        txt.text = $"+{seconds}s";

        RectTransform rt = txt.rectTransform;
        rt.localScale = Vector3.zero;

        rt.DOScale(1f, 0.2f).SetEase(Ease.OutBack)
          .OnComplete(() =>
          {
              rt.DOScale(0f, 0.15f).SetDelay(0.4f)
                .OnComplete(() => {
                    Destroy(txt.gameObject);
                    m_isUsingPowerup = false;
                });
          });
    }


}
