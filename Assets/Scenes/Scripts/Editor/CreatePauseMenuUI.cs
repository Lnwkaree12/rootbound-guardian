#if UNITY_EDITOR
using System.IO;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// Editor utility to generate authentic, cozy fairytale Pause Menu (ESC Menu) UI prefab
/// for Rootbound Guardian matching the game's exact aesthetic (cat paws, sprouts, warm wood cards,
/// golden accents, and rich typography).
/// Strictly builds prefabs without modifying or opening any scene.
/// </summary>
public static class CreatePauseMenuUI
{
    private const string PREFAB_PATH = "Assets/Prefabs/PauseMenu_Canvas.prefab";
    private const string SCREENSHOT_PATH = @"C:\Users\Artemis\Downloads\Pause_UI.png";

    private const string FONT_ONELITTLE = "Assets/Image/OneLittleFont-Full SDF.asset";
    private const string FONT_ITIM = "Assets/Fonts/Itim_SDF_Valid.asset";

    private const string SPRITE_MODAL_CARD = "Assets/Main Screen/UI/ModalCardBg.png";
    private const string SPRITE_KEY_BADGE = "Assets/Main Screen/UI/KeyBadgeBg.png";
    private const string SPRITE_PAW = "Assets/Main Screen/UI/PawIcon.png";
    private const string SPRITE_LEAF = "Assets/Main Screen/UI/LeafIcon.png";
    private const string SPRITE_SPROUT = "Assets/Image/UI/Cute_Sprout_Badge.png";
    private const string SPRITE_BUBBLE = "Assets/Image/UI/Cute_Tree_Bubble_Bg.png";
    private const string SPRITE_TRACK = "Assets/Main Screen/UI/SliderTrack.png";
    private const string SPRITE_KNOB = "Assets/Main Screen/UI/DiamondKnob.png";

    [MenuItem("Tools/Rootbound Guardian/Build Pause Menu UI Prefab")]
    public static void BuildPauseMenuPrefab()
    {
        Debug.Log("[CreatePauseMenuUI] Starting build of PauseMenu_Canvas.prefab...");

        if (!Directory.Exists("Assets/Prefabs"))
        {
            Directory.CreateDirectory("Assets/Prefabs");
        }

        TMP_FontAsset titleFont = LoadFont(FONT_ONELITTLE);
        TMP_FontAsset bodyFont = LoadFont(FONT_ITIM);

        Sprite modalCardSprite = AssetDatabase.LoadAssetAtPath<Sprite>(SPRITE_MODAL_CARD);
        Sprite keyBadgeSprite = AssetDatabase.LoadAssetAtPath<Sprite>(SPRITE_KEY_BADGE);
        Sprite leafSprite = AssetDatabase.LoadAssetAtPath<Sprite>(SPRITE_LEAF);
        if (leafSprite == null) leafSprite = AssetDatabase.LoadAssetAtPath<Sprite>(SPRITE_PAW);
        Sprite sproutSprite = AssetDatabase.LoadAssetAtPath<Sprite>(SPRITE_SPROUT);
        Sprite trackSprite = AssetDatabase.LoadAssetAtPath<Sprite>(SPRITE_TRACK);
        Sprite knobSprite = AssetDatabase.LoadAssetAtPath<Sprite>(SPRITE_KNOB);

        if (trackSprite == null) trackSprite = keyBadgeSprite;
        if (knobSprite == null) knobSprite = leafSprite;

        // 1. Root Canvas
        GameObject canvasGO = new GameObject("PauseMenu_Canvas");
        Canvas canvas = canvasGO.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 110; // On top of standard game HUD

        CanvasScaler scaler = canvasGO.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);
        scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
        scaler.matchWidthOrHeight = 0.5f;

        canvasGO.AddComponent<GraphicRaycaster>();

        AudioSource audioSource = canvasGO.AddComponent<AudioSource>();
        audioSource.playOnAwake = false;
        audioSource.spatialBlend = 0f;

        PauseMenuController pauseController = canvasGO.AddComponent<PauseMenuController>();
        pauseController.audioSource = audioSource;

