using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// LocalizedText — Component ผูก TextMeshProUGUI หรือ UnityEngine.UI.Text กับ localization key
/// วิธีใช้: ติดบน GameObject ที่มี TextMeshProUGUI หรือ Text แล้วกำหนด localizationKey
/// ข้อความจะอัปเดตอัตโนมัติทุกครั้งที่ภาษาเปลี่ยน
/// </summary>
[DisallowMultipleComponent]
public class LocalizedText : MonoBehaviour
{
    [Tooltip("Key ใน LocalizationManager เช่น 'btn_start', 'quest_collect_key'")]
    public string localizationKey;

    [Tooltip("ถ้า true: ไม่อัปเดตอัตโนมัติ (สำหรับ text ที่ปรับ runtime)")]
    public bool staticOverride = false;

    private TextMeshProUGUI tmpText;
    private Text legacyText;

    private void Awake()
    {
        CacheComponents();
    }

    private void OnEnable()
    {
        LocalizationManager.OnLanguageChanged += OnLanguageChanged;
        Refresh();
    }

    private void OnDisable()
    {
        LocalizationManager.OnLanguageChanged -= OnLanguageChanged;
    }

    private void Start()
    {
        Refresh();
    }

    private void CacheComponents()
    {
        if (tmpText == null) tmpText = GetComponent<TextMeshProUGUI>();
        if (legacyText == null) legacyText = GetComponent<Text>();
    }

    private void OnLanguageChanged(LocalizationManager.Language lang)
    {
        Refresh();
    }

    /// <summary>ตั้งค่า localization key ใหม่และรีเฟรชทันที</summary>
    public void SetKey(string newKey)
    {
        localizationKey = newKey;
        Refresh();
    }

    public void Refresh()
    {
        if (staticOverride || string.IsNullOrEmpty(localizationKey)) return;
        if (LocalizationManager.Instance == null) return;

        CacheComponents();
        string localizedString = LocalizationManager.Instance.Get(localizationKey);

        if (tmpText != null)
        {
            tmpText.text = localizedString;
        }
        else if (legacyText != null)
        {
            legacyText.text = localizedString;
        }
    }
}
