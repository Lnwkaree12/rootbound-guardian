using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using TMPro;

public class GameOverUIManager : MonoBehaviour
{
    public static GameOverUIManager Instance { get; private set; }

    [Header("UI References")]
    [SerializeField] public GameObject gameOverPanel;
    [SerializeField] public CanvasGroup gameOverCanvasGroup;
    [SerializeField] public Text titleText;
    [SerializeField] public Text reasonText;

    [Header("Text Displays (TMP Supported)")]
    [SerializeField] public TextMeshProUGUI titleTextTMP;
    [SerializeField] public TextMeshProUGUI reasonTextTMP;

    [Header("Action Buttons")]
    [SerializeField] public Button restartButton;
    [SerializeField] public Button mainMenuButton;

    private bool isGameOver;
    private string currentReasonKey = "gameover_oxygen_depleted";

    private Button[] actionButtons;
    private int currentButtonIndex = 0;
    private AudioSource audioSource;
    private AudioClip navClip;

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
        BindButtonEvents();
        EnsureAudio();
        EnsureEventSystem();

        if (gameOverPanel != null)
        {
            gameOverPanel.SetActive(false);
        }
    }

    private void Update()
    {
        if (!isGameOver) return;

        if (Input.GetKeyDown(KeyCode.LeftArrow) || Input.GetKeyDown(KeyCode.A))
        {
            Navigate(-1);
        }
        else if (Input.GetKeyDown(KeyCode.RightArrow) || Input.GetKeyDown(KeyCode.D))
        {
            Navigate(1);
        }
        else if (Input.GetKeyDown(KeyCode.Tab))
        {
            int dir = (Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift)) ? -1 : 1;
            Navigate(dir);
        }

        if (Input.GetKeyDown(KeyCode.Return) || Input.GetKeyDown(KeyCode.KeypadEnter) || Input.GetKeyDown(KeyCode.Space) || Input.GetKeyDown(KeyCode.JoystickButton0))
        {
            SubmitSelectedButton();
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

    public void TriggerGameOver(string reason = "")
    {
        if (isGameOver) return;
        isGameOver = true;

        FindUIReferences();
        BindButtonEvents();

        // Detect appropriate localization key
        if (!string.IsNullOrEmpty(reason))
        {
            if (reason.Contains("ออกซิเจน") || reason.ToLower().Contains("oxygen"))
            {
                currentReasonKey = "gameover_oxygen_depleted";
            }
            else if (reason.Contains("พลังชีวิต") || reason.ToLower().Contains("health") || reason.ToLower().Contains("hp"))
            {
                currentReasonKey = "gameover_hp_depleted";
            }
            else if (reason.Contains("จุดเซฟ") || reason.ToLower().Contains("checkpoint") || reason.ToLower().Contains("save"))
            {
                currentReasonKey = "gameover_no_checkpoint";
            }
            else
            {
                currentReasonKey = reason;
            }
        }

        UpdateLocalizedTexts();

        if (gameOverPanel != null)
        {
            gameOverPanel.transform.SetAsLastSibling();
            gameOverPanel.SetActive(true);
        }

        if (gameOverCanvasGroup != null)
        {
            gameOverCanvasGroup.alpha = 1f;
            gameOverCanvasGroup.interactable = true;
            gameOverCanvasGroup.blocksRaycasts = true;
        }

        SetupNavigation();
        SelectButton(0, false);

        Cursor.visible = true;
        Cursor.lockState = CursorLockMode.None;
        Time.timeScale = 0f;
    }

    private void SetupNavigation()
    {
        actionButtons = new Button[] { restartButton, mainMenuButton };

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
                    currentButtonIndex = index;
                };
            }

            Navigation nav = new Navigation();
            nav.mode = Navigation.Mode.Explicit;
            int prev = (i - 1 + actionButtons.Length) % actionButtons.Length;
            int next = (i + 1) % actionButtons.Length;
            nav.selectOnLeft = actionButtons[prev];
            nav.selectOnRight = actionButtons[next];
            btn.navigation = nav;
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

        if (actionButtons[currentButtonIndex] != null)
        {
            if (UnityEngine.EventSystems.EventSystem.current != null)
            {
                UnityEngine.EventSystems.EventSystem.current.SetSelectedGameObject(actionButtons[currentButtonIndex].gameObject);
            }
        }

        if (playSound && audioSource != null && navClip != null)
        {
            audioSource.PlayOneShot(navClip);
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
                btn.onClick.Invoke();
            }
        }
    }

    private void EnsureAudio()
    {
        if (audioSource == null)
        {
            audioSource = GetComponent<AudioSource>();
            if (audioSource == null) audioSource = gameObject.AddComponent<AudioSource>();
        }
        audioSource.playOnAwake = false;
        audioSource.spatialBlend = 0f;

        if (navClip == null)
        {
            navClip = GenerateToneClip("GameOverNavTone", 587.33f, 0.05f, 0.2f);
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

    public void UpdateLocalizedTexts()
    {
        if (LocalizationManager.Instance == null) return;
        FindUIReferences();

        string localizedTitle = LocalizationManager.Instance.Get("gameover_title");
        string localizedReason = LocalizationManager.Instance.Get(currentReasonKey);

        if (titleTextTMP != null) titleTextTMP.text = localizedTitle;
        if (titleText != null) titleText.text = localizedTitle;

        if (reasonTextTMP != null) reasonTextTMP.text = localizedReason;
        if (reasonText != null) reasonText.text = localizedReason;

        if (gameOverPanel != null)
        {
            Transform innerCard = gameOverPanel.transform.Find("ModalContainer/InnerCard");
            if (innerCard == null) innerCard = gameOverPanel.transform.Find("ModalContainer");

            if (innerCard != null)
            {
                SetTMPText(innerCard, "BadgeText", "gameover_badge");
                SetTMPText(innerCard, "SubtitleText", "gameover_subtitle");

                Transform tipsHeader = innerCard.Find("TipBox/TipHeader") ?? innerCard.Find("TipsContainer/TipsHeading");
                if (tipsHeader != null)
                {
                    var tmp = tipsHeader.GetComponent<TextMeshProUGUI>();
                    if (tmp != null) tmp.text = LocalizationManager.Instance.Get("gameover_survival_tips");
                }

                SetTMPText(innerCard, "TipBox/TipText", "gameover_tips_body");
                SetTMPText(innerCard, "EncouragementWhisper", "gameover_encouragement");
            }

            // Buttons
            if (restartButton != null)
            {
                var tmp = restartButton.GetComponentInChildren<TextMeshProUGUI>(true);
                if (tmp != null) tmp.text = LocalizationManager.Instance.Get("gameover_btn_retry");
                var txt = restartButton.GetComponentInChildren<Text>(true);
                if (txt != null) txt.text = LocalizationManager.Instance.Get("gameover_btn_retry");
            }

            if (mainMenuButton != null)
            {
                var tmp = mainMenuButton.GetComponentInChildren<TextMeshProUGUI>(true);
                if (tmp != null) tmp.text = LocalizationManager.Instance.Get("gameover_btn_menu");
                var txt = mainMenuButton.GetComponentInChildren<Text>(true);
                if (txt != null) txt.text = LocalizationManager.Instance.Get("gameover_btn_menu");
            }
        }
    }

    private void SetTMPText(Transform root, string path, string locKey)
    {
        Transform t = root.Find(path);
        if (t != null)
        {
            var tmp = t.GetComponent<TextMeshProUGUI>();
            if (tmp != null) tmp.text = LocalizationManager.Instance.Get(locKey);
        }
    }

    public void ResetGameOverState()
    {
        isGameOver = false;
        if (gameOverPanel != null)
        {
            gameOverPanel.SetActive(false);
        }
        if (gameOverCanvasGroup != null)
        {
            gameOverCanvasGroup.alpha = 0f;
            gameOverCanvasGroup.interactable = false;
            gameOverCanvasGroup.blocksRaycasts = false;
        }
    }

    private void FindUIReferences()
    {
        if (gameOverPanel == null)
        {
            Transform panel = transform.Find("GameOverPanel");
            gameOverPanel = panel != null ? panel.gameObject : GameObject.Find("GameOverPanel");
        }

        if (gameOverPanel == null) return;

        if (gameOverCanvasGroup == null)
        {
            gameOverCanvasGroup = gameOverPanel.GetComponent<CanvasGroup>();
            if (gameOverCanvasGroup == null)
            {
                gameOverCanvasGroup = gameOverPanel.AddComponent<CanvasGroup>();
            }
        }

        if (titleText == null && titleTextTMP == null)
        {
            Transform t = gameOverPanel.transform.Find("ModalContainer/InnerCard/TitleText");
            if (t == null) t = gameOverPanel.transform.Find("ModalContainer/TitleText");
            if (t != null)
            {
                titleText = t.GetComponent<Text>();
                titleTextTMP = t.GetComponent<TextMeshProUGUI>();
            }
        }

        if (reasonText == null && reasonTextTMP == null)
        {
            Transform r = gameOverPanel.transform.Find("ModalContainer/InnerCard/ReasonText");
            if (r == null) r = gameOverPanel.transform.Find("ModalContainer/ReasonText");
            if (r != null)
            {
                reasonText = r.GetComponent<Text>();
                reasonTextTMP = r.GetComponent<TextMeshProUGUI>();
            }
        }

        if (restartButton == null)
        {
            restartButton = FindButton("RestartButton");
        }

        if (mainMenuButton == null)
        {
            mainMenuButton = FindButton("MainMenuButton");
        }
    }

    private Button FindButton(string buttonName)
    {
        Transform buttonTransform = gameOverPanel.transform.Find("ModalContainer/InnerCard/ButtonRow/" + buttonName);
        if (buttonTransform == null)
        {
            buttonTransform = gameOverPanel.transform.Find("ModalContainer/ButtonRow/" + buttonName);
        }

        return buttonTransform != null ? buttonTransform.GetComponent<Button>() : null;
    }

    private void BindButtonEvents()
    {
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

    public void RestartLevel()
    {
        Time.timeScale = 1f;
        Scene currentScene = SceneManager.GetActiveScene();
        SceneManager.LoadScene(currentScene.buildIndex);
    }

    public void GoToMainMenu()
    {
        Time.timeScale = 1f;
        if (Application.CanStreamedLevelBeLoaded("MainScreen"))
        {
            SceneManager.LoadScene("MainScreen");
        }
        else
        {
            SceneManager.LoadScene(0);
        }
    }

    private void OnDestroy()
    {
        if (Instance == this)
        {
            Instance = null;
        }

        Time.timeScale = 1f;
    }
}
