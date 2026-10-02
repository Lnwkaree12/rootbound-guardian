using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using UnityEngine.EventSystems;
using TMPro;

/// <summary>
/// Controller for the Start Game UI matching the PDF design & Video reference:
/// 1. Press Any Key to Start (Page 1)
/// 2. Main Menu: Start / Settings / Exit with Living Paw Print indicators (Page 2)
/// 3. Settings: Audio Sliders with dynamic ◀ ▶ row arrows, Resolution, Display Mode, Back (Page 3)
/// 4. Exit Confirmation Modal: Exit the Game? Confirm / Cancel with elastic bounce (Page 4)
/// Features:
/// - Smooth spring scale & alpha transitions for all buttons and rows
/// - Living paws with playful idle floating and tilting
/// - Dynamic ◀ ▶ arrows following the active settings row with tactile punch feedback
/// - Smooth CanvasGroup crossfades and subtle vertical settle on screen transitions
/// - Top-right key hint badges ([ENTER ↵] / [ESC]) with click/press feedback
/// - Procedural audio synthesis for navigation, click, and back actions
/// </summary>
public class MainMenuUIController : MonoBehaviour
{
    public enum MenuState
    {
        PressAnyKey,
        MainMenu,
        Settings,
        ExitConfirm
    }

    [Header("Current State")]
    [SerializeField] private MenuState currentState = MenuState.PressAnyKey;

    [Header("Panels")]
    public GameObject pressAnyKeyPanel;
    public GameObject mainMenuPanel;
    public GameObject settingsPanel;
    public GameObject exitConfirmModal;
    public GameObject keyHintsPanel;

    [Header("Press Any Key Elements")]
    public TextMeshProUGUI pressAnyKeyText;
    public float pulseSpeed = 2.5f;
    public float minAlpha = 0.25f;
    public float maxAlpha = 1.0f;

    [Header("Main Menu Buttons")]
    public Button startButton;
    public Button settingsButton;
    public Button exitButton;
    public TextMeshProUGUI startText;
    public TextMeshProUGUI settingsText;
    public TextMeshProUGUI exitText;
    public GameObject startPaws;
    public GameObject settingsPaws;
    public GameObject exitPaws;

    [Header("Settings Elements")]
    public Slider masterVolumeSlider;
    public Slider bgmVolumeSlider;
    public Slider sfxVolumeSlider;
    public TextMeshProUGUI masterLabel;
    public TextMeshProUGUI bgmLabel;
    public TextMeshProUGUI sfxLabel;
    public TextMeshProUGUI resolutionValueText;
    public TextMeshProUGUI displayModeValueText;
    public Button settingsBackButton;
    public TextMeshProUGUI settingsBackText;
    public GameObject settingsBackPaws;

    [Header("Settings Dynamic Arrows (◀ and ▶)")]
    public GameObject masterArrowLeft;
    public GameObject masterArrowRight;
    public GameObject bgmArrowLeft;
    public GameObject bgmArrowRight;
    public GameObject sfxArrowLeft;
    public GameObject sfxArrowRight;
    public GameObject resolutionArrowLeft;
    public GameObject resolutionArrowRight;
    public GameObject displayModeArrowLeft;
    public GameObject displayModeArrowRight;

    [Header("Exit Modal Elements")]
    public RectTransform exitModalCardRect;
    public Image exitModalDimImage;
    public Button confirmExitButton;
    public Button cancelExitButton;
    public TextMeshProUGUI confirmExitText;
    public TextMeshProUGUI cancelExitText;
    public GameObject confirmExitPaws;
    public GameObject cancelExitPaws;

    [Header("Key Hint Badges (Press Feedback)")]
    public RectTransform confirmBadgeRect;
    public RectTransform backBadgeRect;

    [Header("Scene Transition")]
    public string targetSceneName = "Loading Screen";

    [Header("Audio Feedback")]
    public bool enableProceduralSfx = true;

    // Internal Navigation
    private int mainMenuIndex = 0; // 0: Start, 1: Settings, 2: Exit
    private int settingsIndex = 0; // 0: Master, 1: BGM, 2: SFX, 3: Resolution, 4: DisplayMode, 5: Back
    private int exitModalIndex = 0; // 0: Confirm, 1: Cancel

    // Settings Data
    private List<Resolution> availableResolutions = new List<Resolution>();
    private int currentResolutionIndex = 0;
    private int currentDisplayModeIndex = 0; // 0: Borderless, 1: Fullscreen, 2: Windowed
    private readonly string[] displayModes = { "BORDERLESS", "FULLSCREEN", "WINDOWED" };

    // Procedural Audio
    private AudioSource audioSource;
    private AudioClip navSound;
    private AudioClip confirmSound;
    private AudioClip backSound;
    private AudioClip nudgeSound;

    private bool isTransitioning = false;
    private Coroutine activeTransitionRoutine;

    // CanvasGroups for smooth crossfade
    private CanvasGroup pressAnyKeyCG;
    private CanvasGroup mainMenuCG;
    private CanvasGroup settingsCG;
    private CanvasGroup exitModalCG;
    private CanvasGroup keyHintsCG;

    // Animated Element Wrappers
    private class AnimatedOption
    {
        public Transform rootTransform;
        public TextMeshProUGUI label;
        public GameObject pawsContainer;
        public RectTransform leftPaw;
        public RectTransform rightPaw;
        public Vector2 leftPawBasePos;
        public Vector2 rightPawBasePos;
        public float currentScale = 1.0f;
        public float targetScale = 1.0f;
        public float currentAlpha = 0.55f;
        public float targetAlpha = 0.55f;
        public float pawScale = 0f;
        public float targetPawScale = 0f;
    }

    private AnimatedOption startOption;
    private AnimatedOption settingsOption;
    private AnimatedOption exitOption;

    private AnimatedOption confirmExitOption;
    private AnimatedOption cancelExitOption;
    private AnimatedOption settingsBackOption;

