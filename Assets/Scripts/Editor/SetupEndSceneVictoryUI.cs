#if UNITY_EDITOR
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// Professional Editor tool to construct the breathtaking, cozy fairytale EndGameVictoryUI
/// in EndScene.unity and capture showcase screenshots matching the exact Rootbound Guardian art style.
/// </summary>
public static class SetupEndSceneVictoryUI
{
    private const string SCENE_PATH = "Assets/EndScene.unity";

    // Asset paths
    private const string FONT_ONELITTLE = "Assets/Image/OneLittleFont-Full SDF.asset";
    private const string FONT_ITIM_THAI = "Assets/Fonts/Itim_Thai_SDF.asset";
    private const string FONT_ITIM_DYNAMIC = "Assets/Fonts/Itim_TMP_Dynamic.asset";

    private const string SPRITE_MODAL_CARD = "Assets/Main Screen/UI/ModalCardBg.png";
    private const string SPRITE_LEAF = "Assets/Main Screen/UI/LeafIcon.png";
    private const string SPRITE_BANNER = "Assets/Image/UI/Quest_Banner.png";
    private const string SPRITE_SPROUT = "Assets/Image/UI/Cute_Sprout_Badge.png";
    private const string SPRITE_KEY_E = "Assets/Image/UI/Prompt_Key_E_Cute.png";
    private const string AUDIO_FANFARE = "Assets/Audio/SFX/Key.mp3";

