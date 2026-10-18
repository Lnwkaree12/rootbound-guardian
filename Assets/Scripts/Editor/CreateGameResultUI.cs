#if UNITY_EDITOR
using System.IO;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// Editor utility to generate authentic, cozy fairytale Win and Game Over UI prefabs
/// for Rootbound Guardian matching the game's exact aesthetic (cat paws, sprouts, golden banners,
/// wooden/marshmallow cards, and rich typography).
/// Strictly builds prefabs without modifying or opening any scene.
/// </summary>
public static class CreateGameResultUI
{
    private const string WIN_PREFAB_PATH = "Assets/Prefabs/Win_Canvas.prefab";
    private const string GAMEOVER_PREFAB_PATH = "Assets/Prefabs/GameOver_Canvas.prefab";

    private const string FONT_ONELITTLE = "Assets/Image/OneLittleFont-Full SDF.asset";
    private const string FONT_ITIM = "Assets/Fonts/Itim_SDF_Valid.asset";

    private const string SPRITE_MODAL_CARD = "Assets/Main Screen/UI/ModalCardBg.png";
    private const string SPRITE_KEY_BADGE = "Assets/Main Screen/UI/KeyBadgeBg.png";
    private const string SPRITE_PAW = "Assets/Main Screen/UI/PawIcon.png";
    private const string SPRITE_LEAF = "Assets/Main Screen/UI/LeafIcon.png";
    private const string SPRITE_BANNER = "Assets/Image/UI/Quest_Banner.png";
    private const string SPRITE_CHEST = "Assets/Image/UI/Cute_Chest_Badge.png";
    private const string SPRITE_SPROUT = "Assets/Image/UI/Cute_Sprout_Badge.png";
    private const string SPRITE_HEART = "Assets/Image/UI/Heart_Icon.png";
    private const string SPRITE_BUBBLE = "Assets/Image/UI/Cute_Tree_Bubble_Bg.png";

    [MenuItem("Tools/Rootbound Guardian/Build Win & Game Over UI Prefabs")]
    public static void BuildAllResultPrefabs()
    {
        Debug.Log("[CreateGameResultUI] Starting build of Win & Game Over UI prefabs...");

        EnsurePrefabsDirectory();
        BuildWinCanvasPrefab();
        BuildGameOverCanvasPrefab();

        AssetDatabase.Refresh();
        Debug.Log("[CreateGameResultUI] Successfully created Win_Canvas.prefab and GameOver_Canvas.prefab!");
    }

    [MenuItem("Tools/Rootbound Guardian/Build Win UI Prefab Only")]
    public static void BuildWinOnly()
    {
        EnsurePrefabsDirectory();
        BuildWinCanvasPrefab();
        AssetDatabase.Refresh();
    }

    [MenuItem("Tools/Rootbound Guardian/Build Game Over UI Prefab Only")]
    public static void BuildGameOverOnly()
    {
        EnsurePrefabsDirectory();
        BuildGameOverCanvasPrefab();
        AssetDatabase.Refresh();
    }

    private static void EnsurePrefabsDirectory()
    {
        if (!Directory.Exists("Assets/Prefabs"))
        {
            Directory.CreateDirectory("Assets/Prefabs");
        }
    }

    #region Win Canvas Prefab

    public static void BuildWinCanvasPrefab()
    {
        TMP_FontAsset titleFont = LoadFont(FONT_ONELITTLE);
        TMP_FontAsset bodyFont = LoadFont(FONT_ITIM);

        Sprite modalCardSprite = AssetDatabase.LoadAssetAtPath<Sprite>(SPRITE_MODAL_CARD);
        Sprite keyBadgeSprite = AssetDatabase.LoadAssetAtPath<Sprite>(SPRITE_KEY_BADGE);
        Sprite leafSprite = AssetDatabase.LoadAssetAtPath<Sprite>(SPRITE_LEAF);
        if (leafSprite == null) leafSprite = AssetDatabase.LoadAssetAtPath<Sprite>(SPRITE_PAW);
        Sprite bannerSprite = AssetDatabase.LoadAssetAtPath<Sprite>(SPRITE_BANNER);
        Sprite chestSprite = AssetDatabase.LoadAssetAtPath<Sprite>(SPRITE_CHEST);
        Sprite sproutSprite = AssetDatabase.LoadAssetAtPath<Sprite>(SPRITE_SPROUT);
        Sprite heartSprite = AssetDatabase.LoadAssetAtPath<Sprite>(SPRITE_HEART);
        Sprite bubbleSprite = AssetDatabase.LoadAssetAtPath<Sprite>(SPRITE_BUBBLE);
        AudioClip victoryAudio = AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Audio/SFX/Key.mp3");

        // 1. Root Canvas
        GameObject canvasGO = new GameObject("Win_Canvas");
        Canvas canvas = canvasGO.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 100;

        CanvasScaler scaler = canvasGO.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);
        scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
        scaler.matchWidthOrHeight = 0.5f;

        canvasGO.AddComponent<GraphicRaycaster>();

        AudioSource audioSource = canvasGO.AddComponent<AudioSource>();
        audioSource.playOnAwake = false;

        WinUIManager winManager = canvasGO.AddComponent<WinUIManager>();
        winManager.audioSource = audioSource;
        winManager.victorySound = victoryAudio;

        // 2. WinPanel (Full Screen Overlay)
        GameObject winPanelGO = CreateFullScreenOverlay(canvasGO.transform, "WinPanel", new Color(0.04f, 0.06f, 0.05f, 0.86f));
        CanvasGroup canvasGroup = winPanelGO.AddComponent<CanvasGroup>();
        canvasGroup.alpha = 1f;
        canvasGroup.interactable = true;
        canvasGroup.blocksRaycasts = true;

        winManager.winPanel = winPanelGO;
        winManager.winCanvasGroup = canvasGroup;

