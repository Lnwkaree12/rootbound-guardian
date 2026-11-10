using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using TMPro;

/// <summary>
/// Controls the End Credit cinematic screen:
/// - Smooth vertical auto-scroll with fast-forward support (hold Left Click / Space / Enter).
/// - Skip button to immediately jump to the ending card & action buttons.
/// - Atmospheric BGM playback with soft fade in/out.
/// - Clean scene transitions to MainScreen or MainGameplay.
/// </summary>
public class EndCreditController : MonoBehaviour
{
    [Header("Scroll Configuration")]
    [Tooltip("The RectTransform containing all credit items to scroll up")]
    [SerializeField] private RectTransform scrollContent;
    [Tooltip("Normal auto-scroll speed in units/sec")]
    [SerializeField] private float normalScrollSpeed = 60f;
    [Tooltip("Speed multiplier when holding fast-forward")]
    [SerializeField] private float fastForwardMultiplier = 3.5f;
    [Tooltip("Y position threshold where auto-scroll stops at the bottom card")]
    [SerializeField] private float targetEndY = 1850f;

    [Header("UI Controls")]
    [SerializeField] private ScrollRect scrollRect;
    [SerializeField] private Button skipButton;
    [SerializeField] private GameObject fastForwardIndicator;
    [SerializeField] private CanvasGroup blackScreenFader;

    [Header("End Action Buttons")]
    [SerializeField] private Button mainMenuButton;
    [SerializeField] private Button replayButton;
    [SerializeField] private string mainMenuSceneName = "MainScreen";
    [SerializeField] private string gameplaySceneName = "MainGameplay";

    [Header("Audio Settings")]
    [SerializeField] private AudioSource bgmSource;
    [SerializeField] private AudioClip bgmClip;
    [SerializeField] [Range(0f, 1f)] private float maxBgmVolume = 0.65f;
    [SerializeField] private AudioClip buttonClickSFX;

    private bool isAutoScrolling = true;
    private bool isTransitioning = false;
    private float currentSpeed;
    private bool isUserDragging = false;

