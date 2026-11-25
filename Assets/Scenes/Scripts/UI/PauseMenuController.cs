using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using TMPro;

/// <summary>
/// ควบคุมเมนู Pause (ปุ่ม ESC) ในเกม Rootbound Guardian
/// จัดการการหยุด/เล่นต่อของเกม สถานะ Cursor การรีสตาร์ทฉาก กลับหน้าเมนู และการตั้งค่าเสียงแบบเรียลไทม์
/// </summary>
public class PauseMenuController : MonoBehaviour
{
    public static PauseMenuController Instance { get; private set; }

    [Header("UI Panels")]
    [Tooltip("พาเนลหลักของเมนู Pause")]
    public GameObject pausePanel;
    public CanvasGroup pauseCanvasGroup;
    public RectTransform modalContainer;

    [Header("Action Buttons")]
    public Button resumeButton;
    public Button restartButton;
    public Button mainMenuButton;

    [Header("Audio Settings Sliders")]
    public Slider masterVolumeSlider;
    public TextMeshProUGUI masterVolumeText;
    public Slider bgmVolumeSlider;
    public TextMeshProUGUI bgmVolumeText;
    public Slider sfxVolumeSlider;
    public TextMeshProUGUI sfxVolumeText;

    [Header("Audio Feedback")]
    public AudioSource audioSource;
    public AudioClip pauseClip;
    public AudioClip resumeClip;

    [Header("Key Controls")]
    public KeyCode pauseKey = KeyCode.Escape;

    [Header("State")]
    public bool isPaused = false;

    private const float SliderStepAmount = 0.05f;

    private CursorLockMode previousLockMode = CursorLockMode.None;
    private bool previousCursorVisible = true;

    private AudioClip proceduralPauseClip;
    private AudioClip proceduralResumeClip;
    private AudioClip proceduralNavClip;

    private Button[] actionButtons;
    private int currentButtonIndex = 0;
    private int selectedRow = 3; // 0=Master, 1=BGM, 2=SFX, 3=Bottom Buttons
    private int heldHorizDir = 0;
    private float horizRepeatTimer = 0f;
    private const float RepeatInitialDelay = 0.32f;
    private const float RepeatInterval = 0.06f;

    private static readonly Color DefaultInkColor = new Color(0.259f, 0.165f, 0.106f, 1f);
    private static readonly Color HighlightInkColor = new Color(0.68f, 0.28f, 0.04f, 1f);

    private readonly Dictionary<AudioSource, float> initialSourceVolumes = new Dictionary<AudioSource, float>();

