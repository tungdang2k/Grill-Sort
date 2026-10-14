using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

public class GrillShakeChecker : MonoBehaviour
{
    [SerializeField] private float m_idleTime = 2f;

    private float m_timer;

    private void Update()
    {   
        // player có đang thao tác không?
        bool isHolding =
              (Mouse.current != null && Mouse.current.leftButton.isPressed) ||
              (Touchscreen.current != null && Touchscreen.current.primaryTouch.press.isPressed);

        if (isHolding)
        {
            m_timer = 0f;
            return;
        }

        m_timer += Time.deltaTime;

        if (m_timer >= m_idleTime)
        {
            m_timer = 0f;
            CheckAndShake();
        }
    }

    private void CheckAndShake()
    {
        var grills = GameManager.Instance.ListGrill;

        if (grills == null || grills.Count == 0)
            return;

        Dictionary<string, List<FoodSlot>> group = new();

        foreach (var grill in grills)
        {
            if (!grill || !grill.gameObject.activeInHierarchy)
                continue;

            foreach (var slot in grill.totalSlot)
            {
                if (!slot.HasVisibleFood)
                    continue;

                string name = slot.GetSpriteFood().name;

                if (!group.ContainsKey(name))
                    group[name] = new List<FoodSlot>();

                group[name].Add(slot);
            }
        }

        foreach (var kvp in group)
        {
            if (kvp.Value.Count >= 3)
            {
                for (int i = 0; i < 3; i++)
                {
                    kvp.Value[i].DoShake();
                }

                return;
            }
        }
    }
}
