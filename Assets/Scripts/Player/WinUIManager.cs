using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using TMPro;

public class WinUIManager : MonoBehaviour
{
    private static WinUIManager instance;
    public static WinUIManager Instance => instance;

    [Header("UI References")]
    public GameObject winPanel;
    public CanvasGroup winCanvasGroup;
    public RectTransform modalContainer;

    [Header("Text Displays (TMP Supported)")]
    public TextMeshProUGUI titleTextTMP;
    public TextMeshProUGUI subtitleTextTMP;
    public TextMeshProUGUI questStatusTextTMP;
    public TextMeshProUGUI clearTimeTextTMP;
    public TextMeshProUGUI hpRemainingTextTMP;

    public Text titleText;
    public Text subtitleText;
    public Text questStatusText;
    public Text clearTimeText;
    public Text hpRemainingText;

    [Header("Action Buttons")]
    public Button restartButton;
    public Button mainMenuButton;

    [Header("Audio")]
    public AudioSource audioSource;
    public AudioClip victorySound;

    [Header("State")]
    public bool isGameWon = false;

    private float levelStartTime;

    void Awake()
    {
        if (instance == null) instance = this;
        else if (instance != this) { Destroy(gameObject); return; }

        levelStartTime = Time.timeSinceLevelLoad;
        FindUIReferences();

        if (winPanel != null) winPanel.SetActive(false);

        BindButtonEvents();
        EnsureEventSystem();
    }

    private void EnsureEventSystem()
    {
        if (FindObjectOfType<UnityEngine.EventSystems.EventSystem>() == null)
        {
            GameObject es = new GameObject("EventSystem");
            es.AddComponent<UnityEngine.EventSystems.EventSystem>();
            es.AddComponent<UnityEngine.EventSystems.StandaloneInputModule>();
        }
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

    private void FindUIReferences()
    {
        if (winPanel == null)
        {
            Transform t = transform.Find("WinPanel");
            if (t != null) winPanel = t.gameObject;
            else winPanel = GameObject.Find("WinPanel");
        }

        if (winPanel != null)
        {
            if (winCanvasGroup == null)
            {
                winCanvasGroup = winPanel.GetComponent<CanvasGroup>();
                if (winCanvasGroup == null) winCanvasGroup = winPanel.AddComponent<CanvasGroup>();
            }

            if (modalContainer == null)
            {
                Transform m = winPanel.transform.Find("ModalContainer");
                if (m != null) modalContainer = m.GetComponent<RectTransform>();
            }

            if (restartButton == null)
            {
                Transform t = winPanel.transform.Find("ModalContainer/InnerCard/ButtonRow/RestartButton");
                if (t == null) t = winPanel.transform.Find("ModalContainer/ButtonRow/RestartButton");
                if (t != null) restartButton = t.GetComponent<Button>();
            }

            if (mainMenuButton == null)
            {
                Transform t = winPanel.transform.Find("ModalContainer/InnerCard/ButtonRow/MainMenuButton");
                if (t == null) t = winPanel.transform.Find("ModalContainer/ButtonRow/MainMenuButton");
                if (t != null) mainMenuButton = t.GetComponent<Button>();
            }
        }
    }

    public void TriggerWin(float delay = 0.6f)
    {
        if (isGameWon) return;
        isGameWon = true;

        StartCoroutine(ShowWinRoutine(delay));
    }

    private IEnumerator ShowWinRoutine(float delay)
    {
        if (delay > 0f) yield return new WaitForSeconds(delay);

        EnsureEventSystem();
        FindUIReferences();

        if (winPanel != null)
        {
            // บังคับย้าย WinPanel ให้ไปอยู่ด้านหน้าสุดของ Canvas เพื่อไม่ให้โดน UI อื่นบัง
            winPanel.transform.SetAsLastSibling();
            winPanel.SetActive(true);

            // บังคับเปิดการรับคลิก UI
            if (winCanvasGroup != null)
            {
                winCanvasGroup.interactable = true;
                winCanvasGroup.blocksRaycasts = true;
            }
        }

        BindButtonEvents();

        // แสดงผล Cursor ให้เมาส์กดปุ่มได้
        Cursor.visible = true;
        Cursor.lockState = CursorLockMode.None;

        Time.timeScale = 0f;
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
            SceneManager.LoadScene("MainScreen");
        else
            SceneManager.LoadScene(0);
    }

    private void OnDestroy()
    {
        Time.timeScale = 1f;
    }
}