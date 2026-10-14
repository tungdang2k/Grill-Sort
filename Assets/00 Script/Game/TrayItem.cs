using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class TrayItem : MonoBehaviour
{
    private List<Image> m_FoodList = new List<Image>();
    public List<Image> FoodList => m_FoodList;

    private void Awake()
    {
        m_FoodList = Utils.GetListInChild<Image>(transform);
        for (int i = 0; i < m_FoodList.Count; i++)
        {
            m_FoodList[i].gameObject.SetActive(false);
        }


        HorizontalLayoutGroup hlg = GetComponent<HorizontalLayoutGroup>();
        if (hlg != null)
        {
            hlg.childControlWidth = true;
            hlg.childControlHeight = true;
            hlg.childForceExpandWidth = true;
            hlg.childForceExpandHeight = true;
        }

        for (int i = 0; i < m_FoodList.Count; i++)
            m_FoodList[i].gameObject.SetActive(false);
    }


    private void Start()
    {
        if (IsEmpty())
        {
            gameObject.SetActive(false);
        }
    }

    public  void OnSetFood(List<Sprite> items)
    {
        if (items == null) return;

        foreach (var img in m_FoodList)
        {
            img.gameObject.SetActive(false);
            img.sprite = null;
        }

        if (items.Count <= m_FoodList.Count)
        {
            for (int i = 0; i < items.Count; i++)
            {
                Image slot = RandomSlot();
                if (slot != null)
                {
                    slot.sprite = items[i];
                    slot.gameObject.SetActive(true);

                    RectTransform rt = slot.rectTransform;

                    rt.anchorMin = Vector2.zero;
                    rt.anchorMax = Vector2.one;
                    rt.offsetMin = Vector2.zero;
                    rt.offsetMax = Vector2.zero;

                    slot.preserveAspect = true;
                }
            }
        }

    }



    private Image RandomSlot()
    {
        List<Image> inactiveSlots = new List<Image>();

        foreach (var img in m_FoodList)
        {
            if (!img.gameObject.activeSelf)
                inactiveSlots.Add(img);
        }

        if (inactiveSlots.Count == 0)
            return null;

        int n = Random.Range(0, inactiveSlots.Count);
        return inactiveSlots[n];
    }


    private bool IsEmpty()
    {
        foreach (var img in FoodList)
        {
            if (img.gameObject.activeSelf)
                return false;
        }
        return true;
    }

    public void OnFoodRemoved()
    {
        if (IsEmpty())
        {
            gameObject.SetActive(false);

            GrillStation grill = GetComponentInParent<GrillStation>();
            grill?.OnTrayEmpty(this);
        }
    }

}
