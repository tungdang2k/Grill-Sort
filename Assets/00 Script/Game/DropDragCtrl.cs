using DG.Tweening;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class DropDragCtrl : MonoBehaviour
{

    [SerializeField] private Image m_imgFoodDrag;
    [SerializeField] private float m_dragSize = 160f;

    private enum DragState { Idle, Dragging, Animating }
    private DragState m_state = DragState.Idle;

    private FoodSlot m_sourceSlot;   
    private FoodSlot m_previewSlot; 
    private int m_pointerID = -1;

    private void Awake()
    {
        if (m_imgFoodDrag == null)
        {
            Debug.LogError("DropDragCtrl requires a drag Image reference.", this);
            enabled = false;
            return;
        }

        m_imgFoodDrag.gameObject.SetActive(false);
        m_imgFoodDrag.raycastTarget = false;
    }

    private void OnDisable()
    {
        if (m_state == DragState.Idle) return;

        m_imgFoodDrag?.transform.DOKill();
        ClearPreview();
        if (m_sourceSlot != null)
            m_sourceSlot.OnActiveFood(true);
        if (m_imgFoodDrag != null)
            m_imgFoodDrag.gameObject.SetActive(false);
        ResetState();
    }

    public void OnBtnDown(PointerEventData evt, FoodSlot sourceSlot)
    {
        if (m_state != DragState.Idle || sourceSlot == null || !sourceSlot.HasVisibleFood) return;

        // ── Lock drag ──
        m_state = DragState.Dragging;
        m_pointerID = evt.pointerId;

        m_sourceSlot = sourceSlot;

        AudioManager.Instance?.PlaySFX(SFXType.Click);

        // show dummy
        m_sourceSlot.OnActiveFood(false);
        DummyShow(m_sourceSlot.GetSpriteFood(), evt.position, evt.pressEventCamera);
        m_imgFoodDrag.transform.DOScale(Vector3.one * 1.1f, 0.15f);
    }

    public void OnHasDrag(PointerEventData evt)
    {
        if (m_state != DragState.Dragging) return;
        if (evt.pointerId != m_pointerID) return;


        DummyMoveTo(evt.position, evt.pressEventCamera);
        UpdatePreview(ResolveTarget(evt));
    }

    public void OnBtnUp(PointerEventData evt)
    {
        if (m_state != DragState.Dragging) return;
        if (evt.pointerId != m_pointerID) return;

        // Re-evaluate the release position in case no final OnDrag event was sent.
        UpdatePreview(ResolveTarget(evt));
        m_state = DragState.Animating;
        AudioManager.Instance?.PlaySFX(SFXType.Smoke);

      

        if (m_previewSlot != null)
            DropToSlot(m_previewSlot);
        else
            ReturnToSource();
    }


    private void DropToSlot(FoodSlot dest)
    {
        FoodSlot src = m_sourceSlot;  

        if (dest == null || m_sourceSlot == null || dest == m_sourceSlot)
        {
            ReturnToSource();
            return;
        }

        dest.OnHideFood();             // hide preview 

        m_imgFoodDrag.transform.DOKill();
        m_imgFoodDrag.transform.DOScale(Vector3.one, 0.2f);
        m_imgFoodDrag.transform.DOMove(dest.transform.position, 0.2f)
            .OnComplete(() =>
            {
                DummyHide();
                if (src != null && dest != null && dest.isActiveAndEnabled)
                {
                    dest.OnSetSlot(src.GetSpriteFood());
                    dest.OnActiveFood(true);
                    dest.OnCheckMerge();
                    src.OnCheckPrepareTray();
                }
                else if (src != null)
                {
                    src.OnActiveFood(true);
                }
                ResetState();
            });
    }

    private void ReturnToSource()
    {
        FoodSlot src = m_sourceSlot; 
        ClearPreview();

        m_imgFoodDrag.transform.DOKill();
        m_imgFoodDrag.transform.DOScale(Vector3.one, 0.3f);
        m_imgFoodDrag.transform.DOMove(src.transform.position, 0.3f)
            .OnComplete(() =>
            {
                DummyHide();
                if (src != null)
                    src.OnActiveFood(true);
                ResetState();
            });
    }


    private void UpdatePreview(FoodSlot target)
    {
        if (target == m_previewSlot) return;  

        ClearPreview();

        if (target != null)
        {
            m_previewSlot = target;
            m_previewSlot.ShowPreview(m_sourceSlot.GetSpriteFood());
        }
    }

    private void ClearPreview()
    {
        if (m_previewSlot == null) return;
        m_previewSlot.OnHideFood();
        m_previewSlot = null;
    }

    private FoodSlot ResolveTarget(PointerEventData evt)
    {
        FoodSlot hover = GetSlot(evt);
        if (hover == null || hover == m_sourceSlot) return null;

        if (!hover.HasFood()) return hover;

        return hover.GetSlotNull(m_sourceSlot);
    }

    private void DummyShow(Sprite sprite, Vector2 screenPos, Camera eventCamera)
    {
        if (sprite == null) return;
        m_imgFoodDrag.sprite = sprite;
        m_imgFoodDrag.preserveAspect = true;

        var rt = m_imgFoodDrag.rectTransform;
        rt.sizeDelta = new Vector2(m_dragSize, m_dragSize);
        rt.localScale = Vector3.one;
        rt.localRotation = Quaternion.identity;

        m_imgFoodDrag.gameObject.SetActive(true);
        DummyMoveTo(screenPos, eventCamera);
    }

    private void DummyMoveTo(Vector2 screenPos, Camera eventCamera)
    {
        Canvas canvas = m_imgFoodDrag.canvas;
        RectTransform parentRect = m_imgFoodDrag.rectTransform.parent as RectTransform;
        if (canvas == null || parentRect == null) return;

        Camera camera = canvas.renderMode == RenderMode.ScreenSpaceOverlay
            ? null
            : (eventCamera != null ? eventCamera : canvas.worldCamera);

        RectTransformUtility.ScreenPointToLocalPointInRectangle(
            parentRect, screenPos, camera, out Vector2 local);

        m_imgFoodDrag.rectTransform.anchoredPosition = local;
    }

    private void DummyHide()
    {
        m_imgFoodDrag.transform.DOKill();
        m_imgFoodDrag.gameObject.SetActive(false);
    }

 
    private void ResetState()
    {
        m_sourceSlot = null;
        m_previewSlot = null;
        m_pointerID = -1;
        m_state = DragState.Idle;
    }

    private static FoodSlot GetSlot(PointerEventData evt)
    {
        var obj = evt.pointerCurrentRaycast.gameObject;
        return obj != null ? obj.GetComponentInParent<FoodSlot>() : null;
    }
}