        // 3. Modal Container (Outer Glow Frame)
        GameObject modalContainerGO = new GameObject("ModalContainer");
        modalContainerGO.transform.SetParent(winPanelGO.transform, false);
        RectTransform modalRect = modalContainerGO.AddComponent<RectTransform>();
        modalRect.anchorMin = new Vector2(0.5f, 0.5f);
        modalRect.anchorMax = new Vector2(0.5f, 0.5f);
        modalRect.pivot = new Vector2(0.5f, 0.5f);
        modalRect.anchoredPosition = Vector2.zero;
        modalRect.sizeDelta = new Vector2(860f, 620f);

        Image modalBorder = modalContainerGO.AddComponent<Image>();
        modalBorder.sprite = modalCardSprite;
        modalBorder.type = Image.Type.Sliced;
        modalBorder.color = new Color(0.96f, 0.78f, 0.32f, 0.55f); // Golden glow outline
        winManager.modalContainer = modalRect;

        // 4. Inner Card (Midnight Wood Background)
        GameObject innerCardGO = new GameObject("InnerCard");
        innerCardGO.transform.SetParent(modalContainerGO.transform, false);
        RectTransform innerRect = innerCardGO.AddComponent<RectTransform>();
        innerRect.anchorMin = Vector2.zero;
        innerRect.anchorMax = Vector2.one;
        innerRect.offsetMin = new Vector2(5f, 5f);
        innerRect.offsetMax = new Vector2(-5f, -5f);

        Image cardImg = innerCardGO.AddComponent<Image>();
        cardImg.sprite = modalCardSprite;
        cardImg.type = Image.Type.Sliced;
        cardImg.color = new Color(0.10f, 0.08f, 0.12f, 0.98f);

        // 5. Floating Chest / Sprout Crest at Top
        GameObject crestGO = new GameObject("FloatingCrest");
        crestGO.transform.SetParent(innerCardGO.transform, false);
        RectTransform crestRect = crestGO.AddComponent<RectTransform>();
        crestRect.anchorMin = new Vector2(0.5f, 1f);
        crestRect.anchorMax = new Vector2(0.5f, 1f);
        crestRect.pivot = new Vector2(0.5f, 0.5f);
        crestRect.anchoredPosition = new Vector2(0f, 22f);
        crestRect.sizeDelta = new Vector2(128f, 128f);

        Image crestImg = crestGO.AddComponent<Image>();
        crestImg.sprite = chestSprite != null ? chestSprite : sproutSprite;
        crestImg.raycastTarget = false;
        crestGO.AddComponent<TitleIdleBobbing>();

        // 6. Header Banner & Title
        GameObject bannerGO = new GameObject("Banner");
        bannerGO.transform.SetParent(innerCardGO.transform, false);
        RectTransform bannerRect = bannerGO.AddComponent<RectTransform>();
        bannerRect.anchorMin = new Vector2(0.5f, 1f);
        bannerRect.anchorMax = new Vector2(0.5f, 1f);
        bannerRect.pivot = new Vector2(0.5f, 1f);
        bannerRect.anchoredPosition = new Vector2(0f, -50f);
        bannerRect.sizeDelta = new Vector2(460f, 85f);

        if (bannerSprite != null)
        {
            Image bannerImg = bannerGO.AddComponent<Image>();
            bannerImg.sprite = bannerSprite;
            bannerImg.color = new Color(1f, 1f, 1f, 0.95f);
            bannerImg.raycastTarget = false;
        }

        // Title Text: VICTORY!
        GameObject titleGO = new GameObject("TitleText");
        titleGO.transform.SetParent(bannerGO.transform, false);
        RectTransform titleRect = titleGO.AddComponent<RectTransform>();
        titleRect.anchorMin = Vector2.zero;
        titleRect.anchorMax = Vector2.one;
        titleRect.offsetMin = Vector2.zero;
        titleRect.offsetMax = Vector2.zero;

        TextMeshProUGUI titleTMP = titleGO.AddComponent<TextMeshProUGUI>();
        titleTMP.font = titleFont;
        titleTMP.text = "VICTORY!";
        titleTMP.fontSize = 50f;
        titleTMP.fontStyle = FontStyles.Bold;
        titleTMP.alignment = TextAlignmentOptions.Center;
        titleTMP.color = new Color(1f, 0.88f, 0.45f, 1f); // Radiant Honey Gold
        titleTMP.raycastTarget = false;
        winManager.titleTextTMP = titleTMP;

        // 7. Subtitle Text
        GameObject subGO = new GameObject("SubtitleText");
        subGO.transform.SetParent(innerCardGO.transform, false);
        RectTransform subRect = subGO.AddComponent<RectTransform>();
        subRect.anchorMin = new Vector2(0.5f, 1f);
        subRect.anchorMax = new Vector2(0.5f, 1f);
        subRect.pivot = new Vector2(0.5f, 1f);
        subRect.anchoredPosition = new Vector2(0f, -140f);
        subRect.sizeDelta = new Vector2(720f, 34f);

        TextMeshProUGUI subTMP = subGO.AddComponent<TextMeshProUGUI>();
        subTMP.font = bodyFont;
        subTMP.text = "ภารกิจสำเร็จ! คุณผ่านด่านและปกป้องผืนป่าสำเร็จ";
        subTMP.fontSize = 22f;
        subTMP.alignment = TextAlignmentOptions.Center;
        subTMP.color = new Color(0.96f, 0.92f, 0.88f, 1f);
        subTMP.raycastTarget = false;
        winManager.subtitleTextTMP = subTMP;

