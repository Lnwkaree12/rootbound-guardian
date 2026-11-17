using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using TMPro;

/// <summary>
/// Controls the final ending cutscene in scene 'cutscene End':
/// - Displays the heartwarming scene of the Elder Elf and creatures rejoicing.
/// - Performs a gentle, smooth zoom-out expansion from a smaller picture to full frame.
/// - Typewriter effect delivers the ending narrative:
///   "หลังวิกฤติต้นไม้โลกเหี่ยวเฉาถูกแก้ไขแล้ว ป่าได้กลับมาอุดมสมบูรณ์และเหล่าสัตว์ต่างก็ร่าเริงอีกครั้ง"
/// - Provides a Skip button and Space/Click to accelerate or advance.
/// - Upon completion, reveals a beautiful 'The End' celebration screen with Return to Main Menu and Play Again options.
/// </summary>
public class EndingCutsceneManager : MonoBehaviour
{
    [Header("Image & Presentation")]
    [SerializeField] private Sprite endIllustrationSprite;
    [SerializeField] private RectTransform illustrationRect;
    [SerializeField] private Image illustrationImage;
    [SerializeField] private CanvasGroup illustrationCanvasGroup;

    [Header("Zoom Expansion Settings")]
    [Tooltip("Starting scale (starts slightly smaller)")]
    [SerializeField] private float startScale = 0.72f;
    [Tooltip("Ending scale (full frame)")]
    [SerializeField] private float endScale = 1.0f;
    [Tooltip("Duration of the zoom expansion")]
    [SerializeField] private float zoomDuration = 7.0f;

    [Header("Dialogue & Typewriter")]
    [TextArea(2, 5)]
    [SerializeField] private string endingNarrativeText = "หลังวิกฤติต้นไม้โลกเหี่ยวเฉาถูกแก้ไขแล้ว ป่าได้กลับมาอุดมสมบูรณ์และเหล่าสัตว์ต่างก็ร่าเริงอีกครั้ง";
    [SerializeField] private TextMeshProUGUI subtitleTMP;
    [SerializeField] private GameObject continuePrompt;
    [SerializeField] private CanvasGroup dialogueCanvasGroup;
    [SerializeField] private float typingSpeed = 0.038f;
    [SerializeField] private float autoAdvanceWaitTime = 5.0f;

    [Header("Screen Faders & Controls")]
    [SerializeField] private CanvasGroup blackScreenFader;
    [SerializeField] private Button skipButton;

    [Header("The End Celebration Screen")]
    [SerializeField] private GameObject theEndPanel;
    [SerializeField] private CanvasGroup theEndCanvasGroup;
    [SerializeField] private Button mainMenuButton;
    [SerializeField] private Button playAgainButton;
    [SerializeField] private Button replayCutsceneButton;
    [SerializeField] private string mainMenuSceneName = "MainScreen";
    [SerializeField] private string gameplaySceneName = "MainGameplay";
    [SerializeField] private string endCreditSceneName = "EndCredit";
    [SerializeField] private bool transitionToEndCredit = true;

    [Header("Audio Settings")]
    [SerializeField] private AudioSource bgmSource;
    [SerializeField] private AudioClip bgmClip;
    [SerializeField] private AudioClip pageTransitionSFX;
    [SerializeField] [Range(0f, 1f)] private float maxBgmVolume = 0.65f;

    private bool isTyping = false;
    private bool skipTypingRequested = false;
    private bool advanceRequested = false;
    private bool isCutsceneFinished = false;
    private Coroutine cutsceneRoutine;
    private Coroutine zoomRoutine;

