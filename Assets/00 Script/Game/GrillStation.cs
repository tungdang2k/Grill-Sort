using System.Collections;
using System.Collections.Generic;
using System.Linq;
using DG.Tweening;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class GrillStation : MonoBehaviour
{
    [SerializeField] private Transform m_trayContainer, m_slotContainer;
    [SerializeField] private GameObject m_normalVisual;  
    [SerializeField] private GameObject m_lockedVisual;
    [SerializeField] private GameObject m_TraysVisual;
    [SerializeField] private TextMeshProUGUI m_lockCountText;
    [SerializeField] private PlateAnimation m_plateAnimation;

    private List<TrayItem> m_totalTrays = new List<TrayItem>();
    private List<FoodSlot> m_totalSlot = new List<FoodSlot>();
    private Stack<TrayItem> m_stackTray = new Stack<TrayItem>();
    public bool IsLocked { get; private set; }
    public int RequiredMerge { get; private set; }
    public List<FoodSlot> totalSlot => m_totalSlot;
    public Stack<TrayItem> totalTrays => m_stackTray;

    private void Awake()
    {
        m_totalSlot = Utils.GetListInChild<FoodSlot>(m_slotContainer);
        m_totalTrays = Utils.GetListInChild<TrayItem>(m_trayContainer);
        if(m_plateAnimation == null)
        {
            m_plateAnimation = FindFirstObjectByType<PlateAnimation>();
        }


    }

    public void SetAsNormal()
    {
        m_lockedVisual.SetActive(false);  // ẩn LockGrill
        m_normalVisual.SetActive(true);   // hiện SlotContainer
    }

    public void SetNullGrill()
    {
        m_lockedVisual.SetActive(false);  
        m_normalVisual.SetActive(false);
        foreach (Transform child in m_TraysVisual.transform)
        {
            child.gameObject.SetActive(false);
        }
        //m_TraysVisual.SetActive(false);
    }

    public void SetBonusGrill()
    {
        

        m_normalVisual.SetActive(true);

        transform.localScale = Vector3.zero;
        transform.DOScale(Vector3.one, 0.4f)
            .SetEase(Ease.OutBack);
    }

    public List<string> GetTrayFoodNames()
    {
        List<string> names = new List<string>();
        foreach (var tray in totalTrays)
        {
            foreach (var img in tray.FoodList)
            {
                if (img.gameObject.activeInHierarchy && img.sprite != null)
                    names.Add(img.sprite.name);
            }
        }
        return names;
    }

   

    public void SetAsLocked()
    {
        IsLocked = true;

        RequiredMerge =  Random.Range(3,6);

        UpdateLockText(RequiredMerge);

        m_lockedVisual.SetActive(true);
        m_normalVisual.SetActive(false);
        
    }

    public void OnMergeHappened(int currentMergeCount)
    {
        if (!IsLocked) return;

        int remaining = RequiredMerge - currentMergeCount;
        remaining = Mathf.Max(0, remaining); // không âm
        UpdateLockText(remaining);
    }
    private void UpdateLockText(int value)
    {
        if (m_lockCountText != null)
            m_lockCountText.text = value.ToString();
    }

    public void Unlock()
    {
        IsLocked = false;

        m_lockedVisual.SetActive(false); 
        m_normalVisual.SetActive(true);

        StartCoroutine(UnlockNextFrame());

    }

    private IEnumerator UnlockNextFrame()
    {

        yield return null;
        OnPrepareTray();
    }


    public void OnInitGrill(
    List<Sprite> slotItems,
    List<List<Sprite>> trayData)
    {
        m_stackTray.Clear();

        int index = 0;

        for (int i = 0; i < m_totalSlot.Count; i++)
        {
            if (index < slotItems.Count)
            {
                m_totalSlot[i].OnSetSlot(slotItems[index]);
                index++;
            }
            else
            {
                m_totalSlot[i].OnHideFood();
            }
        }
         
        // TRAY
        for (int i = 0; i < m_totalTrays.Count; i++)
        {
            bool active = i < trayData.Count;

            m_totalTrays[i].gameObject.SetActive(active);

            if (active)
            {
                m_totalTrays[i].OnSetFood(trayData[i]);
                m_stackTray.Push(m_totalTrays[i]);
            }
        }
    }


    public FoodSlot GetSlotNull(FoodSlot excludedSlot = null)
    {
        for (int i = 0; i < m_totalSlot.Count; i++)
        {
            if (m_totalSlot[i] != excludedSlot && !m_totalSlot[i].HasFood())
            {
                return m_totalSlot[i];
            }
        }
        return null;
    }

    public void OnCheckMerge()
    {

        if (m_totalSlot.Count == 0 || m_totalSlot.Any(slot => slot.IsLocked)) return;
        if (GetSlotNull() != null) return;
        if (!CanMerge()) return;

        AudioManager.Instance?.PlaySFX(SFXType.Merge);

        List<Image> mergeItems = m_totalSlot
            .Where(s => s.HasFood())
            .Select(s => s.ImgFood)
            .ToList();

        m_plateAnimation.PlayPlateAnimation(mergeItems, () =>
        {
            for (int i = 0; i < m_totalSlot.Count; i++)
                m_totalSlot[i].OnActiveFood(false);

            OnPrepareTray();
            GameManager.Instance?.OnMinusFood();

        });

    }


    private void OnPrepareTray()
    {

        if (m_stackTray.Count > 0)
        {
            TrayItem tray = m_stackTray.Pop();

            foreach (var img in tray.FoodList)
            {
                if (!img.gameObject.activeSelf) continue;

                FoodSlot slot = GetSlotNull(); 

                if (slot == null)
                {

                    continue;
                }

                slot.OnPrepareItem(img);
                img.gameObject.SetActive(false);
                
            }

            tray.gameObject.SetActive(false);
            CleanEmptyTraysInStack();
            OnCheckMerge();
           
        }

    }

    private void CleanEmptyTraysInStack()
    {
        while (m_stackTray.Count > 0)
        {
            TrayItem top = m_stackTray.Peek();

            bool isEmpty = true;
            foreach (var img in top.FoodList)
            {
                if (img.gameObject.activeInHierarchy)
                {
                    isEmpty = false;
                    break;
                }
            }

            if (isEmpty)
            {
                m_stackTray.Pop();
                top.gameObject.SetActive(false);
            }
            else
            {
                break; 
            }
        }
    }
    public void OnTrayEmpty(TrayItem tray)
    {
        if (m_stackTray.Count > 0 && m_stackTray.Peek() == tray)
        {
            m_stackTray.Pop();
        }

        if (m_stackTray.Count > 0)
        {
            m_stackTray.Peek().gameObject.SetActive(true);
        }

        CleanEmptyTraysInStack();
    }


    private bool HasGrillEmpty()
    {
       for (int i = 0; i < m_totalSlot.Count; i++)
        {
            if (m_totalSlot[i].HasFood())
            {
                return false;
            }
        }

       return true;
    }
    public void OnCheckPrepareTray()
    {
       if(this.HasGrillEmpty())
        {
            this.OnPrepareTray();
        }
    }

    private bool CanMerge()
    {
        if (m_totalSlot.Count == 0 || m_totalSlot.Any(slot => !slot.HasVisibleFood))
            return false;

        for (int i = 1; i < m_totalSlot.Count; i++)
        {
            string name = m_totalSlot[0].GetSpriteFood().name;
            if (m_totalSlot[i].GetSpriteFood().name != name)
            {
                return false;
            }
        }
        return true;

    }

    public TrayItem GetFistTray()
    {
        if (m_stackTray.Count > 0)
        {
            return m_stackTray.Peek();
        }
        return null;
    }

    public List<Image> ListFoodActive()
    {
        List<Image> list = new List<Image>();

        for(int i = 0; i < m_totalSlot.Count; i++)
        {
            if (m_totalSlot[i].HasVisibleFood)
            {
                list.Add(m_totalSlot[i].ImgFood);
            }
        }


        foreach (var tray in m_stackTray)
        {
            if (!tray.gameObject.activeInHierarchy) continue;
            foreach (var img in tray.FoodList)
            {
                if (img.gameObject.activeInHierarchy)
                    list.Add(img);
            }
        }

        return list;
    }



}
