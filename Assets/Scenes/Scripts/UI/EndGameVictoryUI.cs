using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using TMPro;

/// <summary>
/// Controls the elegant, cozy "Victory / The End" UI screen in EndScene:
/// - Smooth elastic fade-in presentation with animated card pop-in.
/// - Glowing celestial crest & golden banner celebrating the restored Ancient World Tree.
/// - Clear time statistic, World Tree restoration status, and warm heartfelt message from Lumi.
/// - Action Buttons: Freely Explore ("🌸 เดินสำรวจเกาะศักดิ์สิทธิ์"), Return to Title ("🏠 กลับสู่หน้าหลัก"), Replay ("🔄 เริ่มฉากจบใหม่").
/// - Allows toggling UI anytime during free exploration using ESC or the corner badge.
/// </summary>
public class EndGameVictoryUI : MonoBehaviour
{
    public static EndGameVictoryUI Instance { get; private set; }

    [Header("UI Panels & Canvas Groups")]
    [SerializeField] private GameObject victoryPanel;
    [SerializeField] private CanvasGroup victoryCanvasGroup;
    [SerializeField] private RectTransform modalCard;
    [SerializeField] private GameObject cornerReopenBadge;

    [Header("Decorative & Animated Elements")]
    [SerializeField] private RectTransform crestGlowRect;
    [SerializeField] private RectTransform crestBadgeRect;

    [Header("Text Displays (TextMeshPro)")]
    [SerializeField] private TextMeshProUGUI titleTextTMP;
    [SerializeField] private TextMeshProUGUI subtitleTextTMP;
    [SerializeField] private TextMeshProUGUI clearTimeTextTMP;
    [SerializeField] private TextMeshProUGUI treeStatusTextTMP;
    [SerializeField] private TextMeshProUGUI guardianRankTextTMP;
    [SerializeField] private TextMeshProUGUI storyQuoteTextTMP;

    [Header("Action Buttons")]
    [SerializeField] private Button exploreButton;
    [SerializeField] private Button mainMenuButton;
    [SerializeField] private Button restartButton;
    [SerializeField] private Button cornerBadgeButton;

    [Header("Audio")]
    [SerializeField] private AudioSource audioSource;
    [SerializeField] private AudioClip fanfareSound;

    [Header("Configuration")]
    [SerializeField] private string mainMenuSceneName = "MainScreen";
    [SerializeField] private float fadeInDuration = 0.85f;
    [SerializeField] private float fadeOutDuration = 0.35f;

    private bool hasVictoryTriggered = false;
    private bool isPanelShowing = false;
    private float sceneStartTime;
    private Coroutine currentFadeRoutine;

    private void Awake()
    {
        if (Instance == null) Instance = this;
        sceneStartTime = Time.timeSinceLevelLoad;

        if (victoryPanel != null)
        {
            victoryPanel.SetActive(false);
        }

        if (cornerReopenBadge != null)
        {
            cornerReopenBadge.SetActive(false);
        }

        BindButtonEvents();
    }

    private void Start()
    {
        BindButtonEvents();
    }

    private void BindButtonEvents()
    {
        if (exploreButton != null)
        {
            exploreButton.onClick.RemoveAllListeners();
            exploreButton.onClick.AddListener(OnExploreClicked);
        }

        if (mainMenuButton != null)
        {
            mainMenuButton.onClick.RemoveAllListeners();
            mainMenuButton.onClick.AddListener(OnMainMenuClicked);
        }

        if (restartButton != null)
        {
            restartButton.onClick.RemoveAllListeners();
            restartButton.onClick.AddListener(OnRestartClicked);
        }

        if (cornerBadgeButton != null)
        {
            cornerBadgeButton.onClick.RemoveAllListeners();
            cornerBadgeButton.onClick.AddListener(OnCornerBadgeClicked);
        }
    }

    private void Update()
    {
        // Allow pressing ESC to toggle Victory screen once it has been triggered
        if (hasVictoryTriggered && Input.GetKeyDown(KeyCode.Escape))
        {
            if (isPanelShowing)
            {
                OnExploreClicked();
            }
            else
            {
                ShowVictoryScreen(0f);
            }
        }

        // Animate glowing aura and floating badge if visible
        if (isPanelShowing)
        {
            float time = Time.unscaledTime;
            if (crestGlowRect != null)
            {
                crestGlowRect.Rotate(Vector3.forward, 15f * Time.unscaledDeltaTime);
                float glowScale = 1.0f + Mathf.Sin(time * 2.5f) * 0.08f;
                crestGlowRect.localScale = new Vector3(glowScale, glowScale, 1f);
            }

            if (crestBadgeRect != null)
            {
                float badgeBob = Mathf.Sin(time * 3f) * 4f;
                crestBadgeRect.anchoredPosition = new Vector2(0f, badgeBob);
            }
        }
    }