    [MenuItem("Tools/Rootbound Guardian/Setup EndScene Victory UI")]
    public static void SetupVictoryUIInScene()
    {
        Debug.Log("[SetupEndSceneVictoryUI] Opening scene: " + SCENE_PATH);
        var scene = EditorSceneManager.OpenScene(SCENE_PATH, OpenSceneMode.Single);

        // Find or create Canvas
        Canvas canvas = Object.FindFirstObjectByType<Canvas>();
        if (canvas == null)
        {
            GameObject canvasGO = new GameObject("Canvas");
            canvas = canvasGO.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvasGO.AddComponent<CanvasScaler>();
            canvasGO.AddComponent<GraphicRaycaster>();
        }

        ConfigureCanvasScaler(canvas);

        // Find existing VictoryPanel or CornerReopenBadge and destroy for clean construction
        Transform existingPanel = canvas.transform.Find("VictoryPanel");
        if (existingPanel != null) Object.DestroyImmediate(existingPanel.gameObject);

        Transform existingBadge = canvas.transform.Find("CornerReopenBadge");
        if (existingBadge != null) Object.DestroyImmediate(existingBadge.gameObject);

        // Load fonts
        TMP_FontAsset titleFont = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(FONT_ONELITTLE);
        TMP_FontAsset thaiFont = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(FONT_ITIM_THAI);
        if (thaiFont == null) thaiFont = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(FONT_ITIM_DYNAMIC);
        if (titleFont == null) titleFont = thaiFont;

        // Load sprites
        Sprite modalCardSprite = AssetDatabase.LoadAssetAtPath<Sprite>(SPRITE_MODAL_CARD);
        Sprite leafSprite = AssetDatabase.LoadAssetAtPath<Sprite>(SPRITE_LEAF);
        Sprite bannerSprite = AssetDatabase.LoadAssetAtPath<Sprite>(SPRITE_BANNER);
        Sprite sproutSprite = AssetDatabase.LoadAssetAtPath<Sprite>(SPRITE_SPROUT);
        Sprite keyESprite = AssetDatabase.LoadAssetAtPath<Sprite>(SPRITE_KEY_E);
        AudioClip fanfareClip = AssetDatabase.LoadAssetAtPath<AudioClip>(AUDIO_FANFARE);

        // 1. VictoryPanel Root (Full Screen Dimmer Overlay)
        GameObject victoryPanelGO = new GameObject("VictoryPanel");
        victoryPanelGO.transform.SetParent(canvas.transform, false);
        RectTransform vpRect = victoryPanelGO.AddComponent<RectTransform>();
        vpRect.anchorMin = Vector2.zero;
        vpRect.anchorMax = Vector2.one;
        vpRect.sizeDelta = Vector2.zero;

        Image vpImg = victoryPanelGO.AddComponent<Image>();
        vpImg.color = new Color(0.04f, 0.05f, 0.06f, 0.85f); // Deep cozy night dimmer

        CanvasGroup vpCanvasGroup = victoryPanelGO.AddComponent<CanvasGroup>();
        vpCanvasGroup.alpha = 1f;
        vpCanvasGroup.interactable = true;
        vpCanvasGroup.blocksRaycasts = true;

        AudioSource audioSrc = victoryPanelGO.AddComponent<AudioSource>();
        audioSrc.playOnAwake = false;

        // 2. Modal Card Container (Width 820, Height 570)
        GameObject modalCardGO = new GameObject("ModalCard");
        modalCardGO.transform.SetParent(victoryPanelGO.transform, false);
        RectTransform modalRect = modalCardGO.AddComponent<RectTransform>();
        modalRect.anchorMin = new Vector2(0.5f, 0.5f);
        modalRect.anchorMax = new Vector2(0.5f, 0.5f);
        modalRect.pivot = new Vector2(0.5f, 0.5f);
        modalRect.anchoredPosition = new Vector2(0f, -8f);
        modalRect.sizeDelta = new Vector2(820f, 570f);

        // 2.1 Outer Gold Border (Warm Honey Amber Outline)
        Image outerBorder = modalCardGO.AddComponent<Image>();
        outerBorder.sprite = modalCardSprite;
        outerBorder.type = Image.Type.Sliced;
        outerBorder.color = new Color(0.96f, 0.80f, 0.38f, 0.90f);

        // 2.2 Inner Midnight Forest Wood Card
        GameObject innerCardGO = new GameObject("InnerCard");
        innerCardGO.transform.SetParent(modalCardGO.transform, false);
        RectTransform innerRect = innerCardGO.AddComponent<RectTransform>();
        innerRect.anchorMin = Vector2.zero;
        innerRect.anchorMax = Vector2.one;
        innerRect.offsetMin = new Vector2(5f, 5f);
        innerRect.offsetMax = new Vector2(-5f, -5f);

        Image innerCardImg = innerCardGO.AddComponent<Image>();
        innerCardImg.sprite = modalCardSprite;
        innerCardImg.type = Image.Type.Sliced;
        innerCardImg.color = new Color(0.08f, 0.07f, 0.10f, 0.98f); // Rich midnight wood

        // 3. Top Celestial Crest (Sprout Medallion)
        GameObject crestRootGO = new GameObject("CelestialCrest");
        crestRootGO.transform.SetParent(innerCardGO.transform, false);
        RectTransform crestRootRect = crestRootGO.AddComponent<RectTransform>();
        crestRootRect.anchorMin = new Vector2(0.5f, 1f);
        crestRootRect.anchorMax = new Vector2(0.5f, 1f);
        crestRootRect.pivot = new Vector2(0.5f, 0.5f);
        crestRootRect.anchoredPosition = new Vector2(0f, 20f);
        crestRootRect.sizeDelta = new Vector2(115f, 115f);

        Image sproutBadgeImg = crestRootGO.AddComponent<Image>();
        sproutBadgeImg.sprite = sproutSprite;
        sproutBadgeImg.preserveAspect = true;

        // 4. Header Ribbon Banner
        GameObject bannerGO = new GameObject("HeaderBanner");
        bannerGO.transform.SetParent(innerCardGO.transform, false);
        RectTransform bannerRect = bannerGO.AddComponent<RectTransform>();
        bannerRect.anchorMin = new Vector2(0.5f, 1f);
        bannerRect.anchorMax = new Vector2(0.5f, 1f);
        bannerRect.pivot = new Vector2(0.5f, 1f);
        bannerRect.anchoredPosition = new Vector2(0f, -42f);
        bannerRect.sizeDelta = new Vector2(520f, 78f);

        Image bannerImg = bannerGO.AddComponent<Image>();
        bannerImg.sprite = bannerSprite;
        bannerImg.preserveAspect = false;

        // Banner Text: "VICTORY!" with bright radiant gold text
        GameObject bannerTextGO = new GameObject("BannerText");
        bannerTextGO.transform.SetParent(bannerGO.transform, false);
        RectTransform bannerTextRect = bannerTextGO.AddComponent<RectTransform>();
        bannerTextRect.anchorMin = Vector2.zero;
        bannerTextRect.anchorMax = Vector2.one;
        bannerTextRect.anchoredPosition = new Vector2(0f, 5f);
        bannerTextRect.sizeDelta = Vector2.zero;

        TextMeshProUGUI bannerTMP = bannerTextGO.AddComponent<TextMeshProUGUI>();
        bannerTMP.font = titleFont;
        bannerTMP.text = "VICTORY!";
        bannerTMP.fontSize = 29f;
        bannerTMP.fontStyle = FontStyles.Bold;
        bannerTMP.alignment = TextAlignmentOptions.Center;
        bannerTMP.color = new Color(1.0f, 0.95f, 0.78f, 1f); // Radiant honey cream
        bannerTMP.outlineWidth = 0.25f;
        bannerTMP.outlineColor = new Color(0.28f, 0.12f, 0.02f, 0.9f);
        bannerTMP.raycastTarget = false;

        // 5. Title & Subtitle
        GameObject titleGO = new GameObject("TitleText");
        titleGO.transform.SetParent(innerCardGO.transform, false);
        RectTransform titleRect = titleGO.AddComponent<RectTransform>();
        titleRect.anchorMin = new Vector2(0.5f, 1f);
        titleRect.anchorMax = new Vector2(0.5f, 1f);
        titleRect.pivot = new Vector2(0.5f, 1f);
        titleRect.anchoredPosition = new Vector2(0f, -120f);
        titleRect.sizeDelta = new Vector2(760f, 42f);

        TextMeshProUGUI titleTMP = titleGO.AddComponent<TextMeshProUGUI>();
        titleTMP.font = thaiFont;
        titleTMP.text = "พันธนาการแห่งรากไม้ได้รับการปลดปล่อย";
        titleTMP.fontSize = 25f;
        titleTMP.fontStyle = FontStyles.Bold;
        titleTMP.alignment = TextAlignmentOptions.Center;
        titleTMP.color = new Color(0.85f, 1.0f, 0.82f, 1f); // Luminous soft emerald
        titleTMP.raycastTarget = false;

        GameObject subGO = new GameObject("SubtitleText");
        subGO.transform.SetParent(innerCardGO.transform, false);
        RectTransform subRect = subGO.AddComponent<RectTransform>();
        subRect.anchorMin = new Vector2(0.5f, 1f);
        subRect.anchorMax = new Vector2(0.5f, 1f);
        subRect.pivot = new Vector2(0.5f, 1f);
        subRect.anchoredPosition = new Vector2(0f, -158f);
        subRect.sizeDelta = new Vector2(760f, 26f);

        TextMeshProUGUI subTMP = subGO.AddComponent<TextMeshProUGUI>();
        subTMP.font = thaiFont;
        subTMP.text = "ต้นไม้โลกโบราณผลิบานอีกครั้ง • เกาะศักดิ์สิทธิ์กลับคืนสู่ความสงบสุข";
        subTMP.fontSize = 16f;
        subTMP.alignment = TextAlignmentOptions.Center;
        subTMP.color = new Color(0.96f, 0.84f, 0.48f, 0.95f); // Honey amber
        subTMP.raycastTarget = false;

        // 6. Lumi Gratitude Speech Bubble Capsule
        GameObject loreCardGO = new GameObject("LoreGratitudeCard");
        loreCardGO.transform.SetParent(innerCardGO.transform, false);
        RectTransform loreCardRect = loreCardGO.AddComponent<RectTransform>();
        loreCardRect.anchorMin = new Vector2(0.5f, 1f);
        loreCardRect.anchorMax = new Vector2(0.5f, 1f);
        loreCardRect.pivot = new Vector2(0.5f, 1f);
        loreCardRect.anchoredPosition = new Vector2(0f, -192f);
        loreCardRect.sizeDelta = new Vector2(760f, 102f);

        Image loreBorder = loreCardGO.AddComponent<Image>();
        loreBorder.sprite = modalCardSprite;
        loreBorder.type = Image.Type.Sliced;
        loreBorder.color = new Color(0.90f, 0.76f, 0.38f, 0.60f); // Soft gold rim

        GameObject loreInnerGO = new GameObject("Inner");
        loreInnerGO.transform.SetParent(loreCardGO.transform, false);
        RectTransform loreInnerRect = loreInnerGO.AddComponent<RectTransform>();
        loreInnerRect.anchorMin = Vector2.zero;
        loreInnerRect.anchorMax = Vector2.one;
        loreInnerRect.offsetMin = new Vector2(3f, 3f);
        loreInnerRect.offsetMax = new Vector2(-3f, -3f);

        Image loreInnerImg = loreInnerGO.AddComponent<Image>();
        loreInnerImg.sprite = modalCardSprite;
        loreInnerImg.type = Image.Type.Sliced;
        loreInnerImg.color = new Color(0.12f, 0.11f, 0.16f, 0.98f); // Deep velvety violet

        // Quote Header Label
        GameObject quoteHeaderGO = new GameObject("HeaderLabel");
        quoteHeaderGO.transform.SetParent(loreInnerGO.transform, false);
        RectTransform qhRect = quoteHeaderGO.AddComponent<RectTransform>();
        qhRect.anchorMin = new Vector2(0.5f, 1f);
        qhRect.anchorMax = new Vector2(0.5f, 1f);
        qhRect.pivot = new Vector2(0.5f, 1f);
        qhRect.anchoredPosition = new Vector2(0f, -7f);
        qhRect.sizeDelta = new Vector2(740f, 22f);

        TextMeshProUGUI qhTMP = quoteHeaderGO.AddComponent<TextMeshProUGUI>();
        qhTMP.font = thaiFont;
        qhTMP.text = "ถ้อยคำขอบคุณจากภูตแห่งแสง Lumi";
        qhTMP.fontSize = 14f;
        qhTMP.fontStyle = FontStyles.Bold;
        qhTMP.alignment = TextAlignmentOptions.Center;
        qhTMP.color = new Color(1.0f, 0.88f, 0.45f, 1f);
        qhTMP.raycastTarget = false;

        // Quote Body Text
        GameObject quoteGO = new GameObject("LumiQuoteText");
        quoteGO.transform.SetParent(loreInnerGO.transform, false);
        RectTransform quoteRect = quoteGO.AddComponent<RectTransform>();
        quoteRect.anchorMin = Vector2.zero;
        quoteRect.anchorMax = Vector2.one;
        quoteRect.offsetMin = new Vector2(20f, 6f);
        quoteRect.offsetMax = new Vector2(-20f, -28f);

        TextMeshProUGUI quoteTMP = quoteGO.AddComponent<TextMeshProUGUI>();
        quoteTMP.font = thaiFont;
        quoteTMP.text = "“ข้าขอขอบคุณเจ้าจากก้นบึ้งของหัวใจ เจ้าผู้พิทักษ์...\nบัดนี้รากไม้โบราณได้กลับมาผลิบาน ดินแดนศักดิ์สิทธิ์แห่งนี้จะคงความสงบสุขตลอดไป”";
        quoteTMP.fontSize = 15.5f;
        quoteTMP.lineSpacing = 10f;
        quoteTMP.alignment = TextAlignmentOptions.Center;
        quoteTMP.color = new Color(0.95f, 0.93f, 0.88f, 1f);
        quoteTMP.raycastTarget = false;

        // 7. Stats Panel (3 Elegant Badges)
        GameObject statsPanelGO = new GameObject("StatsPanel");
        statsPanelGO.transform.SetParent(innerCardGO.transform, false);
        RectTransform statsPanelRect = statsPanelGO.AddComponent<RectTransform>();
        statsPanelRect.anchorMin = new Vector2(0.5f, 1f);
        statsPanelRect.anchorMax = new Vector2(0.5f, 1f);
        statsPanelRect.pivot = new Vector2(0.5f, 1f);
        statsPanelRect.anchoredPosition = new Vector2(0f, -305f);
        statsPanelRect.sizeDelta = new Vector2(760f, 84f);

        Image statsBg = statsPanelGO.AddComponent<Image>();
        statsBg.sprite = modalCardSprite;
        statsBg.type = Image.Type.Sliced;
        statsBg.color = new Color(0.06f, 0.05f, 0.08f, 0.92f);

        // Stat 1: Clear Time
        TextMeshProUGUI timeValTMP;
        CreateStatBadge(statsPanelGO.transform, modalCardSprite, thaiFont, titleFont,
            new Vector2(-250f, 0f), "เวลาที่ใช้", "02:45",
            new Color(1.0f, 0.88f, 0.38f), out timeValTMP);

        // Stat 2: World Tree Restoration
        TextMeshProUGUI treeValTMP;
        CreateStatBadge(statsPanelGO.transform, modalCardSprite, thaiFont, thaiFont,
            new Vector2(0f, 0f), "ต้นไม้โบราณ", "ผลิบาน 100%",
            new Color(0.48f, 1.0f, 0.60f), out treeValTMP);

        // Stat 3: Guardian Rank
        TextMeshProUGUI rankValTMP;
        CreateStatBadge(statsPanelGO.transform, modalCardSprite, thaiFont, thaiFont,
            new Vector2(250f, 0f), "ระดับผู้พิทักษ์", "ระดับ S • Master",
            new Color(1.0f, 0.78f, 0.28f), out rankValTMP);

        // 8. Action Buttons Row (Marshmallow Pill Style matching Win_UI.png)
        GameObject btnRowGO = new GameObject("ButtonRow");
        btnRowGO.transform.SetParent(innerCardGO.transform, false);
        RectTransform btnRowRect = btnRowGO.AddComponent<RectTransform>();
        btnRowRect.anchorMin = new Vector2(0.5f, 0f);
        btnRowRect.anchorMax = new Vector2(0.5f, 0f);
        btnRowRect.pivot = new Vector2(0.5f, 0f);
        btnRowRect.anchoredPosition = new Vector2(0f, 26f);
        btnRowRect.sizeDelta = new Vector2(760f, 58f);

        // 8.1 Primary Button: Explore Sanctuary ("เดินสำรวจต่อ")
        Button exploreBtn;
        CreateCandyButton(btnRowGO.transform, modalCardSprite, leafSprite, thaiFont,
            new Vector2(-185f, 0f), new Vector2(350f, 52f),
            "เดินสำรวจเกาะศักดิ์สิทธิ์ต่อ",
            new Color(0.96f, 0.80f, 0.35f, 0.95f), // Golden Amber Outline
            new Color(0.08f, 0.14f, 0.10f, 0.98f), // Deep Pine Forest Inner
            out exploreBtn);

        // 8.2 Secondary Button: Main Menu ("กลับหน้าหลัก")
        Button mainMenuBtn;
        CreateCandyButton(btnRowGO.transform, modalCardSprite, leafSprite, thaiFont,
            new Vector2(195f, 0f), new Vector2(280f, 52f),
            "กลับสู่หน้าจอหลัก",
            new Color(0.85f, 0.82f, 0.78f, 0.85f), // Cream Silver Outline
            new Color(0.12f, 0.10f, 0.12f, 0.98f), // Deep Charcoal Inner
            out mainMenuBtn);

        // 9. Bottom Hint Capsule Bar ("[ESC] สลับเปิด-ปิดหน้าต่างภารกิจ")
        GameObject hintBarGO = new GameObject("BottomHintBar");
        hintBarGO.transform.SetParent(modalCardGO.transform, false);
        RectTransform hintRect = hintBarGO.AddComponent<RectTransform>();
        hintRect.anchorMin = new Vector2(0.5f, 0f);
        hintRect.anchorMax = new Vector2(0.5f, 0f);
        hintRect.pivot = new Vector2(0.5f, 1f);
        hintRect.anchoredPosition = new Vector2(0f, -12f);
        hintRect.sizeDelta = new Vector2(400f, 30f);

        Image hintBorder = hintBarGO.AddComponent<Image>();
        hintBorder.sprite = modalCardSprite;
        hintBorder.type = Image.Type.Sliced;
        hintBorder.color = new Color(0.80f, 0.68f, 0.38f, 0.45f);

        GameObject hintInnerGO = new GameObject("Inner");
        hintInnerGO.transform.SetParent(hintBarGO.transform, false);
        RectTransform hintInnerRect = hintInnerGO.AddComponent<RectTransform>();
        hintInnerRect.anchorMin = Vector2.zero;
        hintInnerRect.anchorMax = Vector2.one;
        hintInnerRect.offsetMin = new Vector2(2f, 2f);
        hintInnerRect.offsetMax = new Vector2(-2f, -2f);

        Image hintInnerImg = hintInnerGO.AddComponent<Image>();
        hintInnerImg.sprite = modalCardSprite;
        hintInnerImg.type = Image.Type.Sliced;
        hintInnerImg.color = new Color(0.08f, 0.07f, 0.10f, 0.92f);

        GameObject hintTextGO = new GameObject("Text");
        hintTextGO.transform.SetParent(hintInnerGO.transform, false);
        RectTransform hintTextRect = hintTextGO.AddComponent<RectTransform>();
        hintTextRect.anchorMin = Vector2.zero;
        hintTextRect.anchorMax = Vector2.one;
        hintTextRect.sizeDelta = Vector2.zero;

        TextMeshProUGUI hintTMP = hintTextGO.AddComponent<TextMeshProUGUI>();
        hintTMP.font = thaiFont;
        hintTMP.text = "[ESC] สลับเปิด-ปิดหน้าต่างภารกิจ";
        hintTMP.fontSize = 13f;
        hintTMP.alignment = TextAlignmentOptions.Center;
        hintTMP.color = new Color(0.92f, 0.82f, 0.55f, 0.90f);
        hintTMP.raycastTarget = false;

        // 10. Corner ESC Reopen Badge (Top-Right)
        GameObject cornerBadgeGO = new GameObject("CornerReopenBadge");
        cornerBadgeGO.transform.SetParent(canvas.transform, false);
        RectTransform cbRect = cornerBadgeGO.AddComponent<RectTransform>();
        cbRect.anchorMin = new Vector2(1f, 1f);
        cbRect.anchorMax = new Vector2(1f, 1f);
        cbRect.pivot = new Vector2(1f, 1f);
        cbRect.anchoredPosition = new Vector2(-30f, -30f);
        cbRect.sizeDelta = new Vector2(230f, 46f);

        Image cbBorder = cornerBadgeGO.AddComponent<Image>();
        cbBorder.sprite = modalCardSprite;
        cbBorder.type = Image.Type.Sliced;
        cbBorder.color = new Color(0.95f, 0.80f, 0.35f, 0.85f);

        GameObject cbInnerGO = new GameObject("Inner");
        cbInnerGO.transform.SetParent(cornerBadgeGO.transform, false);
        RectTransform cbInnerRect = cbInnerGO.AddComponent<RectTransform>();
        cbInnerRect.anchorMin = Vector2.zero;
        cbInnerRect.anchorMax = Vector2.one;
        cbInnerRect.offsetMin = new Vector2(3f, 3f);
        cbInnerRect.offsetMax = new Vector2(-3f, -3f);

        Image cbInnerImg = cbInnerGO.AddComponent<Image>();
        cbInnerImg.sprite = modalCardSprite;
        cbInnerImg.type = Image.Type.Sliced;
        cbInnerImg.color = new Color(0.10f, 0.08f, 0.14f, 0.96f);

        Button cornerBtn = cornerBadgeGO.AddComponent<Button>();

        // E key icon on corner badge
        GameObject keyEIconGO = new GameObject("KeyEIcon");
        keyEIconGO.transform.SetParent(cbInnerGO.transform, false);
        RectTransform keyERect = keyEIconGO.AddComponent<RectTransform>();
        keyERect.anchorMin = new Vector2(0f, 0.5f);
        keyERect.anchorMax = new Vector2(0f, 0.5f);
        keyERect.pivot = new Vector2(0.5f, 0.5f);
        keyERect.anchoredPosition = new Vector2(26f, 0f);
        keyERect.sizeDelta = new Vector2(30f, 30f);
        Image keyEImg = keyEIconGO.AddComponent<Image>();
        keyEImg.sprite = keyESprite != null ? keyESprite : sproutSprite;
        keyEImg.preserveAspect = true;

        GameObject cbTextGO = new GameObject("BadgeText");
        cbTextGO.transform.SetParent(cbInnerGO.transform, false);
        RectTransform cbTextRect = cbTextGO.AddComponent<RectTransform>();
        cbTextRect.anchorMin = Vector2.zero;
        cbTextRect.anchorMax = Vector2.one;
        cbTextRect.offsetMin = new Vector2(46f, 0f);
        cbTextRect.offsetMax = new Vector2(-10f, 0f);

        TextMeshProUGUI cbTMP = cbTextGO.AddComponent<TextMeshProUGUI>();
        cbTMP.font = thaiFont;
        cbTMP.text = "สรุปภารกิจ [ESC]";
        cbTMP.fontSize = 15.5f;
        cbTMP.fontStyle = FontStyles.Bold;
        cbTMP.alignment = TextAlignmentOptions.MidlineLeft;
        cbTMP.color = new Color(1.0f, 0.90f, 0.55f);
        cbTMP.raycastTarget = false;

        cornerBadgeGO.SetActive(false);

        // 11. Bind everything into EndGameVictoryUI
        EndGameVictoryUI victoryUI = canvas.GetComponent<EndGameVictoryUI>();
        if (victoryUI == null) victoryUI = canvas.gameObject.AddComponent<EndGameVictoryUI>();

        SerializedObject so = new SerializedObject(victoryUI);
        so.FindProperty("victoryPanel").objectReferenceValue = victoryPanelGO;
        so.FindProperty("victoryCanvasGroup").objectReferenceValue = vpCanvasGroup;
        so.FindProperty("modalCard").objectReferenceValue = modalRect;
        so.FindProperty("cornerReopenBadge").objectReferenceValue = cornerBadgeGO;
        so.FindProperty("crestGlowRect").objectReferenceValue = null;
        so.FindProperty("crestBadgeRect").objectReferenceValue = crestRootRect;

        so.FindProperty("titleTextTMP").objectReferenceValue = titleTMP;
        so.FindProperty("subtitleTextTMP").objectReferenceValue = subTMP;
        so.FindProperty("clearTimeTextTMP").objectReferenceValue = timeValTMP;
        so.FindProperty("treeStatusTextTMP").objectReferenceValue = treeValTMP;
        so.FindProperty("guardianRankTextTMP").objectReferenceValue = rankValTMP;
        so.FindProperty("storyQuoteTextTMP").objectReferenceValue = quoteTMP;

        so.FindProperty("exploreButton").objectReferenceValue = exploreBtn;
        so.FindProperty("mainMenuButton").objectReferenceValue = mainMenuBtn;
        so.FindProperty("cornerBadgeButton").objectReferenceValue = cornerBtn;
        so.FindProperty("audioSource").objectReferenceValue = audioSrc;
        so.FindProperty("fanfareSound").objectReferenceValue = fanfareClip;
        so.ApplyModifiedProperties();

        // Start with VictoryPanel disabled
        victoryPanelGO.SetActive(false);

        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
        Debug.Log("[SetupEndSceneVictoryUI] Successfully created and saved EndGameVictoryUI in EndScene.unity!");
    }