        // 8. Stats Panel (Outer Gold Border Frame)
        GameObject statsGO = new GameObject("StatsPanel");
        statsGO.transform.SetParent(innerCardGO.transform, false);
        RectTransform statsRect = statsGO.AddComponent<RectTransform>();
        statsRect.anchorMin = new Vector2(0.5f, 0.5f);
        statsRect.anchorMax = new Vector2(0.5f, 0.5f);
        statsRect.pivot = new Vector2(0.5f, 0.5f);
        statsRect.anchoredPosition = new Vector2(0f, 10f);
        statsRect.sizeDelta = new Vector2(720f, 185f);

        Image statsBorderImg = statsGO.AddComponent<Image>();
        statsBorderImg.sprite = keyBadgeSprite;
        statsBorderImg.type = Image.Type.Sliced;
        statsBorderImg.color = new Color(0.92f, 0.76f, 0.35f, 0.90f); // Golden frame

        // Stats Inner Dark Background
        GameObject statsInnerGO = new GameObject("InnerBg");
        statsInnerGO.transform.SetParent(statsGO.transform, false);
        RectTransform sInnerRect = statsInnerGO.AddComponent<RectTransform>();
        sInnerRect.anchorMin = Vector2.zero;
        sInnerRect.anchorMax = Vector2.one;
        sInnerRect.offsetMin = new Vector2(3f, 3f);
        sInnerRect.offsetMax = new Vector2(-3f, -3f);

        Image statsInnerImg = statsInnerGO.AddComponent<Image>();
        statsInnerImg.sprite = keyBadgeSprite;
        statsInnerImg.type = Image.Type.Sliced;
        statsInnerImg.color = new Color(0.08f, 0.07f, 0.10f, 1f); // Deep Midnight Charcoal
        statsInnerImg.raycastTarget = false;

        // 8a. Quest Status Row
        GameObject questGO = new GameObject("QuestStatusText");
        questGO.transform.SetParent(statsGO.transform, false);
        RectTransform qRect = questGO.AddComponent<RectTransform>();
        qRect.anchorMin = new Vector2(0.5f, 1f);
        qRect.anchorMax = new Vector2(0.5f, 1f);
        qRect.pivot = new Vector2(0.5f, 1f);
        qRect.anchoredPosition = new Vector2(0f, -20f);
        qRect.sizeDelta = new Vector2(660f, 34f);

        TextMeshProUGUI qTMP = questGO.AddComponent<TextMeshProUGUI>();
        qTMP.font = bodyFont;
        qTMP.text = "ภารกิจ: ใช้กุญแจไขประตูดันเจี้ยนสำเร็จ! (1/1)";
        qTMP.fontSize = 21f;
        qTMP.fontStyle = FontStyles.Bold;
        qTMP.alignment = TextAlignmentOptions.Center;
        qTMP.color = new Color(1f, 0.88f, 0.45f, 1f);
        qTMP.raycastTarget = false;
        winManager.questStatusTextTMP = qTMP;

        // 8b. Time Row
        GameObject timeGO = new GameObject("TimeText");
        timeGO.transform.SetParent(statsGO.transform, false);
        RectTransform timeRect = timeGO.AddComponent<RectTransform>();
        timeRect.anchorMin = new Vector2(0.5f, 1f);
        timeRect.anchorMax = new Vector2(0.5f, 1f);
        timeRect.pivot = new Vector2(0.5f, 1f);
        timeRect.anchoredPosition = new Vector2(0f, -68f);
        timeRect.sizeDelta = new Vector2(660f, 32f);

        TextMeshProUGUI timeTMP = timeGO.AddComponent<TextMeshProUGUI>();
        timeTMP.font = bodyFont;
        timeTMP.text = "เวลาที่ใช้: 00:00";
        timeTMP.fontSize = 20f;
        timeTMP.alignment = TextAlignmentOptions.Center;
        timeTMP.color = Color.white;
        timeTMP.raycastTarget = false;
        winManager.clearTimeTextTMP = timeTMP;

        // 8c. HP Row
        GameObject hpGO = new GameObject("HPText");
        hpGO.transform.SetParent(statsGO.transform, false);
        RectTransform hpRect = hpGO.AddComponent<RectTransform>();
        hpRect.anchorMin = new Vector2(0.5f, 1f);
        hpRect.anchorMax = new Vector2(0.5f, 1f);
        hpRect.pivot = new Vector2(0.5f, 1f);
        hpRect.anchoredPosition = new Vector2(0f, -116f);
        hpRect.sizeDelta = new Vector2(660f, 32f);

        TextMeshProUGUI hpTMP = hpGO.AddComponent<TextMeshProUGUI>();
        hpTMP.font = bodyFont;
        hpTMP.text = "พลังชีวิตคงเหลือ: 100 / 100 HP";
        hpTMP.fontSize = 20f;
        hpTMP.fontStyle = FontStyles.Bold;
        hpTMP.alignment = TextAlignmentOptions.Center;
        hpTMP.color = new Color(0.51f, 0.88f, 0.52f, 1f); // Vibrant Life Green
        hpTMP.raycastTarget = false;
        winManager.hpRemainingTextTMP = hpTMP;

        // 9. Forest Whisper Bubble
        GameObject bubbleGO = new GameObject("LoreBubble");
        bubbleGO.transform.SetParent(innerCardGO.transform, false);
        RectTransform bRect = bubbleGO.AddComponent<RectTransform>();
        bRect.anchorMin = new Vector2(0.5f, 0.5f);
        bRect.anchorMax = new Vector2(0.5f, 0.5f);
        bRect.pivot = new Vector2(0.5f, 0.5f);
        bRect.anchoredPosition = new Vector2(0f, -120f);
        bRect.sizeDelta = new Vector2(680f, 44f);

        if (bubbleSprite != null)
        {
            Image bImg = bubbleGO.AddComponent<Image>();
            bImg.sprite = bubbleSprite;
            bImg.type = Image.Type.Sliced;
            bImg.color = new Color(0.18f, 0.15f, 0.12f, 0.85f);
            bImg.raycastTarget = false;
        }