    // Settings Rows Wrappers
    private class SettingsRowItem
    {
        public TextMeshProUGUI label;
        public GameObject arrowLeft;
        public GameObject arrowRight;
        public RectTransform arrowLeftRT;
        public RectTransform arrowRightRT;
        public Vector2 arrowLeftBasePos;
        public Vector2 arrowRightBasePos;
        public Slider slider;
        public RectTransform sliderHandleRT;
        public float currentScale = 1.0f;
        public float targetScale = 1.0f;
        public float currentAlpha = 0.55f;
        public float targetAlpha = 0.55f;
        public float arrowScale = 0f;
        public float targetArrowScale = 0f;
        public float punchOffsetLeft = 0f;
        public float punchOffsetRight = 0f;
    }

    private SettingsRowItem[] settingsRows;

    // Key badge punch animation
    private float confirmBadgeScale = 1.0f;
    private float backBadgeScale = 1.0f;

    private void Awake()
    {
        SetupAudio();
        InitResolutions();
        LoadPreferences();
        InitCanvasGroups();
        InitAnimatedOptions();
        InitSettingsRows();
    }

    private void Start()
    {
        SetupButtonListeners();
        SetupSliderListeners();
        SetMenuState(MenuState.PressAnyKey, false, true);
    }

    private void Update()
    {
        UpdateAnimations();

        if (isTransitioning) return;

        switch (currentState)
        {
            case MenuState.PressAnyKey:
                UpdatePressAnyKey();
                break;
            case MenuState.MainMenu:
                UpdateMainMenuInput();
                break;
            case MenuState.Settings:
                UpdateSettingsInput();
                break;
            case MenuState.ExitConfirm:
                UpdateExitConfirmInput();
                break;
        }
    }

    #region Initialization

    private void InitCanvasGroups()
    {
        pressAnyKeyCG = EnsureCanvasGroup(pressAnyKeyPanel);
        mainMenuCG = EnsureCanvasGroup(mainMenuPanel);
        settingsCG = EnsureCanvasGroup(settingsPanel);
        exitModalCG = EnsureCanvasGroup(exitConfirmModal);
        keyHintsCG = EnsureCanvasGroup(keyHintsPanel);
    }

    private CanvasGroup EnsureCanvasGroup(GameObject go)
    {
        if (go == null) return null;
        CanvasGroup cg = go.GetComponent<CanvasGroup>();
        if (cg == null) cg = go.AddComponent<CanvasGroup>();
        return cg;
    }

    private void InitAnimatedOptions()
    {
        startOption = CreateAnimatedOption(startButton != null ? startButton.transform : null, startText, startPaws);
        settingsOption = CreateAnimatedOption(settingsButton != null ? settingsButton.transform : null, settingsText, settingsPaws);
        exitOption = CreateAnimatedOption(exitButton != null ? exitButton.transform : null, exitText, exitPaws);

        confirmExitOption = CreateAnimatedOption(confirmExitButton != null ? confirmExitButton.transform : null, confirmExitText, confirmExitPaws);
        cancelExitOption = CreateAnimatedOption(cancelExitButton != null ? cancelExitButton.transform : null, cancelExitText, cancelExitPaws);
        settingsBackOption = CreateAnimatedOption(settingsBackButton != null ? settingsBackButton.transform : null, settingsBackText, settingsBackPaws);
    }

    private AnimatedOption CreateAnimatedOption(Transform root, TextMeshProUGUI text, GameObject paws)
    {
        AnimatedOption opt = new AnimatedOption();
        opt.rootTransform = root;
        opt.label = text;
        opt.pawsContainer = paws;

        if (paws != null)
        {
            // Find LeftPaw and RightPaw
            Transform lp = paws.transform.Find("LeftPaw");
            Transform rp = paws.transform.Find("RightPaw");

            // If paws container is tracker, search inside siblings or root
            if (lp == null && root != null) lp = root.Find("LeftPaw");
            if (rp == null && root != null) rp = root.Find("RightPaw");

            if (lp != null)
            {
                opt.leftPaw = lp as RectTransform;
                opt.leftPawBasePos = opt.leftPaw.anchoredPosition;
            }
            if (rp != null)
            {
                opt.rightPaw = rp as RectTransform;
                opt.rightPawBasePos = opt.rightPaw.anchoredPosition;
            }
        }

        return opt;
    }

    private void InitSettingsRows()
    {
        settingsRows = new SettingsRowItem[5];

        // 0: Master
        settingsRows[0] = CreateSettingsRowItem(masterLabel, masterArrowLeft, masterArrowRight, masterVolumeSlider);
        // 1: BGM
        settingsRows[1] = CreateSettingsRowItem(bgmLabel, bgmArrowLeft, bgmArrowRight, bgmVolumeSlider);
        // 2: SFX
        settingsRows[2] = CreateSettingsRowItem(sfxLabel, sfxArrowLeft, sfxArrowRight, sfxVolumeSlider);
        // 3: Resolution
        settingsRows[3] = CreateSettingsRowItem(resolutionValueText, resolutionArrowLeft, resolutionArrowRight, null);
        // 4: Display Mode
        settingsRows[4] = CreateSettingsRowItem(displayModeValueText, displayModeArrowLeft, displayModeArrowRight, null);

        // Auto-find arrows if not explicitly assigned in inspector
        AutoFindSettingsArrows();
    }

    private SettingsRowItem CreateSettingsRowItem(TextMeshProUGUI label, GameObject aL, GameObject aR, Slider slider)
    {
        SettingsRowItem item = new SettingsRowItem();
        item.label = label;
        item.arrowLeft = aL;
        item.arrowRight = aR;
        item.slider = slider;

        if (aL != null)
        {
            item.arrowLeftRT = aL.GetComponent<RectTransform>();
            if (item.arrowLeftRT != null) item.arrowLeftBasePos = item.arrowLeftRT.anchoredPosition;
        }
        if (aR != null)
        {
            item.arrowRightRT = aR.GetComponent<RectTransform>();
            if (item.arrowRightRT != null) item.arrowRightBasePos = item.arrowRightRT.anchoredPosition;
        }
        if (slider != null && slider.handleRect != null)
        {
            item.sliderHandleRT = slider.handleRect;
        }

        return item;
    }

