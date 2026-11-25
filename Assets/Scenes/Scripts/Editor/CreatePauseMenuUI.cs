#if UNITY_EDITOR
using System.IO;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// Editor utility to build the authentic wooden Pause Menu (ESC Menu) UI prefab
/// using ONLY the artist-provided wooden board, banner, planks, keycap box, knob disc, and arrow sprites.
/// </summary>
public static class CreatePauseMenuUI
{
    private const string PREFAB_PATH = "Assets/Prefabs/PauseMenu_Canvas.prefab";
    private const string SCREENSHOT_PATH = @"C:\Users\Artemis\Downloads\Pause_UI.png";

    private const string FONT_THAI = "Assets/Fonts/Itim_Thai_SDF.asset";
    private const string FONT_THAI_FALLBACK = "Assets/Fonts/Itim_SDF_Valid.asset";
    private const string FONT_TITLE = "Assets/Image/OneLittleFont-Full SDF.asset";

    private const string SPRITE_MAIN_BOARD = "Assets/Scenes/UI New/Menu_is_a_big.png";
    private const string SPRITE_BANNER = "Assets/Asset 2D/Banners.png";
    private const string SPRITE_PLANK_FULL = "Assets/Image/UI/Button_Menu.png";
    private const string SPRITE_PLANK_TIGHT = "Assets/Image/UI/Lumi_Dialogue_Frame.png";
    private const string SPRITE_KNOB_DISC = "Assets/Image/UI/skill.png";
    private const string SPRITE_ARROW = "Assets/Asset 2D/Arrow.png";
    private const string SPRITE_KEY_BOX = "Assets/Asset 2D/button_key.png";

    private static readonly Color DarkWoodTextColor = new Color(0.259f, 0.165f, 0.106f, 1f);
    private static readonly Color SubWoodTextColor = new Color(0.35f, 0.22f, 0.14f, 1f);
    private static readonly Color BadgeRoseColor = new Color(0.92f, 0.42f, 0.40f, 1f);
    private static readonly Color SectionHeaderGold = new Color(1.0f, 0.88f, 0.45f, 1f);

    [MenuItem("Tools/Rootbound Guardian/Build Pause Menu UI Prefab")]
    public static void BuildPauseMenuPrefab()
    {
        Debug.Log("[CreatePauseMenuUI] Building wooden PauseMenu_Canvas.prefab using artist assets...");

        if (!Directory.Exists("Assets/Prefabs"))
        {
            Directory.CreateDirectory("Assets/Prefabs");
        }

        TMP_FontAsset thaiFont = LoadFont(FONT_THAI, FONT_THAI_FALLBACK);
        TMP_FontAsset titleFont = LoadFont(FONT_TITLE, FONT_THAI);

        Sprite boardSprite = LoadRequiredSprite(SPRITE_MAIN_BOARD);
        Sprite bannerSprite = LoadRequiredSprite(SPRITE_BANNER);
        Sprite plankFullSprite = LoadRequiredSprite(SPRITE_PLANK_FULL);
        Sprite plankTightSprite = LoadRequiredSprite(SPRITE_PLANK_TIGHT);
        Sprite knobSprite = LoadRequiredSprite(SPRITE_KNOB_DISC);
        Sprite arrowSprite = LoadRequiredSprite(SPRITE_ARROW);
        Sprite keyBoxSprite = LoadRequiredSprite(SPRITE_KEY_BOX);

        // 1. Root Canvas
        GameObject canvasGO = new GameObject("PauseMenu_Canvas");
        Canvas canvas = canvasGO.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 110;

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

        // 2. Full-screen PauseMenuPanel dim backdrop
        GameObject pausePanelGO = CreateRectObject("PauseMenuPanel", canvasGO.transform, Vector2.zero, Vector2.one, Vector2.one * 0.5f, Vector2.zero, Vector2.zero);
        Image panelImg = pausePanelGO.AddComponent<Image>();
        panelImg.color = new Color(0.04f, 0.03f, 0.02f, 0.72f);

        CanvasGroup canvasGroup = pausePanelGO.AddComponent<CanvasGroup>();
        canvasGroup.alpha = 1f;
        canvasGroup.interactable = true;
        canvasGroup.blocksRaycasts = true;

        pauseController.pausePanel = pausePanelGO;
        pauseController.pauseCanvasGroup = canvasGroup;

        // 3. ModalContainer (no Image behind wooden board)
        GameObject modalContainerGO = CreateRectObject("ModalContainer", pausePanelGO.transform,
            new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
            Vector2.zero, new Vector2(860f, 680f));
        pauseController.modalContainer = modalContainerGO.GetComponent<RectTransform>();

        // 4. InnerCard (stretched, no Image)
        GameObject innerCardGO = CreateRectObject("InnerCard", modalContainerGO.transform,
            Vector2.zero, Vector2.one, new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.zero);

        // 4a. Main Wooden Board (Menu_is_a_big.png)
        GameObject boardBgGO = CreateRectObject("BoardBg", innerCardGO.transform,
            new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
            new Vector2(0f, 15f), new Vector2(1820f, 960f));
        Image boardImg = boardBgGO.AddComponent<Image>();
        boardImg.sprite = boardSprite;
        boardImg.color = Color.white;
        boardImg.preserveAspect = true;
        boardImg.raycastTarget = false;

        // 4b. Top Decorative Banner (Banners.png) positioned cleanly below top wood frame
        GameObject bannerGO = CreateRectObject("HeaderBanner", innerCardGO.transform,
            new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0.5f, 0.5f),
            new Vector2(0f, -130f), new Vector2(680f, 382f));
        Image bannerImg = bannerGO.AddComponent<Image>();
        bannerImg.sprite = bannerSprite;
        bannerImg.color = Color.white;
        bannerImg.preserveAspect = true;
        bannerImg.raycastTarget = false;