    private void Awake()
    {
        // ตั้งค่า Singleton และทำลาย GameObject ซ้ำหากมี Instance อยู่แล้ว
        if (Instance == null)
        {
            Instance = this;
        }
        else if (Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        FindUIReferences();
        SetupAudioClips();
        LoadAudioPreferences();
        BindButtonEvents();
        EnsureEventSystem();

        // ซ่อนหน้า Pause ตอนเริ่มซีน
        if (pausePanel != null)
        {
            pausePanel.SetActive(false);
        }
    }

    private void OnEnable()
    {
        LocalizationManager.OnLanguageChanged += OnLanguageChanged;
        UpdateLocalizedTexts();
    }

    private void OnDisable()
    {
        LocalizationManager.OnLanguageChanged -= OnLanguageChanged;
    }

    private void OnLanguageChanged(LocalizationManager.Language lang)
    {
        UpdateLocalizedTexts();
    }

    public void UpdateLocalizedTexts()
    {
        if (LocalizationManager.Instance == null) return;
        FindUIReferences();
        if (pausePanel == null) return;

        Transform innerCard = pausePanel.transform.Find("ModalContainer/InnerCard");
        if (innerCard == null) innerCard = pausePanel.transform.Find("ModalContainer");
        if (innerCard == null) return;

        SetTMPText(innerCard, "BadgeText", "Badge/BadgeText", "pause_badge");
        SetTMPText(innerCard, "TitleText", null, "pause_title");
        SetTMPText(innerCard, "SubtitleText", null, "pause_subtitle");
        SetTMPText(innerCard, "LoreBubble/Text", "SafeBanner/BannerText", "pause_safe_notice");
        SetTMPText(innerCard, "AudioSettingsSection/Header", "AudioSettingsSection/Heading/HeadingText", "pause_audio_heading");
        SetTMPText(innerCard, "AudioSettingsSection/MasterRow/Label", null, "label_master");
        SetTMPText(innerCard, "AudioSettingsSection/BgmRow/Label", null, "label_bgm");
        SetTMPText(innerCard, "AudioSettingsSection/SfxRow/Label", null, "label_sfx");
        SetButtonLabel(innerCard, resumeButton, "ButtonRow/ResumeButton/Content/Label", "ButtonRow/ResumeButton/Button_Content/Label", "pause_resume");
        SetButtonLabel(innerCard, restartButton, "ButtonRow/RestartButton/Content/Label", "ButtonRow/RestartButton/Button_Content/Label", "pause_restart");
        SetButtonLabel(innerCard, mainMenuButton, "ButtonRow/MainMenuButton/Content/Label", "ButtonRow/MainMenuButton/Button_Content/Label", "pause_main_menu");
        SetTMPText(innerCard, "KeyHintsBar/Text", "KeyHintsBar/KeyHintsText", "pause_key_hints");
    }

    private void SetTMPText(Transform root, string primaryPath, string fallbackPath, string locKey)
    {
        Transform t = root.Find(primaryPath);
        if (t == null && !string.IsNullOrEmpty(fallbackPath)) t = root.Find(fallbackPath);
        if (t != null)
        {
            var tmp = t.GetComponent<TextMeshProUGUI>();
            if (tmp != null) tmp.text = LocalizationManager.Instance.Get(locKey);
        }
    }

    private void SetButtonLabel(Transform root, Button btn, string primaryPath, string fallbackPath, string locKey)
    {
        Transform t = root.Find(primaryPath);
        if (t == null && !string.IsNullOrEmpty(fallbackPath)) t = root.Find(fallbackPath);
        TextMeshProUGUI tmp = t != null ? t.GetComponent<TextMeshProUGUI>() : null;
        if (tmp == null && btn != null) tmp = btn.GetComponentInChildren<TextMeshProUGUI>(true);
        if (tmp != null) tmp.text = LocalizationManager.Instance.Get(locKey);
    }

    private void Update()
    {
        if (isPaused)
        {
            // กด ESC ซ้ำเพื่อปิดเมนู Pause และเล่นต่อ
            if (Input.GetKeyDown(pauseKey))
            {
                Resume();
                return;
            }

            // เลื่อนขึ้น/ลงระหว่างแถวเสียง (Master/BGM/SFX) กับแถวปุ่มด้านล่าง
            if (Input.GetKeyDown(KeyCode.UpArrow) || Input.GetKeyDown(KeyCode.W))
            {
                MoveRow(-1);
            }
            else if (Input.GetKeyDown(KeyCode.DownArrow) || Input.GetKeyDown(KeyCode.S))
            {
                MoveRow(1);
            }
            else if (Input.GetKeyDown(KeyCode.Tab))
            {
                int dir = (Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift)) ? -1 : 1;
                if (selectedRow < 3) MoveRow(dir);
                else Navigate(dir);
            }

            if (selectedRow < 3)
            {
                HandleSliderRowKeyboard();

                if (Input.GetKeyDown(KeyCode.Return) || Input.GetKeyDown(KeyCode.KeypadEnter) || Input.GetKeyDown(KeyCode.Space))
                {
                    MoveRow(1);
                }
            }
            else
            {
                heldHorizDir = 0;
                // เลื่อนปุ่มเมนูด้วยปุ่มทิศทาง A/D หรือ Left/Right
                if (Input.GetKeyDown(KeyCode.LeftArrow) || Input.GetKeyDown(KeyCode.A))
                {
                    Navigate(-1);
                }
                else if (Input.GetKeyDown(KeyCode.RightArrow) || Input.GetKeyDown(KeyCode.D))
                {
                    Navigate(1);
                }

                // กดยืนยันปุ่มที่เลือกอยู่
                if (Input.GetKeyDown(KeyCode.Return) || Input.GetKeyDown(KeyCode.KeypadEnter) || Input.GetKeyDown(KeyCode.Space) || Input.GetKeyDown(KeyCode.JoystickButton0))
                {
                    SubmitSelectedButton();
                }
            }
        }
        else
        {
            heldHorizDir = 0;
            if (Input.GetKeyDown(pauseKey))
            {
                Pause();
            }
        }
    }

