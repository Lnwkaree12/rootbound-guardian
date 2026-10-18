using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using TMPro;

/// <summary>
/// Controller for the in-game Pause Menu (ESC menu) in Rootbound Guardian.
/// Matches the game's cozy fairytale aesthetic and handles game pause/resume,
/// cursor state, scene reloading/main menu navigation, and real-time audio settings.
/// </summary>
public class PauseMenuController : MonoBehaviour
{
    public static PauseMenuController Instance { get; private set; }

    [Header("UI Panels")]
    [Tooltip("The root overlay panel for the pause menu.")]
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

    private CursorLockMode previousLockMode = CursorLockMode.None;
    private bool previousCursorVisible = true;

    private AudioClip proceduralPauseClip;
    private AudioClip proceduralResumeClip;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else if (Instance != this)
        {
            Destroy(this);
            return;
        }

        FindUIReferences();
        SetupAudioClips();
        LoadAudioPreferences();
        BindButtonEvents();
        EnsureEventSystem();

        if (pausePanel != null)
        {
            pausePanel.SetActive(false);
        }
    }

    private void Update()
    {
        if (Input.GetKeyDown(pauseKey))
        {
            TogglePause();
        }
    }

    public void TogglePause()
    {
        if (isPaused)
        {
            Resume();
        }
        else
        {
            Pause();
        }
    }

    public void Pause()
    {
        if (isPaused) return;

        // Do not pause if game is already won or lost
        if (WinUIManager.Instance != null && WinUIManager.Instance.isGameWon) return;
        if (GameOverUIManager.Instance != null && GameOverUIManager.Instance.gameOverPanel != null && GameOverUIManager.Instance.gameOverPanel.activeSelf) return;

        isPaused = true;
        previousLockMode = Cursor.lockState;
        previousCursorVisible = Cursor.visible;

        Time.timeScale = 0f;

        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;

        FindUIReferences();
        LoadAudioPreferences();
        BindButtonEvents();

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

        PlayAudio(pauseClip != null ? pauseClip : proceduralPauseClip);
    }

    public void Resume()
    {
        if (!isPaused) return;

        isPaused = false;
        Time.timeScale = 1f;

        Cursor.lockState = previousLockMode;
        Cursor.visible = previousCursorVisible;

        if (pausePanel != null)
        {
            pausePanel.SetActive(false);
        }

        if (pauseCanvasGroup != null)
        {
            pauseCanvasGroup.alpha = 0f;
            pauseCanvasGroup.interactable = false;
            pauseCanvasGroup.blocksRaycasts = false;
        }

        PlayerPrefs.Save();
        PlayAudio(resumeClip != null ? resumeClip : proceduralResumeClip);
    }

    public void RestartLevel()
    {
        Time.timeScale = 1f;
        isPaused = false;
        PlayerPrefs.Save();
        Scene currentScene = SceneManager.GetActiveScene();
        SceneManager.LoadScene(currentScene.buildIndex);
    }

    public void GoToMainMenu()
    {
        Time.timeScale = 1f;
        isPaused = false;
        PlayerPrefs.Save();
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

        if (masterVolumeSlider != null)
        {
            masterVolumeSlider.onValueChanged.RemoveAllListeners();
            masterVolumeSlider.value = master;
            masterVolumeSlider.onValueChanged.AddListener(OnMasterVolumeChanged);
        }
        UpdateSliderLabel(masterVolumeText, master);

        if (bgmVolumeSlider != null)
        {
            bgmVolumeSlider.onValueChanged.RemoveAllListeners();
            bgmVolumeSlider.value = bgm;
            bgmVolumeSlider.onValueChanged.AddListener(OnBGMVolumeChanged);
        }
        UpdateSliderLabel(bgmVolumeText, bgm);

        if (sfxVolumeSlider != null)
        {
            sfxVolumeSlider.onValueChanged.RemoveAllListeners();
            sfxVolumeSlider.value = sfx;
            sfxVolumeSlider.onValueChanged.AddListener(OnSFXVolumeChanged);
        }
        UpdateSliderLabel(sfxVolumeText, sfx);

        AudioListener.volume = master;
    }

    public void OnMasterVolumeChanged(float val)
    {
        AudioListener.volume = val;
        PlayerPrefs.SetFloat("MasterVolume", val);
        UpdateSliderLabel(masterVolumeText, val);
    }

    public void OnBGMVolumeChanged(float val)
    {
        PlayerPrefs.SetFloat("BGMVolume", val);
        UpdateSliderLabel(bgmVolumeText, val);
    }

    public void OnSFXVolumeChanged(float val)
    {
        PlayerPrefs.SetFloat("SFXVolume", val);
        UpdateSliderLabel(sfxVolumeText, val);
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
