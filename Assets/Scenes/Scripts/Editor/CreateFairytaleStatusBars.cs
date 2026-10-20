#if UNITY_EDITOR
using System.IO;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

public class CreateFairytaleStatusBars
{
    private const string PREFAB_STATUS_BARS = "Assets/Prefabs/HUD_StatusBars.prefab";
    private const string PREFAB_HP_BAR = "Assets/Prefabs/HP_Bar.prefab";
    private const string PREFAB_OXYGEN_BAR = "Assets/Prefabs/Oxygen_Bar.prefab";
    private const string PREFAB_HUD_CANVAS_OLD = "Assets/Image/UI/HUD Canvas.prefab";

    private const string SPRITE_HP_MEDALLION = "Assets/Image/UI/Fairytale_HP_Medallion.png";
    private const string SPRITE_O2_MEDALLION = "Assets/Image/UI/Fairytale_Oxygen_Medallion.png";
    private const string SPRITE_FRAME = "Assets/Image/UI/Fairytale_Bar_Frame.png";
    private const string SPRITE_TROUGH = "Assets/Image/UI/Fairytale_Bar_Trough.png";
    private const string SPRITE_HP_FILL = "Assets/Image/UI/Fairytale_HP_Fill.png";
    private const string SPRITE_O2_FILL = "Assets/Image/UI/Fairytale_Oxygen_Fill.png";
    private const string SPRITE_GLOSS = "Assets/Image/UI/Fairytale_Bar_GlassGloss.png";
    private const string SPRITE_LEAF = "Assets/Main Screen/UI/LeafIcon.png";

    private const string SCREENSHOT_PATH = "C:/Users/Artemis/Downloads/StatusBars_UI.png";

    [MenuItem("Tools/Rootbound Guardian/Build Fairytale Status Bars")]
    public static void BuildAllStatusBars()
    {
        Sprite hpMedallion = AssetDatabase.LoadAssetAtPath<Sprite>(SPRITE_HP_MEDALLION);
        Sprite o2Medallion = AssetDatabase.LoadAssetAtPath<Sprite>(SPRITE_O2_MEDALLION);
        Sprite frameSprite = AssetDatabase.LoadAssetAtPath<Sprite>(SPRITE_FRAME);
        Sprite troughSprite = AssetDatabase.LoadAssetAtPath<Sprite>(SPRITE_TROUGH);
        Sprite hpFillSprite = AssetDatabase.LoadAssetAtPath<Sprite>(SPRITE_HP_FILL);
        Sprite o2FillSprite = AssetDatabase.LoadAssetAtPath<Sprite>(SPRITE_O2_FILL);
        Sprite glossSprite = AssetDatabase.LoadAssetAtPath<Sprite>(SPRITE_GLOSS);
        Font font = LoadItimFont();

        // 1. Build Combined HUD_StatusBars Prefab
        GameObject rootGO = new GameObject("HUD_StatusBars");
        RectTransform rootRT = rootGO.AddComponent<RectTransform>();
        rootRT.sizeDelta = new Vector2(380f, 96f);
        rootRT.anchorMin = new Vector2(0f, 1f); // Top-Left friendly default for HUD
        rootRT.anchorMax = new Vector2(0f, 1f);
        rootRT.pivot = new Vector2(0f, 1f);
        rootRT.anchoredPosition = new Vector2(35f, -35f);

        BuildSingleBar(rootGO.transform, "HP_Bar", HealthBarUI.BarType.Health,
            hpMedallion, frameSprite, troughSprite, hpFillSprite, glossSprite, font,
            new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(0f, 0.5f),
            new Vector2(28f, -24f), 340f, 34f, "HP: ", new Color(1f, 0.98f, 0.92f), new Color(0.14f, 0.04f, 0.06f));

        BuildSingleBar(rootGO.transform, "Oxygen_Bar", HealthBarUI.BarType.Oxygen,
            o2Medallion, frameSprite, troughSprite, o2FillSprite, glossSprite, font,
            new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(0f, 0.5f),
            new Vector2(28f, -68f), 340f, 34f, "O₂: ", new Color(0.92f, 0.98f, 1.0f), new Color(0.04f, 0.12f, 0.18f));

        SaveAsPrefab(rootGO, PREFAB_STATUS_BARS);
        Object.DestroyImmediate(rootGO);

        // 2. Build Standalone HP_Bar Prefab
        GameObject standaloneHP = BuildSingleBar(null, "HP_Bar", HealthBarUI.BarType.Health,
            hpMedallion, frameSprite, troughSprite, hpFillSprite, glossSprite, font,
            new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
            Vector2.zero, 340f, 34f, "HP: ", new Color(1f, 0.98f, 0.92f), new Color(0.14f, 0.04f, 0.06f));
        SaveAsPrefab(standaloneHP, PREFAB_HP_BAR);
        Object.DestroyImmediate(standaloneHP);

        // 3. Build Standalone Oxygen_Bar Prefab
        GameObject standaloneO2 = BuildSingleBar(null, "Oxygen_Bar", HealthBarUI.BarType.Oxygen,
            o2Medallion, frameSprite, troughSprite, o2FillSprite, glossSprite, font,
            new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
            Vector2.zero, 340f, 34f, "O₂: ", new Color(0.92f, 0.98f, 1.0f), new Color(0.04f, 0.12f, 0.18f));
        SaveAsPrefab(standaloneO2, PREFAB_OXYGEN_BAR);
        Object.DestroyImmediate(standaloneO2);

        // 4. Update HUD Canvas Prefab in Image/UI if it exists
        UpdateOldHUDCanvas(hpMedallion, o2Medallion, frameSprite, troughSprite, hpFillSprite, o2FillSprite, glossSprite, font);

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        Debug.Log("[CreateFairytaleStatusBars] All status bar prefabs built successfully!");
    }