    private void HandleSliderRowKeyboard()
    {
        int pressDir = 0;
        if (Input.GetKeyDown(KeyCode.LeftArrow) || Input.GetKeyDown(KeyCode.A)) pressDir = -1;
        else if (Input.GetKeyDown(KeyCode.RightArrow) || Input.GetKeyDown(KeyCode.D)) pressDir = 1;

        if (pressDir != 0)
        {
            AdjustSelectedSlider(pressDir);
            heldHorizDir = pressDir;
            horizRepeatTimer = RepeatInitialDelay;
            return;
        }

        bool leftHeld = Input.GetKey(KeyCode.LeftArrow) || Input.GetKey(KeyCode.A);
        bool rightHeld = Input.GetKey(KeyCode.RightArrow) || Input.GetKey(KeyCode.D);
        int holdDir = (leftHeld && !rightHeld) ? -1 : ((rightHeld && !leftHeld) ? 1 : 0);

        if (holdDir == 0 || holdDir != heldHorizDir)
        {
            heldHorizDir = holdDir;
            horizRepeatTimer = RepeatInitialDelay;
            return;
        }

        horizRepeatTimer -= Time.unscaledDeltaTime;
        if (horizRepeatTimer <= 0f)
        {
            AdjustSelectedSlider(holdDir);
            horizRepeatTimer = RepeatInterval;
        }
    }

    private void AdjustSelectedSlider(int direction)
    {
        Slider target = selectedRow == 0 ? masterVolumeSlider : (selectedRow == 1 ? bgmVolumeSlider : sfxVolumeSlider);
        if (target == null) return;
        target.value = Mathf.Clamp(target.value + direction * SliderStepAmount, target.minValue, target.maxValue);
        PlayAudio(proceduralNavClip);
    }

    private void MoveRow(int direction)
    {
        selectedRow = (selectedRow + direction + 4) % 4;
        heldHorizDir = 0;
        SelectRow(selectedRow, true);
    }

    private void SelectRow(int row, bool playSound)
    {
        selectedRow = Mathf.Clamp(row, 0, 3);
        UpdateSliderRowsVisuals();

        if (selectedRow == 3)
        {
            SelectButton(currentButtonIndex, playSound);
        }
        else
        {
            if (actionButtons != null)
            {
                for (int i = 0; i < actionButtons.Length; i++)
                {
                    if (actionButtons[i] == null) continue;
                    var juicy = actionButtons[i].GetComponent<UIJuicyButton>();
                    if (juicy != null) juicy.TriggerVisualSelection(false);
                }
            }
            if (UnityEngine.EventSystems.EventSystem.current != null)
            {
                UnityEngine.EventSystems.EventSystem.current.SetSelectedGameObject(null);
            }
            if (playSound) PlayAudio(proceduralNavClip);
        }
    }

