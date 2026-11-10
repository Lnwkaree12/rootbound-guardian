using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// GameWideLocalization — ผู้ควบคุมการเปลี่ยนภาษาทั่วทั้งเกม (Game-Wide Localization Controller)
/// ─────────────────────────────────────────────────────────────────────────────
/// ทำงานอัตโนมัติตั้งแต่เกมเริ่ม (DontDestroyOnLoad)
/// - ดักจับเมื่อมีการเปลี่ยนฉาก (SceneManager.sceneLoaded)
/// - ดักจับเมื่อมีการเปลี่ยนภาษา (LocalizationManager.OnLanguageChanged)
/// - ตรวจหาและอัปเดตข้อความ UI ทุกจุดใน MainGameplay, Cutscene, LoadingScreen, EndScene, EndCredit
/// - รองรับการกดปุ่ม [L] บนคีย์บอร์ดเพื่อสลับภาษาได้ตลอดเวลา (TH -> EN -> ZH -> TH)
/// </summary>
public class GameWideLocalization : MonoBehaviour
{
    private static GameWideLocalization _instance;
    public static GameWideLocalization Instance => _instance;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void AutoInitialize()
    {
        if (_instance == null)
        {
            var existing = Object.FindFirstObjectByType<GameWideLocalization>();
            if (existing != null)
            {
                _instance = existing;
            }
            else
            {
                GameObject go = new GameObject("[GameWideLocalization]");
                _instance = go.AddComponent<GameWideLocalization>();
                DontDestroyOnLoad(go);
            }
        }
    }

    private void Awake()
    {
        if (_instance != null && _instance != this)
        {
            Destroy(gameObject);
            return;
        }

        _instance = this;
        DontDestroyOnLoad(gameObject);

        // Ensure LocalizationManager is awake and loaded
        if (LocalizationManager.Instance != null)
        {
            LocalizationManager.Instance.EnsureInitialized();
        }
    }

    private void OnEnable()
    {
        SceneManager.sceneLoaded += OnSceneLoaded;
        LocalizationManager.OnLanguageChanged += OnLanguageChanged;
    }