        // 2. PauseMenuPanel (Full Screen Overlay)
        GameObject pausePanelGO = new GameObject("PauseMenuPanel");
        pausePanelGO.transform.SetParent(canvasGO.transform, false);
        RectTransform panelRect = pausePanelGO.AddComponent<RectTransform>();
        panelRect.anchorMin = Vector2.zero;
        panelRect.anchorMax = Vector2.one;
        panelRect.sizeDelta = Vector2.zero;

        Image panelImg = pausePanelGO.AddComponent<Image>();
        panelImg.color = new Color(0.04f, 0.05f, 0.08f, 0.88f); // Twilight Forest Vignette

        CanvasGroup canvasGroup = pausePanelGO.AddComponent<CanvasGroup>();
        canvasGroup.alpha = 1f;
        canvasGroup.interactable = true;
        canvasGroup.blocksRaycasts = true;

        pauseController.pausePanel = pausePanelGO;
        pauseController.pauseCanvasGroup = canvasGroup;

        // 3. Modal Container (Outer Glow Frame)
        GameObject modalContainerGO = new GameObject("ModalContainer");
        modalContainerGO.transform.SetParent(pausePanelGO.transform, false);
        RectTransform modalRect = modalContainerGO.AddComponent<RectTransform>();
        modalRect.anchorMin = new Vector2(0.5f, 0.5f);
        modalRect.anchorMax = new Vector2(0.5f, 0.5f);
        modalRect.pivot = new Vector2(0.5f, 0.5f);
        modalRect.anchoredPosition = Vector2.zero;
        modalRect.sizeDelta = new Vector2(860f, 740f);

        Image modalBorder = modalContainerGO.AddComponent<Image>();
        modalBorder.sprite = modalCardSprite;
        modalBorder.type = Image.Type.Sliced;
        modalBorder.color = new Color(1f, 0.85f, 0.40f, 0.85f); // Rich radiant golden glow
        pauseController.modalContainer = modalRect;

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
        cardImg.color = new Color(0.10f, 0.08f, 0.12f, 0.98f); // Deep Tavern Oak Charcoal

        // 5. Floating Sprout Crest at Top
        GameObject crestGO = new GameObject("FloatingCrest");
        crestGO.transform.SetParent(innerCardGO.transform, false);
        RectTransform crestRect = crestGO.AddComponent<RectTransform>();
        crestRect.anchorMin = new Vector2(0.5f, 1f);
        crestRect.anchorMax = new Vector2(0.5f, 1f);
        crestRect.pivot = new Vector2(0.5f, 0.5f);
        crestRect.anchoredPosition = new Vector2(0f, 26f);
        crestRect.sizeDelta = new Vector2(120f, 120f);

        Image crestImg = crestGO.AddComponent<Image>();
        crestImg.sprite = sproutSprite;
        crestImg.raycastTarget = false;
        crestGO.AddComponent<TitleIdleBobbing>();

        // 6. Header Badge & Plaque
        // 6a. Sub-Badge "G A M E   P A U S E D"
        GameObject badgeTxtGO = new GameObject("BadgeText");
        badgeTxtGO.transform.SetParent(innerCardGO.transform, false);
        RectTransform btRect = badgeTxtGO.AddComponent<RectTransform>();
        btRect.anchorMin = new Vector2(0.5f, 1f);
        btRect.anchorMax = new Vector2(0.5f, 1f);
        btRect.pivot = new Vector2(0.5f, 1f);
        btRect.anchoredPosition = new Vector2(0f, -44f);
        btRect.sizeDelta = new Vector2(600f, 26f);

        TextMeshProUGUI badgeTMP = badgeTxtGO.AddComponent<TextMeshProUGUI>();
        badgeTMP.font = titleFont;
        badgeTMP.text = "P A U S E D";
        badgeTMP.fontSize = 20f;
        badgeTMP.fontStyle = FontStyles.Bold;
        badgeTMP.alignment = TextAlignmentOptions.Center;
        badgeTMP.color = new Color(1f, 0.88f, 0.55f, 1f);
        badgeTMP.raycastTarget = false;

        // 6b. Title Text: ADVENTURE PAUSED (พักการผจญภัย)
        GameObject titleGO = new GameObject("TitleText");
        titleGO.transform.SetParent(innerCardGO.transform, false);
        RectTransform titleRect = titleGO.AddComponent<RectTransform>();
        titleRect.anchorMin = new Vector2(0.5f, 1f);
        titleRect.anchorMax = new Vector2(0.5f, 1f);
        titleRect.pivot = new Vector2(0.5f, 1f);
        titleRect.anchoredPosition = new Vector2(0f, -68f);
        titleRect.sizeDelta = new Vector2(760f, 54f);

