using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text.RegularExpressions;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>Small offline localization system for the game's UI.</summary>
public sealed class LocalizationManager : MonoBehaviour
{
    public const string SystemLanguageCode = "system";
    private const string PreferenceKey = "game.language";
    private static LocalizationManager instance;
    private static string activeLanguage;
    private static bool useSystemLanguage = true;
    private static readonly string[] languageCodes = { SystemLanguageCode, "en", "vi", "zh-Hans", "th", "hi" };
    private static readonly Dictionary<string, string[]> translations = BuildTranslations();
    private static readonly Dictionary<string, TMP_FontAsset> fallbackByLanguage = new Dictionary<string, TMP_FontAsset>();
    private static readonly Dictionary<TMP_Text, TMP_FontAsset> originalFontByText = new Dictionary<TMP_Text, TMP_FontAsset>();

    public static event Action LanguageChanged;
    public static string CurrentLanguage => activeLanguage ?? "en";
    public static bool IsUsingSystemLanguage => useSystemLanguage;
    public static IReadOnlyList<string> LanguageCodes => languageCodes;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void Bootstrap()
    {
        if (instance != null) return;
        var host = new GameObject("LocalizationManager");
        DontDestroyOnLoad(host);
        instance = host.AddComponent<LocalizationManager>();
        string savedLanguage = PlayerPrefs.GetString(PreferenceKey, SystemLanguageCode);
        useSystemLanguage = savedLanguage == SystemLanguageCode;
        activeLanguage = useSystemLanguage ? DetectSystemLanguage() : savedLanguage;
        if (!IsSupported(activeLanguage)) activeLanguage = "en";
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    private void Awake()
    {
        if (instance != null && instance != this) { Destroy(gameObject); return; }
        instance = this;
        DontDestroyOnLoad(gameObject);
    }

    private static void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        EnsureFontFallbacks();
        LocalizeLoadedScene(scene);
        AttachSelectorToSettings(scene);
    }

    private static void LocalizeLoadedScene(Scene scene)
    {
        foreach (var root in scene.GetRootGameObjects())
        {
            foreach (var text in root.GetComponentsInChildren<TMP_Text>(true))
            {
                var localizer = text.GetComponent<LocalizedTMPText>();
                if (localizer == null && IsKnownKey(text.text)) localizer = text.gameObject.AddComponent<LocalizedTMPText>();
                if (localizer != null) localizer.RefreshNow();
            }
        }
    }

    public static void SetLanguage(string code)
    {
        bool selectSystemLanguage = code == SystemLanguageCode;
        useSystemLanguage = selectSystemLanguage;
        if (selectSystemLanguage) code = DetectSystemLanguage();
        if (!IsSupported(code)) code = "en";
        activeLanguage = code;
        PlayerPrefs.SetString(PreferenceKey, selectSystemLanguage ? SystemLanguageCode : code);
        PlayerPrefs.Save();
        EnsureFontFallbacks();
        foreach (var text in Resources.FindObjectsOfTypeAll<LocalizedTMPText>()) text.RefreshNow();
        LanguageChanged?.Invoke();
    }

    public static string Get(string key)
    {
        if (string.IsNullOrEmpty(key)) return key;
        string trimmedKey = key.Trim();
        int leadingWhitespace = key.Length - key.TrimStart().Length;
        int trailingWhitespace = key.Length - key.TrimEnd().Length;
        string leading = key.Substring(0, leadingWhitespace);
        string trailing = trailingWhitespace > 0 ? key.Substring(key.Length - trailingWhitespace) : string.Empty;
        if (translations.TryGetValue(trimmedKey, out var values))
        {
            int index = LanguageIndex(CurrentLanguage);
            string translated = string.IsNullOrEmpty(values[index]) ? values[1] : values[index];
            return leading + translated + trailing;
        }

        var level = Regex.Match(trimmedKey, "^Level\\s+(\\d+)$", RegexOptions.IgnoreCase);
        if (level.Success)
        {
            string prefix = CurrentLanguage == "vi" ? "Màn " : CurrentLanguage == "zh-Hans" ? "第" : CurrentLanguage == "th" ? "ด่าน " : CurrentLanguage == "hi" ? "स्तर " : "Level ";
            string suffix = CurrentLanguage == "zh-Hans" ? "关" : string.Empty;
            return leading + prefix + level.Groups[1].Value + suffix + trailing;
        }
        return key;
    }

    public static string GetLanguageDisplayName(string code)
    {
        switch (code)
        {
            case SystemLanguageCode: return Get("System Default");
            case "en": return "English";
            case "vi": return "Tiếng Việt";
            case "zh-Hans": return "简体中文";
            case "th": return "ไทย";
            case "hi": return "हिन्दी";
            default: return "English";
        }
    }