    private void UpdateSliderRowsVisuals()
    {
        Transform audioSection = FindAudioSettingsSection();
        if (audioSection == null) return;

        string[] rowNames = { "MasterRow", "BgmRow", "SfxRow" };
        for (int i = 0; i < rowNames.Length; i++)
        {
            Transform row = audioSection.Find(rowNames[i]);
            if (row == null) continue;
            bool active = (selectedRow == i);

            Transform lbl = row.Find("Label");
            if (lbl != null)
            {
                lbl.localScale = Vector3.one * (active ? 1.08f : 1.0f);
                var tmp = lbl.GetComponent<TextMeshProUGUI>();
                if (tmp != null) tmp.color = active ? HighlightInkColor : DefaultInkColor;
            }

            Transform val = row.Find("ValueText");
            if (val != null)
            {
                var tmp = val.GetComponent<TextMeshProUGUI>();
                if (tmp != null) tmp.color = active ? HighlightInkColor : DefaultInkColor;
            }

            Transform handle = row.Find("Slider/Handle Slide Area/Handle");
            if (handle != null)
            {
                handle.localScale = Vector3.one * (active ? 1.24f : 1.0f);
            }

            foreach (string arrowName in new[] { "ArrowLeft", "ArrowRight" })
            {
                Transform arrow = row.Find(arrowName);
                if (arrow == null) continue;
                float dirX = arrowName == "ArrowLeft" ? -1f : 1f;
                float scaleFactor = active ? 1.18f : 1.0f;
                Color iconColor = active ? new Color(1.12f, 0.92f, 0.55f, 1f) : Color.white;

                Transform iconTf = arrow.Find("Icon");
                if (iconTf != null)
                {
                    arrow.localScale = Vector3.one;
                    var hitImg = arrow.GetComponent<Image>();
                    if (hitImg != null) hitImg.color = new Color(1f, 1f, 1f, 0f);

                    var iconImg = iconTf.GetComponent<Image>();
                    if (iconImg != null) iconImg.color = iconColor;
                    iconTf.localScale = new Vector3(dirX, 1f, 1f) * scaleFactor;
                }
                else
                {
                    var img = arrow.GetComponent<Image>();
                    if (img != null && img.sprite != null) img.color = iconColor;
                    arrow.localScale = new Vector3(dirX, 1f, 1f) * scaleFactor;
                }
            }
        }
    }

    public void TogglePause()
    {
        if (isPaused) Resume();
        else Pause();
    }

    public void Pause()
    {
        if (isPaused) return;

        // ไม่เปิด Pause หากชนะไปแล้วหรืออยู่ในหน้า Game Over
        if (WinUIManager.Instance != null && WinUIManager.Instance.isGameWon) return;
        if (GameOverUIManager.Instance != null && GameOverUIManager.Instance.gameOverPanel != null && GameOverUIManager.Instance.gameOverPanel.activeSelf) return;

        isPaused = true;
        previousLockMode = Cursor.lockState;
        previousCursorVisible = Cursor.visible;

        // หยุดเวลาของเกมและแสดงเมาส์
        Time.timeScale = 0f;
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;

        FindUIReferences();
        LoadAudioPreferences();
        BindButtonEvents();
        UpdateLocalizedTexts();
        SetupNavigation();

        if (pausePanel != null)
        {
            pausePanel.transform.SetAsLastSibling();
            pausePanel.SetActive(true);
        }

        if (pauseCanvasGroup != null)
        {
            pauseCanvasGroup.alpha = 1f;
            pauseCanvasGroup.interactable = true;
            pauseCanvasGroup.blocksRaycasts = true;
        }

        currentButtonIndex = 0;
        selectedRow = 3;
        SelectRow(3, false);
        PlayAudio(pauseClip != null ? pauseClip : proceduralPauseClip);
    }

    private void SetupNavigation()
    {
        actionButtons = new Button[] { resumeButton, restartButton, mainMenuButton };

        for (int i = 0; i < actionButtons.Length; i++)
        {
            Button btn = actionButtons[i];
            if (btn == null) continue;

            int index = i;
            var juicy = btn.GetComponent<UIJuicyButton>();
            if (juicy != null)
            {
                juicy.onHoverOrSelect = (jb) =>
                {
                    selectedRow = 3;
                    currentButtonIndex = index;
                    UpdateSliderRowsVisuals();
                };
            }

            Navigation nav = new Navigation { mode = Navigation.Mode.None };
            btn.navigation = nav;
        }

        Slider[] sliders = { masterVolumeSlider, bgmVolumeSlider, sfxVolumeSlider };
        for (int i = 0; i < sliders.Length; i++)
        {
            if (sliders[i] == null) continue;
            sliders[i].navigation = new Navigation { mode = Navigation.Mode.None };
        }

        Transform audioSection = FindAudioSettingsSection();
        if (audioSection != null)
        {
            string[] rowNames = { "MasterRow", "BgmRow", "SfxRow" };
            for (int r = 0; r < rowNames.Length; r++)
            {
                Transform row = audioSection.Find(rowNames[r]);
                if (row == null) continue;
                int rowIdx = r;
                var trigger = row.gameObject.GetComponent<UnityEngine.EventSystems.EventTrigger>();
                if (trigger == null) trigger = row.gameObject.AddComponent<UnityEngine.EventSystems.EventTrigger>();
                trigger.triggers.Clear();
                var entry = new UnityEngine.EventSystems.EventTrigger.Entry { eventID = UnityEngine.EventSystems.EventTriggerType.PointerEnter };
                entry.callback.AddListener(_ => { if (selectedRow != rowIdx) SelectRow(rowIdx, false); });
                trigger.triggers.Add(entry);
            }
        }
    }