        TextMeshProUGUI titleTMP = titleGO.AddComponent<TextMeshProUGUI>();
        titleTMP.font = titleFont;
        titleTMP.text = "ADVENTURE PAUSED";
        titleTMP.fontSize = 46f;
        titleTMP.fontStyle = FontStyles.Bold;
        titleTMP.alignment = TextAlignmentOptions.Center;
        titleTMP.color = new Color(1f, 0.90f, 0.48f, 1f); // Radiant Honey Gold
        titleTMP.raycastTarget = false;

        // 6c. Subtitle Text
        GameObject subGO = new GameObject("SubtitleText");
        subGO.transform.SetParent(innerCardGO.transform, false);
        RectTransform subRect = subGO.AddComponent<RectTransform>();
        subRect.anchorMin = new Vector2(0.5f, 1f);
        subRect.anchorMax = new Vector2(0.5f, 1f);
        subRect.pivot = new Vector2(0.5f, 1f);
        subRect.anchoredPosition = new Vector2(0f, -124f);
        subRect.sizeDelta = new Vector2(720f, 30f);

        TextMeshProUGUI subTMP = subGO.AddComponent<TextMeshProUGUI>();
        subTMP.font = bodyFont;
        subTMP.text = "หยุดพักการผจญภัยชั่วคราว — ผืนป่ากำลังรอคุณอยู่";
        subTMP.fontSize = 21f;
        subTMP.alignment = TextAlignmentOptions.Center;
        subTMP.color = new Color(0.96f, 0.92f, 0.88f, 1f);
        subTMP.raycastTarget = false;

        // 7. Safe Haven Lore / Tip Capsule (Framed)
        GameObject bubbleGO = new GameObject("LoreBubble");
        bubbleGO.transform.SetParent(innerCardGO.transform, false);
        RectTransform bRect = bubbleGO.AddComponent<RectTransform>();
        bRect.anchorMin = new Vector2(0.5f, 1f);
        bRect.anchorMax = new Vector2(0.5f, 1f);
        bRect.pivot = new Vector2(0.5f, 1f);
        bRect.anchoredPosition = new Vector2(0f, -164f);
        bRect.sizeDelta = new Vector2(740f, 44f);

        Image bImg = bubbleGO.AddComponent<Image>();
        bImg.sprite = keyBadgeSprite;
        bImg.type = Image.Type.Sliced;
        bImg.color = new Color(0.92f, 0.78f, 0.38f, 0.75f); // Gold Border Frame

        GameObject bubbleInnerGO = new GameObject("InnerBg");
        bubbleInnerGO.transform.SetParent(bubbleGO.transform, false);
        RectTransform biRect = bubbleInnerGO.AddComponent<RectTransform>();
        biRect.anchorMin = Vector2.zero;
        biRect.anchorMax = Vector2.one;
        biRect.offsetMin = new Vector2(2f, 2f);
        biRect.offsetMax = new Vector2(-2f, -2f);

        Image biImg = bubbleInnerGO.AddComponent<Image>();
        biImg.sprite = keyBadgeSprite;
        biImg.type = Image.Type.Sliced;
        biImg.color = new Color(0.12f, 0.10f, 0.14f, 0.95f);
        biImg.raycastTarget = false;

        GameObject bubbleTxtGO = new GameObject("Text");
        bubbleTxtGO.transform.SetParent(bubbleGO.transform, false);
        RectTransform bTxtRect = bubbleTxtGO.AddComponent<RectTransform>();
        bTxtRect.anchorMin = Vector2.zero;
        bTxtRect.anchorMax = Vector2.one;
        bTxtRect.offsetMin = Vector2.zero;
        bTxtRect.offsetMax = Vector2.zero;

        TextMeshProUGUI bTMP = bubbleTxtGO.AddComponent<TextMeshProUGUI>();
        bTMP.font = bodyFont;
        bTMP.text = "ผืนป่าเงียบสงบลง... ปลอดภัยจากการโจมตีในระหว่างนี้";
        bTMP.fontSize = 17f;
        bTMP.alignment = TextAlignmentOptions.Center;
        bTMP.color = new Color(1f, 0.92f, 0.60f, 1f); // Cozy Warm Gold
        bTMP.raycastTarget = false;