    private void OnDisable()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
        LocalizationManager.OnLanguageChanged -= OnLanguageChanged;
    }

    private void Start()
    {
        ApplyLocalizationToActiveScene();
    }

    private void Update()
    {
        // Global hotkey: Press L to cycle language
        if (Input.GetKeyDown(KeyCode.L))
        {
            CycleLanguage();
        }
    }

    public void CycleLanguage()
    {
        if (LocalizationManager.Instance == null) return;
        int next = ((int)LocalizationManager.Instance.CurrentLanguage + 1) % 3;
        LocalizationManager.Instance.SetLanguage((LocalizationManager.Language)next);
        Debug.Log($"[GameWideLocalization] Language switched to: {LocalizationManager.Instance.CurrentLanguage}");
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        StartCoroutine(ApplyLocalizationDelayedRoutine());
    }

    private void OnLanguageChanged(LocalizationManager.Language lang)
    {
        ApplyLocalizationToActiveScene();
    }

    private IEnumerator ApplyLocalizationDelayedRoutine()
    {
        // Wait 1 frame for Awake/Start on scene objects to finish
        yield return null;
        ApplyLocalizationToActiveScene();
    }

    /// <summary>
    /// อัปเดตข้อความ UI ทุกจุดในฉากปัจจุบันให้ตรงกับภาษาที่เลือก
    /// </summary>
    public void ApplyLocalizationToActiveScene()
    {
        if (LocalizationManager.Instance == null) return;

        // 1. Refresh all LocalizedText components
        var localizedTexts = Object.FindObjectsByType<LocalizedText>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        for (int i = 0; i < localizedTexts.Length; i++)
        {
            if (localizedTexts[i] != null) localizedTexts[i].Refresh();
        }

        // 2. QuestManager
        if (QuestManager.Instance != null)
        {
            QuestManager.Instance.UpdateQuestUI();
        }

        // 3. PauseMenuController
        if (PauseMenuController.Instance != null)
        {
            PauseMenuController.Instance.UpdateLocalizedTexts();
        }

        // 4. GameOverUIManager
        if (GameOverUIManager.Instance != null)
        {
            GameOverUIManager.Instance.UpdateLocalizedTexts();
        }

        // 5. WinUIManager
        if (WinUIManager.Instance != null)
        {
            WinUIManager.Instance.UpdateLocalizedTexts();
        }

        // 6. DoorControllers in scene
        var doors = Object.FindObjectsByType<DoorController>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        for (int i = 0; i < doors.Length; i++)
        {
            if (doors[i] != null && doors[i].promptTextTMP != null)
            {
                bool hasKey = doors[i].CheckPlayerHasKey();
                doors[i].promptTextTMP.text = hasKey
                    ? LocalizationManager.Instance.Get("door_prompt_open")
                    : LocalizationManager.Instance.Get("door_prompt_locked");
            }
        }

        // 7. WorldTreeRootInteraction in scene
        var rootInteractions = Object.FindObjectsByType<WorldTreeRootInteraction>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        for (int i = 0; i < rootInteractions.Length; i++)
        {
            // Prompt text is updated by WorldTreeRootInteraction's own OnLanguageChanged subscriber
        }

        // 8. HUD & Backpack Panels (Search by well-known hierarchy or names)
        LocalizeHUDPanels();

        // 9. NewLoadingScreenManager in scene
        var loadingManagers = Object.FindObjectsByType<NewLoadingScreenManager>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        for (int i = 0; i < loadingManagers.Length; i++)
        {
            // Handled by NewLoadingScreenManager's own OnLanguageChanged subscriber
        }

        // 10. EndCreditController in scene
        if (Object.FindFirstObjectByType<EndCreditController>() is EndCreditController creditCtrl)
        {
            creditCtrl.UpdateLocalizedTexts();
        }
    }

    private void LocalizeHUDPanels()
    {
        // Localize Backpack Title & Instructions
        GameObject backpackPanel = GameObject.Find("BackpackPanel");
        if (backpackPanel != null)
        {
            Transform title = backpackPanel.transform.Find("TitleText");
            if (title != null)
            {
                var txt = title.GetComponent<Text>();
                if (txt != null) txt.text = LocalizationManager.Instance.Get("hud_backpack_name");
                var tmp = title.GetComponent<TextMeshProUGUI>();
                if (tmp != null) tmp.text = LocalizationManager.Instance.Get("hud_backpack_name");
            }

            Transform instructions = backpackPanel.transform.Find("InstructionsText");
            if (instructions != null)
            {
                var txt = instructions.GetComponent<Text>();
                if (txt != null) txt.text = LocalizationManager.Instance.Get("hud_backpack_hint");
                var tmp = instructions.GetComponent<TextMeshProUGUI>();
                if (tmp != null) tmp.text = LocalizationManager.Instance.Get("hud_backpack_hint");
            }
        }

        // Localize Potion Hint Text
        GameObject potionSlot = GameObject.Find("HUD_PotionQuickSlot");
        if (potionSlot != null)
        {
            Transform hint = potionSlot.transform.Find("HintText");
            if (hint != null)
            {
                var txt = hint.GetComponent<Text>();
                if (txt != null) txt.text = LocalizationManager.Instance.Get("hud_potion_hint");
                var tmp = hint.GetComponent<TextMeshProUGUI>();
                if (tmp != null) tmp.text = LocalizationManager.Instance.Get("hud_potion_hint");
            }
        }

        // Localize Dash Cooldown Shift Hint
        GameObject dashUI = GameObject.Find("DashCooldownUI");
        if (dashUI != null)
        {
            Transform keyBadge = dashUI.transform.Find("KeybindBadge/Text");
            if (keyBadge != null)
            {
                var txt = keyBadge.GetComponent<Text>();
                if (txt != null) txt.text = LocalizationManager.Instance.Get("hud_shift_hint");
                var tmp = keyBadge.GetComponent<TextMeshProUGUI>();
                if (tmp != null) tmp.text = LocalizationManager.Instance.Get("hud_shift_hint");
            }
        }
    }
}