    private void Navigate(int direction)
    {
        if (actionButtons == null || actionButtons.Length == 0) return;
        currentButtonIndex = (currentButtonIndex + direction + actionButtons.Length) % actionButtons.Length;
        SelectButton(currentButtonIndex, true);
    }

    public void SelectButton(int index, bool playSound = true)
    {
        if (actionButtons == null || actionButtons.Length == 0) return;
        currentButtonIndex = Mathf.Clamp(index, 0, actionButtons.Length - 1);

        for (int i = 0; i < actionButtons.Length; i++)
        {
            Button btn = actionButtons[i];
            if (btn == null) continue;
            var juicy = btn.GetComponent<UIJuicyButton>();
            if (juicy != null)
            {
                juicy.TriggerVisualSelection(i == currentButtonIndex);
            }
        }

        if (actionButtons[currentButtonIndex] != null && UnityEngine.EventSystems.EventSystem.current != null)
        {
            UnityEngine.EventSystems.EventSystem.current.SetSelectedGameObject(actionButtons[currentButtonIndex].gameObject);
        }

        if (playSound)
        {
            PlayAudio(proceduralNavClip);
        }
    }

    private void SubmitSelectedButton()
    {
        if (actionButtons == null || actionButtons.Length == 0) return;
        if (currentButtonIndex >= 0 && currentButtonIndex < actionButtons.Length)
        {
            Button btn = actionButtons[currentButtonIndex];
            if (btn != null && btn.interactable)
            {
                if (UnityEngine.EventSystems.EventSystem.current == null ||
                    UnityEngine.EventSystems.EventSystem.current.currentSelectedGameObject != btn.gameObject)
                {
                    btn.onClick.Invoke();
                }
            }
        }
    }

    public void Resume()
    {
        if (!isPaused) return;

        isPaused = false;
        Time.timeScale = 1f;

        Cursor.lockState = previousLockMode;
        Cursor.visible = previousCursorVisible;

        if (pausePanel != null) pausePanel.SetActive(false);
        if (pauseCanvasGroup != null)
        {
            pauseCanvasGroup.alpha = 0f;
            pauseCanvasGroup.interactable = false;
            pauseCanvasGroup.blocksRaycasts = false;
        }

        PlayerPrefs.Save();
        PlayAudio(resumeClip != null ? resumeClip : proceduralResumeClip);
    }

    /// <summary>
    /// กดเล่นใหม่จากหน้า Pause: นำค่าที่เซฟไว้กลับมาใช้และตั้งค่าให้เล่น Tutorial เมื่อโหลดซีนใหม่
    /// </summary>
    public void RestartLevel()
    {
        Time.timeScale = 1f;
        isPaused = false;
        PlayerPrefs.Save();

        if (pausePanel != null) pausePanel.SetActive(false);
        if (pauseCanvasGroup != null)
        {
            pauseCanvasGroup.alpha = 0f;
            pauseCanvasGroup.interactable = false;
            pauseCanvasGroup.blocksRaycasts = false;
        }

        if (CheckpointManager.Instance != null)
        {
            CheckpointManager.Instance.RestartFromPauseWithSavedCheckpoint();
        }

        Scene currentScene = SceneManager.GetActiveScene();
        SceneManager.LoadScene(currentScene.buildIndex);
    }