        GameObject bubbleTxtGO = new GameObject("Text");
        bubbleTxtGO.transform.SetParent(bubbleGO.transform, false);
        RectTransform btRect = bubbleTxtGO.AddComponent<RectTransform>();
        btRect.anchorMin = Vector2.zero;
        btRect.anchorMax = Vector2.one;
        btRect.offsetMin = Vector2.zero;
        btRect.offsetMax = Vector2.zero;

        TextMeshProUGUI bTMP = bubbleTxtGO.AddComponent<TextMeshProUGUI>();
        bTMP.font = bodyFont;
        bTMP.text = "ต้นไม้โบราณสัมผัสได้ถึงแสงสว่าง... ประตูสู่ดินแดนใหม่เปิดออกแล้ว!";
        bTMP.fontSize = 16f;
        bTMP.alignment = TextAlignmentOptions.Center;
        bTMP.color = new Color(1f, 0.90f, 0.50f, 1f);
        bTMP.raycastTarget = false;

        // 10. Action Buttons Row (Cozy Flanking Paws)
        GameObject buttonRowGO = new GameObject("ButtonRow");
        buttonRowGO.transform.SetParent(innerCardGO.transform, false);
        RectTransform rowRect = buttonRowGO.AddComponent<RectTransform>();
        rowRect.anchorMin = new Vector2(0.5f, 0f);
        rowRect.anchorMax = new Vector2(0.5f, 0f);
        rowRect.pivot = new Vector2(0.5f, 0f);
        rowRect.anchoredPosition = new Vector2(0f, 38f);
        rowRect.sizeDelta = new Vector2(720f, 62f);

        HorizontalLayoutGroup rowLayout = buttonRowGO.AddComponent<HorizontalLayoutGroup>();
        rowLayout.childAlignment = TextAnchor.MiddleCenter;
        rowLayout.spacing = 40f;
        rowLayout.childForceExpandWidth = false;
        rowLayout.childForceExpandHeight = false;

        // Restart Button (Warm Forest Green / Gold)
        CreateJuicyButton(buttonRowGO.transform, "RestartButton", "เล่นใหม่อีกครั้ง", bodyFont, 22f,
            keyBadgeSprite, leafSprite,
            new Color(0.22f, 0.34f, 0.18f, 1f),
            new Color(0.35f, 0.50f, 0.26f, 1f),
            new Color(0.16f, 0.25f, 0.12f, 1f),
            new Color(0.92f, 0.78f, 0.38f, 0.95f),
            new Color(1f, 0.90f, 0.50f, 1f),
            out Button restartBtn);
        winManager.restartButton = restartBtn;

        // Main Menu Button (Deep Slate Obsidian / Silver-Gold)
        CreateJuicyButton(buttonRowGO.transform, "MainMenuButton", "กลับหน้าหลัก", bodyFont, 22f,
            keyBadgeSprite, leafSprite,
            new Color(0.20f, 0.18f, 0.24f, 1f),
            new Color(0.34f, 0.30f, 0.38f, 1f),
            new Color(0.14f, 0.12f, 0.18f, 1f),
            new Color(0.78f, 0.72f, 0.68f, 0.95f),
            new Color(0.92f, 0.88f, 0.84f, 1f),
            out Button menuBtn);
        winManager.mainMenuButton = menuBtn;

        // Start disabled in prefab
        winPanelGO.SetActive(false);

        // Save Prefab
        PrefabUtility.SaveAsPrefabAsset(canvasGO, WIN_PREFAB_PATH);
        Object.DestroyImmediate(canvasGO);