        // 8. Audio Settings Section (Inset Wood Panel)
        GameObject audioSectionGO = new GameObject("AudioSettingsSection");
        audioSectionGO.transform.SetParent(innerCardGO.transform, false);
        RectTransform audioSecRect = audioSectionGO.AddComponent<RectTransform>();
        audioSecRect.anchorMin = new Vector2(0.5f, 1f);
        audioSecRect.anchorMax = new Vector2(0.5f, 1f);
        audioSecRect.pivot = new Vector2(0.5f, 1f);
        audioSecRect.anchoredPosition = new Vector2(0f, -220f);
        audioSecRect.sizeDelta = new Vector2(740f, 280f);

        Image audioBorderImg = audioSectionGO.AddComponent<Image>();
        audioBorderImg.sprite = keyBadgeSprite;
        audioBorderImg.type = Image.Type.Sliced;
        audioBorderImg.color = new Color(0.92f, 0.76f, 0.35f, 0.90f); // Warm Bronze-Gold Frame

        GameObject audioInnerGO = new GameObject("InnerBg");
        audioInnerGO.transform.SetParent(audioSectionGO.transform, false);
        RectTransform aInnerRect = audioInnerGO.AddComponent<RectTransform>();
        aInnerRect.anchorMin = Vector2.zero;
        aInnerRect.anchorMax = Vector2.one;
        aInnerRect.offsetMin = new Vector2(3f, 3f);
        aInnerRect.offsetMax = new Vector2(-3f, -3f);

        Image aInnerImg = audioInnerGO.AddComponent<Image>();
        aInnerImg.sprite = keyBadgeSprite;
        aInnerImg.type = Image.Type.Sliced;
        aInnerImg.color = new Color(0.08f, 0.07f, 0.10f, 0.98f); // Deep Tavern Charcoal
        aInnerImg.raycastTarget = false;

        // Audio Header
        GameObject audioHeaderGO = new GameObject("Header");
        audioHeaderGO.transform.SetParent(audioSectionGO.transform, false);
        RectTransform ahRect = audioHeaderGO.AddComponent<RectTransform>();
        ahRect.anchorMin = new Vector2(0.5f, 1f);
        ahRect.anchorMax = new Vector2(0.5f, 1f);
        ahRect.pivot = new Vector2(0.5f, 1f);
        ahRect.anchoredPosition = new Vector2(0f, -14f);
        ahRect.sizeDelta = new Vector2(700f, 30f);

        TextMeshProUGUI ahTMP = audioHeaderGO.AddComponent<TextMeshProUGUI>();
        ahTMP.font = bodyFont;
        ahTMP.text = "การตั้งค่าเสียง (AUDIO SETTINGS)";
        ahTMP.fontSize = 20f;
        ahTMP.fontStyle = FontStyles.Bold;
        ahTMP.alignment = TextAlignmentOptions.Center;
        ahTMP.color = new Color(1f, 0.88f, 0.45f, 1f); // Radiant Honey Gold
        ahTMP.raycastTarget = false;

        // 8a. Master Volume Row
        CreateSliderRow(audioSectionGO.transform, "MasterRow", "เสียงหลัก (Master)", -66f,
            bodyFont, trackSprite, keyBadgeSprite, knobSprite,
            out Slider masterSlider, out TextMeshProUGUI masterValTMP);
        pauseController.masterVolumeSlider = masterSlider;
        pauseController.masterVolumeText = masterValTMP;

        // 8b. BGM Volume Row
        CreateSliderRow(audioSectionGO.transform, "BgmRow", "ดนตรีประกอบ (BGM)", -132f,
            bodyFont, trackSprite, keyBadgeSprite, knobSprite,
            out Slider bgmSlider, out TextMeshProUGUI bgmValTMP);
        pauseController.bgmVolumeSlider = bgmSlider;
        pauseController.bgmVolumeText = bgmValTMP;

