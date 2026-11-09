using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

/// <summary>
/// Controls the new polished Loading Screen:
/// - Staging Lumi companion on the left side with floating bobbing & rotating dialogue.
/// - Displaying a 7-row interactive Tutorial guide on the right side.
/// - Smooth asynchronous scene loading with progress bar and percentage.
/// - "Press Any Key / Space to Start" prompt when ready with smooth transition.
/// </summary>
public class NewLoadingScreenManager : MonoBehaviour
{
    [Header("Scene Loading Configuration")]
    [Tooltip("Target scene to load")]
    [SerializeField] private string targetSceneName = "MainGameplay";
    [Tooltip("Minimum display duration in seconds to allow reading tutorial")]
    [SerializeField] private float minimumDisplayTime = 4.5f;

    [Header("Loading Progress UI")]
    [SerializeField] private Slider progressBar;
    [SerializeField] private UnityEngine.UI.Image progressFillImage;
    [SerializeField] private TextMeshProUGUI statusText;
    [SerializeField] private TextMeshProUGUI percentText;
    [SerializeField] private GameObject readyPromptObj;
    [SerializeField] private TextMeshProUGUI readyPromptText;
    [SerializeField] private CanvasGroup blackFader;

    [Header("Lumi Display & Banter")]
    [SerializeField] private Transform lumiTransform;
    [SerializeField] private TextMeshProUGUI lumiDialogueTMP;
    [SerializeField] private CanvasGroup lumiSpeechBubbleGroup;
    [SerializeField] private float lumiHoverSpeed = 2.4f;
    [SerializeField] private float lumiHoverHeight = 0.12f;

    [Header("Lumi Dialogue Lines")]
    [SerializeField] private List<string> lumiTips = new List<string>()
    {
        "จำปุ่มควบคุมพวกนี้ไว้ให้ดีนะเจ้ามนุษย์! ข้าจะคอยส่องทางให้เจ้าในดันเจี้ยนเอง ฮิๆ~",
        "ถ้าเจอต้นไม้เล็ก อย่าลืมใช้โพชั่นฟื้นฟูเพื่อเซฟจุดเกิดด้วยล่ะ!",
        "ระวังอย่าให้ออกซิเจนหมดนะ! ถ้าเหนื่อยให้รีบกลับมาพักใกล้ต้นไม้ศักดิ์สิทธิ์!",
        "กับดักในดันเจี้ยนอันตรายมาก... ใช้แดชหลบหลีกให้คล่องแคล่วนะ!",
        "บันไดไม้พวกนั้น แค่เดินชนแล้วกดปุ่มทิศทางค้างไว้เจ้าก็ปีนขึ้นได้สบายๆ!"
    };

    [Header("Audio Settings")]
    [SerializeField] private AudioSource bgmSource;
    [SerializeField] private AudioClip bgmClip;
    [SerializeField] private AudioClip startConfirmSFX;
    [SerializeField] [Range(0f, 1f)] private float maxBgmVolume = 0.55f;

    // Internal State
    private AsyncOperation asyncOperation;
    private float currentProgress = 0f;
    private float targetProgress = 0f;
    private bool isLoadingComplete = false;
    private bool isTransitioning = false;
    private Vector3 lumiInitialPos;
    private int currentTipIndex = 0;
    private Coroutine tipCycleRoutine;

    private void Awake()
    {
        if (blackFader != null)
        {
            blackFader.alpha = 1f;
            blackFader.blocksRaycasts = true;
        }

        if (readyPromptObj != null)
        {
            readyPromptObj.SetActive(false);
        }

        if (lumiTransform != null)
        {
            lumiInitialPos = lumiTransform.position;
        }
    }

    private void OnEnable()
    {
        LocalizationManager.OnLanguageChanged += OnLanguageChanged;
        UpdateLocalizedTutorialUI();
    }

    private void OnDisable()
    {
        LocalizationManager.OnLanguageChanged -= OnLanguageChanged;
    }

    private void OnLanguageChanged(LocalizationManager.Language lang)
    {
        UpdateLocalizedTutorialUI();
    }

