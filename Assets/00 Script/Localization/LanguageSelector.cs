using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public sealed class LanguageSelector : MonoBehaviour
{
    private const string RowName = "LanguageSelectorRow";
    private string[] codes;
    private int selectedIndex;
    private TMP_Text valueLabel;

    private void OnEnable()
    {
        bool hasRow = false;
        foreach (var child in GetComponentsInChildren<Transform>(true))
            if (child.name == RowName) { hasRow = true; break; }
        if (!hasRow) BuildRow();
        WireArrowButtons();
        LocalizationManager.LanguageChanged += Refresh;
        Refresh();
    }

    private void OnDisable() => LocalizationManager.LanguageChanged -= Refresh;

    private void BuildRow()
    {
        codes = new string[LocalizationManager.LanguageCodes.Count];
        for (int i = 0; i < codes.Length; i++) codes[i] = LocalizationManager.LanguageCodes[i];
        Transform container = null;
        foreach (var t in GetComponentsInChildren<Transform>(true))
            if (t.name == "Conainer") { container = t; break; }
        if (container == null) container = transform;

        var row = new GameObject(RowName, typeof(RectTransform));
        row.transform.SetParent(container, false);
        var rowRect = (RectTransform)row.transform;
        rowRect.anchorMin = rowRect.anchorMax = new Vector2(.5f, .5f);
        rowRect.sizeDelta = new Vector2(740, 120);
        rowRect.anchoredPosition = new Vector2(0, -130);
        // The settings content container is rotated in the scene. Keep this
        // runtime-created row facing the screen so TMP glyphs and UI raycasts work.
        rowRect.rotation = transform.rotation;

        var title = CreateText(row.transform, "LanguageLabel", LocalizationManager.Get("Language"), 42, TextAlignmentOptions.Center);
        title.color = new Color32(38, 48, 64, 255);
        var titleRect = (RectTransform)title.transform;
        titleRect.anchorMin = titleRect.anchorMax = new Vector2(.5f, .5f);
        titleRect.sizeDelta = new Vector2(360, 48);
        titleRect.anchoredPosition = new Vector2(0, 30);

        valueLabel = CreateText(row.transform, "LanguageValue", string.Empty, 38, TextAlignmentOptions.Center);
        valueLabel.color = new Color32(38, 48, 64, 255);
        var valueRect = (RectTransform)valueLabel.transform;
        valueRect.anchorMin = valueRect.anchorMax = new Vector2(.5f, .5f);
        valueRect.sizeDelta = new Vector2(290, 60);
        valueRect.anchoredPosition = new Vector2(0, -20);

        CreateArrow(row.transform, "PreviousLanguage", new Vector2(-205, -20), "<");
        CreateArrow(row.transform, "NextLanguage", new Vector2(205, -20), ">");
    }

    private TMP_Text CreateText(Transform parent, string objectName, string value, float size, TextAlignmentOptions alignment)
    {
        var go = new GameObject(objectName, typeof(RectTransform), typeof(TextMeshProUGUI));
        go.transform.SetParent(parent, false);
        var label = go.GetComponent<TextMeshProUGUI>();
        label.text = value;
        label.fontSize = size;
        label.alignment = alignment;
        label.color = Color.white;
        label.raycastTarget = false;
        var baseFont = Resources.Load<TMP_FontAsset>("Fonts & Materials/LiberationSans SDF");
        if (baseFont != null) label.font = baseFont;
        LocalizationManager.ApplyFontFallback(label);
        return label;
    }

    private void CreateArrow(Transform parent, string objectName, Vector2 position, string symbol)
    {
        var go = new GameObject(objectName, typeof(RectTransform), typeof(Image), typeof(Button));
        go.transform.SetParent(parent, false);
        var rect = (RectTransform)go.transform;
        rect.anchorMin = rect.anchorMax = new Vector2(.5f, .5f);
        rect.sizeDelta = new Vector2(80, 60);
        rect.anchoredPosition = position;
        var image = go.GetComponent<Image>();
        image.color = new Color(.2f, .25f, .3f, .95f);
        go.GetComponent<Button>().targetGraphic = image;

        var label = CreateText(go.transform, "Label", symbol, 40, TextAlignmentOptions.Center);
        var labelRect = (RectTransform)label.transform;
        labelRect.anchorMin = Vector2.zero;
        labelRect.anchorMax = Vector2.one;
        labelRect.offsetMin = labelRect.offsetMax = Vector2.zero;
    }

    private void WireArrowButtons()
    {
        foreach (var button in GetComponentsInChildren<Button>(true))
        {
            if (button.gameObject.name == "PreviousLanguage")
            {
                button.onClick.RemoveAllListeners();
                button.onClick.AddListener(() => Step(-1));
            }
            else if (button.gameObject.name == "NextLanguage")
            {
                button.onClick.RemoveAllListeners();
                button.onClick.AddListener(() => Step(1));
            }
        }
    }

    private void Step(int amount)
    {
        if (codes == null || codes.Length == 0) return;
        selectedIndex = (selectedIndex + amount + codes.Length) % codes.Length;
        LocalizationManager.SetLanguage(codes[selectedIndex]);
        Refresh();
    }

    private void Refresh()
    {
        if (codes == null)
        {
            codes = new string[LocalizationManager.LanguageCodes.Count];
            for (int i = 0; i < codes.Length; i++) codes[i] = LocalizationManager.LanguageCodes[i];
        }

        selectedIndex = Array.IndexOf(codes, LocalizationManager.CurrentLanguage);
        if (selectedIndex < 0) selectedIndex = 1;
        if (LocalizationManager.IsUsingSystemLanguage) selectedIndex = 0;

        Transform labelTransform = null;
        foreach (var item in GetComponentsInChildren<Transform>(true))
        {
            if (item.name == "LanguageValue") valueLabel = item.GetComponent<TMP_Text>();
            else if (item.name == "LanguageLabel") labelTransform = item;
        }
        if (valueLabel != null) valueLabel.text = LocalizationManager.GetLanguageDisplayName(codes[selectedIndex]);
        var title = labelTransform != null ? labelTransform.GetComponent<TMP_Text>() : null;
        if (title != null) title.text = LocalizationManager.Get("Language");
    }
}