        // 8c. SFX Volume Row
        CreateSliderRow(audioSectionGO.transform, "SfxRow", "เอฟเฟกต์ (SFX)", -198f,
            bodyFont, trackSprite, keyBadgeSprite, knobSprite,
            out Slider sfxSlider, out TextMeshProUGUI sfxValTMP);
        pauseController.sfxVolumeSlider = sfxSlider;
        pauseController.sfxVolumeText = sfxValTMP;

        // 9. Action Buttons Row (Living Flanking Paws)
        GameObject buttonRowGO = new GameObject("ButtonRow");
        buttonRowGO.transform.SetParent(innerCardGO.transform, false);
        RectTransform rowRect = buttonRowGO.AddComponent<RectTransform>();
        rowRect.anchorMin = new Vector2(0.5f, 0f);
        rowRect.anchorMax = new Vector2(0.5f, 0f);
        rowRect.pivot = new Vector2(0.5f, 0f);
        rowRect.anchoredPosition = new Vector2(0f, 88f);
        rowRect.sizeDelta = new Vector2(740f, 64f);

        HorizontalLayoutGroup rowLayout = buttonRowGO.AddComponent<HorizontalLayoutGroup>();
        rowLayout.childAlignment = TextAnchor.MiddleCenter;
        rowLayout.spacing = 18f;
        rowLayout.childForceExpandWidth = false;
        rowLayout.childForceExpandHeight = false;

        // 1. Resume Button (Forest Green / Gold)
        CreateJuicyButton(buttonRowGO.transform, "ResumeButton", "ดำเนินการต่อ", bodyFont, 21f,
            keyBadgeSprite, leafSprite,
            new Color(0.22f, 0.40f, 0.22f, 1f),
            new Color(0.34f, 0.58f, 0.30f, 1f),
            new Color(0.14f, 0.26f, 0.12f, 1f),
            new Color(0.95f, 0.82f, 0.40f, 0.98f),
            new Color(1f, 0.90f, 0.55f, 1f),
            232f, 58f,
            out Button resumeBtn);
        pauseController.resumeButton = resumeBtn;

        // 2. Restart Button (Warm Amber Oak)
        CreateJuicyButton(buttonRowGO.transform, "RestartButton", "เริ่มใหม่อีกครั้ง", bodyFont, 21f,
            keyBadgeSprite, leafSprite,
            new Color(0.40f, 0.22f, 0.14f, 1f),
            new Color(0.58f, 0.34f, 0.20f, 1f),
            new Color(0.24f, 0.12f, 0.08f, 1f),
            new Color(0.96f, 0.68f, 0.35f, 0.98f),
            new Color(1f, 0.82f, 0.55f, 1f),
            232f, 58f,
            out Button restartBtn);
        pauseController.restartButton = restartBtn;

        // 3. Main Menu Button (Midnight Slate Obsidian)
        CreateJuicyButton(buttonRowGO.transform, "MainMenuButton", "กลับหน้าหลัก", bodyFont, 21f,
            keyBadgeSprite, leafSprite,
            new Color(0.20f, 0.18f, 0.25f, 1f),
            new Color(0.35f, 0.32f, 0.40f, 1f),
            new Color(0.14f, 0.12f, 0.18f, 1f),
            new Color(0.80f, 0.75f, 0.72f, 0.98f),
            new Color(0.95f, 0.90f, 0.86f, 1f),
            232f, 58f,
            out Button menuBtn);
        pauseController.mainMenuButton = menuBtn;

        // 10. Bottom Key Hints Bar
        GameObject keyHintsGO = new GameObject("KeyHintsBar");
        keyHintsGO.transform.SetParent(innerCardGO.transform, false);
        RectTransform khRect = keyHintsGO.AddComponent<RectTransform>();
        khRect.anchorMin = new Vector2(0.5f, 0f);
        khRect.anchorMax = new Vector2(0.5f, 0f);
        khRect.pivot = new Vector2(0.5f, 0f);
        khRect.anchoredPosition = new Vector2(0f, 26f);
        khRect.sizeDelta = new Vector2(540f, 38f);

        Image khImg = keyHintsGO.AddComponent<Image>();
        khImg.sprite = keyBadgeSprite;
        khImg.type = Image.Type.Sliced;
        khImg.color = new Color(0.85f, 0.70f, 0.35f, 0.65f); // Warm Gold Pill Border