    private void AutoFindSettingsArrows()
    {
        if (settingsPanel == null) return;

        // Search MasterRow
        WireRowArrows(0, "MasterRow");
        WireRowArrows(1, "BgmRow");
        WireRowArrows(2, "SfxRow");
        WireRowArrows(3, "ResolutionRow");
        WireRowArrows(4, "DisplayModeRow");
    }

    private void WireRowArrows(int rowIndex, string rowName)
    {
        if (settingsRows[rowIndex] == null) return;
        Transform rowT = settingsPanel.transform.Find("SettingsContent/" + rowName);
        if (rowT == null) rowT = settingsPanel.transform.Find(rowName);
        if (rowT == null) return;

        if (settingsRows[rowIndex].arrowLeft == null)
        {
            Transform al = rowT.Find("ArrowLeft");
            if (al != null)
            {
                settingsRows[rowIndex].arrowLeft = al.gameObject;
                settingsRows[rowIndex].arrowLeftRT = al as RectTransform;
                settingsRows[rowIndex].arrowLeftBasePos = settingsRows[rowIndex].arrowLeftRT.anchoredPosition;
            }
        }

        if (settingsRows[rowIndex].arrowRight == null)
        {
            Transform ar = rowT.Find("ArrowRight");
            if (ar != null)
            {
                settingsRows[rowIndex].arrowRight = ar.gameObject;
                settingsRows[rowIndex].arrowRightRT = ar as RectTransform;
                settingsRows[rowIndex].arrowRightBasePos = settingsRows[rowIndex].arrowRightRT.anchoredPosition;
            }
        }
    }

    #endregion

    #region State Management & Smooth Transitions

    public void SetMenuState(MenuState newState, bool playSfx = true, bool instant = false)
    {
        MenuState oldState = currentState;
        currentState = newState;

        if (activeTransitionRoutine != null)
        {
            StopCoroutine(activeTransitionRoutine);
        }

        if (instant)
        {
            ApplyPanelVisibilityInstant(newState);
        }
        else
        {
            activeTransitionRoutine = StartCoroutine(TransitionRoutine(oldState, newState));
        }

        // Initialize target visuals
        if (newState == MenuState.MainMenu)
        {
            UpdateMainMenuVisuals(true);
        }
        else if (newState == MenuState.Settings)
        {
            settingsIndex = 0;
            UpdateSettingsVisuals(true);
        }
        else if (newState == MenuState.ExitConfirm)
        {
            exitModalIndex = 0;
            UpdateExitModalVisuals(true);
            if (exitModalCardRect != null)
            {
                exitModalCardRect.localScale = Vector3.one * 0.82f; // pop-in scale
            }
        }

        if (playSfx)
        {
            PlaySound(confirmSound);
        }
    }

    private void ApplyPanelVisibilityInstant(MenuState state)
    {
        SetPanelAlpha(pressAnyKeyPanel, pressAnyKeyCG, state == MenuState.PressAnyKey ? 1f : 0f);
        SetPanelAlpha(mainMenuPanel, mainMenuCG, state == MenuState.MainMenu ? 1f : 0f);
        SetPanelAlpha(settingsPanel, settingsCG, state == MenuState.Settings ? 1f : 0f);
        SetPanelAlpha(exitConfirmModal, exitModalCG, state == MenuState.ExitConfirm ? 1f : 0f);
        SetPanelAlpha(keyHintsPanel, keyHintsCG, state != MenuState.PressAnyKey ? 1f : 0f);
    }

    private void SetPanelAlpha(GameObject panel, CanvasGroup cg, float alpha)
    {
        if (panel == null) return;
        panel.SetActive(alpha > 0.001f);
        if (cg != null)
        {
            cg.alpha = alpha;
            cg.interactable = alpha > 0.9f;
            cg.blocksRaycasts = alpha > 0.9f;
        }
    }

    private IEnumerator TransitionRoutine(MenuState fromState, MenuState toState)
    {
        isTransitioning = true;

        CanvasGroup fromCG = GetCanvasGroupForState(fromState);
        CanvasGroup toCG = GetCanvasGroupForState(toState);
        GameObject toPanel = GetPanelForState(toState);

        // 1. Fade out current panel
        if (fromCG != null)
        {
            fromCG.interactable = false;
            fromCG.blocksRaycasts = false;
            float elapsed = 0f;
            float duration = 0.10f;
            float startAlpha = fromCG.alpha;
            while (elapsed < duration)
            {
                elapsed += Time.unscaledDeltaTime;
                fromCG.alpha = Mathf.Lerp(startAlpha, 0f, elapsed / duration);
                yield return null;
            }
            fromCG.alpha = 0f;
            GameObject fromPanel = GetPanelForState(fromState);
            if (fromPanel != null) fromPanel.SetActive(false);
        }

        // Manage KeyHints visibility
        if (keyHintsPanel != null && keyHintsCG != null)
        {
            bool showHints = (toState != MenuState.PressAnyKey);
            keyHintsPanel.SetActive(showHints);
            keyHintsCG.alpha = showHints ? 1f : 0f;
        }

        // 2. Prepare & Fade in next panel
        if (toPanel != null)
        {
            toPanel.SetActive(true);
        }

        if (toCG != null)
        {
            RectTransform toRT = toCG.GetComponent<RectTransform>();
            Vector2 basePos = Vector2.zero;
            if (toRT != null)
            {
                basePos = toRT.anchoredPosition;
                toRT.anchoredPosition = new Vector2(basePos.x, basePos.y - 15f); // subtle settle
            }

            float elapsed = 0f;
            float duration = 0.16f;
            while (elapsed < duration)
            {
                elapsed += Time.unscaledDeltaTime;
                float t = Mathf.Clamp01(elapsed / duration);
                float ease = Mathf.SmoothStep(0f, 1f, t);
                toCG.alpha = ease;
                if (toRT != null)
                {
                    toRT.anchoredPosition = Vector2.Lerp(new Vector2(basePos.x, basePos.y - 15f), basePos, ease);
                }
                yield return null;
            }

            toCG.alpha = 1f;
            toCG.interactable = true;
            toCG.blocksRaycasts = true;
            if (toRT != null) toRT.anchoredPosition = basePos;
        }

        isTransitioning = false;
    }