    public void GoToMainMenu()
    {
        Time.timeScale = 1f;
        isPaused = false;
        PlayerPrefs.Save();

        if (CheckpointManager.Instance != null)
        {
            CheckpointManager.Instance.ResetRunState();
        }

        if (Application.CanStreamedLevelBeLoaded("MainScreen"))
        {
            SceneManager.LoadScene("MainScreen");
        }
        else
        {
            SceneManager.LoadScene(0);
        }
    }

    #region Audio Settings Handling

    private void LoadAudioPreferences()
    {
        float master = PlayerPrefs.GetFloat("MasterVolume", 1.0f);
        float bgm = PlayerPrefs.GetFloat("BGMVolume", 0.8f);
        float sfx = PlayerPrefs.GetFloat("SFXVolume", 0.8f);

        BindVolumeSlider(masterVolumeSlider, master, masterVolumeText, OnMasterVolumeChanged);
        BindVolumeSlider(bgmVolumeSlider, bgm, bgmVolumeText, OnBGMVolumeChanged);
        BindVolumeSlider(sfxVolumeSlider, sfx, sfxVolumeText, OnSFXVolumeChanged);

        ApplyVolumesToSceneAudio(master, bgm, sfx);
    }

    private void BindVolumeSlider(Slider slider, float value, TextMeshProUGUI label, UnityEngine.Events.UnityAction<float> callback)
    {
        if (slider != null)
        {
            slider.onValueChanged.RemoveAllListeners();
            slider.value = value;
            slider.onValueChanged.AddListener(callback);
        }
        UpdateSliderLabel(label, value);
    }

    public void OnMasterVolumeChanged(float val)
    {
        PlayerPrefs.SetFloat("MasterVolume", val);
        PlayerPrefs.Save();
        UpdateSliderLabel(masterVolumeText, val);
        ApplyVolumesToSceneAudio(val, GetCurrentBgmVolume(), GetCurrentSfxVolume());
    }

    public void OnBGMVolumeChanged(float val)
    {
        PlayerPrefs.SetFloat("BGMVolume", val);
        PlayerPrefs.Save();
        UpdateSliderLabel(bgmVolumeText, val);
        ApplyVolumesToSceneAudio(GetCurrentMasterVolume(), val, GetCurrentSfxVolume());
    }

    public void OnSFXVolumeChanged(float val)
    {
        PlayerPrefs.SetFloat("SFXVolume", val);
        PlayerPrefs.Save();
        UpdateSliderLabel(sfxVolumeText, val);
        ApplyVolumesToSceneAudio(GetCurrentMasterVolume(), GetCurrentBgmVolume(), val);
    }

    private float GetCurrentMasterVolume()
    {
        return masterVolumeSlider != null ? masterVolumeSlider.value : PlayerPrefs.GetFloat("MasterVolume", 1.0f);
    }

    private float GetCurrentBgmVolume()
    {
        return bgmVolumeSlider != null ? bgmVolumeSlider.value : PlayerPrefs.GetFloat("BGMVolume", 0.8f);
    }

    private float GetCurrentSfxVolume()
    {
        return sfxVolumeSlider != null ? sfxVolumeSlider.value : PlayerPrefs.GetFloat("SFXVolume", 0.8f);
    }

    private void ApplyVolumesToSceneAudio(float master, float bgm, float sfx)
    {
        AudioListener.volume = master;

        if (AudioManager.Instance != null)
        {
            AudioManager.Instance.UpdateBGMVolume(bgm);
            AudioManager.Instance.UpdateSFXVolume(sfx);
        }

        AudioSource[] allSources = FindObjectsByType<AudioSource>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        for (int i = 0; i < allSources.Length; i++)
        {
            AudioSource source = allSources[i];
            if (source == null) continue;

            if (!initialSourceVolumes.TryGetValue(source, out float baseVol))
            {
                baseVol = source.volume;
                initialSourceVolumes[source] = baseVol;
            }

            if (source == audioSource)
            {
                source.volume = baseVol * sfx;
                continue;
            }

            string goName = source.gameObject.name;
            bool isBgm = goName.IndexOf("BGM", System.StringComparison.OrdinalIgnoreCase) >= 0
                      || goName.IndexOf("Music", System.StringComparison.OrdinalIgnoreCase) >= 0;

            if (isBgm)
            {
                source.volume = (baseVol <= 0.001f ? 0.5f : baseVol) * bgm;
            }
            else
            {
                source.volume = baseVol * sfx;
            }
        }
    }