    public static string DetectSystemLanguage()
    {
        string language = string.Empty;
#if UNITY_ANDROID && !UNITY_EDITOR
        try
        {
            using (var locale = new AndroidJavaClass("java.util.Locale"))
            using (var current = locale.CallStatic<AndroidJavaObject>("getDefault"))
                language = current.Call<string>("toLanguageTag");
        }
        catch (Exception e) { Debug.LogWarning("Could not read Android system locale; using Unity's system language. " + e.Message); }
#endif
        if (string.IsNullOrEmpty(language)) language = CultureInfo.CurrentUICulture.Name;
        if (string.IsNullOrEmpty(language) || language == "iv") language = Application.systemLanguage.ToString();
        return MapLocaleToLanguage(language);
    }

    public static string MapLocaleToLanguage(string locale)
    {
        if (string.IsNullOrWhiteSpace(locale)) return "en";
        locale = locale.Trim().Replace('_', '-').ToLowerInvariant();
        if (locale.StartsWith("vi") || locale == "vietnamese") return "vi";
        if (locale.StartsWith("zh") || locale.StartsWith("chinese")) return "zh-Hans";
        if (locale.StartsWith("th") || locale == "thai") return "th";
        if (locale.StartsWith("hi") || locale == "hindi") return "hi";
        return "en";
    }

    private static bool IsSupported(string code) => Array.IndexOf(languageCodes, code) >= 0;
    private static int LanguageIndex(string code) => Array.IndexOf(languageCodes, code) is int index && index >= 0 ? index : 1;
    private static bool IsKnownKey(string key) => !string.IsNullOrWhiteSpace(key) && translations.ContainsKey(key.Trim());
    public static bool IsLocalizableKey(string key) => IsKnownKey(key) || Regex.IsMatch(key ?? string.Empty, "^Level\\s+\\d+$", RegexOptions.IgnoreCase);

    private static void AttachSelectorToSettings(Scene scene)
    {
        foreach (var root in scene.GetRootGameObjects())
        {
            var settings = root.GetComponentsInChildren<Transform>(true);
            foreach (var item in settings)
            {
                if (item.name != "SettingPanel") continue;
                if (item.GetComponent<LanguageSelector>() == null) item.gameObject.AddComponent<LanguageSelector>();
            }
        }
    }

    private static void EnsureFontFallbacks()
    {
        if (fallbackByLanguage.TryGetValue(CurrentLanguage, out var cachedFallback))
        {
            AddFallbackToLoadedTexts(cachedFallback);
            return;
        }
        string resourcePath = CurrentLanguage == "vi" ? "Localization/Fonts/NotoSansVietnamese Compact2 SDF" :
            CurrentLanguage == "zh-Hans" ? "Localization/Fonts/NotoSansChinese Compact2 SDF" :
            CurrentLanguage == "th" ? "Localization/Fonts/NotoSansThai Compact2 SDF" :
            CurrentLanguage == "hi" ? "Localization/Fonts/NotoSansDevanagari Compact2 SDF" : null;
        if (resourcePath == null) return;
        TMP_FontAsset fallback = Resources.Load<TMP_FontAsset>(resourcePath);
        if (fallback == null)
        {
            Debug.LogError("Localization font asset is missing: " + resourcePath);
            return;
        }
        fallbackByLanguage[CurrentLanguage] = fallback;
        AddFallbackToLoadedTexts(fallback);
    }

    private static void AddFallbackToLoadedTexts(TMP_FontAsset fallback)
    {
        var texts = Resources.FindObjectsOfTypeAll<TMP_Text>();
        foreach (var text in texts)
        {
            if (text == null || text.gameObject.scene.IsValid() == false || text.font == null) continue;
            ApplyFontFallback(text);
        }
    }

    public static void ApplyFontFallback(TMP_Text text)
    {
        if (text == null || text.font == null) return;
        if (!originalFontByText.TryGetValue(text, out var originalFont))
        {
            originalFont = text.font;
            originalFontByText[text] = originalFont;
        }
        else if (text.font != originalFont)
        {
            // Preserve fonts assigned by other UI code after this text was localized.
            originalFont = text.font;
            originalFontByText[text] = originalFont;
        }

        if (!fallbackByLanguage.TryGetValue(CurrentLanguage, out var fallback))
        {
            text.font = originalFont;
            return;
        }

        // TMP_FontAsset owns its atlas texture and destroys it in OnDestroy.
        // Cloning a font asset at runtime keeps references to the original atlas,
        // so Unity later reports attempts to destroy an imported asset. Add the
        // language font to the shared font's fallback table instead.
        if (originalFont.fallbackFontAssetTable == null)
            originalFont.fallbackFontAssetTable = new List<TMP_FontAsset>();
        if (!originalFont.fallbackFontAssetTable.Contains(fallback))
            originalFont.fallbackFontAssetTable.Add(fallback);
        text.font = originalFont;
    }