    private static void CreateStatBadge(Transform parent, Sprite cardSprite, TMP_FontAsset labelFont, TMP_FontAsset valFont,
        Vector2 pos, string labelText, string valText, Color valColor, out TextMeshProUGUI valTMPOut)
    {
        GameObject badgeGO = new GameObject("StatBadge_" + labelText);
        badgeGO.transform.SetParent(parent, false);
        RectTransform badgeRect = badgeGO.AddComponent<RectTransform>();
        badgeRect.anchorMin = new Vector2(0.5f, 0.5f);
        badgeRect.anchorMax = new Vector2(0.5f, 0.5f);
        badgeRect.pivot = new Vector2(0.5f, 0.5f);
        badgeRect.anchoredPosition = pos;
        badgeRect.sizeDelta = new Vector2(235f, 68f);

        Image borderImg = badgeGO.AddComponent<Image>();
        borderImg.sprite = cardSprite;
        borderImg.type = Image.Type.Sliced;
        borderImg.color = new Color(0.85f, 0.72f, 0.40f, 0.40f);

        GameObject innerGO = new GameObject("Inner");
        innerGO.transform.SetParent(badgeGO.transform, false);
        RectTransform innerRect = innerGO.AddComponent<RectTransform>();
        innerRect.anchorMin = Vector2.zero;
        innerRect.anchorMax = Vector2.one;
        innerRect.offsetMin = new Vector2(2f, 2f);
        innerRect.offsetMax = new Vector2(-2f, -2f);

        Image innerImg = innerGO.AddComponent<Image>();
        innerImg.sprite = cardSprite;
        innerImg.type = Image.Type.Sliced;
        innerImg.color = new Color(0.12f, 0.11f, 0.15f, 0.98f);

        // Label Text
        GameObject labelGO = new GameObject("Label");
        labelGO.transform.SetParent(innerGO.transform, false);
        RectTransform labelRect = labelGO.AddComponent<RectTransform>();
        labelRect.anchorMin = new Vector2(0f, 1f);
        labelRect.anchorMax = new Vector2(1f, 1f);
        labelRect.pivot = new Vector2(0.5f, 1f);
        labelRect.anchoredPosition = new Vector2(0f, -7f);
        labelRect.sizeDelta = new Vector2(220f, 20f);

        TextMeshProUGUI labelTMP = labelGO.AddComponent<TextMeshProUGUI>();
        labelTMP.font = labelFont;
        labelTMP.text = labelText;
        labelTMP.fontSize = 13.5f;
        labelTMP.alignment = TextAlignmentOptions.Center;
        labelTMP.color = new Color(0.80f, 0.78f, 0.75f);
        labelTMP.raycastTarget = false;

        // Value Text
        GameObject valGO = new GameObject("Value");
        valGO.transform.SetParent(innerGO.transform, false);
        RectTransform valRect = valGO.AddComponent<RectTransform>();
        valRect.anchorMin = new Vector2(0f, 0f);
        valRect.anchorMax = new Vector2(1f, 1f);
        valRect.pivot = new Vector2(0.5f, 0.5f);
        valRect.offsetMin = new Vector2(6f, 6f);
        valRect.offsetMax = new Vector2(-6f, -22f);

        valTMPOut = valGO.AddComponent<TextMeshProUGUI>();
        valTMPOut.font = valFont;
        valTMPOut.text = valText;
        valTMPOut.fontSize = 19.5f;
        valTMPOut.fontStyle = FontStyles.Bold;
        valTMPOut.alignment = TextAlignmentOptions.Center;
        valTMPOut.color = valColor;
        valTMPOut.raycastTarget = false;
    }

