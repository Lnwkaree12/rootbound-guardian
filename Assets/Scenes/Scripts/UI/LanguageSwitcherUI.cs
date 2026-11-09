using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using TMPro;

/// <summary>
/// LanguageSwitcherUI — Widget ปุ่มเปลี่ยนภาษา 3 ภาษา สำหรับหน้า MainScreen
/// ──────────────────────────────────────────────────────────────────────────
/// วิธีติดตั้ง:
///   1. สร้าง GameObject ชื่อ "LanguageSwitcher" ใน MainMenu_Canvas (หรือ MainMenuPanel)
///   2. ใส่ Component LanguageSwitcherUI
///   3. Assign 3 Button references: thButton, enButton, zhButton
///   4. Assign TextMeshProUGUI labels สำหรับแต่ละปุ่ม
///   5. (Optional) Assign Image tabHighlight เพื่อเปลี่ยนพื้นหลังปุ่มที่ active
/// ──────────────────────────────────────────────────────────────────────────
/// ระบบนี้รองรับ:
///   - ปุ่มจิ้ม Mouse / Touch
///   - Keyboard L (กด L วนเปลี่ยนภาษา)
///   - Gamepad (คีย์ JoystickButton6 = Back/Select บนส่วนใหญ่)
///   - Auto-refresh UI ทั้งหน้าเมื่อเปลี่ยนภาษา ผ่าน LocalizationManager.OnLanguageChanged
/// </summary>
public class LanguageSwitcherUI : MonoBehaviour
{
    // ─── Inspector References ────────────────────────────────────────────────
    [Header("Language Buttons")]
    public Button thButton;
    public Button enButton;
    public Button zhButton;

    [Header("Button Label Texts")]
    public TextMeshProUGUI thLabel;
    public TextMeshProUGUI enLabel;
    public TextMeshProUGUI zhLabel;

    [Header("Active Tab Highlight Images (optional)")]
    public Image thHighlight;
    public Image enHighlight;
    public Image zhHighlight;

    [Header("Active Color / Inactive Color")]
    public Color activeColor   = new Color(0.27f, 0.18f, 0.12f, 1.0f);  // #452E1E dark wood
    public Color inactiveColor = new Color(0.27f, 0.18f, 0.12f, 1.0f);  // #452E1E dark wood (ไม่จางเวลาไม่เลือก)

    [Header("Prompt Text (optional, e.g. 'ภาษา / Language')")]
    public TextMeshProUGUI promptText;

    [Header("Animation")]
    [Tooltip("เปิดใช้อนิเมชัน punch เมื่อกดเปลี่ยนภาษา")]
    public bool enablePunchAnimation = true;
    public float punchScale = 1.18f;

    // ─── Internal State ───────────────────────────────────────────────────────
    private float[] scaleTargets = new float[3] { 1f, 1f, 1f };
    private float[] scaleCurrents = new float[3] { 1f, 1f, 1f };
    private RectTransform[] buttonRects = new RectTransform[3];

    // ─────────────────────────────────────────────────────────────────────────
    private void Awake()
    {
        // Cache RectTransforms
        if (thButton != null) buttonRects[0] = thButton.GetComponent<RectTransform>();
        if (enButton != null) buttonRects[1] = enButton.GetComponent<RectTransform>();
        if (zhButton != null) buttonRects[2] = zhButton.GetComponent<RectTransform>();
    }

    private void OnEnable()
    {
        // Subscribe เมื่อ component ถูก enable
        LocalizationManager.OnLanguageChanged += OnLanguageChanged;
        RefreshAll();
    }

    private void OnDisable()
    {
        LocalizationManager.OnLanguageChanged -= OnLanguageChanged;
    }

    private void Start()
    {
        SetupButtonListeners();
        RefreshAll();
    }

    private void Update()
    {
        // Keyboard shortcut: L key = วนเปลี่ยนภาษา
        if (Input.GetKeyDown(KeyCode.L))
        {
            CycleLanguage();
        }

        // Animate scale punch
        if (enablePunchAnimation)
        {
            float dt = Time.unscaledDeltaTime;
            for (int i = 0; i < 3; i++)
            {
                scaleCurrents[i] = Mathf.Lerp(scaleCurrents[i], scaleTargets[i], dt * 16f);
                if (buttonRects[i] != null)
                {
                    buttonRects[i].localScale = Vector3.one * scaleCurrents[i];
                }
            }
        }
    }