        GameObject khInnerGO = new GameObject("InnerBg");
        khInnerGO.transform.SetParent(keyHintsGO.transform, false);
        RectTransform khiRect = khInnerGO.AddComponent<RectTransform>();
        khiRect.anchorMin = Vector2.zero;
        khiRect.anchorMax = Vector2.one;
        khiRect.offsetMin = new Vector2(2f, 2f);
        khiRect.offsetMax = new Vector2(-2f, -2f);

        Image khiImg = khInnerGO.AddComponent<Image>();
        khiImg.sprite = keyBadgeSprite;
        khiImg.type = Image.Type.Sliced;
        khiImg.color = new Color(0.10f, 0.08f, 0.12f, 0.95f);
        khiImg.raycastTarget = false;

        GameObject khTxtGO = new GameObject("Text");
        khTxtGO.transform.SetParent(keyHintsGO.transform, false);
        RectTransform khtRect = khTxtGO.AddComponent<RectTransform>();
        khtRect.anchorMin = Vector2.zero;
        khtRect.anchorMax = Vector2.one;
        khtRect.offsetMin = Vector2.zero;
        khtRect.offsetMax = Vector2.zero;

        TextMeshProUGUI khTMP = khTxtGO.AddComponent<TextMeshProUGUI>();
        khTMP.font = bodyFont;
        khTMP.text = "[ESC] สลับเมนู / ย้อนกลับ       [ENTER] ยืนยัน";
        khTMP.fontSize = 16f;
        khTMP.alignment = TextAlignmentOptions.Center;
        khTMP.color = new Color(0.95f, 0.90f, 0.82f, 1f); // Soft Parchment
        khTMP.raycastTarget = false;

        // Start disabled in prefab for proper in-game instantiation
        pausePanelGO.SetActive(false);

        // Save Prefab
        PrefabUtility.SaveAsPrefabAsset(canvasGO, PREFAB_PATH);
        Object.DestroyImmediate(canvasGO);

