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

        if (gameOverPanel != null)
        {
            gameOverPanel.SetActive(false);
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

        Cursor.visible = true;
        Cursor.lockState = CursorLockMode.None;
        Time.timeScale = 0f;
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
            // Subtitle
            Transform sub = gameOverPanel.transform.Find("ModalContainer/InnerCard/SubtitleText");
            if (sub == null) sub = gameOverPanel.transform.Find("ModalContainer/SubtitleText");
            if (sub != null)
            {
                var tmp = sub.GetComponent<TextMeshProUGUI>();
                if (tmp != null) tmp.text = LocalizationManager.Instance.Get("gameover_subtitle");
            }

            // Survival tips heading
            Transform tips = gameOverPanel.transform.Find("ModalContainer/InnerCard/TipsContainer/TipsHeading");
            if (tips != null)
            {
                var tmp = tips.GetComponent<TextMeshProUGUI>();
                if (tmp != null) tmp.text = LocalizationManager.Instance.Get("gameover_survival_tips");
            }

            // Buttons
            if (restartButton != null)
            {
                var tmp = restartButton.GetComponentInChildren<TextMeshProUGUI>();
                if (tmp != null) tmp.text = LocalizationManager.Instance.Get("gameover_btn_retry");
                var txt = restartButton.GetComponentInChildren<Text>();
                if (txt != null) txt.text = LocalizationManager.Instance.Get("gameover_btn_retry");
            }

            if (mainMenuButton != null)
            {
                var tmp = mainMenuButton.GetComponentInChildren<TextMeshProUGUI>();
                if (tmp != null) tmp.text = LocalizationManager.Instance.Get("gameover_btn_menu");
                var txt = mainMenuButton.GetComponentInChildren<Text>();
                if (txt != null) txt.text = LocalizationManager.Instance.Get("gameover_btn_menu");
            }
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
