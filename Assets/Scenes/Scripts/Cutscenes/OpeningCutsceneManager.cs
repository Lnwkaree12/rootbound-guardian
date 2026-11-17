using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class OpeningCutsceneManager : MonoBehaviour
{
    [System.Serializable]
    public class CutsceneSlide
    {
        public Sprite slideSprite;
        [TextArea(2, 5)]
        public string subtitleText;
        [Tooltip("Additional reading pause in seconds after typewriter completes")]
        public float readPause = 4.0f;
        [Tooltip("Start scale: starts as a smaller picture")]
        public float startScale = 0.72f;
        [Tooltip("End scale: expands to full image")]
        public float endScale = 1.0f;
        [Tooltip("Duration of zoom expansion animation")]
        public float zoomDuration = 6.0f;
    }

    [Header("Cutscene Sequence")]
    [SerializeField] private List<CutsceneSlide> slides = new List<CutsceneSlide>();
    [SerializeField] private string nextSceneName = "LoadingScreenNew";

    [Header("Slide Image Display")]
    [SerializeField] private RectTransform activeImageRect;
    [SerializeField] private Image activeImage;
    [SerializeField] private CanvasGroup activeImageGroup;

    [SerializeField] private RectTransform crossfadeImageRect;
    [SerializeField] private Image crossfadeImage;
    [SerializeField] private CanvasGroup crossfadeImageGroup;

    [Header("Dialogue Box & Typewriter")]
    [SerializeField] private TextMeshProUGUI subtitleTMP;
    [SerializeField] private GameObject continuePrompt;
    [SerializeField] private float typingSpeed = 0.036f;

    [Header("Progression & UI")]
    [SerializeField] private TextMeshProUGUI progressDotsTMP;
    [SerializeField] private TextMeshProUGUI chapterTitleTMP;
    [SerializeField] private Button skipButton;
    [SerializeField] private CanvasGroup blackFader;

    [Header("Audio Settings")]
    [SerializeField] private AudioSource bgmSource;
    [SerializeField] private AudioClip bgmClip;
    [SerializeField] private AudioClip transitionSFX;
    [SerializeField] [Range(0f, 1f)] private float maxBgmVolume = 0.65f;

    [Header("Particles")]
    [SerializeField] private GameObject dustmotesObj;

    private int currentIndex = 0;
    private bool isTyping = false;
    private bool skipTypingRequested = false;
    private bool advanceRequested = false;
    private bool isSkipping = false;
    private Coroutine cutsceneRoutine;
    private Coroutine zoomRoutine;

    private void Awake()
    {
        if (blackFader != null)
        {
            blackFader.alpha = 1f;
            blackFader.blocksRaycasts = true;
        }

        if (crossfadeImageGroup != null)
        {
            crossfadeImageGroup.alpha = 0f;
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
    }

    private void Start()
    {
        if (bgmSource != null && bgmClip != null)
        {
            bgmSource.clip = bgmClip;
            bgmSource.loop = true;
            bgmSource.volume = 0f;
            bgmSource.Play();
            StartCoroutine(FadeAudio(bgmSource, maxBgmVolume, 1.8f));
        }

        if (slides != null && slides.Count > 0)
        {
            cutsceneRoutine = StartCoroutine(PlayCutsceneSequence());
        }
        else
        {
            LoadNextScene();
        }
    }

    private void Update()
    {
        if (isSkipping) return;

        // Skip hotkeys
        if (Input.GetKeyDown(KeyCode.Escape) || Input.GetKeyDown(KeyCode.Tab))
        {
            OnSkipClicked();
            return;
        }

        // Advance or skip typing: Space, Enter, E, or Left Mouse Click
        if (Input.GetKeyDown(KeyCode.Space) || Input.GetKeyDown(KeyCode.Return) || 
            Input.GetKeyDown(KeyCode.E) || Input.GetMouseButtonDown(0))
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

    private IEnumerator PlayCutsceneSequence()
    {
        // Fade in from black
        yield return StartCoroutine(FadeGroup(blackFader, 1f, 0f, 1.0f));
        if (blackFader != null) blackFader.blocksRaycasts = false;

        for (int i = 0; i < slides.Count; i++)
        {
            currentIndex = i;
            advanceRequested = false;
            skipTypingRequested = false;

            yield return StartCoroutine(PlaySlide(slides[i], i));

            if (isSkipping) yield break;
        }

        // All slides finished -> Fade to black and enter gameplay
        yield return StartCoroutine(EndCutscene());
    }

    private IEnumerator PlaySlide(CutsceneSlide slide, int index)
    {
        UpdateProgressUI(index);

        // Setup active image
        if (activeImage != null)
        {
            activeImage.sprite = slide.slideSprite;
            activeImage.color = Color.white;
        }

        if (activeImageGroup != null)
        {
            activeImageGroup.alpha = 1f;
        }

        // Start Zoom Animation: starts small (startScale) and expands outward to full (endScale)
        if (zoomRoutine != null) StopCoroutine(zoomRoutine);
        zoomRoutine = StartCoroutine(AnimateZoomOut(slide.startScale, slide.endScale, slide.zoomDuration));

        // Play subtle page transition whoosh SFX
        if (transitionSFX != null && bgmSource != null && index > 0)
        {
            bgmSource.PlayOneShot(transitionSFX, 0.45f);
        }

        if (continuePrompt != null)
        {
            continuePrompt.SetActive(false);
        }

        // Run Typewriter Effect for narrative text
        string textToType = LocalizationManager.Instance != null 
            ? LocalizationManager.Instance.GetOpeningCutsceneSlide(index) 
            : slide.subtitleText;
        yield return StartCoroutine(TypewriterRoutine(textToType));

        // Typing finished, show continue prompt
        if (continuePrompt != null)
        {
            continuePrompt.SetActive(true);
        }

        // Wait for player to press advance or wait out read pause
        float pauseTimer = 0f;
        while (pauseTimer < slide.readPause && !advanceRequested && !isSkipping)
        {
            pauseTimer += Time.deltaTime;
            yield return null;
        }

        // Transition to next slide if available
        if (index < slides.Count - 1 && !isSkipping)
        {
            yield return StartCoroutine(TransitionToNextSlide(index));
        }
    }

    private IEnumerator AnimateZoomOut(float fromScale, float toScale, float duration)
    {
        if (activeImageRect == null) yield break;

        float t = 0f;
        activeImageRect.localScale = Vector3.one * fromScale;

        while (t < duration)
        {
            t += Time.deltaTime;
            float progress = Mathf.Clamp01(t / duration);
            // Smooth natural ease-out curve
            float smooth = Mathf.SmoothStep(0f, 1f, progress);
            activeImageRect.localScale = Vector3.one * Mathf.Lerp(fromScale, toScale, smooth);
            yield return null;
        }

        activeImageRect.localScale = Vector3.one * toScale;
    }

    private IEnumerator TypewriterRoutine(string fullText)
    {
        isTyping = true;
        skipTypingRequested = false;

        if (subtitleTMP != null)
        {
            subtitleTMP.text = "";

            for (int i = 0; i < fullText.Length; i++)
            {
                if (skipTypingRequested)
                {
                    subtitleTMP.text = fullText;
                    break;
                }

                subtitleTMP.text += fullText[i];

                char c = fullText[i];
                // Slight pause on commas and periods for natural storytelling rhythm
                if (c == ',' || c == '،') yield return new WaitForSeconds(typingSpeed * 2.5f);
                else if (c == '.' || c == '!' || c == '?') yield return new WaitForSeconds(typingSpeed * 4f);
                else yield return new WaitForSeconds(typingSpeed);
            }
        }

        isTyping = false;
        skipTypingRequested = false;
    }

    private IEnumerator TransitionToNextSlide(int currentIndex)
    {
        if (continuePrompt != null) continuePrompt.SetActive(false);

        // Copy current slide image state into crossfade
        if (crossfadeImage != null && activeImage != null)
        {
            crossfadeImage.sprite = activeImage.sprite;
            crossfadeImage.color = Color.white;
        }
        if (crossfadeImageRect != null && activeImageRect != null)
        {
            crossfadeImageRect.localScale = activeImageRect.localScale;
        }
        if (crossfadeImageGroup != null)
        {
            crossfadeImageGroup.alpha = 1f;
        }

        // Set next slide sprite into active image
        var nextSlide = slides[currentIndex + 1];
        if (activeImage != null)
        {
            activeImage.sprite = nextSlide.slideSprite;
        }
        if (activeImageRect != null)
        {
            activeImageRect.localScale = Vector3.one * nextSlide.startScale;
        }

        // Crossfade out the old image
        float elapsed = 0f;
        float duration = 0.55f;
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float progress = Mathf.Clamp01(elapsed / duration);
            if (crossfadeImageGroup != null)
            {
                crossfadeImageGroup.alpha = 1f - progress;
            }
            yield return null;
        }

        if (crossfadeImageGroup != null)
        {
            crossfadeImageGroup.alpha = 0f;
        }
    }

    private void UpdateProgressUI(int index)
    {
        if (progressDotsTMP != null)
        {
            // Build glowing diamond indicators: ◆ for active/completed, ◇ for upcoming
            System.Text.StringBuilder sb = new System.Text.StringBuilder();
            for (int i = 0; i < slides.Count; i++)
            {
                if (i == index)
                {
                    sb.Append("<color=#FFDF70><b>◆</b></color> ");
                }
                else if (i < index)
                {
                    sb.Append("<color=#E6C665CC>◆</color> ");
                }
                else
                {
                    sb.Append("<color=#FFFFFF40>◇</color> ");
                }
            }
            progressDotsTMP.text = sb.ToString().TrimEnd();
        }

        if (chapterTitleTMP != null)
        {
            chapterTitleTMP.text = $"บทที่ {index + 1} / {slides.Count}";
        }
    }

    public void OnSkipClicked()
    {
        if (isSkipping) return;
        isSkipping = true;

        if (cutsceneRoutine != null) StopCoroutine(cutsceneRoutine);
        if (zoomRoutine != null) StopCoroutine(zoomRoutine);

        StartCoroutine(FastSkipTransition());
    }

    private IEnumerator FastSkipTransition()
    {
        if (blackFader != null) blackFader.blocksRaycasts = true;
        if (bgmSource != null) StartCoroutine(FadeAudio(bgmSource, 0f, 0.4f));

        yield return StartCoroutine(FadeGroup(blackFader, blackFader != null ? blackFader.alpha : 0f, 1f, 0.45f));
        yield return new WaitForSeconds(0.1f);
        LoadNextScene();
    }

    private IEnumerator EndCutscene()
    {
        isSkipping = true;
        if (blackFader != null) blackFader.blocksRaycasts = true;
        if (bgmSource != null) StartCoroutine(FadeAudio(bgmSource, 0f, 0.9f));

        yield return StartCoroutine(FadeGroup(blackFader, 0f, 1f, 0.9f));
        yield return new WaitForSeconds(0.2f);
        LoadNextScene();
    }

    private void LoadNextScene()
    {
        if (!string.IsNullOrEmpty(nextSceneName))
        {
            SceneManager.LoadScene(nextSceneName);
        }
        else
        {
            int currentBuildIndex = SceneManager.GetActiveScene().buildIndex;
            SceneManager.LoadScene(currentBuildIndex + 1);
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