    public static GameObject BuildSingleBar(Transform parent, string barName, HealthBarUI.BarType barType,
        Sprite medallionSprite, Sprite frameSprite, Sprite troughSprite, Sprite fillSprite, Sprite glossSprite, Font font,
        Vector2 anchorMin, Vector2 anchorMax, Vector2 pivot, Vector2 anchoredPos, float width, float height,
        string prefix, Color textColor, Color shadowColor)
    {
        GameObject barGO = new GameObject(barName);
        if (parent != null) barGO.transform.SetParent(parent, false);

        RectTransform barRT = barGO.AddComponent<RectTransform>();
        barRT.anchorMin = anchorMin;
        barRT.anchorMax = anchorMax;
        barRT.pivot = pivot;
        barRT.anchoredPosition = anchoredPos;
        barRT.sizeDelta = new Vector2(width, height);

        // 1. Background Trough
        GameObject bgGO = new GameObject("Background");
        bgGO.transform.SetParent(barGO.transform, false);
        Image bgImg = bgGO.AddComponent<Image>();
        bgImg.sprite = troughSprite;
        bgImg.type = Image.Type.Sliced;
        bgImg.color = Color.white;
        RectTransform bgRT = bgGO.GetComponent<RectTransform>();
        bgRT.anchorMin = Vector2.zero;
        bgRT.anchorMax = Vector2.one;
        bgRT.sizeDelta = Vector2.zero;

        // 2. Ghost Slider (delayed trailing bar)
        GameObject ghostArea = new GameObject("Ghost Fill Area");
        ghostArea.transform.SetParent(barGO.transform, false);
        RectTransform ghostAreaRT = ghostArea.AddComponent<RectTransform>();
        ghostAreaRT.anchorMin = Vector2.zero;
        ghostAreaRT.anchorMax = Vector2.one;
        ghostAreaRT.offsetMin = new Vector2(16f, 4f);
        ghostAreaRT.offsetMax = new Vector2(-16f, -4f);

        GameObject ghostFillGO = new GameObject("Ghost Fill");
        ghostFillGO.transform.SetParent(ghostArea.transform, false);
        Image ghostFillImg = ghostFillGO.AddComponent<Image>();
        ghostFillImg.sprite = fillSprite;
        ghostFillImg.type = Image.Type.Sliced;
        ghostFillImg.color = new Color(1.0f, 0.90f, 0.55f, 0.65f); // Warm amber trailing glow
        RectTransform ghostFillRT = ghostFillGO.GetComponent<RectTransform>();
        ghostFillRT.anchorMin = Vector2.zero;
        ghostFillRT.anchorMax = Vector2.one;
        ghostFillRT.sizeDelta = Vector2.zero;

        Slider ghostSlider = ghostArea.AddComponent<Slider>();
        ghostSlider.transition = Selectable.Transition.None;
        ghostSlider.interactable = false;
        ghostSlider.targetGraphic = null;
        ghostSlider.fillRect = ghostFillRT;
        ghostSlider.minValue = 0f;
        ghostSlider.maxValue = 100f;
        ghostSlider.value = 100f;

        // 3. Main Fill Area & Fill
        GameObject fillArea = new GameObject("Fill Area");
        fillArea.transform.SetParent(barGO.transform, false);
        RectTransform fillAreaRT = fillArea.AddComponent<RectTransform>();
        fillAreaRT.anchorMin = Vector2.zero;
        fillAreaRT.anchorMax = Vector2.one;
        fillAreaRT.offsetMin = new Vector2(16f, 4f);
        fillAreaRT.offsetMax = new Vector2(-16f, -4f);

        GameObject fillGO = new GameObject("Fill");
        fillGO.transform.SetParent(fillArea.transform, false);
        Image fillImg = fillGO.AddComponent<Image>();
        fillImg.sprite = fillSprite;
        fillImg.type = Image.Type.Sliced;
        fillImg.color = Color.white;
        RectTransform fillRT = fillGO.GetComponent<RectTransform>();
        fillRT.anchorMin = Vector2.zero;
        fillRT.anchorMax = Vector2.one;
        fillRT.sizeDelta = Vector2.zero;

        Slider mainSlider = barGO.AddComponent<Slider>();
        mainSlider.transition = Selectable.Transition.None;
        mainSlider.interactable = false;
        mainSlider.targetGraphic = null;
        mainSlider.fillRect = fillRT;
        mainSlider.minValue = 0f;
        mainSlider.maxValue = 100f;
        mainSlider.value = 100f;

        // 4. Glass Gloss Layer (lays over the fill)
        GameObject glossGO = new GameObject("GlassGloss");
        glossGO.transform.SetParent(barGO.transform, false);
        Image glossImg = glossGO.AddComponent<Image>();
        glossImg.sprite = glossSprite;
        glossImg.type = Image.Type.Sliced;
        glossImg.raycastTarget = false;
        RectTransform glossRT = glossGO.GetComponent<RectTransform>();
        glossRT.anchorMin = Vector2.zero;
        glossRT.anchorMax = Vector2.one;
        glossRT.offsetMin = new Vector2(12f, 2f);
        glossRT.offsetMax = new Vector2(-12f, -2f);

        // 5. Ornate Frame Layer (lays over background & fill)
        GameObject frameGO = new GameObject("Frame");
        frameGO.transform.SetParent(barGO.transform, false);
        Image frameImg = frameGO.AddComponent<Image>();
        frameImg.sprite = frameSprite;
        frameImg.type = Image.Type.Sliced;
        frameImg.raycastTarget = false;
        RectTransform frameRT = frameGO.GetComponent<RectTransform>();
        frameRT.anchorMin = Vector2.zero;
        frameRT.anchorMax = Vector2.one;
        frameRT.sizeDelta = Vector2.zero;

        // 6. Left Medallion Icon (protruding proudly on the left edge)
        GameObject iconGO = new GameObject("Icon");
        iconGO.transform.SetParent(barGO.transform, false);
        Image iconImg = iconGO.AddComponent<Image>();
        iconImg.sprite = medallionSprite;
        iconImg.preserveAspect = true;
        iconImg.raycastTarget = false;
        RectTransform iconRT = iconGO.GetComponent<RectTransform>();
        iconRT.anchorMin = new Vector2(0f, 0.5f);
        iconRT.anchorMax = new Vector2(0f, 0.5f);
        iconRT.pivot = new Vector2(0.5f, 0.5f);
        iconRT.anchoredPosition = new Vector2(-6f, 0f);
        iconRT.sizeDelta = new Vector2(54f, 54f);

        // 7. Value Text
        GameObject valGO = new GameObject("ValueText");
        valGO.transform.SetParent(barGO.transform, false);
        Text valText = valGO.AddComponent<Text>();
        valText.font = font;
        valText.fontSize = 15;
        valText.fontStyle = FontStyle.Bold;
        valText.alignment = TextAnchor.MiddleCenter;
        valText.color = textColor;
        valText.text = $"{prefix}100 / 100";

        var outline = valGO.AddComponent<Outline>();
        outline.effectColor = new Color(0.06f, 0.04f, 0.02f, 0.95f);
        outline.effectDistance = new Vector2(1f, -1f);

        var shadow = valGO.AddComponent<Shadow>();
        shadow.effectColor = shadowColor;
        shadow.effectDistance = new Vector2(1.5f, -1.5f);

        RectTransform valRT = valGO.GetComponent<RectTransform>();
        valRT.anchorMin = Vector2.zero;
        valRT.anchorMax = Vector2.one;
        valRT.offsetMin = new Vector2(36f, 0f);
        valRT.offsetMax = new Vector2(-16f, 0f);

        // 8. Attach HealthBarUI
        HealthBarUI healthUI = barGO.AddComponent<HealthBarUI>();
        healthUI.Type = barType;
        healthUI.HealthSlider = mainSlider;
        healthUI.GhostSlider = ghostSlider;
        healthUI.ValueText = valText;
        healthUI.Prefix = prefix;
        healthUI.SmoothTransition = true;

        return barGO;
    }