        // 5. Header Texts
        TextMeshProUGUI badgeTMP = CreateTMP("BadgeText", innerCardGO.transform,
            new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f),
            new Vector2(0f, -54f), new Vector2(560f, 28f),
            titleFont, "P A U S E D", 20f, FontStyles.Bold, SectionHeaderGold, TextAlignmentOptions.Center);
        Shadow badgeShadow = badgeTMP.gameObject.AddComponent<Shadow>();
        badgeShadow.effectColor = new Color(0.14f, 0.08f, 0.04f, 0.9f);
        badgeShadow.effectDistance = new Vector2(1.5f, -1.5f);

        CreateTMP("TitleText", innerCardGO.transform,
            new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0.5f, 0.5f),
            new Vector2(0f, -147f), new Vector2(480f, 44f),
            thaiFont, "พักการผจญภัย", 32f, FontStyles.Bold, DarkWoodTextColor, TextAlignmentOptions.Center);

        CreateTMP("SubtitleText", innerCardGO.transform,
            new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f),
            new Vector2(0f, -206f), new Vector2(640f, 30f),
            thaiFont, "หยุดพักการผจญภัยชั่วคราว", 20f, FontStyles.Normal, SubWoodTextColor, TextAlignmentOptions.Center);

        // 6. Central Audio Settings Wooden Frame
        GameObject audioSectionGO = CreateRectObject("AudioSettingsSection", innerCardGO.transform,
            new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
            new Vector2(0f, -38f), new Vector2(700f, 276f));

        GameObject frameBgGO = CreateRectObject("SectionFrameBg", audioSectionGO.transform,
            new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
            new Vector2(0f, 74f), new Vector2(1740f, 1080f));
        Image frameBgImg = frameBgGO.AddComponent<Image>();
        frameBgImg.sprite = plankFullSprite;
        frameBgImg.color = Color.white;
        frameBgImg.raycastTarget = false;

        TextMeshProUGUI audioHeaderTMP = CreateTMP("Header", audioSectionGO.transform,
            new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f),
            new Vector2(0f, -14f), new Vector2(620f, 32f),
            thaiFont, "การตั้งค่าเสียง (Audio Settings)", 21f, FontStyles.Bold, SectionHeaderGold, TextAlignmentOptions.Center);
        Shadow headerShadow = audioHeaderTMP.gameObject.AddComponent<Shadow>();
        headerShadow.effectColor = new Color(0.18f, 0.10f, 0.05f, 0.9f);
        headerShadow.effectDistance = new Vector2(1.5f, -1.5f);

        // 6a. Master, BGM, SFX Sliders
        CreateSliderRow(audioSectionGO.transform, "MasterRow", "เสียงรวม", -76f,
            thaiFont, plankTightSprite, knobSprite, arrowSprite, keyBoxSprite, 1.0f,
            out Slider masterSlider, out TextMeshProUGUI masterValTMP);
        pauseController.masterVolumeSlider = masterSlider;
        pauseController.masterVolumeText = masterValTMP;

        CreateSliderRow(audioSectionGO.transform, "BgmRow", "เสียงเพลง", -136f,
            thaiFont, plankTightSprite, knobSprite, arrowSprite, keyBoxSprite, 0.8f,
            out Slider bgmSlider, out TextMeshProUGUI bgmValTMP);
        pauseController.bgmVolumeSlider = bgmSlider;
        pauseController.bgmVolumeText = bgmValTMP;

        CreateSliderRow(audioSectionGO.transform, "SfxRow", "เสียงเอฟเฟกต์", -196f,
            thaiFont, plankTightSprite, knobSprite, arrowSprite, keyBoxSprite, 0.8f,
            out Slider sfxSlider, out TextMeshProUGUI sfxValTMP);
        pauseController.sfxVolumeSlider = sfxSlider;
        pauseController.sfxVolumeText = sfxValTMP;

        // 7. Bottom Action Buttons Row (เล่นต่อ / เริ่มใหม่ / กลับหน้าหลัก)
        GameObject buttonRowGO = CreateRectObject("ButtonRow", innerCardGO.transform,
            new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0.5f, 0f),
            new Vector2(0f, 80f), new Vector2(700f, 74f));

        HorizontalLayoutGroup rowLayout = buttonRowGO.AddComponent<HorizontalLayoutGroup>();
        rowLayout.childAlignment = TextAnchor.MiddleCenter;
        rowLayout.spacing = 12f;
        rowLayout.childForceExpandWidth = false;
        rowLayout.childForceExpandHeight = false;

        pauseController.resumeButton = CreateWoodenButton(buttonRowGO.transform, "ResumeButton", "เล่นต่อ", thaiFont, plankTightSprite, arrowSprite);
        pauseController.restartButton = CreateWoodenButton(buttonRowGO.transform, "RestartButton", "เริ่มใหม่", thaiFont, plankTightSprite, arrowSprite);
        pauseController.mainMenuButton = CreateWoodenButton(buttonRowGO.transform, "MainMenuButton", "กลับหน้าหลัก", thaiFont, plankTightSprite, arrowSprite);

        pausePanelGO.SetActive(false);

        PrefabUtility.SaveAsPrefabAsset(canvasGO, PREFAB_PATH);
        Object.DestroyImmediate(canvasGO);

        AssetDatabase.Refresh();
        Debug.Log($"[CreatePauseMenuUI] Built and saved: {PREFAB_PATH}");
    }

    [MenuItem("Tools/Rootbound Guardian/Capture Pause Menu Screenshot")]
    public static void CapturePauseMenuScreenshot()
    {
        Debug.Log("[CreatePauseMenuUI] Capturing screenshot of PauseMenu_Canvas.prefab...");

        BuildPauseMenuPrefab();

        GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(PREFAB_PATH);
        if (prefab == null)
        {
            Debug.LogError($"[CreatePauseMenuUI] Cannot load prefab at {PREFAB_PATH}");
            return;
        }

        GameObject instance = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
        instance.name = "PauseMenu_Canvas_CaptureTemp";

        Transform pausePanel = instance.transform.Find("PauseMenuPanel");
        if (pausePanel != null)
        {
            pausePanel.gameObject.SetActive(true);
            CanvasGroup cg = pausePanel.GetComponent<CanvasGroup>();
            if (cg != null) cg.alpha = 1f;
        }

        int uiLayer = LayerMask.NameToLayer("UI");
        if (uiLayer < 0) uiLayer = 5;
        SetLayerRecursively(instance, uiLayer);

        GameObject camGO = new GameObject("PauseUICaptureCamera");
        Camera cam = camGO.AddComponent<Camera>();
        cam.clearFlags = CameraClearFlags.SolidColor;
        cam.backgroundColor = new Color(0.06f, 0.05f, 0.04f, 1f);
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

        RenderTexture.active = null;
        cam.targetTexture = null;
        rt.Release();
        Object.DestroyImmediate(rt);
        Object.DestroyImmediate(tex);
        Object.DestroyImmediate(camGO);
        Object.DestroyImmediate(instance);

        Debug.Log($"[CreatePauseMenuUI] Saved screenshot to: {SCREENSHOT_PATH} ({bytes.Length} bytes)");
    }

    private static void CreateSliderRow(
        Transform parent,
        string rowName,
        string labelText,
        float anchoredY,
        TMP_FontAsset font,
        Sprite plankSprite,
        Sprite knobSprite,
        Sprite arrowSprite,
        Sprite keyBoxSprite,
        float defaultValue,
        out Slider slider,
        out TextMeshProUGUI valueTMP)
    {
        GameObject rowGO = CreateRectObject(rowName, parent,
            new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0.5f, 0.5f),
            new Vector2(16f, anchoredY), new Vector2(560f, 52f));

        // 1. Row Label
        CreateTMP("Label", rowGO.transform,
            new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(0f, 0.5f),
            new Vector2(6f, 0f), new Vector2(132f, 38f),
            font, labelText, 20f, FontStyles.Bold, DarkWoodTextColor, TextAlignmentOptions.Left);

        // 2. Arrow Left Button (mirrored wooden arrow)
        CreateArrowStepButton(rowGO.transform, "ArrowLeft", new Vector2(154f, 0f), -1f, arrowSprite);

        // 3. Slider Track + Sprout Green Fill + Round Wooden Disc Knob
        GameObject sliderGO = CreateRectObject("Slider", rowGO.transform,
            new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(0f, 0.5f),
            new Vector2(180f, 0f), new Vector2(236f, 36f));

        slider = sliderGO.AddComponent<Slider>();
        slider.minValue = 0f;
        slider.maxValue = 1f;
        slider.value = defaultValue;
        slider.direction = Slider.Direction.LeftToRight;

        GameObject trackGO = CreateRectObject("Background", sliderGO.transform,
            new Vector2(0f, 0.5f), new Vector2(1f, 0.5f), new Vector2(0.5f, 0.5f),
            Vector2.zero, new Vector2(0f, 32f));
        Image trackImg = trackGO.AddComponent<Image>();
        trackImg.sprite = plankSprite;
        trackImg.color = new Color(0.78f, 0.65f, 0.48f, 1f);
        trackImg.preserveAspect = false;

        GameObject fillAreaGO = new GameObject("Fill Area");
        fillAreaGO.transform.SetParent(sliderGO.transform, false);
        RectTransform fillAreaRect = fillAreaGO.AddComponent<RectTransform>();
        fillAreaRect.anchorMin = new Vector2(0f, 0.5f);
        fillAreaRect.anchorMax = new Vector2(1f, 0.5f);
        fillAreaRect.pivot = new Vector2(0.5f, 0.5f);
        fillAreaRect.offsetMin = new Vector2(16f, -7f);
        fillAreaRect.offsetMax = new Vector2(-16f, 7f);

        GameObject fillGO = CreateRectObject("Fill", fillAreaGO.transform,
            Vector2.zero, Vector2.one, new Vector2(0.5f, 0.5f),
            Vector2.zero, Vector2.zero);
        Image fillImg = fillGO.AddComponent<Image>();
        fillImg.sprite = plankSprite;
        fillImg.color = new Color(0.46f, 0.72f, 0.34f, 1f);
        fillImg.preserveAspect = false;
        fillImg.raycastTarget = false;

        slider.fillRect = fillGO.GetComponent<RectTransform>();

        GameObject handleAreaGO = new GameObject("Handle Slide Area");
        handleAreaGO.transform.SetParent(sliderGO.transform, false);
        RectTransform handleAreaRect = handleAreaGO.AddComponent<RectTransform>();
        handleAreaRect.anchorMin = Vector2.zero;
        handleAreaRect.anchorMax = Vector2.one;
        handleAreaRect.offsetMin = new Vector2(14f, 0f);
        handleAreaRect.offsetMax = new Vector2(-14f, 0f);

        GameObject handleGO = CreateRectObject("Handle", handleAreaGO.transform,
            new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
            Vector2.zero, new Vector2(64f, 64f));
        Image handleImg = handleGO.AddComponent<Image>();
        handleImg.sprite = knobSprite;
        handleImg.color = Color.white;
        handleImg.preserveAspect = true;

        slider.handleRect = handleGO.GetComponent<RectTransform>();
        slider.targetGraphic = handleImg;

        // 4. Arrow Right Button
        CreateArrowStepButton(rowGO.transform, "ArrowRight", new Vector2(440f, 0f), 1f, arrowSprite);

        // 5. Percentage Wooden Key Box Badge + Text
        GameObject badgeGO = CreateRectObject("ValueBadge", rowGO.transform,
            new Vector2(1f, 0.5f), new Vector2(1f, 0.5f), new Vector2(0.5f, 0.5f),
            new Vector2(-36f, 0f), new Vector2(118f, 118f));
        Image badgeImg = badgeGO.AddComponent<Image>();
        badgeImg.sprite = keyBoxSprite;
        badgeImg.color = Color.white;
        badgeImg.preserveAspect = true;
        badgeImg.raycastTarget = false;

        int pct = Mathf.RoundToInt(defaultValue * 100f);
        valueTMP = CreateTMP("ValueText", rowGO.transform,
            new Vector2(1f, 0.5f), new Vector2(1f, 0.5f), new Vector2(0.5f, 0.5f),
            new Vector2(-36f, 0f), new Vector2(70f, 34f),
            font, $"{pct}%", 16f, FontStyles.Bold, DarkWoodTextColor, TextAlignmentOptions.Center);
    }

    private static void CreateArrowStepButton(Transform parent, string name, Vector2 anchoredPos, float scaleX, Sprite arrowSprite)
    {
        GameObject btnGO = CreateRectObject(name, parent,
            new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(0.5f, 0.5f),
            anchoredPos, new Vector2(44f, 44f));

        Image hitImg = btnGO.AddComponent<Image>();
        hitImg.color = new Color(1f, 1f, 1f, 0f);
        hitImg.raycastTarget = true;

        GameObject iconGO = CreateRectObject("Icon", btnGO.transform,
            new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
            Vector2.zero, new Vector2(76f, 76f));
        iconGO.transform.localScale = new Vector3(scaleX, 1f, 1f);
        Image iconImg = iconGO.AddComponent<Image>();
        iconImg.sprite = arrowSprite;
        iconImg.color = Color.white;
        iconImg.preserveAspect = true;
        iconImg.raycastTarget = false;

        Button btn = btnGO.AddComponent<Button>();
        btn.targetGraphic = iconImg;
        ColorBlock cb = btn.colors;
        cb.normalColor = Color.white;
        cb.highlightedColor = new Color(1f, 0.94f, 0.80f, 1f);
        cb.pressedColor = new Color(0.82f, 0.72f, 0.60f, 1f);
        cb.selectedColor = cb.highlightedColor;
        cb.fadeDuration = 0.1f;
        btn.colors = cb;
    }

    private static Button CreateWoodenButton(Transform parent, string name, string labelText, TMP_FontAsset font, Sprite plankSprite, Sprite arrowSprite)
    {
        GameObject btnGO = new GameObject(name);
        btnGO.transform.SetParent(parent, false);
        RectTransform btnRect = btnGO.AddComponent<RectTransform>();
        btnRect.sizeDelta = new Vector2(230f, 72f);

        Image btnImg = btnGO.AddComponent<Image>();
        btnImg.sprite = plankSprite;
        btnImg.color = Color.white;
        btnImg.preserveAspect = false;

        Button button = btnGO.AddComponent<Button>();
        button.targetGraphic = btnImg;
        ColorBlock colors = button.colors;
        colors.normalColor = Color.white;
        colors.highlightedColor = new Color(1f, 0.95f, 0.82f, 1f);
        colors.pressedColor = new Color(0.85f, 0.76f, 0.65f, 1f);
        colors.selectedColor = colors.highlightedColor;
        colors.fadeDuration = 0.1f;
        button.colors = colors;

        GameObject contentGO = CreateRectObject("Content", btnGO.transform,
            Vector2.zero, Vector2.one, new Vector2(0.5f, 0.5f),
            Vector2.zero, Vector2.zero);

        // Left Focus Arrow (hidden unless hovered/selected)
        GameObject leftPaw = CreateRectObject("LeftPaw", contentGO.transform,
            new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(0.5f, 0.5f),
            new Vector2(26f, 0f), new Vector2(54f, 54f));
        Image lpImg = leftPaw.AddComponent<Image>();
        lpImg.sprite = arrowSprite;
        lpImg.color = Color.white;
        lpImg.preserveAspect = true;
        lpImg.raycastTarget = false;

        // Center Label
        CreateTMP("Label", contentGO.transform,
            Vector2.zero, Vector2.one, new Vector2(0.5f, 0.5f),
            Vector2.zero, new Vector2(-56f, 0f),
            font, labelText, 23f, FontStyles.Bold, DarkWoodTextColor, TextAlignmentOptions.Center);

        // Right Focus Arrow (mirrored, hidden unless hovered/selected)
        GameObject rightPaw = CreateRectObject("RightPaw", contentGO.transform,
            new Vector2(1f, 0.5f), new Vector2(1f, 0.5f), new Vector2(0.5f, 0.5f),
            new Vector2(-26f, 0f), new Vector2(54f, 54f));
        rightPaw.transform.localScale = new Vector3(-1f, 1f, 1f);
        Image rpImg = rightPaw.AddComponent<Image>();
        rpImg.sprite = arrowSprite;
        rpImg.color = Color.white;
        rpImg.preserveAspect = true;
        rpImg.raycastTarget = false;

        UIJuicyButton juicy = btnGO.AddComponent<UIJuicyButton>();
        juicy.leftPaw = leftPaw;
        juicy.rightPaw = rightPaw;
        juicy.alwaysShowIcons = false;
        juicy.hoverScale = 1.06f;
        juicy.pressScale = 0.95f;
        juicy.transitionSpeed = 16f;
        juicy.SetPaws(false);

        return button;
    }

    private static GameObject CreateRectObject(string name, Transform parent, Vector2 anchorMin, Vector2 anchorMax, Vector2 pivot, Vector2 anchoredPos, Vector2 sizeDelta)
    {
        GameObject go = new GameObject(name);
        go.transform.SetParent(parent, false);
        RectTransform rt = go.AddComponent<RectTransform>();
        rt.anchorMin = anchorMin;
        rt.anchorMax = anchorMax;
        rt.pivot = pivot;
        rt.anchoredPosition = anchoredPos;
        rt.sizeDelta = sizeDelta;
        return go;
    }

    private static TextMeshProUGUI CreateTMP(string name, Transform parent, Vector2 anchorMin, Vector2 anchorMax, Vector2 pivot,
        Vector2 anchoredPos, Vector2 sizeDelta, TMP_FontAsset font, string text, float fontSize, FontStyles style, Color color, TextAlignmentOptions alignment)
    {
        GameObject go = CreateRectObject(name, parent, anchorMin, anchorMax, pivot, anchoredPos, sizeDelta);
        TextMeshProUGUI tmp = go.AddComponent<TextMeshProUGUI>();
        tmp.font = font;
        tmp.text = text;
        tmp.fontSize = fontSize;
        tmp.fontStyle = style;
        tmp.color = color;
        tmp.alignment = alignment;
        tmp.raycastTarget = false;
        return tmp;
    }

    private static Sprite LoadRequiredSprite(string path)
    {
        Sprite sp = AssetDatabase.LoadAssetAtPath<Sprite>(path);
        if (sp == null)
        {
            Debug.LogError($"[CreatePauseMenuUI] Required artist sprite missing or not imported as Sprite: {path}");
        }
        return sp;
    }

    private static void SetLayerRecursively(GameObject go, int layer)
    {
        go.layer = layer;
        foreach (Transform child in go.transform)
        {
            SetLayerRecursively(child.gameObject, layer);
        }
    }

    private static TMP_FontAsset LoadFont(string primaryPath, string fallbackPath)
    {
        TMP_FontAsset font = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(primaryPath);
        if (font == null && !string.IsNullOrEmpty(fallbackPath))
        {
            font = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(fallbackPath);
        }
        if (font == null)
        {
            font = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(FONT_THAI);
        }
        return font;
    }
}
#endif
