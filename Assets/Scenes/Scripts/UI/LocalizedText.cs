using UnityEngine;
using TMPro;

/// <summary>
/// LocalizedText — Component ผูก TextMeshProUGUI กับ localization key
/// วิธีใช้: ติดบน GameObject ที่มี TextMeshProUGUI แล้วกำหนด localizationKey
/// ข้อความจะอัปเดตอัตโนมัติทุกครั้งที่ภาษาเปลี่ยน
/// </summary>
[RequireComponent(typeof(TextMeshProUGUI))]
public class LocalizedText : MonoBehaviour
{
    [Tooltip("Key ใน LocalizationManager เช่น 'btn_start', 'label_master'")]
    public string localizationKey;

    [Tooltip("ถ้า true: ไม่อัปเดตอัตโนมัติ (สำหรับ text ที่ปรับ runtime เช่น resolution value)")]
    public bool staticOverride = false;

    private TextMeshProUGUI textComponent;

    private void Awake()
    {
        textComponent = GetComponent<TextMeshProUGUI>();
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

    private void OnLanguageChanged(LocalizationManager.Language lang)
    {
        Refresh();
    }

    public void Refresh()
    {
        if (staticOverride || string.IsNullOrEmpty(localizationKey)) return;
        if (LocalizationManager.Instance == null) return;
        if (textComponent == null) textComponent = GetComponent<TextMeshProUGUI>();
        if (textComponent != null)
        {
            textComponent.text = LocalizationManager.Instance.Get(localizationKey);
        }
    }
}
