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
        FindUIReferences();

        if (instance == null || (instance.winPanel == null && winPanel != null)) instance = this;

        levelStartTime = Time.timeSinceLevelLoad;

        if (winPanel != null) winPanel.SetActive(false);

        BindButtonEvents();
        EnsureEventSystem();
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

        if (titleTextTMP != null) titleTextTMP.text = LocalizationManager.Instance.Get("win_badge");
        if (titleText != null) titleText.text = LocalizationManager.Instance.Get("win_badge");

        if (subtitleTextTMP != null) subtitleTextTMP.text = LocalizationManager.Instance.Get("win_congrats");
        if (subtitleText != null) subtitleText.text = LocalizationManager.Instance.Get("win_congrats");

        if (questStatusTextTMP != null) questStatusTextTMP.text = LocalizationManager.Instance.Get("win_quest_completed");
        if (questStatusText != null) questStatusText.text = LocalizationManager.Instance.Get("win_quest_completed");

        if (restartButton != null)
        {
            var tmp = restartButton.GetComponentInChildren<TextMeshProUGUI>();
            if (tmp != null) tmp.text = LocalizationManager.Instance.Get("win_btn_replay");
            var txt = restartButton.GetComponentInChildren<Text>();
            if (txt != null) txt.text = LocalizationManager.Instance.Get("win_btn_replay");
        }

        if (mainMenuButton != null)
        {
            var tmp = mainMenuButton.GetComponentInChildren<TextMeshProUGUI>();
            if (tmp != null) tmp.text = LocalizationManager.Instance.Get("win_btn_menu");
            var txt = mainMenuButton.GetComponentInChildren<Text>();
            if (txt != null) txt.text = LocalizationManager.Instance.Get("win_btn_menu");
        }
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

            if (titleTextTMP == null && titleText == null)
            {
                Transform t = winPanel.transform.Find("ModalContainer/InnerCard/TitleText");
                if (t == null) t = winPanel.transform.Find("ModalContainer/TitleText");
                if (t != null)
                {
                    titleTextTMP = t.GetComponent<TextMeshProUGUI>();
                    titleText = t.GetComponent<Text>();
                }
            }

            if (subtitleTextTMP == null && subtitleText == null)
            {
                Transform s = winPanel.transform.Find("ModalContainer/InnerCard/SubtitleText");
                if (s == null) s = winPanel.transform.Find("ModalContainer/SubtitleText");
                if (s != null)
                {
                    subtitleTextTMP = s.GetComponent<TextMeshProUGUI>();
                    subtitleText = s.GetComponent<Text>();
                }
            }

            if (questStatusTextTMP == null && questStatusText == null)
            {
                Transform q = winPanel.transform.Find("ModalContainer/InnerCard/StatsPanel/QuestStatusText");
                if (q == null) q = winPanel.transform.Find("ModalContainer/StatsPanel/QuestStatusText");
                if (q != null)
                {
                    questStatusTextTMP = q.GetComponent<TextMeshProUGUI>();
                    questStatusText = q.GetComponent<Text>();
                }
            }

            if (clearTimeTextTMP == null && clearTimeText == null)
            {
                Transform c = winPanel.transform.Find("ModalContainer/InnerCard/StatsPanel/TimeText");
                if (c == null) c = winPanel.transform.Find("ModalContainer/StatsPanel/TimeText");
                if (c != null)
                {
                    clearTimeTextTMP = c.GetComponent<TextMeshProUGUI>();
                    clearTimeText = c.GetComponent<Text>();
                }
            }

            if (hpRemainingTextTMP == null && hpRemainingText == null)
            {
                Transform h = winPanel.transform.Find("ModalContainer/InnerCard/StatsPanel/HPText");
                if (h == null) h = winPanel.transform.Find("ModalContainer/StatsPanel/HPText");
                if (h != null)
                {
                    hpRemainingTextTMP = h.GetComponent<TextMeshProUGUI>();
                    hpRemainingText = h.GetComponent<Text>();
                }
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

        if (winPanel == null)
        {
            Debug.LogError("[WinUIManager] WinPanel was not found. Add a WinPanel reference to the active WinUIManager.");
            yield break;
        }

        // Calculate and display statistics
        float elapsed = Mathf.Max(0f, Time.timeSinceLevelLoad - levelStartTime);
        int minutes = Mathf.FloorToInt(elapsed / 60f);
        int seconds = Mathf.FloorToInt(elapsed % 60f);
        UpdateLocalizedTexts();

        string timePrefix = LocalizationManager.Instance != null ? LocalizationManager.Instance.Get("win_clear_time") : "⏱️ เวลาที่ใช้:";
        string timeStr = $"{timePrefix} {minutes:00}:{seconds:00}";

        if (clearTimeTextTMP != null) clearTimeTextTMP.text = timeStr;
        if (clearTimeText != null) clearTimeText.text = timeStr;

        PlayerHealth ph = Object.FindFirstObjectByType<PlayerHealth>();
        if (ph != null)
        {
            string hpPrefix = LocalizationManager.Instance != null ? LocalizationManager.Instance.Get("win_hp_remaining") : "❤️ พลังชีวิตคงเหลือ:";
            string hpStr = $"{hpPrefix} {ph.CurrentHealth} / {ph.MaxHealth} HP";
            if (hpRemainingTextTMP != null) hpRemainingTextTMP.text = hpStr;
            if (hpRemainingText != null) hpRemainingText.text = hpStr;
        }

        if (audioSource != null && victorySound != null)
        {
            audioSource.PlayOneShot(victorySound);
        }

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