    private void Awake()
    {
        if (scrollRect != null && scrollRect.viewport != null)
        {
            var vpImg = scrollRect.viewport.GetComponent<Image>();
            if (vpImg != null) vpImg.enabled = true;
            var mask = scrollRect.viewport.GetComponent<Mask>();
            if (mask != null) mask.enabled = true;
        }

        if (blackScreenFader != null)
        {
            blackScreenFader.gameObject.SetActive(true);
            blackScreenFader.alpha = 1f;
            blackScreenFader.blocksRaycasts = true;
        }

        if (fastForwardIndicator != null)
        {
            fastForwardIndicator.SetActive(false);
        }

        if (skipButton != null)
        {
            skipButton.onClick.AddListener(OnSkipClicked);
        }

        if (mainMenuButton != null)
        {
            mainMenuButton.onClick.AddListener(OnMainMenuClicked);
        }

        if (replayButton != null)
        {
            replayButton.onClick.AddListener(OnReplayClicked);
        }

        if (scrollRect != null)
        {
            scrollRect.onValueChanged.AddListener((pos) =>
            {
                // If content reached top/bottom, handle accordingly
            });
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

        SetTMPText("AdvisorRole", "credits_advisor_role");
        SetTMPText("TeamHeader", "credits_team_role");
        SetTMPText("ThanksHeader", "credits_thanks_role");
        SetTMPText("ThaiThanksMsg", "credits_finale_title");
        SetTMPText("EnglishSubMsg", "credits_finale_sub");
        SetTMPText("FastForwardPrompt/Text", "credits_hint_ff");

        if (mainMenuButton != null)
        {
            var tmp = mainMenuButton.GetComponentInChildren<TextMeshProUGUI>();
            if (tmp != null) tmp.text = LocalizationManager.Instance.Get("credits_btn_menu");
        }

        if (replayButton != null)
        {
            var tmp = replayButton.GetComponentInChildren<TextMeshProUGUI>();
            if (tmp != null) tmp.text = LocalizationManager.Instance.Get("credits_btn_replay");
        }

        if (skipButton != null)
        {
            var tmp = skipButton.GetComponentInChildren<TextMeshProUGUI>();
            if (tmp != null) tmp.text = LocalizationManager.Instance.Get("credits_hint_skip");
        }
    }

    private void SetTMPText(string goNameOrPath, string locKey)
    {
        GameObject go = GameObject.Find(goNameOrPath);
        if (go != null)
        {
            var tmp = go.GetComponent<TextMeshProUGUI>();
            if (tmp != null) tmp.text = LocalizationManager.Instance.Get(locKey);
        }
    }

    private void Start()
    {
        StartCoroutine(ScreenFadeInRoutine());
        StartCoroutine(StartBGMRoutine());
    }

    private void Update()
    {
        if (isTransitioning) return;

        HandleInputAndScrolling();
    }

    private void HandleInputAndScrolling()
    {
        if (scrollContent == null) return;

        // Check if fast forward is held (Space, Enter, down arrow, or holding left mouse button)
        bool isFastForwarding = Input.GetKey(KeyCode.Space) ||
                               Input.GetKey(KeyCode.Return) ||
                               Input.GetKey(KeyCode.DownArrow) ||
                               (Input.GetMouseButton(0) && !IsPointerOverInteractiveButton());

        if (fastForwardIndicator != null)
        {
            fastForwardIndicator.SetActive(isFastForwarding && isAutoScrolling);
        }

        float targetSpeed = isFastForwarding ? (normalScrollSpeed * fastForwardMultiplier) : normalScrollSpeed;
        currentSpeed = Mathf.MoveTowards(currentSpeed, targetSpeed, Time.deltaTime * 500f);

        if (isAutoScrolling && !isUserDragging)
        {
            Vector2 pos = scrollContent.anchoredPosition;
            if (pos.y < targetEndY)
            {
                pos.y += currentSpeed * Time.deltaTime;
                scrollContent.anchoredPosition = pos;

                if (pos.y >= targetEndY)
                {
                    pos.y = targetEndY;
                    scrollContent.anchoredPosition = pos;
                    isAutoScrolling = false;
                    if (fastForwardIndicator != null) fastForwardIndicator.SetActive(false);
                }
            }
            else
            {
                isAutoScrolling = false;
                if (fastForwardIndicator != null) fastForwardIndicator.SetActive(false);
            }
        }
    }

    private bool IsPointerOverInteractiveButton()
    {
        // Check if cursor is over specific UI buttons
        if (UnityEngine.EventSystems.EventSystem.current == null) return false;
        var selected = UnityEngine.EventSystems.EventSystem.current.currentSelectedGameObject;
        if (selected != null)
        {
            if (selected.GetComponent<Button>() != null) return true;
        }
        return false;
    }

    public void OnSkipClicked()
    {
        if (isTransitioning) return;
        PlayButtonSFX();

        isAutoScrolling = false;
        if (fastForwardIndicator != null) fastForwardIndicator.SetActive(false);

        // Smoothly snap to final card position
        StopAllCoroutines();
        StartCoroutine(SnapToEndRoutine());
    }

    private IEnumerator SnapToEndRoutine()
    {
        if (scrollContent == null) yield break;

        Vector2 startPos = scrollContent.anchoredPosition;
        Vector2 targetPos = new Vector2(startPos.x, targetEndY);
        float t = 0f;
        float duration = 0.6f;

        while (t < duration)
        {
            t += Time.deltaTime;
            float p = Mathf.SmoothStep(0f, 1f, t / duration);
            scrollContent.anchoredPosition = Vector2.Lerp(startPos, targetPos, p);
            yield return null;
        }

        scrollContent.anchoredPosition = targetPos;
        isAutoScrolling = false;
    }

    private void OnMainMenuClicked()
    {
        if (isTransitioning) return;
        PlayButtonSFX();
        StartCoroutine(TransitionToSceneRoutine(mainMenuSceneName));
    }

    private void OnReplayClicked()
    {
        if (isTransitioning) return;
        PlayButtonSFX();
        StartCoroutine(TransitionToSceneRoutine(gameplaySceneName));
    }

    private void PlayButtonSFX()
    {
        if (buttonClickSFX != null && bgmSource != null)
        {
            bgmSource.PlayOneShot(buttonClickSFX, 0.9f);
        }
    }

    private IEnumerator ScreenFadeInRoutine()
    {
        if (blackScreenFader == null) yield break;

        float t = 0f;
        float duration = 0.9f;

        while (t < duration)
        {
            t += Time.deltaTime;
            blackScreenFader.alpha = Mathf.Lerp(1f, 0f, t / duration);
            yield return null;
        }

        blackScreenFader.alpha = 0f;
        blackScreenFader.blocksRaycasts = false;
    }

    private IEnumerator StartBGMRoutine()
    {
        if (bgmSource == null) yield break;

        if (bgmClip != null)
        {
            bgmSource.clip = bgmClip;
        }

        bgmSource.loop = true;
        bgmSource.volume = 0f;
        bgmSource.Play();

        float t = 0f;
        float duration = 1.6f;

        while (t < duration)
        {
            t += Time.deltaTime;
            bgmSource.volume = Mathf.Lerp(0f, maxBgmVolume, t / duration);
            yield return null;
        }

        bgmSource.volume = maxBgmVolume;
    }

    private IEnumerator TransitionToSceneRoutine(string sceneName)
    {
        isTransitioning = true;
        if (blackScreenFader != null) blackScreenFader.blocksRaycasts = true;

        // Fade audio
        if (bgmSource != null)
        {
            StartCoroutine(FadeAudioRoutine(0f, 0.65f));
        }

        // Fade screen to black
        if (blackScreenFader != null)
        {
            float t = 0f;
            float duration = 0.65f;
            while (t < duration)
            {
                t += Time.deltaTime;
                blackScreenFader.alpha = Mathf.Lerp(0f, 1f, t / duration);
                yield return null;
            }
            blackScreenFader.alpha = 1f;
        }

        yield return new WaitForSeconds(0.12f);

        if (!string.IsNullOrEmpty(sceneName) && Application.CanStreamedLevelBeLoaded(sceneName))
        {
            SceneManager.LoadScene(sceneName);
        }
        else
        {
            SceneManager.LoadScene(0);
        }
    }

    private IEnumerator FadeAudioRoutine(float targetVol, float duration)
    {
        if (bgmSource == null) yield break;

        float startVol = bgmSource.volume;
        float t = 0f;

        while (t < duration)
        {
            t += Time.deltaTime;
            bgmSource.volume = Mathf.Lerp(startVol, targetVol, t / duration);
            yield return null;
        }

        bgmSource.volume = targetVol;
    }

    public void SetTargetEndY(float endY)
    {
        targetEndY = endY;
    }
}

