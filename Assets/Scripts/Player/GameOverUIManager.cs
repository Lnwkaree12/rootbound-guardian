using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class GameOverUIManager : MonoBehaviour
{
    public static GameOverUIManager Instance { get; private set; }

    [Header("UI References")]
    [SerializeField] public GameObject gameOverPanel;
    [SerializeField] public CanvasGroup gameOverCanvasGroup;
    [SerializeField] public Text titleText;
    [SerializeField] public Text reasonText;

    [Header("Action Buttons")]
    [SerializeField] public Button restartButton;
    [SerializeField] public Button mainMenuButton;

    private bool isGameOver;

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

    public void TriggerGameOver(string reason = "")
    {
        if (isGameOver) return;
        isGameOver = true;

        FindUIReferences();
        BindButtonEvents();

        if (reasonText != null && !string.IsNullOrEmpty(reason))
        {
            reasonText.text = reason;
        }

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

        if (titleText == null)
        {
            Transform t = gameOverPanel.transform.Find("ModalContainer/InnerCard/TitleText");
            if (t == null) t = gameOverPanel.transform.Find("ModalContainer/TitleText");
            if (t != null) titleText = t.GetComponent<Text>();
        }

        if (reasonText == null)
        {
            Transform r = gameOverPanel.transform.Find("ModalContainer/InnerCard/ReasonText");
            if (r == null) r = gameOverPanel.transform.Find("ModalContainer/ReasonText");
            if (r != null) reasonText = r.GetComponent<Text>();
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