    private void Awake()
    {
        if (blackScreenFader != null)
        {
            blackScreenFader.alpha = 1f;
            blackScreenFader.blocksRaycasts = true;
        }

        if (theEndPanel != null)
        {
            theEndPanel.SetActive(false);
        }

        if (theEndCanvasGroup != null)
        {
            theEndCanvasGroup.alpha = 0f;
            theEndCanvasGroup.blocksRaycasts = false;
        }

        if (continuePrompt != null)
        {
            continuePrompt.SetActive(false);
        }

        if (skipButton != null)
        {
            skipButton.onClick.AddListener(OnSkipClicked);
            var tmp = skipButton.GetComponentInChildren<TextMeshProUGUI>();
            if (tmp != null && LocalizationManager.Instance != null)
            {
                tmp.text = LocalizationManager.Instance.Get("hint_skip");
            }
        }

        if (continuePrompt != null && LocalizationManager.Instance != null)
        {
            var tmp = continuePrompt.GetComponentInChildren<TextMeshProUGUI>();
            if (tmp != null)
            {
                tmp.text = LocalizationManager.Instance.Get("hint_continue");
            }
        }

        if (mainMenuButton != null)
        {
            mainMenuButton.onClick.AddListener(OnMainMenuClicked);
        }

        if (playAgainButton != null)
        {
            playAgainButton.onClick.AddListener(OnPlayAgainClicked);
        }

        if (replayCutsceneButton != null)
        {
            replayCutsceneButton.onClick.AddListener(OnReplayCutsceneClicked);
        }
    }

    private void Start()
    {
        // Setup BGM
        if (bgmSource != null && bgmClip != null)
        {
            bgmSource.clip = bgmClip;
            bgmSource.loop = true;
            bgmSource.volume = 0f;
            bgmSource.Play();
            StartCoroutine(FadeAudio(bgmSource, maxBgmVolume, 2.0f));
        }

        cutsceneRoutine = StartCoroutine(PlayEndingSequence());
    }

    private void Update()
    {
        if (isCutsceneFinished) return;

        // Spacebar or mouse click interaction
        if (Input.GetKeyDown(KeyCode.Space) || Input.GetMouseButtonDown(0))
        {
            if (isTyping)
            {
                skipTypingRequested = true;
            }
            else
            {
                advanceRequested = true;
            }
        }
    }

    private IEnumerator PlayEndingSequence()
    {
        // 1. Initial visual setup
        if (illustrationImage != null && endIllustrationSprite != null)
        {
            illustrationImage.sprite = endIllustrationSprite;
        }

        if (illustrationRect != null)
        {
            illustrationRect.localScale = Vector3.one * startScale;
        }

        if (illustrationCanvasGroup != null)
        {
            illustrationCanvasGroup.alpha = 1f;
        }

        // Fade in from black
        yield return StartCoroutine(FadeGroup(blackScreenFader, 1f, 0f, 1.2f));
        if (blackScreenFader != null) blackScreenFader.blocksRaycasts = false;

        // 2. Start Zoom Expansion Animation (small -> full)
        zoomRoutine = StartCoroutine(AnimateZoomExpansion(startScale, endScale, zoomDuration));

        // 3. Run narrative typewriter
        string narrative = LocalizationManager.Instance != null 
            ? LocalizationManager.Instance.GetEndingNarrative() 
            : endingNarrativeText;
        yield return StartCoroutine(TypewriterRoutine(narrative));

        // 4. Show continue prompt
        if (continuePrompt != null)
        {
            continuePrompt.SetActive(true);
        }

        // 5. Wait for player interaction or auto advance timer
        float timer = 0f;
        while (timer < autoAdvanceWaitTime && !advanceRequested)
        {
            timer += Time.deltaTime;
            yield return null;
        }

        // 6. Transition to The End Celebration Screen
        yield return StartCoroutine(ShowTheEndScreenRoutine());
    }

    private IEnumerator AnimateZoomExpansion(float fromScale, float toScale, float duration)
    {
        if (illustrationRect == null) yield break;

        float t = 0f;
        illustrationRect.localScale = Vector3.one * fromScale;

        while (t < duration)
        {
            t += Time.deltaTime;
            float progress = Mathf.Clamp01(t / duration);
            float smooth = Mathf.SmoothStep(0f, 1f, progress);
            illustrationRect.localScale = Vector3.one * Mathf.Lerp(fromScale, toScale, smooth);
            yield return null;
        }

        illustrationRect.localScale = Vector3.one * toScale;
    }