    private CanvasGroup GetCanvasGroupForState(MenuState state)
    {
        switch (state)
        {
            case MenuState.PressAnyKey: return pressAnyKeyCG;
            case MenuState.MainMenu: return mainMenuCG;
            case MenuState.Settings: return settingsCG;
            case MenuState.ExitConfirm: return exitModalCG;
            default: return null;
        }
    }

    private GameObject GetPanelForState(MenuState state)
    {
        switch (state)
        {
            case MenuState.PressAnyKey: return pressAnyKeyPanel;
            case MenuState.MainMenu: return mainMenuPanel;
            case MenuState.Settings: return settingsPanel;
            case MenuState.ExitConfirm: return exitConfirmModal;
            default: return null;
        }
    }

    #endregion

    #region Per-Frame Animations (Juice & Smooth Easing)

    private void UpdateAnimations()
    {
        float dt = Time.unscaledDeltaTime;

        // 1. Update Main Menu Buttons
        UpdateAnimatedOption(startOption, dt);
        UpdateAnimatedOption(settingsOption, dt);
        UpdateAnimatedOption(exitOption, dt);

        // 2. Update Settings Rows & Arrows
        UpdateSettingsRowsAnimation(dt);
        UpdateAnimatedOption(settingsBackOption, dt);

        // 3. Update Exit Modal Options & Card Elastic Scale
        UpdateAnimatedOption(confirmExitOption, dt);
        UpdateAnimatedOption(cancelExitOption, dt);

        if (currentState == MenuState.ExitConfirm && exitModalCardRect != null)
        {
            exitModalCardRect.localScale = Vector3.Lerp(exitModalCardRect.localScale, Vector3.one, dt * 14f);
        }

        // 4. Update Key Badges Scale Punch
        confirmBadgeScale = Mathf.Lerp(confirmBadgeScale, 1.0f, dt * 14f);
        backBadgeScale = Mathf.Lerp(backBadgeScale, 1.0f, dt * 14f);

        if (confirmBadgeRect != null) confirmBadgeRect.localScale = Vector3.one * confirmBadgeScale;
        if (backBadgeRect != null) backBadgeRect.localScale = Vector3.one * backBadgeScale;
    }

    private void UpdateAnimatedOption(AnimatedOption opt, float dt)
    {
        if (opt == null) return;

        // Smooth Scale
        opt.currentScale = Mathf.Lerp(opt.currentScale, opt.targetScale, dt * 15f);
        if (opt.rootTransform != null)
        {
            opt.rootTransform.localScale = Vector3.one * opt.currentScale;
        }

        // Smooth Alpha
        opt.currentAlpha = Mathf.Lerp(opt.currentAlpha, opt.targetAlpha, dt * 15f);
        if (opt.label != null)
        {
            Color c = Color.white;
            c.a = opt.currentAlpha;
            opt.label.color = c;
        }

        // Smooth Paws Scale & Idle Floating
        opt.pawScale = Mathf.Lerp(opt.pawScale, opt.targetPawScale, dt * 18f);

        if (opt.pawScale > 0.01f)
        {
            if (opt.pawsContainer != null && !opt.pawsContainer.activeSelf)
                opt.pawsContainer.SetActive(true);

            // Living paws: cute subtle bobbing and tilting
            float bob = Mathf.Sin(Time.unscaledTime * 4.5f) * 3.5f;
            float tilt = Mathf.Sin(Time.unscaledTime * 3.5f) * 6.0f;

            if (opt.leftPaw != null)
            {
                opt.leftPaw.localScale = Vector3.one * opt.pawScale;
                opt.leftPaw.anchoredPosition = new Vector2(opt.leftPawBasePos.x, opt.leftPawBasePos.y + bob);
                opt.leftPaw.localEulerAngles = new Vector3(0f, 0f, tilt);
            }

            if (opt.rightPaw != null)
            {
                opt.rightPaw.localScale = Vector3.one * opt.pawScale;
                opt.rightPaw.anchoredPosition = new Vector2(opt.rightPawBasePos.x, opt.rightPawBasePos.y + bob);
                opt.rightPaw.localEulerAngles = new Vector3(0f, 0f, -tilt);
            }
        }
        else
        {
            if (opt.pawsContainer != null && opt.pawsContainer.activeSelf)
                opt.pawsContainer.SetActive(false);
        }
    }