    // ─────────────────────────────────────────────────────────────────────────
    #region Setup

    private void SetupButtonListeners()
    {
        if (thButton != null)
        {
            thButton.onClick.AddListener(() => SelectLanguage(LocalizationManager.Language.TH, 0));
            AddHover(thButton.gameObject, 0);
        }
        if (enButton != null)
        {
            enButton.onClick.AddListener(() => SelectLanguage(LocalizationManager.Language.EN, 1));
            AddHover(enButton.gameObject, 1);
        }
        if (zhButton != null)
        {
            zhButton.onClick.AddListener(() => SelectLanguage(LocalizationManager.Language.ZH, 2));
            AddHover(zhButton.gameObject, 2);
        }
    }

    private void AddHover(GameObject go, int index)
    {
        EventTrigger trigger = go.GetComponent<EventTrigger>();
        if (trigger == null) trigger = go.AddComponent<EventTrigger>();

        EventTrigger.Entry entry = new EventTrigger.Entry();
        entry.eventID = EventTriggerType.PointerEnter;
        entry.callback.AddListener((d) => OnHoverButton(index));
        trigger.triggers.Add(entry);
    }

    private void OnHoverButton(int index)
    {
        // แค่ nudge scale เล็กน้อยเมื่อ hover (ไม่เปลี่ยนภาษา)
        if (enablePunchAnimation)
        {
            scaleCurrents[index] = 1.06f;
        }
    }

    #endregion

    // ─────────────────────────────────────────────────────────────────────────
    #region Language Actions

    private void SelectLanguage(LocalizationManager.Language lang, int buttonIndex)
    {
        if (LocalizationManager.Instance == null) return;

        // Punch animation
        if (enablePunchAnimation)
        {
            scaleCurrents[buttonIndex] = punchScale;
            scaleTargets[buttonIndex] = 1f;
        }

        LocalizationManager.Instance.SetLanguage(lang);
        // RefreshAll จะถูกเรียกผ่าน OnLanguageChanged event
    }

    private void CycleLanguage()
    {
        if (LocalizationManager.Instance == null) return;

        var cur = LocalizationManager.Instance.CurrentLanguage;
        var next = (LocalizationManager.Language)(((int)cur + 1) % 3);
        int idx = (int)next;
        SelectLanguage(next, idx);
    }

    private void OnLanguageChanged(LocalizationManager.Language lang)
    {
        RefreshAll();
    }

    #endregion

    // ─────────────────────────────────────────────────────────────────────────
    #region Refresh UI

    public void RefreshAll()
    {
        if (LocalizationManager.Instance == null) return;

        var cur = LocalizationManager.Instance.CurrentLanguage;

        // Prompt label
        if (promptText != null)
            promptText.text = LocalizationManager.Instance.Get("lang_select_prompt");

        // Button labels
        SetButtonState(thLabel, thHighlight, 0, cur == LocalizationManager.Language.TH);
        SetButtonState(enLabel, enHighlight,  1, cur == LocalizationManager.Language.EN);
        SetButtonState(zhLabel, zhHighlight,  2, cur == LocalizationManager.Language.ZH);

        // Static label text (not localized — always show own language name)
        if (thLabel != null) thLabel.text = "ไทย";
        if (enLabel != null) enLabel.text = "EN";
        if (zhLabel != null) zhLabel.text = "中文";
    }

    private void SetButtonState(TextMeshProUGUI label, Image highlight, int index, bool isActive)
    {
        if (label != null)
            label.color = isActive ? activeColor : inactiveColor;

        Button targetBtn = (index == 0) ? thButton : ((index == 1) ? enButton : zhButton);

        // Always ensure the button GameObject itself is active!
        if (targetBtn != null && !targetBtn.gameObject.activeSelf)
        {
            targetBtn.gameObject.SetActive(true);
        }

        if (highlight != null)
{
    if (targetBtn != null && highlight.gameObject == targetBtn.gameObject)
    {
        highlight.color = isActive 
            ? new Color(0.27f, 0.18f, 0.12f, 0.80f)  // #452E1E เมื่อเลือก
            : new Color(0.27f, 0.18f, 0.12f, 0.20f); // #452E1E จางๆ เมื่อไม่ได้เลือก
    }
    else
    {
        highlight.gameObject.SetActive(isActive);
    }
}

        scaleTargets[index] = isActive ? 1.08f : 1.0f;
    }

    #endregion
}
