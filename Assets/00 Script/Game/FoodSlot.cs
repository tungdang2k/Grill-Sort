using DG.Tweening;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class FoodSlot : MonoBehaviour, IBeginDragHandler,
    IDragHandler,
    IEndDragHandler
{
    private float m_padding = 16f;
    //[SerializeField] private float m_slotScale = 0.65f;
    private DropDragCtrl m_dropDragCtrl;

    private Color m_nomalColor = new Color(1f, 1f, 1f, 1f);
    private Color m_fadeColor = new Color(1f,1f, 1f, 0.5f);
    private Image m_imgFood;
    

    private GrillStation m_grillCtrl;
    public Image ImgFood => m_imgFood;

    public bool IsLocked { get; private set; }
    public bool HasVisibleFood => !IsLocked && m_imgFood != null &&
                                  m_imgFood.gameObject.activeInHierarchy &&
                                  m_imgFood.sprite != null && m_imgFood.color == m_nomalColor;

    private void Awake()
    {
       
        m_dropDragCtrl = FindAnyObjectByType<DropDragCtrl>();
        m_imgFood = this.transform.GetChild(0).GetComponent<Image>();
        m_imgFood.gameObject.SetActive(false);
        m_grillCtrl = GetComponentInParent<GrillStation>();


    }

  

    public void Lock() => IsLocked = true;
    public void Unlock() => IsLocked = false;
    public void OnBeginDrag(PointerEventData eventData)
    {
        if (!HasVisibleFood) return;

        DropDragCtrl dragCtrl = GetDropDragCtrl();
        if (dragCtrl != null)
            dragCtrl.OnBtnDown(eventData, this);
    }

    public void OnDrag(PointerEventData eventData)
    {
        DropDragCtrl dragCtrl = GetDropDragCtrl();
        if (dragCtrl != null)
            dragCtrl.OnHasDrag(eventData);
    }

    public void OnEndDrag(PointerEventData eventData)
    {
        DropDragCtrl dragCtrl = GetDropDragCtrl();
        if (dragCtrl != null)
            dragCtrl.OnBtnUp(eventData);
    }

    private DropDragCtrl GetDropDragCtrl()
    {
        if (m_dropDragCtrl == null)
            m_dropDragCtrl = FindAnyObjectByType<DropDragCtrl>();

        return m_dropDragCtrl;
    }
    private void ApplyLayout()
    {
        RectTransform rt = m_imgFood.rectTransform;

        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = new Vector2(m_padding, m_padding);
        rt.offsetMax = new Vector2(-m_padding, -m_padding);

        rt.localScale = Vector3.one;
        rt.localRotation = Quaternion.identity;

        m_imgFood.preserveAspect = true;
    }

    public void ShowPreview(Sprite sprite)
    {
        if (sprite == null) return;

        m_imgFood.sprite = sprite;
        m_imgFood.color = m_fadeColor;
        m_imgFood.gameObject.SetActive(true);
        ApplyLayout();
    }
    public void OnSetSlot(Sprite foodSprite)
    {
        if (foodSprite == null) return;

        m_imgFood.sprite = foodSprite;
        m_imgFood.color = m_nomalColor;
        m_imgFood.gameObject.SetActive(true);

        RectTransform rt = m_imgFood.rectTransform;
        rt.pivot = new Vector2(0.5f, 0.5f);

        //  Reset transform 
        rt.localScale = Vector3.one;
        rt.localRotation = Quaternion.identity;
        rt.anchoredPosition = Vector2.zero;
        ApplyLayout();
    }

    

    public void OnFadeFood()
    {
        OnActiveFood(true);
        m_imgFood.color = m_fadeColor;
    }
     
    public void OnHideFood()
    {
        OnActiveFood(false);
        m_imgFood.color = m_nomalColor;
    }

    public void OnActiveFood(bool active)
    {
        m_imgFood.gameObject.SetActive(active);
        m_imgFood.color = m_nomalColor;
    }

    public bool HasFood()
    {
        if (IsLocked) return true;
        return m_imgFood.gameObject.activeSelf && m_imgFood.color == m_nomalColor;
    }
    public Sprite GetSpriteFood()
    {
        return m_imgFood.sprite;
    } 

    public FoodSlot GetSlotNull(FoodSlot excludedSlot = null)
    {
        return m_grillCtrl != null ? m_grillCtrl.GetSlotNull(excludedSlot) : null;
    }

    public void OnCheckMerge()
    {
        m_grillCtrl?.OnCheckMerge();
    }

    public void OnPrepareItem(Image img)
    {
        OnSetSlot(img.sprite);
        m_imgFood.color = m_nomalColor;
        m_imgFood.transform.position = img.transform.position;

        var rt = m_imgFood.rectTransform;

        m_imgFood.sprite = img.sprite;
        m_imgFood.color = m_nomalColor;
        m_imgFood.gameObject.SetActive(true);


        rt.localScale = Vector3.one;
        rt.localRotation = Quaternion.identity;
        rt.anchoredPosition = Vector2.zero;


        m_imgFood.preserveAspect = true;


        rt.position = img.transform.position;


        rt.DOKill();
        rt.DOMove(transform.position, 0.3f).SetEase(Ease.OutBack);
        m_imgFood.transform.DOScale(1, 0.3f);
        

    }
    
    public void OnCheckPrepareTray()
    {
        m_grillCtrl?.OnCheckPrepareTray();
    }

    public void DoShake()
    {
        m_imgFood.transform.DOShakePosition(0.5f, 10f, 20, 180f).SetLink(gameObject); 
    }
    public void ClearByMagnet()
    {
        OnHideFood();            
        m_imgFood.sprite = null;
        OnCheckPrepareTray();
    }



}