    private IEnumerator TypewriterRoutine(string text)
    {
        isTyping = true;
        skipTypingRequested = false;

        if (subtitleTMP != null)
        {
            subtitleTMP.text = "";

            for (int i = 0; i < text.Length; i++)
            {
                if (skipTypingRequested)
                {
                    subtitleTMP.text = text;
                    break;
                }

                subtitleTMP.text = text.Substring(0, i + 1);

                char c = text[i];
                if (c == '.' || c == '!' || c == '?')
                    yield return new WaitForSeconds(typingSpeed * 4f);
                else if (c == ' ' || c == '\n')
                    yield return new WaitForSeconds(typingSpeed * 1.5f);
                else
                    yield return new WaitForSeconds(typingSpeed);
            }
        }

        isTyping = false;
    }

    private void OnSkipClicked()
    {
        if (isCutsceneFinished) return;

        if (cutsceneRoutine != null) StopCoroutine(cutsceneRoutine);
        if (zoomRoutine != null) StopCoroutine(zoomRoutine);

        if (transitionToEndCredit && !string.IsNullOrEmpty(endCreditSceneName))
        {
            isCutsceneFinished = true;
            if (skipButton != null) skipButton.gameObject.SetActive(false);
            StartCoroutine(TransitionToScene(endCreditSceneName));
            return;
        }

        StartCoroutine(ShowTheEndScreenRoutine());
    }

    private IEnumerator ShowTheEndScreenRoutine()
    {
        isCutsceneFinished = true;

        if (skipButton != null)
        {
            skipButton.gameObject.SetActive(false);
        }

        // Fade out dialogue container smoothly
        if (dialogueCanvasGroup != null)
        {
            yield return StartCoroutine(FadeGroup(dialogueCanvasGroup, 1f, 0f, 0.5f));
            dialogueCanvasGroup.gameObject.SetActive(false);
        }

        // Snap illustration to full scale
        if (illustrationRect != null)
        {
            illustrationRect.localScale = Vector3.one * endScale;
        }

        // Transition directly to End Credit scene if configured
        if (transitionToEndCredit && !string.IsNullOrEmpty(endCreditSceneName))
        {
            yield return new WaitForSeconds(1.2f);
            yield return StartCoroutine(TransitionToScene(endCreditSceneName));
            yield break;
        }

        // Fallback: Reveal The End celebration panel
        if (theEndPanel != null)
        {
            theEndPanel.SetActive(true);
        }

        if (theEndCanvasGroup != null)
        {
            theEndCanvasGroup.blocksRaycasts = true;
            yield return StartCoroutine(FadeGroup(theEndCanvasGroup, 0f, 1f, 0.8f));
        }
    }

    private void OnMainMenuClicked()
    {
        StartCoroutine(TransitionToScene(mainMenuSceneName));
    }

    private void OnPlayAgainClicked()
    {
        StartCoroutine(TransitionToScene(gameplaySceneName));
    }

    private void OnReplayCutsceneClicked()
    {
        StartCoroutine(TransitionToScene(SceneManager.GetActiveScene().name));
    }

    private IEnumerator TransitionToScene(string sceneName)
    {
        if (theEndCanvasGroup != null) theEndCanvasGroup.blocksRaycasts = false;
        if (blackScreenFader != null) blackScreenFader.blocksRaycasts = true;

        if (bgmSource != null)
        {
            StartCoroutine(FadeAudio(bgmSource, 0f, 0.6f));
        }

        yield return StartCoroutine(FadeGroup(blackScreenFader, 0f, 1f, 0.65f));
        yield return new WaitForSeconds(0.15f);

        if (!string.IsNullOrEmpty(sceneName) && Application.CanStreamedLevelBeLoaded(sceneName))
        {
            SceneManager.LoadScene(sceneName);
        }
        else
        {
            SceneManager.LoadScene(0);
        }
    }

    private IEnumerator FadeGroup(CanvasGroup cg, float from, float to, float duration)
    {
        if (cg == null) yield break;
        float elapsed = 0f;
        cg.alpha = from;
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            cg.alpha = Mathf.Lerp(from, to, elapsed / duration);
            yield return null;
        }
        cg.alpha = to;
    }

    private IEnumerator FadeAudio(AudioSource source, float targetVol, float duration)
    {
        if (source == null) yield break;
        float startVol = source.volume;
        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            source.volume = Mathf.Lerp(startVol, targetVol, elapsed / duration);
            yield return null;
        }
        source.volume = targetVol;
        if (Mathf.Approximately(targetVol, 0f)) source.Stop();
    }
}