    private static void CreateCandyButton(Transform parent, Sprite cardSprite, Sprite leafSprite, TMP_FontAsset font,
        Vector2 pos, Vector2 size, string btnText, Color borderCol, Color innerCol, out Button btnOut)
    {
        GameObject btnGO = new GameObject("Btn_" + btnText);
        btnGO.transform.SetParent(parent, false);
        RectTransform btnRect = btnGO.AddComponent<RectTransform>();
        btnRect.anchorMin = new Vector2(0.5f, 0.5f);
        btnRect.anchorMax = new Vector2(0.5f, 0.5f);
        btnRect.pivot = new Vector2(0.5f, 0.5f);
        btnRect.anchoredPosition = pos;
        btnRect.sizeDelta = size;

        // Border
        Image borderImg = btnGO.AddComponent<Image>();
        borderImg.sprite = cardSprite;
        borderImg.type = Image.Type.Sliced;
        borderImg.color = borderCol;

        // Inner
        GameObject innerGO = new GameObject("Inner");
        innerGO.transform.SetParent(btnGO.transform, false);
        RectTransform innerRect = innerGO.AddComponent<RectTransform>();
        innerRect.anchorMin = Vector2.zero;
        innerRect.anchorMax = Vector2.one;
        innerRect.offsetMin = new Vector2(3f, 3f);
        innerRect.offsetMax = new Vector2(-3f, -3f);

        Image innerImg = innerGO.AddComponent<Image>();
        innerImg.sprite = cardSprite;
        innerImg.type = Image.Type.Sliced;
        innerImg.color = innerCol;

        btnOut = btnGO.AddComponent<Button>();
        btnOut.targetGraphic = innerImg;
        ColorBlock cb = btnOut.colors;
        cb.highlightedColor = new Color(innerCol.r * 1.5f, innerCol.g * 1.5f, innerCol.b * 1.5f, 1f);
        cb.pressedColor = new Color(innerCol.r * 0.7f, innerCol.g * 0.7f, innerCol.b * 0.7f, 1f);
        btnOut.colors = cb;

        // Leaf Left & Right
        if (leafSprite != null)
        {
            CreateLeafIcon(innerGO.transform, leafSprite, new Vector2(-size.x * 0.42f, 0f), 24f, -30f, borderCol);
            CreateLeafIcon(innerGO.transform, leafSprite, new Vector2(size.x * 0.42f, 0f), 24f, 30f, borderCol);
        }

        // Text
        GameObject textGO = new GameObject("Text");
        textGO.transform.SetParent(innerGO.transform, false);
        RectTransform textRect = textGO.AddComponent<RectTransform>();
        textRect.anchorMin = Vector2.zero;
        textRect.anchorMax = Vector2.one;
        textRect.sizeDelta = Vector2.zero;

        TextMeshProUGUI tmp = textGO.AddComponent<TextMeshProUGUI>();
        tmp.font = font;
        tmp.text = btnText;
        tmp.fontSize = 18f;
        tmp.fontStyle = FontStyles.Bold;
        tmp.alignment = TextAlignmentOptions.Center;
        tmp.color = new Color(0.98f, 0.96f, 0.92f);
        tmp.raycastTarget = false;
    }