    private void UpdateLocalizedTutorialUI()
    {
        if (LocalizationManager.Instance == null) return;

        // Spirit Name
        GameObject nameObj = GameObject.Find("Name_Text");
        if (nameObj != null)
        {
            var tmp = nameObj.GetComponent<TextMeshProUGUI>();
            if (tmp != null) tmp.text = LocalizationManager.Instance.Get("load_guide_spirit_name");
        }

        // Tag text
        GameObject tagObj = GameObject.Find("Tag_Text");
        if (tagObj != null)
        {
            var tmp = tagObj.GetComponent<TextMeshProUGUI>();
            if (tmp != null) tmp.text = LocalizationManager.Instance.Get("load_guide_tag");
        }

        // Header controls
        GameObject headerObj = GameObject.Find("Header_Text");
        if (headerObj != null)
        {
            var tmp = headerObj.GetComponent<TextMeshProUGUI>();
            if (tmp != null) tmp.text = LocalizationManager.Instance.Get("load_header_controls");
        }

        // Rows 1-7
        string[] rowKeys = new string[]
        {
            "load_row_move",
            "load_row_sprint",
            "load_row_interact",
            "load_row_potion",
            "load_row_chest",
            "load_row_tree",
            "load_row_climb"
        };

        for (int i = 0; i < rowKeys.Length; i++)
        {
            GameObject rowObj = GameObject.Find($"Tutorial_Row_{i + 1}");
            if (rowObj != null)
            {
                Transform textContainer = rowObj.transform.Find("Text_Container");
                if (textContainer != null)
                {
                    var tmp = textContainer.GetComponent<TextMeshProUGUI>();
                    if (tmp != null) tmp.text = LocalizationManager.Instance.Get(rowKeys[i]);
                }
            }
        }

        if (readyPromptText != null)
        {
            readyPromptText.text = LocalizationManager.Instance.Get("load_ready_prompt");
        }
    }

    private void Start()
    {
        // Start atmospheric BGM
        if (bgmSource != null && bgmClip != null)
        {
            bgmSource.clip = bgmClip;
            bgmSource.loop = true;
            bgmSource.volume = 0f;
            bgmSource.Play();
            StartCoroutine(FadeAudio(bgmSource, maxBgmVolume, 1.5f));
        }

        // Start tip rotation
        if (lumiDialogueTMP != null && lumiTips.Count > 0)
        {
            tipCycleRoutine = StartCoroutine(CycleLumiTips());
        }

        // Start loading routine
        StartCoroutine(LoadingSequenceRoutine());
    }

    private void Update()
    {
        // 1. Lumi hover floating bobbing
        if (lumiTransform != null)
        {
            float yOffset = Mathf.Sin(Time.time * lumiHoverSpeed) * lumiHoverHeight;
            lumiTransform.position = lumiInitialPos + new Vector3(0f, yOffset, 0f);
        }

        // 2. Smooth progress bar lerp
        if (!isLoadingComplete)
        {
            currentProgress = Mathf.MoveTowards(currentProgress, targetProgress, Time.deltaTime * 0.85f);
            UpdateProgressDisplay(currentProgress);
        }

        // 3. Ready to start input
        if (isLoadingComplete && !isTransitioning)
        {
            // Pulse ready prompt text
            if (readyPromptText != null)
            {
                float pulse = 0.75f + Mathf.PingPong(Time.time * 2.0f, 0.25f);
                readyPromptText.alpha = pulse;
            }

            // Check any key or mouse click to enter game
            if (Input.anyKeyDown || Input.GetMouseButtonDown(0))
            {
                StartCoroutine(EnterGameRoutine());
            }
        }
    }

    private void UpdateProgressDisplay(float progress)
    {
        if (progressBar != null)
        {
            progressBar.value = progress;
        }

        if (percentText != null)
        {
            int p = Mathf.RoundToInt(progress * 100f);
            percentText.text = $"{p}%";
        }
    }