    private static void UpdateOldHUDCanvas(Sprite hpMedallion, Sprite o2Medallion, Sprite frameSprite, Sprite troughSprite, Sprite hpFillSprite, Sprite o2FillSprite, Sprite glossSprite, Font font)
    {
        if (!File.Exists(PREFAB_HUD_CANVAS_OLD)) return;

        GameObject contents = PrefabUtility.LoadPrefabContents(PREFAB_HUD_CANVAS_OLD);
        Transform oldHP = contents.transform.Find("HP_Bar");
        Transform oldO2 = contents.transform.Find("Oxygen_Bar");

        Vector2 hpPos = oldHP != null ? oldHP.GetComponent<RectTransform>().anchoredPosition : new Vector2(180f, -24f);
        Vector2 o2Pos = oldO2 != null ? oldO2.GetComponent<RectTransform>().anchoredPosition : new Vector2(180f, -72f);

        if (oldHP != null) Object.DestroyImmediate(oldHP.gameObject);
        if (oldO2 != null) Object.DestroyImmediate(oldO2.gameObject);

        BuildSingleBar(contents.transform, "HP_Bar", HealthBarUI.BarType.Health,
            hpMedallion, frameSprite, troughSprite, hpFillSprite, glossSprite, font,
            new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(0f, 0.5f),
            hpPos, 340f, 34f, "HP: ", new Color(1f, 0.98f, 0.92f), new Color(0.14f, 0.04f, 0.06f));

        BuildSingleBar(contents.transform, "Oxygen_Bar", HealthBarUI.BarType.Oxygen,
            o2Medallion, frameSprite, troughSprite, o2FillSprite, glossSprite, font,
            new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(0f, 0.5f),
            o2Pos, 340f, 34f, "O₂: ", new Color(0.92f, 0.98f, 1.0f), new Color(0.04f, 0.12f, 0.18f));

        PrefabUtility.SaveAsPrefabAsset(contents, PREFAB_HUD_CANVAS_OLD);
        PrefabUtility.UnloadPrefabContents(contents);
    }