    private static void CreateLeafIcon(Transform parent, Sprite leafSprite, Vector2 pos, float size, float rotZ, Color col)
    {
        GameObject leafGO = new GameObject("LeafIcon");
        leafGO.transform.SetParent(parent, false);
        RectTransform lRect = leafGO.AddComponent<RectTransform>();
        lRect.anchorMin = new Vector2(0.5f, 0.5f);
        lRect.anchorMax = new Vector2(0.5f, 0.5f);
        lRect.anchoredPosition = pos;
        lRect.sizeDelta = new Vector2(size, size);
        lRect.localRotation = Quaternion.Euler(0f, 0f, rotZ);

        Image lImg = leafGO.AddComponent<Image>();
        lImg.sprite = leafSprite;
        lImg.color = col;
        lImg.preserveAspect = true;
        lImg.raycastTarget = false;
    }

    private static void ConfigureCanvasScaler(Canvas canvas)
    {
        CanvasScaler scaler = canvas.GetComponent<CanvasScaler>();
        if (scaler == null) scaler = canvas.gameObject.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);
        scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
        scaler.matchWidthOrHeight = 0.5f;
    }

    [MenuItem("Tools/Rootbound Guardian/Capture EndScene Victory Screenshot")]
    public static void CaptureEndSceneScreenshot()
    {
        SetupVictoryUIInScene();

        var scene = EditorSceneManager.OpenScene(SCENE_PATH, OpenSceneMode.Single);

        // 1. Ensure the ancient tree leaves are fully bloomed for the screenshot
        var treeObj = GameObject.Find("sakura_cqz");
        if (treeObj != null)
        {
            var rend = treeObj.GetComponent<MeshRenderer>();
            if (rend != null && rend.sharedMaterials.Length > 1)
            {
                var mats = rend.materials;
                if (mats[1] != null)
                {
                    mats[1].SetFloat("_Cutoff", 0.23f); // Fully bloomed
                    rend.materials = mats;
                }
            }
        }

        // 2. Align Player & Lumi on the stone bridge
        GameObject player = GameObject.Find("Player Variant");
        if (player == null) player = GameObject.FindGameObjectWithTag("Player");
        if (player != null)
        {
            player.transform.position = new Vector3(44.75f, 5.89f, 29.99f);
            player.transform.rotation = Quaternion.identity;
        }

        GameObject lumi = GameObject.Find("Lumi_Companion");
        if (lumi != null)
        {
            lumi.transform.position = new Vector3(43.65f, 5.70f, 28.80f);
            lumi.transform.rotation = Quaternion.Euler(0f, 180f, 0f);
        }

        // 3. Find and position Camera for a majestic cinematic backdrop
        Camera cam = Camera.main;
        if (cam == null) cam = Object.FindFirstObjectByType<Camera>();
        if (cam == null)
        {
            Debug.LogError("[SetupEndSceneVictoryUI] No camera found in scene to capture screenshot!");
            return;
        }

        Transform origCamParent = cam.transform.parent;
        Vector3 origCamPos = cam.transform.position;
        Quaternion origCamRot = cam.transform.rotation;

        cam.transform.SetParent(null, true);
        cam.transform.position = new Vector3(44.20f, 6.20f, 24.80f);
        cam.transform.rotation = Quaternion.Euler(4.5f, 0f, 0f);

        // 4. Configure Canvas for Camera rendering
        Canvas canvas = Object.FindFirstObjectByType<Canvas>();
        RenderMode origRenderMode = RenderMode.ScreenSpaceOverlay;
        Camera origWorldCam = null;
        float origPlaneDistance = 100f;

        if (canvas != null)
        {
            origRenderMode = canvas.renderMode;
            origWorldCam = canvas.worldCamera;
            origPlaneDistance = canvas.planeDistance;

            canvas.renderMode = RenderMode.ScreenSpaceCamera;
            canvas.worldCamera = cam;
            canvas.planeDistance = 1.2f;

            Transform vp = canvas.transform.Find("VictoryPanel");
            if (vp != null)
            {
                vp.gameObject.SetActive(true);
                CanvasGroup cg = vp.GetComponent<CanvasGroup>();
                if (cg != null) cg.alpha = 1f;
            }
        }

        // 5. Create high-res RenderTexture (1920x1080)
        int width = 1920;
        int height = 1080;
        RenderTexture rt = new RenderTexture(width, height, 24, RenderTextureFormat.ARGB32);
        RenderTexture prevActive = RenderTexture.active;
        RenderTexture prevTarget = cam.targetTexture;

        cam.targetTexture = rt;
        cam.Render();

        RenderTexture.active = rt;
        Texture2D screenShot = new Texture2D(width, height, TextureFormat.RGB24, false);
        screenShot.ReadPixels(new Rect(0, 0, width, height), 0, 0);
        screenShot.Apply();

        // 6. Reset camera & canvas
        cam.targetTexture = prevTarget;
        RenderTexture.active = prevActive;
        Object.DestroyImmediate(rt);

        if (canvas != null)
        {
            canvas.renderMode = origRenderMode;
            canvas.worldCamera = origWorldCam;
            canvas.planeDistance = origPlaneDistance;

            Transform vp = canvas.transform.Find("VictoryPanel");
            if (vp != null)
            {
                vp.gameObject.SetActive(false);
            }
        }

        cam.transform.position = origCamPos;
        cam.transform.rotation = origCamRot;
        if (origCamParent != null) cam.transform.SetParent(origCamParent, true);

        // 7. Save screenshot
        byte[] bytes = screenShot.EncodeToPNG();
        Object.DestroyImmediate(screenShot);

        string outputPath = @"C:\Users\Artemis\Downloads\EndScene_Victory_UI_Showcase.png";
        File.WriteAllBytes(outputPath, bytes);
        Debug.Log("[SetupEndSceneVictoryUI] Screenshot saved successfully to: " + outputPath);

        // Copy to brain artifacts folder
        string artifactDir = @"C:\Users\Artemis\.gemini\antigravity-cli\brain\c9b8b45e-dfed-47a5-b01e-f8452dc557e4";
        if (Directory.Exists(artifactDir))
        {
            string artifactDest = Path.Combine(artifactDir, "EndScene_Victory_UI_Showcase.png");
            File.WriteAllBytes(artifactDest, bytes);
            Debug.Log("[SetupEndSceneVictoryUI] Copied screenshot to artifacts: " + artifactDest);
        }

        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
    }
}
#endif