    private void UpdateSliderLabel(TextMeshProUGUI label, float val)
    {
        if (label != null)
        {
            int percentage = Mathf.RoundToInt(val * 100f);
            label.text = $"{percentage}%";
        }
    }

    #endregion

    #region Internal Helpers

    private Transform FindAudioSettingsSection()
    {
        if (pausePanel == null) return null;
        Transform innerCard = pausePanel.transform.Find("ModalContainer/InnerCard");
        if (innerCard == null) innerCard = pausePanel.transform.Find("ModalContainer");
        return innerCard != null ? innerCard.Find("AudioSettingsSection") : null;
    }

    private void FindUIReferences()
    {
        if (pausePanel == null)
        {
            Transform t = transform.Find("PauseMenuPanel");
            if (t == null) t = transform.Find("PausePanel");
            if (t != null) pausePanel = t.gameObject;
        }

        if (pausePanel != null)
        {
            if (pauseCanvasGroup == null)
            {
                pauseCanvasGroup = pausePanel.GetComponent<CanvasGroup>();
                if (pauseCanvasGroup == null) pauseCanvasGroup = pausePanel.AddComponent<CanvasGroup>();
            }

            if (modalContainer == null)
            {
                Transform m = pausePanel.transform.Find("ModalContainer");
                if (m != null) modalContainer = m.GetComponent<RectTransform>();
            }

            Transform innerCard = pausePanel.transform.Find("ModalContainer/InnerCard");
            if (innerCard == null) innerCard = pausePanel.transform.Find("ModalContainer");

            if (innerCard != null)
            {
                if (resumeButton == null)
                {
                    Transform b = innerCard.Find("ButtonRow/ResumeButton");
                    if (b != null) resumeButton = b.GetComponent<Button>();
                }
                if (restartButton == null)
                {
                    Transform b = innerCard.Find("ButtonRow/RestartButton");
                    if (b != null) restartButton = b.GetComponent<Button>();
                }
                if (mainMenuButton == null)
                {
                    Transform b = innerCard.Find("ButtonRow/MainMenuButton");
                    if (b != null) mainMenuButton = b.GetComponent<Button>();
                }

                Transform audioSection = innerCard.Find("AudioSettingsSection");
                if (audioSection != null)
                {
                    if (masterVolumeSlider == null)
                    {
                        Transform s = audioSection.Find("MasterRow/Slider");
                        if (s != null) masterVolumeSlider = s.GetComponent<Slider>();
                    }
                    if (masterVolumeText == null)
                    {
                        Transform t = audioSection.Find("MasterRow/ValueText");
                        if (t != null) masterVolumeText = t.GetComponent<TextMeshProUGUI>();
                    }

                    if (bgmVolumeSlider == null)
                    {
                        Transform s = audioSection.Find("BgmRow/Slider");
                        if (s != null) bgmVolumeSlider = s.GetComponent<Slider>();
                    }
                    if (bgmVolumeText == null)
                    {
                        Transform t = audioSection.Find("BgmRow/ValueText");
                        if (t != null) bgmVolumeText = t.GetComponent<TextMeshProUGUI>();
                    }

                    if (sfxVolumeSlider == null)
                    {
                        Transform s = audioSection.Find("SfxRow/Slider");
                        if (s != null) sfxVolumeSlider = s.GetComponent<Slider>();
                    }
                    if (sfxVolumeText == null)
                    {
                        Transform t = audioSection.Find("SfxRow/ValueText");
                        if (t != null) sfxVolumeText = t.GetComponent<TextMeshProUGUI>();
                    }
                }
            }
        }
    }