    private static Dictionary<string, string[]> BuildTranslations()
    {
        // Language order: system, English, Vietnamese, Simplified Chinese, Thai, Hindi.
        var d = new Dictionary<string, string[]>(StringComparer.OrdinalIgnoreCase);
        void Add(string key, string vi, string zh, string th, string hi) => d[key] = new[] { "", key, vi, zh, th, hi };
        Add("System Default", "Theo thiết bị", "跟随系统", "ตามระบบ", "सिस्टम डिफ़ॉल्ट");
        Add("Language", "Ngôn ngữ", "语言", "ภาษา", "भाषा");
        Add("Music", "Nhạc", "音乐", "เพลง", "संगीत");
        Add("Sound", "Âm thanh", "音效", "เสียง", "ध्वनि");
        Add("Continue", "Tiếp tục", "继续", "เล่นต่อ", "जारी रखें");
        Add("Exit Game", "Thoát game", "退出游戏", "ออกจากเกม", "गेम से बाहर निकलें");
        Add("Guide", "Hướng dẫn", "指南", "วิธีเล่น", "मार्गदर्शिका");
        Add("Home", "Trang chủ", "主页", "หน้าหลัก", "होम");
        Add("Loading....", "Đang tải....", "正在加载……", "กำลังโหลด....", "लोड हो रहा है....");
        Add("Big Bundle", "Gói lớn", "大型礼包", "ชุดใหญ่", "बड़ा बंडल");
        Add("Bonus", "Thưởng", "奖励", "โบนัส", "बोनस");
        Add("Bundles", "Gói vật phẩm", "礼包", "ชุดไอเทม", "बंडल");
        Add("Coin", "Xu", "金币", "เหรียญ", "सिक्का");
        Add("Get", "Nhận", "领取", "รับ", "पाएँ");
        Add("Legendary Bundle", "Gói huyền thoại", "传奇礼包", "ชุดระดับตำนาน", "लेजेंडरी बंडल");
        Add("No Ads", "Không quảng cáo", "去除广告", "ไม่มีโฆษณา", "विज्ञापन नहीं");
        Add("Normal", "Thường", "普通", "ปกติ", "सामान्य");
        Add("Nomal", "Thường", "普通", "ปกติ", "सामान्य");
        Add("Out Of Time", "Hết thời gian", "时间到", "หมดเวลา", "समय समाप्त");
        Add("Pack 3 Skewers Together", "Gộp 3 xiên cùng loại", "将3串相同食物合并", "รวมไม้เสียบ 3 อัน", "3 सींक एक साथ जोड़ें");
        Add("Pause", "Tạm dừng", "暂停", "หยุดชั่วคราว", "रोकें");
        Add("Play With", "Chơi với", "使用道具", "เล่นด้วย", "इसके साथ खेलें");
        Add("Play", "Chơi", "玩", "เล่น", "खेलें");
        Add("Remove Ads", "Bỏ quảng cáo", "移除广告", "ลบโฆษณา", "विज्ञापन हटाएँ");
        Add("Restart", "Chơi lại", "重新开始", "เริ่มใหม่", "फिर से शुरू करें");
        Add("Restast", "Chơi lại", "重新开始", "เริ่มใหม่", "फिर से शुरू करें");
        Add("Select Booster", "Chọn trợ lực", "选择助推道具", "เลือกตัวช่วย", "बूस्टर चुनें");
        Add("Select Booter", "Chọn trợ lực", "选择助推道具", "เลือกตัวช่วย", "बूस्टर चुनें");
        Add("Settings", "Cài đặt", "设置", "การตั้งค่า", "सेटिंग्स");
        Add("Setiing", "Cài đặt", "设置", "การตั้งค่า", "सेटिंग्स");
        Add("Shop", "Cửa hàng", "商店", "ร้านค้า", "दुकान");
        Add("Small Bundle", "Gói nhỏ", "小型礼包", "ชุดเล็ก", "छोटा बंडल");
        Add("Starter Bundle", "Gói khởi đầu", "新手礼包", "ชุดเริ่มต้น", "स्टार्टर बंडल");
        Add("Storage", "Kho", "仓库", "คลังเก็บของ", "भंडार");
        Add("Watch Ad", "Xem quảng cáo", "观看广告", "ดูโฆษณา", "विज्ञापन देखें");
        Add("Watch Ads", "Xem quảng cáo", "观看广告", "ดูโฆษณา", "विज्ञापन देखें");
        Add("Well Done", "Làm tốt lắm", "做得好", "เยี่ยมมาก", "बहुत बढ़िया");
        Add("You're all most there", "Bạn sắp hoàn thành rồi", "马上就完成了", "ใกล้เสร็จแล้ว", "आप लगभग पहुँच गए हैं");
        Add("You're almost there", "Bạn sắp hoàn thành rồi", "马上就完成了", "ใกล้เสร็จแล้ว", "आप लगभग पहुँच गए हैं");
        Add("Button", "Nút", "按钮", "ปุ่ม", "बटन");
        return d;
    }
}