        AssetDatabase.Refresh();
        Debug.Log($"[CreatePauseMenuUI] Built and saved: {PREFAB_PATH}");
    }

    [MenuItem("Tools/Rootbound Guardian/Capture Pause Menu Screenshot")]
    public static void CapturePauseMenuScreenshot()
    {
        Debug.Log("[CreatePauseMenuUI] Capturing screenshot of PauseMenu_Canvas.prefab...");

        // Ensure prefab is built
        BuildPauseMenuPrefab();

        GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(PREFAB_PATH);
        if (prefab == null)
        {
            Debug.LogError($"[CreatePauseMenuUI] Cannot load prefab at {PREFAB_PATH}");
            return;
        }

        GameObject instance = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
        instance.name = "PauseMenu_Canvas_CaptureTemp";

        // Enable PauseMenuPanel
        Transform pausePanel = instance.transform.Find("PauseMenuPanel");
        if (pausePanel != null)
        {
            pausePanel.gameObject.SetActive(true);
            CanvasGroup cg = pausePanel.GetComponent<CanvasGroup>();
            if (cg != null) cg.alpha = 1f;
        }

        // Set layer recursively to UI (5)
        int uiLayer = LayerMask.NameToLayer("UI");
        if (uiLayer < 0) uiLayer = 5;
        SetLayerRecursively(instance, uiLayer);

        // Setup temporary camera
        GameObject camGO = new GameObject("PauseUICaptureCamera");
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
        File.WriteAllBytes(SCREENSHOT_PATH, bytes);

        // Cleanup
        RenderTexture.active = null;
        cam.targetTexture = null;
        rt.Release();
        Object.DestroyImmediate(rt);
        Object.DestroyImmediate(tex);
        Object.DestroyImmediate(camGO);
        Object.DestroyImmediate(instance);

        Debug.Log($"[CreatePauseMenuUI] Saved screenshot to: {SCREENSHOT_PATH} ({bytes.Length} bytes)");
    }

    private static void CreateSliderRow(Transform parent, string rowName, string labelText, float anchoredY,
        TMP_FontAsset font, Sprite trackSprite, Sprite fillSprite, Sprite knobSprite,
        out Slider slider, out TextMeshProUGUI valueTMP)
    {
        GameObject rowGO = new GameObject(rowName);
        rowGO.transform.SetParent(parent, false);
        RectTransform rowRect = rowGO.AddComponent<RectTransform>();
        rowRect.anchorMin = new Vector2(0.5f, 1f);
        rowRect.anchorMax = new Vector2(0.5f, 1f);
        rowRect.pivot = new Vector2(0.5f, 0.5f);
        rowRect.anchoredPosition = new Vector2(0f, anchoredY);
        rowRect.sizeDelta = new Vector2(680f, 44f);

        // 1. Label Text
        GameObject labelGO = new GameObject("Label");
        labelGO.transform.SetParent(rowGO.transform, false);
        RectTransform labelRect = labelGO.AddComponent<RectTransform>();
        labelRect.anchorMin = new Vector2(0f, 0.5f);
        labelRect.anchorMax = new Vector2(0f, 0.5f);
        labelRect.pivot = new Vector2(0f, 0.5f);
        labelRect.anchoredPosition = new Vector2(10f, 0f);
        labelRect.sizeDelta = new Vector2(200f, 32f);

        TextMeshProUGUI labelTMP = labelGO.AddComponent<TextMeshProUGUI>();
        labelTMP.font = font;
        labelTMP.text = labelText;
        labelTMP.fontSize = 19f;
        labelTMP.alignment = TextAlignmentOptions.Left;
        labelTMP.color = new Color(0.96f, 0.92f, 0.88f, 1f);
        labelTMP.raycastTarget = false;

        // 2. Slider Component
        GameObject sliderGO = new GameObject("Slider");
        sliderGO.transform.SetParent(rowGO.transform, false);
        RectTransform sliderRect = sliderGO.AddComponent<RectTransform>();
        sliderRect.anchorMin = new Vector2(0f, 0.5f);
        sliderRect.anchorMax = new Vector2(0f, 0.5f);
        sliderRect.pivot = new Vector2(0f, 0.5f);
        sliderRect.anchoredPosition = new Vector2(210f, 0f);
        sliderRect.sizeDelta = new Vector2(380f, 26f);

        slider = sliderGO.AddComponent<Slider>();
        slider.minValue = 0f;
        slider.maxValue = 1f;
        slider.value = 0.8f;
        slider.direction = Slider.Direction.LeftToRight;

        // Slider Background Track
        GameObject trackGO = new GameObject("Background");
        trackGO.transform.SetParent(sliderGO.transform, false);
        RectTransform trackRect = trackGO.AddComponent<RectTransform>();
        trackRect.anchorMin = new Vector2(0f, 0.5f);
        trackRect.anchorMax = new Vector2(1f, 0.5f);
        trackRect.pivot = new Vector2(0.5f, 0.5f);
        trackRect.sizeDelta = new Vector2(0f, 14f);

        Image trackImg = trackGO.AddComponent<Image>();
        trackImg.sprite = trackSprite;
        trackImg.type = Image.Type.Sliced;
        trackImg.color = new Color(0.18f, 0.16f, 0.20f, 1f); // Dark Charcoal Track

        // Fill Area
        GameObject fillAreaGO = new GameObject("Fill Area");
        fillAreaGO.transform.SetParent(sliderGO.transform, false);
        RectTransform fillAreaRect = fillAreaGO.AddComponent<RectTransform>();
        fillAreaRect.anchorMin = new Vector2(0f, 0.5f);
        fillAreaRect.anchorMax = new Vector2(1f, 0.5f);
        fillAreaRect.pivot = new Vector2(0.5f, 0.5f);
        fillAreaRect.offsetMin = new Vector2(5f, -7f);
        fillAreaRect.offsetMax = new Vector2(-5f, 7f);

        GameObject fillGO = new GameObject("Fill");
        fillGO.transform.SetParent(fillAreaGO.transform, false);
        RectTransform fillRect = fillGO.AddComponent<RectTransform>();
        fillRect.sizeDelta = Vector2.zero;

        Image fillImg = fillGO.AddComponent<Image>();
        fillImg.sprite = fillSprite;
        fillImg.type = Image.Type.Sliced;
        fillImg.color = new Color(0.96f, 0.72f, 0.25f, 1f); // Warm Amber Gold Fill

        slider.fillRect = fillRect;

        // Handle Slide Area
        GameObject handleAreaGO = new GameObject("Handle Slide Area");
        handleAreaGO.transform.SetParent(sliderGO.transform, false);
        RectTransform handleAreaRect = handleAreaGO.AddComponent<RectTransform>();
        handleAreaRect.anchorMin = Vector2.zero;
        handleAreaRect.anchorMax = Vector2.one;
        handleAreaRect.offsetMin = new Vector2(12f, 0f);
        handleAreaRect.offsetMax = new Vector2(-12f, 0f);

        GameObject handleGO = new GameObject("Handle");
        handleGO.transform.SetParent(handleAreaGO.transform, false);
        RectTransform handleRect = handleGO.AddComponent<RectTransform>();
        handleRect.sizeDelta = new Vector2(28f, 28f);

        Image handleImg = handleGO.AddComponent<Image>();
        handleImg.sprite = knobSprite;
        handleImg.color = new Color(1f, 0.90f, 0.55f, 1f); // Diamond Knob Glow

        slider.handleRect = handleRect;
        slider.targetGraphic = handleImg;

        // 3. Percentage Text
        GameObject valGO = new GameObject("ValueText");
        valGO.transform.SetParent(rowGO.transform, false);
        RectTransform valRect = valGO.AddComponent<RectTransform>();
        valRect.anchorMin = new Vector2(1f, 0.5f);
        valRect.anchorMax = new Vector2(1f, 0.5f);
        valRect.pivot = new Vector2(1f, 0.5f);
        valRect.anchoredPosition = new Vector2(-10f, 0f);
        valRect.sizeDelta = new Vector2(70f, 32f);

        valueTMP = valGO.AddComponent<TextMeshProUGUI>();
        valueTMP.font = font;
        valueTMP.text = "80%";
        valueTMP.fontSize = 20f;
        valueTMP.fontStyle = FontStyles.Bold;
        valueTMP.alignment = TextAlignmentOptions.Right;
        valueTMP.color = new Color(1f, 0.90f, 0.50f, 1f); // Amber Gold
        valueTMP.raycastTarget = false;
    }

    private static void CreateJuicyButton(Transform parent, string name, string labelText, TMP_FontAsset font, float fontSize,
        Sprite bgSprite, Sprite pawSprite, Color normalColor, Color hoverColor, Color pressColor, Color borderColor, Color pawColor,
        float width, float height, out Button button)
    {
        GameObject btnGO = new GameObject(name);
        btnGO.transform.SetParent(parent, false);
        RectTransform btnRect = btnGO.AddComponent<RectTransform>();
        btnRect.sizeDelta = new Vector2(width, height);

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
        hl.spacing = 8f;
        hl.childForceExpandWidth = false;
        hl.childForceExpandHeight = false;

        // Left Paw
        // Left Leaf
        GameObject leftPaw = new GameObject("LeftPaw");
        leftPaw.transform.SetParent(contentGO.transform, false);
        RectTransform lpRect = leftPaw.AddComponent<RectTransform>();
        lpRect.sizeDelta = new Vector2(26f, 26f);
        Image lpImg = leftPaw.AddComponent<Image>();
        lpImg.sprite = pawSprite;
        lpImg.color = pawColor;
        lpImg.raycastTarget = false;

        // Center Label
        GameObject labelGO = new GameObject("Label");
        labelGO.transform.SetParent(contentGO.transform, false);
        RectTransform lRect = labelGO.AddComponent<RectTransform>();
        lRect.sizeDelta = new Vector2(width - 80f, 38f);

        TextMeshProUGUI labelTMP = labelGO.AddComponent<TextMeshProUGUI>();
        labelTMP.font = font;
        labelTMP.text = labelText;
        labelTMP.fontSize = fontSize;
        labelTMP.fontStyle = FontStyles.Bold;
        labelTMP.alignment = TextAlignmentOptions.Center;
        labelTMP.color = Color.white;
        labelTMP.raycastTarget = false;

        // Right Leaf (mirrored horizontally)
        GameObject rightPaw = new GameObject("RightPaw");
        rightPaw.transform.SetParent(contentGO.transform, false);
        RectTransform rpRect = rightPaw.AddComponent<RectTransform>();
        rpRect.sizeDelta = new Vector2(26f, 26f);
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

    private static void SetLayerRecursively(GameObject go, int layer)
    {
        go.layer = layer;
        foreach (Transform child in go.transform)
        {
            SetLayerRecursively(child.gameObject, layer);
        }
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
}
#endif
