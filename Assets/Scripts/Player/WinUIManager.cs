using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

public class WinUIManager : MonoBehaviour
{
    private static WinUIManager instance;
    public static WinUIManager Instance => instance;

    [Header("UI References")]
    public GameObject winPanel;
    public CanvasGroup winCanvasGroup;
    public RectTransform modalContainer;

    [Header("Text Displays")]
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
        if (instance == null)
        {
            instance = this;
        }
        else if (instance != this)
        {
            Destroy(this);
            return;
        }

        levelStartTime = Time.timeSinceLevelLoad;

        FindUIReferences();

        // Ensure win panel starts hidden
        if (winPanel != null)
        {
            winPanel.SetActive(false);
        }

        // Setup button listeners
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

        // Prepare audio source
        if (audioSource == null)
        {
            audioSource = GetComponent<AudioSource>();
            if (audioSource == null)
            {
                audioSource = gameObject.AddComponent<AudioSource>();
            }
            audioSource.playOnAwake = false;
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

            if (titleText == null)
            {
                Transform t = winPanel.transform.Find("ModalContainer/InnerCard/TitleText");
                if (t == null) t = winPanel.transform.Find("ModalContainer/TitleText");
                if (t != null) titleText = t.GetComponent<Text>();
            }

            if (subtitleText == null)
            {
                Transform t = winPanel.transform.Find("ModalContainer/InnerCard/SubtitleText");
                if (t == null) t = winPanel.transform.Find("ModalContainer/SubtitleText");
                if (t != null) subtitleText = t.GetComponent<Text>();
            }

            if (questStatusText == null)
            {
                Transform t = winPanel.transform.Find("ModalContainer/InnerCard/StatsPanel/QuestStatusText");
                if (t == null) t = winPanel.transform.Find("ModalContainer/StatsPanel/QuestStatusText");
                if (t != null) questStatusText = t.GetComponent<Text>();
            }

            if (clearTimeText == null)
            {
                Transform t = winPanel.transform.Find("ModalContainer/InnerCard/StatsPanel/TimeText");
                if (t == null) t = winPanel.transform.Find("ModalContainer/StatsPanel/TimeText");
                if (t != null) clearTimeText = t.GetComponent<Text>();
            }

            if (hpRemainingText == null)
            {
                Transform t = winPanel.transform.Find("ModalContainer/InnerCard/StatsPanel/HPText");
                if (t == null) t = winPanel.transform.Find("ModalContainer/StatsPanel/HPText");
                if (t != null) hpRemainingText = t.GetComponent<Text>();
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

    /// <summary>
    /// Trigger victory sequence with an optional delay (e.g. while door swings open)
    /// </summary>
    public void TriggerWin(float delay = 0.6f)
    {
        if (isGameWon) return;
        isGameWon = true;

        StartCoroutine(ShowWinRoutine(delay));
    }

    private IEnumerator ShowWinRoutine(float delay)
    {
        if (delay > 0f)
        {
            yield return new WaitForSeconds(delay);
        }

        FindUIReferences();

        // If winPanel is still missing from the scene, construct it at runtime!
        if (winPanel == null)
        {
            Debug.Log("[WinUIManager] winPanel not found in scene. Creating runtime Win UI...");
            CreateRuntimeWinUI();
        }

        // 1. Calculate and populate stats
        float totalTime = Time.timeSinceLevelLoad - levelStartTime;
        if (totalTime < 0f) totalTime = 0f;
        int minutes = Mathf.FloorToInt(totalTime / 60f);
        int seconds = Mathf.FloorToInt(totalTime % 60f);
        string timeStr = string.Format("{0:00}:{1:00}", minutes, seconds);

        if (clearTimeText != null)
        {
            clearTimeText.text = $"⏱️ เวลาที่ใช้: {timeStr}";
        }

        if (questStatusText != null)
        {
            questStatusText.text = "🔑 ภารกิจ: ใช้กุญแจไขประตูสำเร็จ! (1/1)";
        }

        // Get Player HP
        PlayerStats stats = FindObjectOfType<PlayerStats>();
        if (hpRemainingText != null)
        {
            if (stats != null)
            {
                hpRemainingText.text = $"❤️ พลังชีวิตคงเหลือ: {Mathf.CeilToInt(stats.currentHP)} / {Mathf.CeilToInt(stats.maxHP)} HP";
            }
            else
            {
                hpRemainingText.text = "❤️ พลังชีวิตคงเหลือ: เต็ม 100%";
            }
        }

        // 2. Play Victory Sound / Fanfare
        PlayVictoryAudio();

        // 3. Enable cursor for UI interaction
        Cursor.visible = true;
        Cursor.lockState = CursorLockMode.None;

        // 4. Disable player movement inputs if player exists
        PlayerMovement pm = FindObjectOfType<PlayerMovement>();
        if (pm != null)
        {
            pm.StopVelocity();
            pm.enabled = false;
        }

        // 5. Activate Panel & Run smooth animation
        winPanel.SetActive(true);

        if (winCanvasGroup != null) winCanvasGroup.alpha = 0f;
        if (modalContainer != null) modalContainer.localScale = new Vector3(0.75f, 0.75f, 1f);

        float elapsed = 0f;
        float animDuration = 0.45f;

        while (elapsed < animDuration)
        {
            elapsed += Time.unscaledDeltaTime;
            float t = Mathf.Clamp01(elapsed / animDuration);
            
            // Smooth ease out cubic
            float ease = 1f - Mathf.Pow(1f - t, 3);

            if (winCanvasGroup != null)
            {
                winCanvasGroup.alpha = t;
            }

            if (modalContainer != null)
            {
                modalContainer.localScale = Vector3.LerpUnclamped(new Vector3(0.75f, 0.75f, 1f), Vector3.one, ease);
            }

            yield return null;
        }

        if (winCanvasGroup != null) winCanvasGroup.alpha = 1f;
        if (modalContainer != null) modalContainer.localScale = Vector3.one;

        // 6. Pause game physics and time
        Time.timeScale = 0f;

        Debug.Log("[WinUIManager] Victory UI displayed successfully!");
    }

    private void CreateRuntimeWinUI()
    {
        // Find existing canvas or create new ScreenSpace overlay canvas
        Canvas canvas = GetComponentInParent<Canvas>();
        if (canvas == null)
        {
            Canvas[] allCanvases = FindObjectsOfType<Canvas>();
            foreach (var c in allCanvases)
            {
                if (c.renderMode == RenderMode.ScreenSpaceOverlay)
                {
                    canvas = c;
                    break;
                }
            }
        }

        if (canvas == null)
        {
            GameObject canvasGO = new GameObject("Runtime_Win_Canvas");
            canvas = canvasGO.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 999;

            CanvasScaler scaler = canvasGO.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);
            scaler.matchWidthOrHeight = 0.5f;

            canvasGO.AddComponent<GraphicRaycaster>();
        }

        Font font = LoadFont();

        // 1. WinPanel
        winPanel = new GameObject("WinPanel");
        winPanel.transform.SetParent(canvas.transform, false);
        Image bg = winPanel.AddComponent<Image>();
        bg.color = new Color(0f, 0f, 0f, 0.82f);
        RectTransform winRect = winPanel.GetComponent<RectTransform>();
        winRect.anchorMin = Vector2.zero;
        winRect.anchorMax = Vector2.one;
        winRect.sizeDelta = Vector2.zero;

        winCanvasGroup = winPanel.AddComponent<CanvasGroup>();
        winCanvasGroup.alpha = 1f;
        winCanvasGroup.interactable = true;
        winCanvasGroup.blocksRaycasts = true;

        // 2. ModalContainer (Gold Border)
        GameObject modalGO = new GameObject("ModalContainer");
        modalGO.transform.SetParent(winPanel.transform, false);
        Image border = modalGO.AddComponent<Image>();
        border.color = new Color(0.85f, 0.65f, 0.25f, 1f);
        modalContainer = modalGO.GetComponent<RectTransform>();
        modalContainer.anchorMin = new Vector2(0.5f, 0.5f);
        modalContainer.anchorMax = new Vector2(0.5f, 0.5f);
        modalContainer.pivot = new Vector2(0.5f, 0.5f);
        modalContainer.sizeDelta = new Vector2(580f, 440f);

        // Inner Card (Dark Wood)
        GameObject cardGO = new GameObject("InnerCard");
        cardGO.transform.SetParent(modalGO.transform, false);
        Image card = cardGO.AddComponent<Image>();
        card.color = new Color(0.12f, 0.08f, 0.06f, 0.98f);
        RectTransform cardRect = cardGO.GetComponent<RectTransform>();
        cardRect.anchorMin = Vector2.zero;
        cardRect.anchorMax = Vector2.one;
        cardRect.offsetMin = new Vector2(4f, 4f);
        cardRect.offsetMax = new Vector2(-4f, -4f);

        // Badge
        GameObject badgeGO = new GameObject("BadgeText");
        badgeGO.transform.SetParent(cardGO.transform, false);
        Text badge = badgeGO.AddComponent<Text>();
        badge.font = font;
        badge.fontSize = 18;
        badge.color = new Color(1f, 0.85f, 0.35f);
        badge.alignment = TextAnchor.MiddleCenter;
        badge.text = "★ ✦  VICTORY  ✦ ★";
        RectTransform bRect = badgeGO.GetComponent<RectTransform>();
        bRect.anchorMin = new Vector2(0.5f, 1f);
        bRect.anchorMax = new Vector2(0.5f, 1f);
        bRect.pivot = new Vector2(0.5f, 1f);
        bRect.anchoredPosition = new Vector2(0f, -22f);
        bRect.sizeDelta = new Vector2(400f, 26f);

        // Title
        GameObject titleGO = new GameObject("TitleText");
        titleGO.transform.SetParent(cardGO.transform, false);
        titleText = titleGO.AddComponent<Text>();
        titleText.font = font;
        titleText.fontSize = 38;
        titleText.fontStyle = FontStyle.Bold;
        titleText.color = new Color(1f, 0.82f, 0.2f);
        titleText.alignment = TextAnchor.MiddleCenter;
        titleText.text = "ภารกิจสำเร็จ!";
        RectTransform tRect = titleGO.GetComponent<RectTransform>();
        tRect.anchorMin = new Vector2(0.5f, 1f);
        tRect.anchorMax = new Vector2(0.5f, 1f);
        tRect.pivot = new Vector2(0.5f, 1f);
        tRect.anchoredPosition = new Vector2(0f, -50f);
        tRect.sizeDelta = new Vector2(460f, 48f);

        // Subtitle
        GameObject subGO = new GameObject("SubtitleText");
        subGO.transform.SetParent(cardGO.transform, false);
        subtitleText = subGO.AddComponent<Text>();
        subtitleText.font = font;
        subtitleText.fontSize = 16;
        subtitleText.color = new Color(0.9f, 0.86f, 0.8f);
        subtitleText.alignment = TextAnchor.MiddleCenter;
        subtitleText.text = "ยินดีด้วย! คุณใช้กุญแจไขประตูดันเจี้ยนและผ่านด่านได้สำเร็จ";
        RectTransform sRect = subGO.GetComponent<RectTransform>();
        sRect.anchorMin = new Vector2(0.5f, 1f);
        sRect.anchorMax = new Vector2(0.5f, 1f);
        sRect.pivot = new Vector2(0.5f, 1f);
        sRect.anchoredPosition = new Vector2(0f, -100f);
        sRect.sizeDelta = new Vector2(500f, 28f);

        // Stats Panel
        GameObject statsGO = new GameObject("StatsPanel");
        statsGO.transform.SetParent(cardGO.transform, false);
        Image statsBg = statsGO.AddComponent<Image>();
        statsBg.color = new Color(0.08f, 0.05f, 0.04f, 0.95f);
        RectTransform statsRect = statsGO.GetComponent<RectTransform>();
        statsRect.anchorMin = new Vector2(0.5f, 0.5f);
        statsRect.anchorMax = new Vector2(0.5f, 0.5f);
        statsRect.pivot = new Vector2(0.5f, 0.5f);
        statsRect.anchoredPosition = new Vector2(0f, -15f);
        statsRect.sizeDelta = new Vector2(500f, 130f);

        // Quest Stat
        GameObject qStatGO = new GameObject("QuestStatusText");
        qStatGO.transform.SetParent(statsGO.transform, false);
        questStatusText = qStatGO.AddComponent<Text>();
        questStatusText.font = font;
        questStatusText.fontSize = 18;
        questStatusText.color = new Color(1f, 0.85f, 0.3f);
        questStatusText.alignment = TextAnchor.MiddleCenter;
        questStatusText.text = "🔑 ภารกิจ: ใช้กุญแจไขประตูสำเร็จ! (1/1)";
        RectTransform qRect = qStatGO.GetComponent<RectTransform>();
        qRect.anchorMin = new Vector2(0.5f, 1f);
        qRect.anchorMax = new Vector2(0.5f, 1f);
        qRect.pivot = new Vector2(0.5f, 1f);
        qRect.anchoredPosition = new Vector2(0f, -16f);
        qRect.sizeDelta = new Vector2(460f, 28f);

        // Time Stat
        GameObject timeGO = new GameObject("TimeText");
        timeGO.transform.SetParent(statsGO.transform, false);
        clearTimeText = timeGO.AddComponent<Text>();
        clearTimeText.font = font;
        clearTimeText.fontSize = 17;
        clearTimeText.color = Color.white;
        clearTimeText.alignment = TextAnchor.MiddleCenter;
        clearTimeText.text = "⏱️ เวลาที่ใช้: 00:00";
        RectTransform tmRect = timeGO.GetComponent<RectTransform>();
        tmRect.anchorMin = new Vector2(0.5f, 1f);
        tmRect.anchorMax = new Vector2(0.5f, 1f);
        tmRect.pivot = new Vector2(0.5f, 1f);
        tmRect.anchoredPosition = new Vector2(0f, -50f);
        tmRect.sizeDelta = new Vector2(460f, 26f);

        // HP Stat
        GameObject hpGO = new GameObject("HPText");
        hpGO.transform.SetParent(statsGO.transform, false);
        hpRemainingText = hpGO.AddComponent<Text>();
        hpRemainingText.font = font;
        hpRemainingText.fontSize = 16;
        hpRemainingText.color = new Color(0.45f, 0.9f, 0.45f);
        hpRemainingText.alignment = TextAnchor.MiddleCenter;
        hpRemainingText.text = "❤️ พลังชีวิตคงเหลือ: 100 / 100 HP";
        RectTransform hpRect = hpGO.GetComponent<RectTransform>();
        hpRect.anchorMin = new Vector2(0.5f, 1f);
        hpRect.anchorMax = new Vector2(0.5f, 1f);
        hpRect.pivot = new Vector2(0.5f, 1f);
        hpRect.anchoredPosition = new Vector2(0f, -84f);
        hpRect.sizeDelta = new Vector2(460f, 26f);

        // Button Row
        GameObject rowGO = new GameObject("ButtonRow");
        rowGO.transform.SetParent(cardGO.transform, false);
        RectTransform rowRect = rowGO.AddComponent<RectTransform>();
        rowRect.anchorMin = new Vector2(0.5f, 0f);
        rowRect.anchorMax = new Vector2(0.5f, 0f);
        rowRect.pivot = new Vector2(0.5f, 0f);
        rowRect.anchoredPosition = new Vector2(0f, 25f);
        rowRect.sizeDelta = new Vector2(500f, 52f);

        // Restart Button
        GameObject rBtnGO = new GameObject("RestartButton");
        rBtnGO.transform.SetParent(rowGO.transform, false);
        Image rBg = rBtnGO.AddComponent<Image>();
        rBg.color = new Color(0.24f, 0.16f, 0.12f, 1f);
        restartButton = rBtnGO.AddComponent<Button>();
        RectTransform rBtnRect = rBtnGO.GetComponent<RectTransform>();
        rBtnRect.anchorMin = new Vector2(0f, 0.5f);
        rBtnRect.anchorMax = new Vector2(0f, 0.5f);
        rBtnRect.pivot = new Vector2(0f, 0.5f);
        rBtnRect.anchoredPosition = new Vector2(15f, 0f);
        rBtnRect.sizeDelta = new Vector2(220f, 48f);

        GameObject rTextGO = new GameObject("Text");
        rTextGO.transform.SetParent(rBtnGO.transform, false);
        Text rText = rTextGO.AddComponent<Text>();
        rText.font = font;
        rText.fontSize = 18;
        rText.color = new Color(1f, 0.9f, 0.5f);
        rText.alignment = TextAnchor.MiddleCenter;
        rText.text = "🔄 เล่นใหม่อีกครั้ง";
        RectTransform rtRect = rTextGO.GetComponent<RectTransform>();
        rtRect.anchorMin = Vector2.zero;
        rtRect.anchorMax = Vector2.one;
        rtRect.sizeDelta = Vector2.zero;

        restartButton.onClick.RemoveAllListeners();
        restartButton.onClick.AddListener(RestartLevel);

        // Main Menu Button
        GameObject mBtnGO = new GameObject("MainMenuButton");
        mBtnGO.transform.SetParent(rowGO.transform, false);
        Image mBg = mBtnGO.AddComponent<Image>();
        mBg.color = new Color(0.24f, 0.16f, 0.12f, 1f);
        mainMenuButton = mBtnGO.AddComponent<Button>();
        RectTransform mBtnRect = mBtnGO.GetComponent<RectTransform>();
        mBtnRect.anchorMin = new Vector2(1f, 0.5f);
        mBtnRect.anchorMax = new Vector2(1f, 0.5f);
        mBtnRect.pivot = new Vector2(1f, 0.5f);
        mBtnRect.anchoredPosition = new Vector2(-15f, 0f);
        mBtnRect.sizeDelta = new Vector2(220f, 48f);

        GameObject mTextGO = new GameObject("Text");
        mTextGO.transform.SetParent(mBtnGO.transform, false);
        Text mText = mTextGO.AddComponent<Text>();
        mText.font = font;
        mText.fontSize = 18;
        mText.color = Color.white;
        mText.alignment = TextAnchor.MiddleCenter;
        mText.text = "🏠 กลับหน้าหลัก";
        RectTransform mtRect = mTextGO.GetComponent<RectTransform>();
        mtRect.anchorMin = Vector2.zero;
        mtRect.anchorMax = Vector2.one;
        mtRect.sizeDelta = Vector2.zero;

        mainMenuButton.onClick.RemoveAllListeners();
        mainMenuButton.onClick.AddListener(GoToMainMenu);
    }

    private Font LoadFont()
    {
        Font font = Resources.Load<Font>("Itim-Regular");
        #if UNITY_EDITOR
        if (font == null) font = UnityEditor.AssetDatabase.LoadAssetAtPath<Font>("Assets/Itim-Regular.ttf");
        #endif
        if (font == null)
        {
            try { font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf"); }
            catch { try { font = Resources.GetBuiltinResource<Font>("Arial.ttf"); } catch { } }
        }
        return font;
    }

    private void PlayVictoryAudio()
    {
        if (audioSource == null) return;

        if (victorySound != null)
        {
            audioSource.PlayOneShot(victorySound);
        }
        else
        {
            // Synthesize a cheerful procedural fantasy victory fanfare arpeggio!
            AudioClip proceduralFanfare = CreateProceduralFanfare();
            if (proceduralFanfare != null)
            {
                audioSource.PlayOneShot(proceduralFanfare);
            }
        }
    }

    private AudioClip CreateProceduralFanfare()
    {
        int sampleRate = 44100;
        float duration = 1.4f;
        int sampleCount = Mathf.FloorToInt(sampleRate * duration);
        float[] samples = new float[sampleCount];

        // Notes: C5 (523.25Hz), E5 (659.25Hz), G5 (783.99Hz), C6 (1046.50Hz)
        float[] notes = new float[] { 523.25f, 659.25f, 783.99f, 1046.50f };
        float noteDuration = 0.28f;

        for (int i = 0; i < sampleCount; i++)
        {
            float time = (float)i / sampleRate;
            int noteIndex = Mathf.Clamp(Mathf.FloorToInt(time / noteDuration), 0, notes.Length - 1);
            float freq = notes[noteIndex];
            
            float noteTime = time - (noteIndex * noteDuration);
            float envelope = Mathf.Exp(-3.2f * noteTime);

            // Sine wave + harmonic
            float val = (Mathf.Sin(2f * Mathf.PI * freq * time) * 0.7f +
                         Mathf.Sin(4f * Mathf.PI * freq * time) * 0.3f) * envelope;

            samples[i] = val * 0.45f;
        }

        AudioClip clip = AudioClip.Create("ProceduralVictoryFanfare", sampleCount, 1, sampleRate, false);
        clip.SetData(samples, 0);
        return clip;
    }

    public void RestartLevel()
    {
        Debug.Log("[WinUIManager] Restarting level...");
        Time.timeScale = 1f;
        Scene currentScene = SceneManager.GetActiveScene();
        SceneManager.LoadScene(currentScene.buildIndex);
    }

    public void GoToMainMenu()
    {
        Debug.Log("[WinUIManager] Returning to Main Menu...");
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

    void Update()
    {
        // Keyboard shortcuts when victory is active
        if (isGameWon)
        {
            if (Input.GetKeyDown(KeyCode.R))
            {
                RestartLevel();
            }
            else if (Input.GetKeyDown(KeyCode.M) || Input.GetKeyDown(KeyCode.Escape))
            {
                GoToMainMenu();
            }
        }
    }
}