    private void UpdateSettingsRowsAnimation(float dt)
    {
        if (settingsRows == null) return;

        for (int i = 0; i < settingsRows.Length; i++)
        {
            var row = settingsRows[i];
            if (row == null) continue;

            // Smooth Scale & Alpha
            row.currentScale = Mathf.Lerp(row.currentScale, row.targetScale, dt * 15f);
            row.currentAlpha = Mathf.Lerp(row.currentAlpha, row.targetAlpha, dt * 15f);

            if (row.label != null)
            {
                row.label.transform.localScale = Vector3.one * row.currentScale;
                Color c = Color.white;
                c.a = row.currentAlpha;
                row.label.color = c;
            }

            // Arrow Scale & Visibility
            row.arrowScale = Mathf.Lerp(row.arrowScale, row.targetArrowScale, dt * 18f);

            bool showArrows = row.arrowScale > 0.02f;
            if (row.arrowLeft != null && row.arrowLeft.activeSelf != showArrows) row.arrowLeft.SetActive(showArrows);
            if (row.arrowRight != null && row.arrowRight.activeSelf != showArrows) row.arrowRight.SetActive(showArrows);

            // Arrow Tactile Punch Recovery
            row.punchOffsetLeft = Mathf.Lerp(row.punchOffsetLeft, 0f, dt * 18f);
            row.punchOffsetRight = Mathf.Lerp(row.punchOffsetRight, 0f, dt * 18f);

            if (row.arrowLeftRT != null)
            {
                row.arrowLeftRT.localScale = Vector3.one * row.arrowScale;
                row.arrowLeftRT.anchoredPosition = row.arrowLeftBasePos + new Vector2(row.punchOffsetLeft, 0f);
            }

            if (row.arrowRightRT != null)
            {
                row.arrowRightRT.localScale = Vector3.one * row.arrowScale;
                row.arrowRightRT.anchoredPosition = row.arrowRightBasePos + new Vector2(row.punchOffsetRight, 0f);
            }

            // Active Slider Handle Pulse
            if (row.sliderHandleRT != null)
            {
                float targetHandle = (i == settingsIndex) ? (1.25f + Mathf.Sin(Time.unscaledTime * 5f) * 0.08f) : 1.0f;
                row.sliderHandleRT.localScale = Vector3.Lerp(row.sliderHandleRT.localScale, Vector3.one * targetHandle, dt * 14f);
            }
        }
    }

    #endregion

    #region Press Any Key Screen

    private void UpdatePressAnyKey()
    {
        if (pressAnyKeyText != null)
        {
            float t = (Mathf.Sin(Time.unscaledTime * pulseSpeed) + 1f) * 0.5f;
            float alpha = Mathf.Lerp(minAlpha, maxAlpha, t);
            Color c = pressAnyKeyText.color;
            c.a = alpha;
            pressAnyKeyText.color = c;
        }

        if (Input.anyKeyDown && !Input.GetKeyDown(KeyCode.Escape))
        {
            PunchConfirmBadge();
            SetMenuState(MenuState.MainMenu, true);
        }
    }

    #endregion

    #region Main Menu Screen

    private void UpdateMainMenuInput()
    {
        if (Input.GetKeyDown(KeyCode.LeftArrow) || Input.GetKeyDown(KeyCode.A))
        {
            mainMenuIndex = (mainMenuIndex - 1 + 3) % 3;
            PlaySound(navSound);
            UpdateMainMenuVisuals();
        }
        else if (Input.GetKeyDown(KeyCode.RightArrow) || Input.GetKeyDown(KeyCode.D))
        {
            mainMenuIndex = (mainMenuIndex + 1) % 3;
            PlaySound(navSound);
            UpdateMainMenuVisuals();
        }

        if (Input.GetKeyDown(KeyCode.Return) || Input.GetKeyDown(KeyCode.KeypadEnter) || Input.GetKeyDown(KeyCode.Space))
        {
            PunchConfirmBadge();
            TriggerMainMenuSelection();
        }

        if (Input.GetKeyDown(KeyCode.Escape))
        {
            PunchBackBadge();
            PlaySound(backSound);
            SetMenuState(MenuState.PressAnyKey, false);
        }
    }

    public void SelectMainMenuButton(int index)
    {
        if (mainMenuIndex != index)
        {
            mainMenuIndex = index;
            PlaySound(navSound);
            UpdateMainMenuVisuals();
        }
    }

    private void UpdateMainMenuVisuals(bool snap = false)
    {
        SetOptionVisualState(startOption, mainMenuIndex == 0, snap);
        SetOptionVisualState(settingsOption, mainMenuIndex == 1, snap);
        SetOptionVisualState(exitOption, mainMenuIndex == 2, snap);
    }

    private void SetOptionVisualState(AnimatedOption opt, bool isSelected, bool snap = false)
    {
        if (opt == null) return;

        opt.targetScale = isSelected ? 1.10f : 1.0f;
        opt.targetAlpha = isSelected ? 1.0f : 0.55f;
        opt.targetPawScale = isSelected ? 1.0f : 0.0f;

        if (isSelected && !snap)
        {
            opt.currentScale = 1.20f; // Punch bounce!
            opt.pawScale = 0.25f;    // Pop-in start
        }

        if (snap)
        {
            opt.currentScale = opt.targetScale;
            opt.currentAlpha = opt.targetAlpha;
            opt.pawScale = opt.targetPawScale;
        }
    }

    public void TriggerMainMenuSelection()
    {
        PlaySound(confirmSound);
        switch (mainMenuIndex)
        {
            case 0: OnStartClicked(); break;
            case 1: OnSettingsClicked(); break;
            case 2: OnExitClicked(); break;
        }
    }

    public void OnStartClicked()
    {
        if (isTransitioning) return;
        isTransitioning = true;
        PlaySound(confirmSound);
        PunchConfirmBadge();
        StartCoroutine(StartGameRoutine());
    }

    public void OnSettingsClicked()
    {
        PunchConfirmBadge();
        SetMenuState(MenuState.Settings);
    }

    public void OnExitClicked()
    {
        PunchConfirmBadge();
        SetMenuState(MenuState.ExitConfirm);
    }

    private IEnumerator StartGameRoutine()
    {
        yield return new WaitForSecondsRealtime(0.25f);

        if (!string.IsNullOrEmpty(targetSceneName) && Application.CanStreamedLevelBeLoaded(targetSceneName))
        {
            SceneManager.LoadScene(targetSceneName);
        }
        else if (SceneManager.sceneCountInBuildSettings > 1)
        {
            SceneManager.LoadScene(1);
        }
        else
        {
            Debug.LogWarning($"[MainMenuUIController] Target scene '{targetSceneName}' not found in Build Settings.");
            isTransitioning = false;
        }
    }