    private IEnumerator LoadingSequenceRoutine()
    {
        // Fade in from black
        yield return StartCoroutine(FadeGroup(blackFader, 1f, 0f, 0.8f));
        if (blackFader != null) blackFader.blocksRaycasts = false;

        float startTime = Time.time;

        // Start asynchronous scene loading in background
        if (!string.IsNullOrEmpty(targetSceneName) && Application.CanStreamedLevelBeLoaded(targetSceneName))
        {
            asyncOperation = SceneManager.LoadSceneAsync(targetSceneName);
            if (asyncOperation != null)
            {
                asyncOperation.allowSceneActivation = false;
            }
        }

        if (statusText != null)
        {
            statusText.text = LocalizationManager.Instance != null ? LocalizationManager.Instance.Get("load_status_preparing") : "กำลังเตรียมพร้อมเข้าสู่ดันเจี้ยน...";
        }

        // Progress simulation & tracking
        while ((Time.time - startTime < minimumDisplayTime) || (asyncOperation != null && asyncOperation.progress < 0.9f))
        {
            float timeFactor = Mathf.Clamp01((Time.time - startTime) / minimumDisplayTime);
            float asyncFactor = asyncOperation != null ? Mathf.Clamp01(asyncOperation.progress / 0.9f) : 1f;

            // Combine both so it looks organic and guarantees minimum display time
            targetProgress = Mathf.Min(timeFactor, asyncFactor) * 0.95f;
            yield return null;
        }

        // Final 100% completion
        targetProgress = 1.0f;
        while (currentProgress < 0.99f)
        {
            currentProgress = Mathf.MoveTowards(currentProgress, 1.0f, Time.deltaTime * 1.5f);
            UpdateProgressDisplay(currentProgress);
            yield return null;
        }

        currentProgress = 1.0f;
        UpdateProgressDisplay(1.0f);
        isLoadingComplete = true;

        if (statusText != null)
        {
            statusText.text = LocalizationManager.Instance != null ? LocalizationManager.Instance.Get("load_status_ready") : "เตรียมพร้อมผจญภัยเรียบร้อย!";
        }

        // Show Ready Prompt
        if (readyPromptObj != null)
        {
            readyPromptObj.SetActive(true);
        }
    }

    private IEnumerator EnterGameRoutine()
    {
        isTransitioning = true;

        // Play confirm SFX
        if (startConfirmSFX != null && bgmSource != null)
        {
            bgmSource.PlayOneShot(startConfirmSFX, 0.7f);
        }

        // Fade audio & screen
        if (bgmSource != null) StartCoroutine(FadeAudio(bgmSource, 0f, 0.5f));
        if (blackFader != null)
        {
            blackFader.blocksRaycasts = true;
            yield return StartCoroutine(FadeGroup(blackFader, 0f, 1f, 0.5f));
        }

        yield return new WaitForSeconds(0.15f);

        // Activate scene
        if (asyncOperation != null)
        {
            asyncOperation.allowSceneActivation = true;
        }
        else
        {
            SceneManager.LoadScene(targetSceneName);
        }
    }

    private IEnumerator CycleLumiTips()
    {
        while (true)
        {
            if (lumiDialogueTMP != null && lumiTips.Count > 0)
            {
                // Fade out bubble
                if (lumiSpeechBubbleGroup != null)
                {
                    yield return StartCoroutine(FadeGroup(lumiSpeechBubbleGroup, 1f, 0f, 0.25f));
                }

                string tip = LocalizationManager.Instance != null 
                    ? LocalizationManager.Instance.GetLoadingTip(currentTipIndex) 
                    : (currentTipIndex < lumiTips.Count ? lumiTips[currentTipIndex] : "");
                lumiDialogueTMP.text = tip;
                currentTipIndex = (currentTipIndex + 1) % Mathf.Max(1, lumiTips.Count);

                // Fade in bubble
                if (lumiSpeechBubbleGroup != null)
                {
                    yield return StartCoroutine(FadeGroup(lumiSpeechBubbleGroup, 0f, 1f, 0.35f));
                }
            }

            yield return new WaitForSeconds(4.2f);
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