    private static Font LoadItimFont()
    {
        Font font = AssetDatabase.LoadAssetAtPath<Font>("Assets/Itim-Regular.ttf");
        if (font == null)
        {
            try { font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf"); }
            catch { font = Resources.GetBuiltinResource<Font>("Arial.ttf"); }
        }
        return font;
    }

    private static void SaveAsPrefab(GameObject go, string path)
    {
        string dir = Path.GetDirectoryName(path);
        if (!Directory.Exists(dir)) Directory.CreateDirectory(dir);

        PrefabUtility.SaveAsPrefabAsset(go, path);
        Debug.Log("[CreateFairytaleStatusBars] Saved prefab: " + path);
    }

    [MenuItem("Tools/Rootbound Guardian/Capture Status Bars Screenshot")]
    public static void CaptureStatusBarsScreenshot()
    {
        if (File.Exists(SCREENSHOT_PATH))
        {
            try { File.Delete(SCREENSHOT_PATH); } catch { }
        }

        // Camera setup
        GameObject camGO = new GameObject("TempScreenshotCam");
        Camera cam = camGO.AddComponent<Camera>();
        cam.clearFlags = CameraClearFlags.SolidColor;
        cam.backgroundColor = new Color(0.06f, 0.05f, 0.04f, 1f); // Deep fantasy night
        cam.cullingMask = 1 << 5;

        // Canvas setup
        GameObject canvasGO = new GameObject("TempScreenshotCanvas");
        Canvas canvas = canvasGO.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceCamera;
        canvas.worldCamera = cam;

        CanvasScaler scaler = canvasGO.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);
        scaler.matchWidthOrHeight = 0.5f;

        Font font = LoadItimFont();
        Sprite leafSprite = AssetDatabase.LoadAssetAtPath<Sprite>(SPRITE_LEAF);
        Sprite hpMedallion = AssetDatabase.LoadAssetAtPath<Sprite>(SPRITE_HP_MEDALLION);
        Sprite o2Medallion = AssetDatabase.LoadAssetAtPath<Sprite>(SPRITE_O2_MEDALLION);
        Sprite frameSprite = AssetDatabase.LoadAssetAtPath<Sprite>(SPRITE_FRAME);
        Sprite troughSprite = AssetDatabase.LoadAssetAtPath<Sprite>(SPRITE_TROUGH);
        Sprite hpFillSprite = AssetDatabase.LoadAssetAtPath<Sprite>(SPRITE_HP_FILL);
        Sprite o2FillSprite = AssetDatabase.LoadAssetAtPath<Sprite>(SPRITE_O2_FILL);
        Sprite glossSprite = AssetDatabase.LoadAssetAtPath<Sprite>(SPRITE_GLOSS);

        // 1. Center Presentation Showcase Card (Luxurious Oak & Gold)
        GameObject cardGO = new GameObject("ShowcaseCard");
        cardGO.transform.SetParent(canvasGO.transform, false);
        Image cardImg = cardGO.AddComponent<Image>();
        cardImg.color = new Color(0.14f, 0.10f, 0.07f, 0.98f); // Deep walnut oak

        RectTransform cardRT = cardGO.GetComponent<RectTransform>();
        cardRT.anchorMin = new Vector2(0.5f, 0.5f);
        cardRT.anchorMax = new Vector2(0.5f, 0.5f);
        cardRT.pivot = new Vector2(0.5f, 0.5f);
        cardRT.anchoredPosition = Vector2.zero;
        cardRT.sizeDelta = new Vector2(1040f, 660f);

        // Outer Gold Trim
        GameObject borderGO = new GameObject("Border");
        borderGO.transform.SetParent(cardGO.transform, false);
        Image bImg = borderGO.AddComponent<Image>();
        bImg.color = new Color(0.85f, 0.68f, 0.32f, 0.95f);
        bImg.raycastTarget = false;
        RectTransform bRT = borderGO.GetComponent<RectTransform>();
        bRT.anchorMin = Vector2.zero;
        bRT.anchorMax = Vector2.one;
        bRT.sizeDelta = new Vector2(6f, 6f);
        borderGO.transform.SetAsFirstSibling();

        // Inner Card Panel (Deep contrast mahogany velvet)
        GameObject innerCardGO = new GameObject("InnerPanel");
        innerCardGO.transform.SetParent(cardGO.transform, false);
        Image innerImg = innerCardGO.AddComponent<Image>();
        innerImg.color = new Color(0.09f, 0.07f, 0.05f, 0.98f);
        RectTransform innerRT = innerCardGO.GetComponent<RectTransform>();
        innerRT.anchorMin = Vector2.zero;
        innerRT.anchorMax = Vector2.one;
        innerRT.offsetMin = new Vector2(8f, 8f);
        innerRT.offsetMax = new Vector2(-8f, -8f);
        innerCardGO.transform.SetSiblingIndex(1);

        // 2. Title & Header
        GameObject headerGO = new GameObject("HeaderContainer");
        headerGO.transform.SetParent(innerCardGO.transform, false);
        RectTransform hRT = headerGO.AddComponent<RectTransform>();
        hRT.anchorMin = new Vector2(0.5f, 1f);
        hRT.anchorMax = new Vector2(0.5f, 1f);
        hRT.pivot = new Vector2(0.5f, 1f);
        hRT.anchoredPosition = new Vector2(0f, -20f);
        hRT.sizeDelta = new Vector2(900f, 75f);

        // Left Leaf
        if (leafSprite != null)
        {
            GameObject lLeaf = new GameObject("LeftLeaf");
            lLeaf.transform.SetParent(headerGO.transform, false);
            Image lImg = lLeaf.AddComponent<Image>();
            lImg.sprite = leafSprite;
            lImg.color = new Color(0.35f, 0.90f, 0.45f);
            RectTransform lRT = lLeaf.GetComponent<RectTransform>();
            lRT.anchorMin = new Vector2(0.5f, 0.5f);
            lRT.anchorMax = new Vector2(0.5f, 0.5f);
            lRT.pivot = new Vector2(0.5f, 0.5f);
            lRT.anchoredPosition = new Vector2(-260f, 10f);
            lRT.sizeDelta = new Vector2(32f, 32f);

            GameObject rLeaf = new GameObject("RightLeaf");
            rLeaf.transform.SetParent(headerGO.transform, false);
            Image rImg = rLeaf.AddComponent<Image>();
            rImg.sprite = leafSprite;
            rImg.color = new Color(0.35f, 0.90f, 0.45f);
            RectTransform rRT = rLeaf.GetComponent<RectTransform>();
            rRT.anchorMin = new Vector2(0.5f, 0.5f);
            rRT.anchorMax = new Vector2(0.5f, 0.5f);
            rRT.pivot = new Vector2(0.5f, 0.5f);
            rRT.anchoredPosition = new Vector2(260f, 10f);
            rRT.sizeDelta = new Vector2(32f, 32f);
            rRT.localScale = new Vector3(-1f, 1f, 1f);
        }

        GameObject titleGO = new GameObject("Title");
        titleGO.transform.SetParent(headerGO.transform, false);
        Text titleTxt = titleGO.AddComponent<Text>();
        titleTxt.font = font;
        titleTxt.fontSize = 28;
        titleTxt.fontStyle = FontStyle.Bold;
        titleTxt.alignment = TextAnchor.MiddleCenter;
        titleTxt.color = new Color(1.0f, 0.88f, 0.45f);
        titleTxt.text = "ROOTBOUND GUARDIAN";
        var tOutline = titleGO.AddComponent<Outline>();
        tOutline.effectColor = new Color(0.1f, 0.05f, 0.02f, 0.95f);
        tOutline.effectDistance = new Vector2(1.5f, -1.5f);
        RectTransform titleRT = titleGO.GetComponent<RectTransform>();
        titleRT.anchorMin = Vector2.zero;
        titleRT.anchorMax = Vector2.one;
        titleRT.anchoredPosition = new Vector2(0f, 12f);

        GameObject subtitleGO = new GameObject("Subtitle");
        subtitleGO.transform.SetParent(headerGO.transform, false);
        Text subTxt = subtitleGO.AddComponent<Text>();
        subTxt.font = font;
        subTxt.fontSize = 16;
        subTxt.alignment = TextAnchor.MiddleCenter;
        subTxt.color = new Color(0.85f, 0.80f, 0.72f);
        subTxt.text = "FAIRYTALE COZY FANTASY • STATUS BARS REDESIGN";
        RectTransform subRT = subtitleGO.GetComponent<RectTransform>();
        subRT.anchorMin = Vector2.zero;
        subRT.anchorMax = Vector2.one;
        subRT.anchoredPosition = new Vector2(0f, -18f);

        // 3. Section 1: Full Vitality (100% HP / 100% O2)
        GameObject sec1TitleGO = new GameObject("Sec1_Title");
        sec1TitleGO.transform.SetParent(innerCardGO.transform, false);
        Text s1Txt = sec1TitleGO.AddComponent<Text>();
        s1Txt.font = font;
        s1Txt.fontSize = 16;
        s1Txt.fontStyle = FontStyle.Bold;
        s1Txt.alignment = TextAnchor.MiddleCenter;
        s1Txt.color = new Color(0.42f, 0.92f, 0.55f); // Emerald
        s1Txt.text = "✦ 1. สถานะสมบูรณ์ใน Safe Zone (Full Vitality: 100% HP • 100% O₂) ✦";
        RectTransform s1RT = sec1TitleGO.GetComponent<RectTransform>();
        s1RT.anchorMin = new Vector2(0.5f, 1f);
        s1RT.anchorMax = new Vector2(0.5f, 1f);
        s1RT.pivot = new Vector2(0.5f, 1f);
        s1RT.anchoredPosition = new Vector2(0f, -108f);
        s1RT.sizeDelta = new Vector2(800f, 24f);

        // Group 1: 100% Bars Container
        GameObject grp1GO = new GameObject("BarsGroup_100");
        grp1GO.transform.SetParent(innerCardGO.transform, false);
        RectTransform grp1RT = grp1GO.AddComponent<RectTransform>();
        grp1RT.anchorMin = new Vector2(0.5f, 1f);
        grp1RT.anchorMax = new Vector2(0.5f, 1f);
        grp1RT.pivot = new Vector2(0.5f, 1f);
        grp1RT.anchoredPosition = new Vector2(0f, -136f);
        grp1RT.sizeDelta = new Vector2(800f, 110f);

        GameObject hp100 = BuildSingleBar(grp1GO.transform, "HP_100", HealthBarUI.BarType.Health,
            hpMedallion, frameSprite, troughSprite, hpFillSprite, glossSprite, font,
            new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
            new Vector2(10f, 26f), 760f, 38f, "HP: ", new Color(1f, 0.98f, 0.92f), new Color(0.14f, 0.04f, 0.06f));
        hp100.GetComponent<HealthBarUI>().UpdateHealthBar(100, 100);

        GameObject o2_100 = BuildSingleBar(grp1GO.transform, "O2_100", HealthBarUI.BarType.Oxygen,
            o2Medallion, frameSprite, troughSprite, o2FillSprite, glossSprite, font,
            new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
            new Vector2(10f, -26f), 760f, 38f, "O₂: ", new Color(0.92f, 0.98f, 1.0f), new Color(0.04f, 0.12f, 0.18f));
        o2_100.GetComponent<HealthBarUI>().UpdateHealthBar(100, 100);

        // 4. Section 2: In-Combat & Outside Safe Zone (78% HP with Ghost Bar / 45% O2)
        GameObject sec2TitleGO = new GameObject("Sec2_Title");
        sec2TitleGO.transform.SetParent(innerCardGO.transform, false);
        Text s2Txt = sec2TitleGO.AddComponent<Text>();
        s2Txt.font = font;
        s2Txt.fontSize = 16;
        s2Txt.fontStyle = FontStyle.Bold;
        s2Txt.alignment = TextAnchor.MiddleCenter;
        s2Txt.color = new Color(1.0f, 0.68f, 0.40f); // Amber Coral
        s2Txt.text = "✦ 2. ขณะรับความเสียหาย & สิ้นเปลืองออกซิเจน (In Combat: 78% HP [Ghost Bar] • 45% O₂) ✦";
        RectTransform s2RT = sec2TitleGO.GetComponent<RectTransform>();
        s2RT.anchorMin = new Vector2(0.5f, 1f);
        s2RT.anchorMax = new Vector2(0.5f, 1f);
        s2RT.pivot = new Vector2(0.5f, 1f);
        s2RT.anchoredPosition = new Vector2(0f, -265f);
        s2RT.sizeDelta = new Vector2(800f, 24f);

        // Group 2: Depleted Bars Container
        GameObject grp2GO = new GameObject("BarsGroup_Action");
        grp2GO.transform.SetParent(innerCardGO.transform, false);
        RectTransform grp2RT = grp2GO.AddComponent<RectTransform>();
        grp2RT.anchorMin = new Vector2(0.5f, 1f);
        grp2RT.anchorMax = new Vector2(0.5f, 1f);
        grp2RT.pivot = new Vector2(0.5f, 1f);
        grp2RT.anchoredPosition = new Vector2(0f, -293f);
        grp2RT.sizeDelta = new Vector2(800f, 110f);

        GameObject hpAction = BuildSingleBar(grp2GO.transform, "HP_Action", HealthBarUI.BarType.Health,
            hpMedallion, frameSprite, troughSprite, hpFillSprite, glossSprite, font,
            new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
            new Vector2(10f, 26f), 760f, 38f, "HP: ", new Color(1f, 0.98f, 0.92f), new Color(0.14f, 0.04f, 0.06f));
        var hpUI = hpAction.GetComponent<HealthBarUI>();
        hpUI.UpdateHealthBar(78, 100);
        if (hpUI.GhostSlider != null) hpUI.GhostSlider.value = 92f;

        GameObject o2Action = BuildSingleBar(grp2GO.transform, "O2_Action", HealthBarUI.BarType.Oxygen,
            o2Medallion, frameSprite, troughSprite, o2FillSprite, glossSprite, font,
            new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
            new Vector2(10f, -26f), 760f, 38f, "O₂: ", new Color(0.92f, 0.98f, 1.0f), new Color(0.04f, 0.12f, 0.18f));
        var o2UI = o2Action.GetComponent<HealthBarUI>();
        o2UI.UpdateHealthBar(45, 100);
        if (o2UI.GhostSlider != null) o2UI.GhostSlider.value = 60f;

        // 5. Section 3: Feature Highlights
        GameObject footerCardGO = new GameObject("FooterHighlights");
        footerCardGO.transform.SetParent(innerCardGO.transform, false);
        Image fcImg = footerCardGO.AddComponent<Image>();
        fcImg.color = new Color(0.13f, 0.10f, 0.08f, 0.90f);
        RectTransform fcRT = footerCardGO.GetComponent<RectTransform>();
        fcRT.anchorMin = new Vector2(0.5f, 0f);
        fcRT.anchorMax = new Vector2(0.5f, 0f);
        fcRT.pivot = new Vector2(0.5f, 0f);
        fcRT.anchoredPosition = new Vector2(0f, 22f);
        fcRT.sizeDelta = new Vector2(800f, 75f);

        GameObject fcBorderGO = new GameObject("Border");
        fcBorderGO.transform.SetParent(footerCardGO.transform, false);
        Image fcbImg = fcBorderGO.AddComponent<Image>();
        fcbImg.color = new Color(0.55f, 0.42f, 0.20f, 0.6f);
        RectTransform fcbRT = fcBorderGO.GetComponent<RectTransform>();
        fcbRT.anchorMin = Vector2.zero;
        fcbRT.anchorMax = Vector2.one;
        fcbRT.sizeDelta = Vector2.zero;
        fcBorderGO.transform.SetAsFirstSibling();

        GameObject fTxtGO = new GameObject("Text");
        fTxtGO.transform.SetParent(footerCardGO.transform, false);
        Text fTxt = fTxtGO.AddComponent<Text>();
        fTxt.font = font;
        fTxt.fontSize = 15;
        fTxt.alignment = TextAnchor.MiddleCenter;
        fTxt.color = new Color(0.95f, 0.90f, 0.80f);
        fTxt.text = "✦ กรอบไม้โอ๊คแกะสลักเดินเส้นทองคำ • เหรียญตราทับทิมรูปหัวใจ & หยาดน้ำค้างมหาพฤกษา ✦\n✦ ระบบแถบหน่วงความเสียหาย (Ghost Bar) • หลอดแก้วสะท้อนแสงคริสตัล • รองรับ 9-Slice Sizing ✦";
        RectTransform fRT = fTxtGO.GetComponent<RectTransform>();
        fRT.anchorMin = Vector2.zero;
        fRT.anchorMax = Vector2.one;
        fRT.offsetMin = new Vector2(10f, 5f);
        fRT.offsetMax = new Vector2(-10f, -5f);

        SetLayerRecursively(canvasGO, 5);

        // Render to texture
        int w = 1920;
        int h = 1080;
        RenderTexture rt = new RenderTexture(w, h, 24, RenderTextureFormat.ARGB32);
        rt.Create();
        cam.targetTexture = rt;

        Canvas.ForceUpdateCanvases();
        cam.Render();

        RenderTexture.active = rt;
        Texture2D tex = new Texture2D(w, h, TextureFormat.RGB24, false);
        tex.ReadPixels(new Rect(0, 0, w, h), 0, 0);
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
        Object.DestroyImmediate(canvasGO);

        Debug.Log("[CreateFairytaleStatusBars] Saved refined showcase screenshot to: " + SCREENSHOT_PATH);
    }

    private static void SetLayerRecursively(GameObject go, int layer)
    {
        go.layer = layer;
        foreach (Transform child in go.transform)
        {
            SetLayerRecursively(child.gameObject, layer);
        }
    }
}
#endif
