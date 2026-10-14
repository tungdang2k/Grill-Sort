using TMPro;
using UnityEngine;

[DisallowMultipleComponent]
public sealed class LocalizedTMPText : MonoBehaviour
{
    [SerializeField, HideInInspector] private string sourceKey;
    private TMP_Text target;
    private string lastRendered;

    private void Awake() { EnsureTarget(); }
    private void OnEnable() { EnsureTarget(); RefreshNow(); }

    private void LateUpdate()
    {
        EnsureTarget();
        if (target == null) return;
        if (target.text != lastRendered)
        {
            sourceKey = target.text;
            RefreshNow();
        }
    }

    private void EnsureTarget()
    {
        if (target == null) target = GetComponent<TMP_Text>();
        if (target != null && string.IsNullOrEmpty(sourceKey)) sourceKey = target.text;
    }

    public void RefreshNow()
    {
        EnsureTarget();
        if (target == null || string.IsNullOrEmpty(sourceKey)) return;
        LocalizationManager.ApplyFontFallback(target);
        target.text = LocalizationManager.Get(sourceKey);
        lastRendered = target.text;
    }
}