        Debug.Log($"[CreateGameResultUI] Built and saved: {WIN_PREFAB_PATH}");
    }

    #endregion

    #region Game Over Canvas Prefab

    public static void BuildGameOverCanvasPrefab()
    {
        TMP_FontAsset titleFont = LoadFont(FONT_ONELITTLE);
        TMP_FontAsset bodyFont = LoadFont(FONT_ITIM);

        Sprite modalCardSprite = AssetDatabase.LoadAssetAtPath<Sprite>(SPRITE_MODAL_CARD);
        Sprite keyBadgeSprite = AssetDatabase.LoadAssetAtPath<Sprite>(SPRITE_KEY_BADGE);
        Sprite leafSprite = AssetDatabase.LoadAssetAtPath<Sprite>(SPRITE_LEAF);
        if (leafSprite == null) leafSprite = AssetDatabase.LoadAssetAtPath<Sprite>(SPRITE_PAW);
        Sprite heartSprite = AssetDatabase.LoadAssetAtPath<Sprite>(SPRITE_HEART);
        Sprite bubbleSprite = AssetDatabase.LoadAssetAtPath<Sprite>(SPRITE_BUBBLE);
        AudioClip deathAudio = AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Audio/SFX/death.mp3");

        // 1. Root Canvas
        GameObject canvasGO = new GameObject("GameOver_Canvas");
        Canvas canvas = canvasGO.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 100;

        CanvasScaler scaler = canvasGO.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);
        scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
        scaler.matchWidthOrHeight = 0.5f;

        canvasGO.AddComponent<GraphicRaycaster>();

        AudioSource audioSource = canvasGO.AddComponent<AudioSource>();
        audioSource.playOnAwake = false;
        audioSource.clip = deathAudio;

        GameOverUIManager gameOverManager = canvasGO.AddComponent<GameOverUIManager>();

        // 2. GameOverPanel (Full Screen Overlay)
        GameObject goPanelGO = CreateFullScreenOverlay(canvasGO.transform, "GameOverPanel", new Color(0.08f, 0.03f, 0.04f, 0.90f));
        CanvasGroup canvasGroup = goPanelGO.AddComponent<CanvasGroup>();
        canvasGroup.alpha = 1f;
        canvasGroup.interactable = true;
        canvasGroup.blocksRaycasts = true;

        gameOverManager.gameOverPanel = goPanelGO;
        gameOverManager.gameOverCanvasGroup = canvasGroup;

        // 3. Modal Container (Outer Glow Frame)
        GameObject modalContainerGO = new GameObject("ModalContainer");
        modalContainerGO.transform.SetParent(goPanelGO.transform, false);
        RectTransform modalRect = modalContainerGO.AddComponent<RectTransform>();
        modalRect.anchorMin = new Vector2(0.5f, 0.5f);
        modalRect.anchorMax = new Vector2(0.5f, 0.5f);
        modalRect.pivot = new Vector2(0.5f, 0.5f);
        modalRect.anchoredPosition = Vector2.zero;
        modalRect.sizeDelta = new Vector2(860f, 640f);

        Image modalBorder = modalContainerGO.AddComponent<Image>();
        modalBorder.sprite = modalCardSprite;
        modalBorder.type = Image.Type.Sliced;
        modalBorder.color = new Color(0.92f, 0.38f, 0.42f, 0.55f); // Rose-Crimson glow outline

        // 4. Inner Card (Midnight Twilight Wood)
        GameObject innerCardGO = new GameObject("InnerCard");
        innerCardGO.transform.SetParent(modalContainerGO.transform, false);
        RectTransform innerRect = innerCardGO.AddComponent<RectTransform>();
        innerRect.anchorMin = Vector2.zero;
        innerRect.anchorMax = Vector2.one;
        innerRect.offsetMin = new Vector2(5f, 5f);
        innerRect.offsetMax = new Vector2(-5f, -5f);

        Image cardImg = innerCardGO.AddComponent<Image>();
        cardImg.sprite = modalCardSprite;
        cardImg.type = Image.Type.Sliced;
        cardImg.color = new Color(0.13f, 0.08f, 0.10f, 0.98f);

        // 5. Floating Crest at Top (Heart / Broken Lantern Emblem)
        GameObject crestGO = new GameObject("FloatingCrest");
        crestGO.transform.SetParent(innerCardGO.transform, false);
        RectTransform crestRect = crestGO.AddComponent<RectTransform>();
        crestRect.anchorMin = new Vector2(0.5f, 1f);
        crestRect.anchorMax = new Vector2(0.5f, 1f);
        crestRect.pivot = new Vector2(0.5f, 0.5f);
        crestRect.anchoredPosition = new Vector2(0f, 22f);
        crestRect.sizeDelta = new Vector2(110f, 110f);

        Image crestImg = crestGO.AddComponent<Image>();
        crestImg.sprite = heartSprite;
        crestImg.color = new Color(1f, 0.55f, 0.55f, 1f); // Soft dusk coral
        crestImg.raycastTarget = false;
        crestGO.AddComponent<TitleIdleBobbing>();

        // 6. Header Sub-badge Text
        GameObject badgeTxtGO = new GameObject("BadgeText");
        badgeTxtGO.transform.SetParent(innerCardGO.transform, false);
        RectTransform btRect = badgeTxtGO.AddComponent<RectTransform>();
        btRect.anchorMin = new Vector2(0.5f, 1f);
        btRect.anchorMax = new Vector2(0.5f, 1f);
        btRect.pivot = new Vector2(0.5f, 1f);
        btRect.anchoredPosition = new Vector2(0f, -48f);
        btRect.sizeDelta = new Vector2(600f, 28f);

        TextMeshProUGUI badgeTMP = badgeTxtGO.AddComponent<TextMeshProUGUI>();
        badgeTMP.font = titleFont;
        badgeTMP.text = "G A M E   O V E R";
        badgeTMP.fontSize = 20f;
        badgeTMP.fontStyle = FontStyles.Bold;
        badgeTMP.alignment = TextAlignmentOptions.Center;
        badgeTMP.color = new Color(1f, 0.65f, 0.60f, 1f); // Soft Coral
        badgeTMP.raycastTarget = false;

        // Title Text: REST NOW, GUARDIAN (ภารกิจล้มเหลว)
        GameObject titleGO = new GameObject("TitleText");
        titleGO.transform.SetParent(innerCardGO.transform, false);
        RectTransform titleRect = titleGO.AddComponent<RectTransform>();
        titleRect.anchorMin = new Vector2(0.5f, 1f);
        titleRect.anchorMax = new Vector2(0.5f, 1f);
        titleRect.pivot = new Vector2(0.5f, 1f);
        titleRect.anchoredPosition = new Vector2(0f, -80f);
        titleRect.sizeDelta = new Vector2(760f, 58f);

        TextMeshProUGUI titleTMP = titleGO.AddComponent<TextMeshProUGUI>();
        titleTMP.font = titleFont;
        titleTMP.text = "REST NOW, GUARDIAN";
        titleTMP.fontSize = 48f;
        titleTMP.fontStyle = FontStyles.Bold;
        titleTMP.alignment = TextAlignmentOptions.Center;
        titleTMP.color = new Color(1f, 0.42f, 0.45f, 1f); // Warm Coral Crimson
        titleTMP.raycastTarget = false;
        gameOverManager.titleTextTMP = titleTMP;

        // Reason Text
        GameObject reasonGO = new GameObject("ReasonText");
        reasonGO.transform.SetParent(innerCardGO.transform, false);
        RectTransform reasonRect = reasonGO.AddComponent<RectTransform>();
        reasonRect.anchorMin = new Vector2(0.5f, 1f);
        reasonRect.anchorMax = new Vector2(0.5f, 1f);
        reasonRect.pivot = new Vector2(0.5f, 1f);
        reasonRect.anchoredPosition = new Vector2(0f, -142f);
        reasonRect.sizeDelta = new Vector2(720f, 36f);

        TextMeshProUGUI reasonTMP = reasonGO.AddComponent<TextMeshProUGUI>();
        reasonTMP.font = bodyFont;
        reasonTMP.text = "พลังชีวิตของคุณหมดลงแล้ว!";
        reasonTMP.fontSize = 23f;
        reasonTMP.alignment = TextAlignmentOptions.Center;
        reasonTMP.color = new Color(0.98f, 0.88f, 0.90f, 1f); // Soft Peach Cream
        reasonTMP.raycastTarget = false;
        gameOverManager.reasonTextTMP = reasonTMP;

        // 7. Lumi's Guidance Survival Tip Box
        GameObject tipBoxGO = new GameObject("TipBox");
        tipBoxGO.transform.SetParent(innerCardGO.transform, false);
        RectTransform tipRect = tipBoxGO.AddComponent<RectTransform>();
        tipRect.anchorMin = new Vector2(0.5f, 0.5f);
        tipRect.anchorMax = new Vector2(0.5f, 0.5f);
        tipRect.pivot = new Vector2(0.5f, 0.5f);
        tipRect.anchoredPosition = new Vector2(0f, -10f);
        tipRect.sizeDelta = new Vector2(720f, 215f);

        Image tipBorderImg = tipBoxGO.AddComponent<Image>();
        tipBorderImg.sprite = bubbleSprite != null ? bubbleSprite : keyBadgeSprite;
        tipBorderImg.type = Image.Type.Sliced;
        tipBorderImg.color = new Color(0.92f, 0.45f, 0.48f, 0.85f); // Rose-Amber frame

        // Tip Box Inner Dark Background
        GameObject tipInnerGO = new GameObject("InnerBg");
        tipInnerGO.transform.SetParent(tipBoxGO.transform, false);
        RectTransform tInnerRect = tipInnerGO.AddComponent<RectTransform>();
        tInnerRect.anchorMin = Vector2.zero;
        tInnerRect.anchorMax = Vector2.one;
        tInnerRect.offsetMin = new Vector2(3f, 3f);
        tInnerRect.offsetMax = new Vector2(-3f, -3f);

        Image tipInnerImg = tipInnerGO.AddComponent<Image>();
        tipInnerImg.sprite = tipBorderImg.sprite;
        tipInnerImg.type = Image.Type.Sliced;
        tipInnerImg.color = new Color(0.12f, 0.08f, 0.10f, 1f); // Deep Twilight Charcoal
        tipInnerImg.raycastTarget = false;

        // Tip Header: 💡 คำแนะนำในการเอาชีวิตรอด
        GameObject tipHeaderGO = new GameObject("TipHeader");
        tipHeaderGO.transform.SetParent(tipBoxGO.transform, false);
        RectTransform thRect = tipHeaderGO.AddComponent<RectTransform>();
        thRect.anchorMin = new Vector2(0.5f, 1f);
        thRect.anchorMax = new Vector2(0.5f, 1f);
        thRect.pivot = new Vector2(0.5f, 1f);
        thRect.anchoredPosition = new Vector2(0f, -16f);
        thRect.sizeDelta = new Vector2(660f, 32f);

        TextMeshProUGUI thTMP = tipHeaderGO.AddComponent<TextMeshProUGUI>();
        thTMP.font = bodyFont;
        thTMP.text = "คำแนะนำในการเอาชีวิตรอด (Survival Guide)";
        thTMP.fontSize = 21f;
        thTMP.fontStyle = FontStyles.Bold;
        thTMP.alignment = TextAlignmentOptions.Center;
        thTMP.color = new Color(1f, 0.85f, 0.45f, 1f); // Warm Amber Gold
        thTMP.raycastTarget = false;

        // Tip Content Text
        GameObject tipTxtGO = new GameObject("TipText");
        tipTxtGO.transform.SetParent(tipBoxGO.transform, false);
        RectTransform ttRect = tipTxtGO.AddComponent<RectTransform>();
        ttRect.anchorMin = new Vector2(0.5f, 0f);
        ttRect.anchorMax = new Vector2(0.5f, 1f);
        ttRect.pivot = new Vector2(0.5f, 0.5f);
        ttRect.anchoredPosition = new Vector2(0f, -16f);
        ttRect.sizeDelta = new Vector2(640f, 140f);

        TextMeshProUGUI ttTMP = tipTxtGO.AddComponent<TextMeshProUGUI>();
        ttTMP.font = bodyFont;
        ttTMP.text = "• ระวังการโจมตีจากศัตรูและกับดักหินกลิ้งในดันเจี้ยน\n• เมื่อออกนอก Safe Zone ออกซิเจนจะลดลงเรื่อยๆ\n• หมั่นกลับเข้า Safe Zone เพื่อฟื้นฟูออกซิเจนและฟื้นพลังชีวิต!\n• พกพาขวดยา (Potion) ไว้ฟื้นฟูเลือดในยามคับขัน";
        ttTMP.fontSize = 17f;
        ttTMP.lineSpacing = 20f;
        ttTMP.alignment = TextAlignmentOptions.Left;
        ttTMP.color = new Color(1f, 0.94f, 0.94f, 1f); // Soft Parchment
        ttTMP.raycastTarget = false;

        // 8. Encouragement Whisper
        GameObject whisperGO = new GameObject("EncouragementWhisper");
        whisperGO.transform.SetParent(innerCardGO.transform, false);
        RectTransform wRect = whisperGO.AddComponent<RectTransform>();
        wRect.anchorMin = new Vector2(0.5f, 0.5f);
        wRect.anchorMax = new Vector2(0.5f, 0.5f);
        wRect.pivot = new Vector2(0.5f, 0.5f);
        wRect.anchoredPosition = new Vector2(0f, -145f);
        wRect.sizeDelta = new Vector2(680f, 30f);

        TextMeshProUGUI wTMP = whisperGO.AddComponent<TextMeshProUGUI>();
        wTMP.font = bodyFont;
        wTMP.text = "อย่ายอมแพ้! ผืนป่ายังคงรอคอยการกลับมาของผู้พิทักษ์";
        wTMP.fontSize = 16f;
        wTMP.alignment = TextAlignmentOptions.Center;
        wTMP.color = new Color(1f, 0.65f, 0.65f, 0.90f);
        wTMP.raycastTarget = false;

        // 9. Action Buttons Row (Cozy Flanking Paws)
        GameObject buttonRowGO = new GameObject("ButtonRow");
        buttonRowGO.transform.SetParent(innerCardGO.transform, false);
        RectTransform rowRect = buttonRowGO.AddComponent<RectTransform>();
        rowRect.anchorMin = new Vector2(0.5f, 0f);
        rowRect.anchorMax = new Vector2(0.5f, 0f);
        rowRect.pivot = new Vector2(0.5f, 0f);
        rowRect.anchoredPosition = new Vector2(0f, 38f);
        rowRect.sizeDelta = new Vector2(720f, 62f);

        HorizontalLayoutGroup rowLayout = buttonRowGO.AddComponent<HorizontalLayoutGroup>();
        rowLayout.childAlignment = TextAnchor.MiddleCenter;
        rowLayout.spacing = 40f;
        rowLayout.childForceExpandWidth = false;
        rowLayout.childForceExpandHeight = false;

        // Restart Button (Warm Coral Crimson)
        CreateJuicyButton(buttonRowGO.transform, "RestartButton", "ลองใหม่อีกครั้ง", bodyFont, 22f,
            keyBadgeSprite, leafSprite,
            new Color(0.38f, 0.15f, 0.19f, 1f),
            new Color(0.54f, 0.22f, 0.28f, 1f),
            new Color(0.24f, 0.09f, 0.12f, 1f),
            new Color(0.95f, 0.48f, 0.48f, 0.95f),
            new Color(1f, 0.70f, 0.70f, 1f),
            out Button restartBtn);
        gameOverManager.restartButton = restartBtn;

        // Main Menu Button (Deep Slate Obsidian)
        CreateJuicyButton(buttonRowGO.transform, "MainMenuButton", "กลับหน้าหลัก", bodyFont, 22f,
            keyBadgeSprite, leafSprite,
            new Color(0.20f, 0.18f, 0.24f, 1f),
            new Color(0.34f, 0.30f, 0.38f, 1f),
            new Color(0.14f, 0.12f, 0.18f, 1f),
            new Color(0.78f, 0.72f, 0.68f, 0.95f),
            new Color(0.92f, 0.88f, 0.84f, 1f),
            out Button menuBtn);
        gameOverManager.mainMenuButton = menuBtn;

        // Start disabled in prefab
        goPanelGO.SetActive(false);

        // Save Prefab
        PrefabUtility.SaveAsPrefabAsset(canvasGO, GAMEOVER_PREFAB_PATH);
        Object.DestroyImmediate(canvasGO);

        Debug.Log($"[CreateGameResultUI] Built and saved: {GAMEOVER_PREFAB_PATH}");
    }

    #endregion

    #region Helpers

    private static GameObject CreateFullScreenOverlay(Transform parent, string name, Color bgColor)
    {
        GameObject go = new GameObject(name);
        go.transform.SetParent(parent, false);

        RectTransform rt = go.AddComponent<RectTransform>();
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.sizeDelta = Vector2.zero;

        Image img = go.AddComponent<Image>();
        img.color = bgColor;

        return go;
    }

    private static void CreateJuicyButton(Transform parent, string name, string labelText, TMP_FontAsset font, float fontSize,
        Sprite bgSprite, Sprite pawSprite, Color normalColor, Color hoverColor, Color pressColor, Color borderColor, Color pawColor,
        out Button button)
    {
        GameObject btnGO = new GameObject(name);
        btnGO.transform.SetParent(parent, false);
        RectTransform btnRect = btnGO.AddComponent<RectTransform>();
        btnRect.sizeDelta = new Vector2(300f, 56f);

        Image btnBorderImg = btnGO.AddComponent<Image>();
        btnBorderImg.sprite = bgSprite;
        btnBorderImg.type = Image.Type.Sliced;
        btnBorderImg.color = borderColor;

        // Button Inner Face
        GameObject innerFaceGO = new GameObject("InnerFace");
        innerFaceGO.transform.SetParent(btnGO.transform, false);
        RectTransform ifRect = innerFaceGO.AddComponent<RectTransform>();
        ifRect.anchorMin = Vector2.zero;
        ifRect.anchorMax = Vector2.one;
        ifRect.offsetMin = new Vector2(3f, 3f);
        ifRect.offsetMax = new Vector2(-3f, -3f);

        Image innerFaceImg = innerFaceGO.AddComponent<Image>();
        innerFaceImg.sprite = bgSprite;
        innerFaceImg.type = Image.Type.Sliced;
        innerFaceImg.color = normalColor;

        button = btnGO.AddComponent<Button>();
        button.targetGraphic = innerFaceImg;
        ColorBlock colors = button.colors;
        colors.normalColor = normalColor;
        colors.highlightedColor = hoverColor;
        colors.pressedColor = pressColor;
        colors.selectedColor = hoverColor;
        colors.fadeDuration = 0.12f;
        button.colors = colors;

        // Content Row with LeftPaw, Label, RightPaw
        GameObject contentGO = new GameObject("Content");
        contentGO.transform.SetParent(btnGO.transform, false);
        RectTransform contentRect = contentGO.AddComponent<RectTransform>();
        contentRect.anchorMin = Vector2.zero;
        contentRect.anchorMax = Vector2.one;
        contentRect.sizeDelta = Vector2.zero;

        HorizontalLayoutGroup hl = contentGO.AddComponent<HorizontalLayoutGroup>();
        hl.childAlignment = TextAnchor.MiddleCenter;
        hl.spacing = 10f;
        hl.childForceExpandWidth = false;
        hl.childForceExpandHeight = false;

        // Left Paw
        GameObject leftPaw = new GameObject("LeftPaw");
        leftPaw.transform.SetParent(contentGO.transform, false);
        RectTransform lpRect = leftPaw.AddComponent<RectTransform>();
        lpRect.sizeDelta = new Vector2(28f, 28f);
        Image lpImg = leftPaw.AddComponent<Image>();
        lpImg.sprite = pawSprite;
        lpImg.color = pawColor;
        lpImg.raycastTarget = false;

        // Center Label
        GameObject labelGO = new GameObject("Label");
        labelGO.transform.SetParent(contentGO.transform, false);
        RectTransform lRect = labelGO.AddComponent<RectTransform>();
        lRect.sizeDelta = new Vector2(190f, 40f);

        TextMeshProUGUI labelTMP = labelGO.AddComponent<TextMeshProUGUI>();
        labelTMP.font = font;
        labelTMP.text = labelText;
        labelTMP.fontSize = fontSize;
        labelTMP.fontStyle = FontStyles.Bold;
        labelTMP.alignment = TextAlignmentOptions.Center;
        labelTMP.color = Color.white;
        labelTMP.raycastTarget = false;

        // Right Paw (Leaf mirrored horizontally)
        GameObject rightPaw = new GameObject("RightPaw");
        rightPaw.transform.SetParent(contentGO.transform, false);
        RectTransform rpRect = rightPaw.AddComponent<RectTransform>();
        rpRect.sizeDelta = new Vector2(28f, 28f);
        rightPaw.transform.localScale = new Vector3(-1f, 1f, 1f); // Mirror leaf
        Image rpImg = rightPaw.AddComponent<Image>();
        rpImg.sprite = pawSprite;
        rpImg.color = pawColor;
        rpImg.raycastTarget = false;

        // Attach Juicy Button Component
        UIJuicyButton juicy = btnGO.AddComponent<UIJuicyButton>();
        juicy.leftPaw = leftPaw;
        juicy.rightPaw = rightPaw;
        juicy.alwaysShowIcons = true;
        juicy.hoverScale = 1.05f;
        juicy.pressScale = 0.95f;
        juicy.transitionSpeed = 16f;

        // Start paws active so they are ready
        juicy.SetPaws(true);
    }

    private static TMP_FontAsset LoadFont(string primaryPath)
    {
        TMP_FontAsset font = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(primaryPath);
        if (font == null)
        {
            font = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(FONT_ITIM);
        }
        if (font == null)
        {
            font = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(FONT_ONELITTLE);
        }
        return font;
    }

    [MenuItem("Tools/Rootbound Guardian/Capture Win & Game Over Screenshots")]
    public static void CaptureResultScreenshots()
    {
        BuildAllResultPrefabs();

        string downloadDir = @"C:\Users\Artemis\Downloads";
        string winOutputPath = Path.Combine(downloadDir, "Win_UI.png");
        string gameOverOutputPath = Path.Combine(downloadDir, "GameOver_UI.png");

        RenderPrefabToImage(WIN_PREFAB_PATH, "WinPanel", winOutputPath);
        RenderPrefabToImage(GAMEOVER_PREFAB_PATH, "GameOverPanel", gameOverOutputPath);

        Debug.Log("[CreateGameResultUI] Captured Win_UI.png and GameOver_UI.png successfully!");
    }

    private static void RenderPrefabToImage(string prefabPath, string panelName, string outputPath)
    {
        GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);
        if (prefab == null) return;

        GameObject instance = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
        Transform panel = instance.transform.Find(panelName);
        if (panel != null)
        {
            panel.gameObject.SetActive(true);
            CanvasGroup cg = panel.GetComponent<CanvasGroup>();
            if (cg != null) cg.alpha = 1f;
        }

        int uiLayer = LayerMask.NameToLayer("UI");
        if (uiLayer < 0) uiLayer = 5;
        SetLayerRecursively(instance, uiLayer);

        GameObject camGO = new GameObject("CaptureCam");
        Camera cam = camGO.AddComponent<Camera>();
        cam.clearFlags = CameraClearFlags.SolidColor;
        cam.backgroundColor = new Color(0.04f, 0.05f, 0.08f, 1f);
        cam.cullingMask = 1 << uiLayer;
        cam.orthographic = true;
        cam.orthographicSize = 5f;
        cam.transform.position = new Vector3(0f, 0f, -10f);

        Canvas canvas = instance.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceCamera;
        canvas.worldCamera = cam;
        canvas.planeDistance = 10f;

        CanvasScaler scaler = instance.GetComponent<CanvasScaler>();
        if (scaler != null)
        {
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);
            scaler.matchWidthOrHeight = 0.5f;
        }

        int width = 1920;
        int height = 1080;
        RenderTexture rt = new RenderTexture(width, height, 24, RenderTextureFormat.ARGB32);
        rt.Create();
        cam.targetTexture = rt;

        Canvas.ForceUpdateCanvases();
        cam.Render();

        RenderTexture.active = rt;
        Texture2D tex = new Texture2D(width, height, TextureFormat.RGB24, false);
        tex.ReadPixels(new Rect(0, 0, width, height), 0, 0);
        tex.Apply();

        byte[] bytes = tex.EncodeToPNG();
        File.WriteAllBytes(outputPath, bytes);

        RenderTexture.active = null;
        cam.targetTexture = null;
        rt.Release();
        Object.DestroyImmediate(rt);
        Object.DestroyImmediate(tex);
        Object.DestroyImmediate(camGO);
        Object.DestroyImmediate(instance);
    }

    private static void SetLayerRecursively(GameObject go, int layer)
    {
        go.layer = layer;
        foreach (Transform child in go.transform)
        {
            SetLayerRecursively(child.gameObject, layer);
        }
    }

    #endregion
}
#endif