    private void BindButtonEvents()
    {
        if (resumeButton != null)
        {
            resumeButton.onClick.RemoveAllListeners();
            resumeButton.onClick.AddListener(Resume);
        }

        if (restartButton != null)
        {
            restartButton.onClick.RemoveAllListeners();
            restartButton.onClick.AddListener(RestartLevel);
        }

        if (mainMenuButton != null)
        {
            mainMenuButton.onClick.RemoveAllListeners();
            mainMenuButton.onClick.AddListener(GoToMainMenu);
        }

        Transform audioSection = FindAudioSettingsSection();
        BindSliderStepButtons(audioSection, "MasterRow", masterVolumeSlider);
        BindSliderStepButtons(audioSection, "BgmRow", bgmVolumeSlider);
        BindSliderStepButtons(audioSection, "SfxRow", sfxVolumeSlider);
    }

    private void BindSliderStepButtons(Transform audioSection, string rowName, Slider targetSlider)
    {
        if (audioSection == null || targetSlider == null) return;
        Transform row = audioSection.Find(rowName);
        if (row == null) return;

        BindStepButton(row.Find("ArrowLeft"), targetSlider, -SliderStepAmount);
        BindStepButton(row.Find("ArrowRight"), targetSlider, SliderStepAmount);
    }

    private void BindStepButton(Transform btnTransform, Slider targetSlider, float delta)
    {
        if (btnTransform == null || targetSlider == null) return;
        Button btn = btnTransform.GetComponent<Button>();
        if (btn == null) return;

        btn.onClick.RemoveAllListeners();
        btn.onClick.AddListener(() =>
        {
            targetSlider.value = Mathf.Clamp(targetSlider.value + delta, targetSlider.minValue, targetSlider.maxValue);
            PlayAudio(proceduralNavClip);
        });
    }

    private void EnsureEventSystem()
    {
        if (FindFirstObjectByType<UnityEngine.EventSystems.EventSystem>() == null)
        {
            GameObject es = new GameObject("EventSystem");
            es.AddComponent<UnityEngine.EventSystems.EventSystem>();
            es.AddComponent<UnityEngine.EventSystems.StandaloneInputModule>();
        }
    }

    private void SetupAudioClips()
    {
        if (audioSource == null)
        {
            audioSource = GetComponent<AudioSource>();
            if (audioSource == null) audioSource = gameObject.AddComponent<AudioSource>();
        }
        audioSource.playOnAwake = false;
        audioSource.spatialBlend = 0f;

        proceduralPauseClip = GenerateChimeClip("PauseChime", 523.25f, 659.25f, 0.15f, 0.35f);
        proceduralResumeClip = GenerateToneClip("ResumeTone", 440f, 0.08f, 0.25f);
        proceduralNavClip = GenerateToneClip("PauseNavTone", 587.33f, 0.05f, 0.2f);
    }

    private void PlayAudio(AudioClip clip)
    {
        if (audioSource != null && clip != null)
        {
            audioSource.PlayOneShot(clip);
        }
    }

    private AudioClip GenerateToneClip(string clipName, float frequency, float duration, float volume)
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

        AudioClip clip = AudioClip.Create(clipName, sampleCount, 1, sampleRate, false);
        clip.SetData(samples, 0);
        return clip;
    }

    private AudioClip GenerateChimeClip(string clipName, float freq1, float freq2, float duration, float volume)
    {
        int sampleRate = 44100;
        int sampleCount = Mathf.CeilToInt(sampleRate * duration);
        float[] samples = new float[sampleCount];

        for (int i = 0; i < sampleCount; i++)
        {
            float t = (float)i / sampleRate;
            float envelope = Mathf.Exp(-t * (1f / duration) * 4f);
            float freq = (t < duration * 0.45f) ? freq1 : freq2;
            samples[i] = Mathf.Sin(2f * Mathf.PI * freq * t) * envelope * volume;
        }

        AudioClip clip = AudioClip.Create(clipName, sampleCount, 1, sampleRate, false);
        clip.SetData(samples, 0);
        return clip;
    }

    private void OnDestroy()
    {
        if (Instance == this)
        {
            Instance = null;
        }
        Time.timeScale = 1f;
    }

    #endregion
}