    /// <summary>
    /// Displays the victory celebration screen.
    /// </summary>
    public void ShowVictoryScreen(float delay = 0f)
    {
        hasVictoryTriggered = true;

        if (victoryPanel != null)
        {
            victoryPanel.transform.SetAsLastSibling();
            victoryPanel.SetActive(true);
        }

        if (victoryCanvasGroup != null)
        {
            victoryCanvasGroup.alpha = 0f;
            victoryCanvasGroup.interactable = false;
            victoryCanvasGroup.blocksRaycasts = false;
        }

        if (gameObject.activeInHierarchy)
        {
            if (currentFadeRoutine != null) StopCoroutine(currentFadeRoutine);
            currentFadeRoutine = StartCoroutine(ShowRoutine(delay));
        }
    }

    private IEnumerator ShowRoutine(float delay)
    {
        if (delay > 0f) yield return new WaitForSeconds(delay);

        isPanelShowing = true;

        if (cornerReopenBadge != null)
        {
            cornerReopenBadge.SetActive(false);
        }

        // Format stats
        float elapsed = Mathf.Max(0f, Time.timeSinceLevelLoad - sceneStartTime);
        int minutes = Mathf.FloorToInt(elapsed / 60f);
        int seconds = Mathf.FloorToInt(elapsed % 60f);
        if (clearTimeTextTMP != null)
        {
            clearTimeTextTMP.text = $"{minutes:00}:{seconds:00}";
        }

        if (treeStatusTextTMP != null)
        {
            treeStatusTextTMP.text = "ผลิบานสมบูรณ์ 100%";
        }

        if (guardianRankTextTMP != null)
        {
            guardianRankTextTMP.text = "ระดับ S • Master Guardian";
        }

        if (victoryPanel != null)
        {
            victoryPanel.transform.SetAsLastSibling();
            victoryPanel.SetActive(true);
        }

        if (audioSource != null && fanfareSound != null)
        {
            audioSource.PlayOneShot(fanfareSound);
        }

        // Spring-overshoot animation
        float t = 0f;
        while (t < fadeInDuration)
        {
            t += Time.unscaledDeltaTime;
            float p = Mathf.Clamp01(t / fadeInDuration);

            // Ease out back formula: c1 * (p-1)^3 + c2 * (p-1)^2 + 1
            float c1 = 1.4f;
            float c3 = c1 + 1f;
            float smoothScale = 1f + c3 * Mathf.Pow(p - 1f, 3) + c1 * Mathf.Pow(p - 1f, 2);

            if (victoryCanvasGroup != null)
            {
                victoryCanvasGroup.alpha = Mathf.SmoothStep(0f, 1f, p * 1.5f);
            }

            if (modalCard != null)
            {
                modalCard.localScale = Vector3.one * Mathf.Lerp(0.75f, 1.0f, smoothScale);
            }

            yield return null;
        }

        if (victoryCanvasGroup != null)
        {
            victoryCanvasGroup.alpha = 1f;
            victoryCanvasGroup.interactable = true;
            victoryCanvasGroup.blocksRaycasts = true;
        }

        if (modalCard != null)
        {
            modalCard.localScale = Vector3.one;
        }

        Cursor.visible = true;
        Cursor.lockState = CursorLockMode.None;
    }

    public void OnExploreClicked()
    {
        if (currentFadeRoutine != null) StopCoroutine(currentFadeRoutine);
        currentFadeRoutine = StartCoroutine(HideRoutine());
    }

    private IEnumerator HideRoutine()
    {
        isPanelShowing = false;

        if (victoryCanvasGroup != null)
        {
            victoryCanvasGroup.interactable = false;
            victoryCanvasGroup.blocksRaycasts = false;
        }

        float t = 0f;
        while (t < fadeOutDuration)
        {
            t += Time.unscaledDeltaTime;
            float progress = Mathf.Clamp01(t / fadeOutDuration);
            float smooth = Mathf.SmoothStep(0f, 1f, progress);

            if (victoryCanvasGroup != null)
            {
                victoryCanvasGroup.alpha = 1f - smooth;
            }

            if (modalCard != null)
            {
                float scale = Mathf.Lerp(1.0f, 0.90f, smooth);
                modalCard.localScale = Vector3.one * scale;
            }

            yield return null;
        }

        if (victoryPanel != null)
        {
            victoryPanel.SetActive(false);
        }

        // Show corner badge so the player can re-open victory screen anytime
        if (cornerReopenBadge != null)
        {
            cornerReopenBadge.SetActive(true);
        }
    }

    public void OnCornerBadgeClicked()
    {
        ShowVictoryScreen(0f);
    }

    public void OnMainMenuClicked()
    {
        Time.timeScale = 1f;
        if (Application.CanStreamedLevelBeLoaded(mainMenuSceneName))
        {
            SceneManager.LoadScene(mainMenuSceneName);
        }
        else
        {
            SceneManager.LoadScene(0);
        }
    }

    public void OnRestartClicked()
    {
        Time.timeScale = 1f;
        Scene activeScene = SceneManager.GetActiveScene();
        SceneManager.LoadScene(activeScene.buildIndex);
    }
}