    #endregion

    #region Settings Screen

    private void UpdateSettingsInput()
    {
        // Up / Down
        if (Input.GetKeyDown(KeyCode.UpArrow) || Input.GetKeyDown(KeyCode.W))
        {
            settingsIndex = (settingsIndex - 1 + 6) % 6;
            PlaySound(navSound);
            UpdateSettingsVisuals();
        }
        else if (Input.GetKeyDown(KeyCode.DownArrow) || Input.GetKeyDown(KeyCode.S))
        {
            settingsIndex = (settingsIndex + 1) % 6;
            PlaySound(navSound);
            UpdateSettingsVisuals();
        }

        // Left / Right adjustments with tactile arrow punch
        if (Input.GetKeyDown(KeyCode.LeftArrow) || Input.GetKeyDown(KeyCode.A))
        {
            PunchCurrentRowArrow(-1);
            AdjustSettingsValue(-1);
        }
        else if (Input.GetKeyDown(KeyCode.RightArrow) || Input.GetKeyDown(KeyCode.D))
        {
            PunchCurrentRowArrow(1);
            AdjustSettingsValue(1);
        }

        // Confirm
        if (Input.GetKeyDown(KeyCode.Return) || Input.GetKeyDown(KeyCode.KeypadEnter) || Input.GetKeyDown(KeyCode.Space))
        {
            PunchConfirmBadge();
            if (settingsIndex == 5)
            {
                OnSettingsBackClicked();
            }
            else if (settingsIndex == 3)
            {
                PunchCurrentRowArrow(1);
                CycleResolution(1);
            }
            else if (settingsIndex == 4)
            {
                PunchCurrentRowArrow(1);
                CycleDisplayMode(1);
            }
        }

        // Back: ESC
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            PunchBackBadge();
            OnSettingsBackClicked();
        }
    }

    private void PunchCurrentRowArrow(int direction)
    {
        if (settingsIndex >= 0 && settingsIndex < settingsRows.Length)
        {
            var row = settingsRows[settingsIndex];
            if (row != null)
            {
                if (direction < 0) row.punchOffsetLeft = -9f;
                else row.punchOffsetRight = +9f;
            }
        }
    }

    public void SelectSettingsItem(int index)
    {
        if (settingsIndex != index)
        {
            settingsIndex = index;
            PlaySound(navSound);
            UpdateSettingsVisuals();
        }
    }

    private void AdjustSettingsValue(int direction)
    {
        PlaySound(nudgeSound);
        switch (settingsIndex)
        {
            case 0:
                if (masterVolumeSlider != null)
                    masterVolumeSlider.value = Mathf.Clamp01(masterVolumeSlider.value + direction * 0.05f);
                break;
            case 1:
                if (bgmVolumeSlider != null)
                    bgmVolumeSlider.value = Mathf.Clamp01(bgmVolumeSlider.value + direction * 0.05f);
                break;
            case 2:
                if (sfxVolumeSlider != null)
                    sfxVolumeSlider.value = Mathf.Clamp01(sfxVolumeSlider.value + direction * 0.05f);
                break;
            case 3:
                CycleResolution(direction);
                break;
            case 4:
                CycleDisplayMode(direction);
                break;
        }
    }

    private void UpdateSettingsVisuals(bool snap = false)
    {
        // 1. Update settings rows 0 to 4
        for (int i = 0; i < settingsRows.Length; i++)
        {
            var row = settingsRows[i];
            if (row == null) continue;

            bool isSelected = (i == settingsIndex);
            row.targetScale = isSelected ? 1.08f : 1.0f;
            row.targetAlpha = isSelected ? 1.0f : 0.55f;
            row.targetArrowScale = isSelected ? 1.0f : 0.0f;

            if (isSelected && !snap)
            {
                row.currentScale = 1.15f; // Punch
            }

            if (snap)
            {
                row.currentScale = row.targetScale;
                row.currentAlpha = row.targetAlpha;
                row.arrowScale = row.targetArrowScale;
            }
        }

        // 2. Update BACK button (row 5)
        SetOptionVisualState(settingsBackOption, settingsIndex == 5, snap);
    }

    public void OnSettingsBackClicked()
    {
        PunchBackBadge();
        PlaySound(backSound);
        SavePreferences();
        SetMenuState(MenuState.MainMenu, false);
    }

    public void CycleResolution(int direction)
    {
        if (availableResolutions == null || availableResolutions.Count == 0) return;
        currentResolutionIndex = (currentResolutionIndex + direction + availableResolutions.Count) % availableResolutions.Count;
        ApplyResolution(currentResolutionIndex);
        PlaySound(navSound);
    }

    public void CycleDisplayMode(int direction)
    {
        currentDisplayModeIndex = (currentDisplayModeIndex + direction + displayModes.Length) % displayModes.Length;
        ApplyDisplayMode(currentDisplayModeIndex);
        PlaySound(navSound);
    }

    private void ApplyResolution(int index)
    {
        if (index >= 0 && index < availableResolutions.Count)
        {
            Resolution res = availableResolutions[index];
            Screen.SetResolution(res.width, res.height, Screen.fullScreenMode, res.refreshRateRatio);
            UpdateResolutionText(res);
        }
    }

    private void UpdateResolutionText(Resolution res)
    {
        if (resolutionValueText != null)
        {
            int refresh = Mathf.RoundToInt((float)res.refreshRateRatio.value);
            if (refresh <= 0) refresh = 60;
            resolutionValueText.text = $"{res.width} X {res.height} ({refresh}HZ)";
        }
    }

    private void ApplyDisplayMode(int index)
    {
        switch (index)
        {
            case 0: Screen.fullScreenMode = FullScreenMode.FullScreenWindow; break;
            case 1: Screen.fullScreenMode = FullScreenMode.ExclusiveFullScreen; break;
            case 2: Screen.fullScreenMode = FullScreenMode.Windowed; break;
        }

        if (displayModeValueText != null)
        {
            displayModeValueText.text = displayModes[index];
        }
    }

    #endregion

    #region Exit Confirmation Modal

    private void UpdateExitConfirmInput()
    {
        if (Input.GetKeyDown(KeyCode.LeftArrow) || Input.GetKeyDown(KeyCode.A))
        {
            exitModalIndex = 0; // Confirm
            PlaySound(navSound);
            UpdateExitModalVisuals();
        }
        else if (Input.GetKeyDown(KeyCode.RightArrow) || Input.GetKeyDown(KeyCode.D))
        {
            exitModalIndex = 1; // Cancel
            PlaySound(navSound);
            UpdateExitModalVisuals();
        }

        if (Input.GetKeyDown(KeyCode.Return) || Input.GetKeyDown(KeyCode.KeypadEnter) || Input.GetKeyDown(KeyCode.Space))
        {
            PunchConfirmBadge();
            if (exitModalIndex == 0) OnConfirmExitClicked();
            else OnCancelExitClicked();
        }

        if (Input.GetKeyDown(KeyCode.Escape))
        {
            PunchBackBadge();
            OnCancelExitClicked();
        }
    }

    public void SelectExitModalOption(int index)
    {
        if (exitModalIndex != index)
        {
            exitModalIndex = index;
            PlaySound(navSound);
            UpdateExitModalVisuals();
        }
    }

    private void UpdateExitModalVisuals(bool snap = false)
    {
        SetOptionVisualState(confirmExitOption, exitModalIndex == 0, snap);
        SetOptionVisualState(cancelExitOption, exitModalIndex == 1, snap);
    }

    public void OnConfirmExitClicked()
    {
        PlaySound(confirmSound);
        PunchConfirmBadge();
        Debug.Log("[MainMenuUIController] Quitting Application...");
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }

    public void OnCancelExitClicked()
    {
        PunchBackBadge();
        PlaySound(backSound);
        SetMenuState(MenuState.MainMenu, false);
    }

    #endregion

    #region Badges Punch Feedback

    public void PunchConfirmBadge()
    {
        confirmBadgeScale = 0.88f;
    }

    public void PunchBackBadge()
    {
        backBadgeScale = 0.88f;
    }

    #endregion

    #region Button & Slider Listeners

    private void SetupButtonListeners()
    {
        if (startButton != null)
        {
            startButton.onClick.AddListener(OnStartClicked);
            AddHoverTrigger(startButton.gameObject, () => SelectMainMenuButton(0));
        }

        if (settingsButton != null)
        {
            settingsButton.onClick.AddListener(OnSettingsClicked);
            AddHoverTrigger(settingsButton.gameObject, () => SelectMainMenuButton(1));
        }

        if (exitButton != null)
        {
            exitButton.onClick.AddListener(OnExitClicked);
            AddHoverTrigger(exitButton.gameObject, () => SelectMainMenuButton(2));
        }

        if (settingsBackButton != null)
        {
            settingsBackButton.onClick.AddListener(OnSettingsBackClicked);
            AddHoverTrigger(settingsBackButton.gameObject, () => SelectSettingsItem(5));
        }

        if (confirmExitButton != null)
        {
            confirmExitButton.onClick.AddListener(OnConfirmExitClicked);
            AddHoverTrigger(confirmExitButton.gameObject, () => SelectExitModalOption(0));
        }

        if (cancelExitButton != null)
        {
            cancelExitButton.onClick.AddListener(OnCancelExitClicked);
            AddHoverTrigger(cancelExitButton.gameObject, () => SelectExitModalOption(1));
        }

        // Add arrow button clicks for rows 0 to 4
        HookArrowClick(masterArrowLeft, () => { PunchCurrentRowArrow(-1); AdjustSettingsValue(-1); });
        HookArrowClick(masterArrowRight, () => { PunchCurrentRowArrow(1); AdjustSettingsValue(1); });
        HookArrowClick(bgmArrowLeft, () => { PunchCurrentRowArrow(-1); AdjustSettingsValue(-1); });
        HookArrowClick(bgmArrowRight, () => { PunchCurrentRowArrow(1); AdjustSettingsValue(1); });
        HookArrowClick(sfxArrowLeft, () => { PunchCurrentRowArrow(-1); AdjustSettingsValue(-1); });
        HookArrowClick(sfxArrowRight, () => { PunchCurrentRowArrow(1); AdjustSettingsValue(1); });
        HookArrowClick(resolutionArrowLeft, () => { PunchCurrentRowArrow(-1); CycleResolution(-1); });
        HookArrowClick(resolutionArrowRight, () => { PunchCurrentRowArrow(1); CycleResolution(1); });
        HookArrowClick(displayModeArrowLeft, () => { PunchCurrentRowArrow(-1); CycleDisplayMode(-1); });
        HookArrowClick(displayModeArrowRight, () => { PunchCurrentRowArrow(1); CycleDisplayMode(1); });
    }

    private void HookArrowClick(GameObject arrowGO, Action onClick)
    {
        if (arrowGO == null) return;
        Button btn = arrowGO.GetComponent<Button>();
        if (btn == null) btn = arrowGO.AddComponent<Button>();
        btn.onClick.AddListener(() => onClick?.Invoke());
    }

    private void SetupSliderListeners()
    {
        if (masterVolumeSlider != null)
        {
            masterVolumeSlider.onValueChanged.AddListener(val =>
            {
                AudioListener.volume = val;
                PlayerPrefs.SetFloat("MasterVolume", val);
            });
            AddHoverTrigger(masterVolumeSlider.gameObject, () => SelectSettingsItem(0));
        }

        if (bgmVolumeSlider != null)
        {
            bgmVolumeSlider.onValueChanged.AddListener(val =>
            {
                PlayerPrefs.SetFloat("BGMVolume", val);
            });
            AddHoverTrigger(bgmVolumeSlider.gameObject, () => SelectSettingsItem(1));
        }

        if (sfxVolumeSlider != null)
        {
            sfxVolumeSlider.onValueChanged.AddListener(val =>
            {
                PlayerPrefs.SetFloat("SFXVolume", val);
            });
            AddHoverTrigger(sfxVolumeSlider.gameObject, () => SelectSettingsItem(2));
        }
    }

    private void AddHoverTrigger(GameObject target, Action onHover)
    {
        EventTrigger trigger = target.GetComponent<EventTrigger>();
        if (trigger == null) trigger = target.AddComponent<EventTrigger>();

        EventTrigger.Entry entry = new EventTrigger.Entry();
        entry.eventID = EventTriggerType.PointerEnter;
        entry.callback.AddListener((eventData) => onHover?.Invoke());
        trigger.triggers.Add(entry);
    }

    private void InitResolutions()
    {
        availableResolutions.Clear();
        Resolution[] screenRes = Screen.resolutions;
        if (screenRes != null && screenRes.Length > 0)
        {
            foreach (var r in screenRes)
            {
                if (r.width >= 1280 && r.height >= 720)
                {
                    availableResolutions.Add(r);
                }
            }
        }

        if (availableResolutions.Count == 0)
        {
            availableResolutions.Add(new Resolution { width = 1920, height = 1080, refreshRateRatio = new RefreshRate { numerator = 144, denominator = 1 } });
            availableResolutions.Add(new Resolution { width = 1920, height = 1080, refreshRateRatio = new RefreshRate { numerator = 60, denominator = 1 } });
            availableResolutions.Add(new Resolution { width = 1600, height = 900, refreshRateRatio = new RefreshRate { numerator = 60, denominator = 1 } });
            availableResolutions.Add(new Resolution { width = 1280, height = 720, refreshRateRatio = new RefreshRate { numerator = 60, denominator = 1 } });
        }

        currentResolutionIndex = availableResolutions.Count - 1;
        for (int i = 0; i < availableResolutions.Count; i++)
        {
            if (availableResolutions[i].width == Screen.currentResolution.width &&
                availableResolutions[i].height == Screen.currentResolution.height)
            {
                currentResolutionIndex = i;
                break;
            }
        }

        UpdateResolutionText(availableResolutions[currentResolutionIndex]);

        switch (Screen.fullScreenMode)
        {
            case FullScreenMode.ExclusiveFullScreen: currentDisplayModeIndex = 1; break;
            case FullScreenMode.Windowed: currentDisplayModeIndex = 2; break;
            default: currentDisplayModeIndex = 0; break;
        }

        if (displayModeValueText != null)
        {
            displayModeValueText.text = displayModes[currentDisplayModeIndex];
        }
    }

    private void LoadPreferences()
    {
        float master = PlayerPrefs.GetFloat("MasterVolume", 1.0f);
        float bgm = PlayerPrefs.GetFloat("BGMVolume", 0.8f);
        float sfx = PlayerPrefs.GetFloat("SFXVolume", 0.8f);

        if (masterVolumeSlider != null) masterVolumeSlider.value = master;
        if (bgmVolumeSlider != null) bgmVolumeSlider.value = bgm;
        if (sfxVolumeSlider != null) sfxVolumeSlider.value = sfx;

        AudioListener.volume = master;
    }

    private void SavePreferences()
    {
        PlayerPrefs.Save();
    }

    #endregion

    #region Procedural Audio Synthesis

    private void SetupAudio()
    {
        if (!enableProceduralSfx) return;

        audioSource = gameObject.GetComponent<AudioSource>();
        if (audioSource == null) audioSource = gameObject.AddComponent<AudioSource>();

        audioSource.playOnAwake = false;
        audioSource.spatialBlend = 0f;

        navSound = GenerateToneClip("NavSound", 950f, 0.035f, 0.22f);
        confirmSound = GenerateChimeClip("ConfirmSound", 587.33f, 880f, 0.12f, 0.40f);
        backSound = GenerateToneClip("BackSound", 420f, 0.06f, 0.30f);
        nudgeSound = GenerateToneClip("NudgeSound", 1100f, 0.025f, 0.18f);
    }

    private void PlaySound(AudioClip clip)
    {
        if (enableProceduralSfx && audioSource != null && clip != null)
        {
            audioSource.PlayOneShot(clip);
        }
    }

    private AudioClip GenerateToneClip(string name, float frequency, float duration, float volume)
    {
        int sampleRate = 44100;
        int sampleCount = Mathf.CeilToInt(sampleRate * duration);
        float[] samples = new float[sampleCount];

        for (int i = 0; i < sampleCount; i++)
        {
            float t = (float)i / sampleRate;
            float envelope = Mathf.Exp(-t * (1f / duration) * 5f);
            samples[i] = Mathf.Sin(2f * Mathf.PI * frequency * t) * envelope * volume;
        }

        AudioClip clip = AudioClip.Create(name, sampleCount, 1, sampleRate, false);
        clip.SetData(samples, 0);
        return clip;
    }

    private AudioClip GenerateChimeClip(string name, float freq1, float freq2, float duration, float volume)
    {
        int sampleRate = 44100;
        int sampleCount = Mathf.CeilToInt(sampleRate * duration);
        float[] samples = new float[sampleCount];

        for (int i = 0; i < sampleCount; i++)
        {
            float t = (float)i / sampleRate;
            float envelope = Mathf.Exp(-t * (1f / duration) * 4f);
            float freq = (t < duration * 0.4f) ? freq1 : freq2;
            samples[i] = Mathf.Sin(2f * Mathf.PI * freq * t) * envelope * volume;
        }

        AudioClip clip = AudioClip.Create(name, sampleCount, 1, sampleRate, false);
        clip.SetData(samples, 0);
        return clip;
    }

    #endregion
}
