#if UNITY_EDITOR
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

[InitializeOnLoad]
public class CreateMainMenuUI
{
    private const string UI_FOLDER = "Assets/Main Screen/UI";
    private const string PREFAB_PATH = "Assets/Prefabs/MainMenu_Canvas.prefab";
    private const string FONT_PATH_PRIMARY = "Assets/Main Screen/OneLittleFont-Full SDF.asset";
    private const string FONT_PATH_SECONDARY = "Assets/Image/OneLittleFont-Full SDF.asset";
    private const string FONT_PATH_FALLBACK = "Assets/Itim-Regular SDF.asset";

    static CreateMainMenuUI()
    {
        EditorApplication.delayCall += EnsureMainMenuUIReady;
    }

    private static void EnsureMainMenuUIReady()
    {
        if (EditorApplication.isPlaying || Application.isPlaying) return;

        // Auto-create if sprites or prefab missing
        if (!File.Exists("Assets/Main Screen/UI/PawIcon.png") || !File.Exists(PREFAB_PATH))
        {
            BuildAllMainMenuAssets(false);
        }
    }

    [MenuItem("Tools/SproutScout/Create Main Menu UI (Design Mockup)")]
    public static void ForceBuildMainMenuUI()
    {
        BuildAllMainMenuAssets(true);
    }

    [MenuItem("Tools/SproutScout/Apply Main Menu UI to Active Scene")]
    public static void ApplyToActiveScene()
    {
        BuildAllMainMenuAssets(true);
        ApplyCanvasToScene(EditorSceneManager.GetActiveScene().path);
    }

    [MenuItem("Tools/SproutScout/Unlock UI (Allow Free Dragging in Scene View)")]
    public static void UnlockUIPositions()
    {
        if (File.Exists("Assets/Scenes/MainScreen.unity"))
        {
            var scn = EditorSceneManager.OpenScene("Assets/Scenes/MainScreen.unity", OpenSceneMode.Single);
            Canvas[] scnCanvases = Object.FindObjectsByType<Canvas>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            foreach (var c in scnCanvases)
            {
                if (c.name.Contains("MainMenu_Canvas"))
                {
                    UnlockCanvasHierarchy(c.gameObject);
                    EditorSceneManager.MarkSceneDirty(scn);
                    EditorSceneManager.SaveScene(scn);
                    Debug.Log("[CreateMainMenuUI] Unlocked UI in Assets/Scenes/MainScreen.unity!");
                }
            }
        }

        if (File.Exists(PREFAB_PATH))
        {
            GameObject prefabRoot = PrefabUtility.LoadPrefabContents(PREFAB_PATH);
            UnlockCanvasHierarchy(prefabRoot);
            PrefabUtility.SaveAsPrefabAsset(prefabRoot, PREFAB_PATH);
            PrefabUtility.UnloadPrefabContents(prefabRoot);
            Debug.Log($"[CreateMainMenuUI] Unlocked UI in {PREFAB_PATH}!");
        }

        AssetDatabase.Refresh();
    }

    public static void UnlockCanvasHierarchy(GameObject canvasGO)
    {
        Canvas.ForceUpdateCanvases();
        RectTransform rootRT = canvasGO.GetComponent<RectTransform>();
        if (rootRT != null)
        {
            LayoutRebuilder.ForceRebuildLayoutImmediate(rootRT);
        }

        LayoutGroup[] allGroups = canvasGO.GetComponentsInChildren<LayoutGroup>(true);
        System.Array.Reverse(allGroups);

        foreach (var lg in allGroups)
        {
            Transform parentT = lg.transform;
            int childCount = parentT.childCount;
            Vector2[] pos = new Vector2[childCount];
            Vector2[] size = new Vector2[childCount];
            Vector2[] aMin = new Vector2[childCount];
            Vector2[] aMax = new Vector2[childCount];
            Vector2[] pivot = new Vector2[childCount];

            for (int i = 0; i < childCount; i++)
            {
                RectTransform crt = parentT.GetChild(i) as RectTransform;
                if (crt != null)
                {
                    pos[i] = crt.anchoredPosition;
                    size[i] = crt.sizeDelta;
                    aMin[i] = crt.anchorMin;
                    aMax[i] = crt.anchorMax;
                    pivot[i] = crt.pivot;
                }
            }

            Object.DestroyImmediate(lg);

            for (int i = 0; i < childCount; i++)
            {
                RectTransform crt = parentT.GetChild(i) as RectTransform;
                if (crt != null)
                {
                    crt.anchorMin = aMin[i];
                    crt.anchorMax = aMax[i];
                    crt.pivot = pivot[i];
                    crt.anchoredPosition = pos[i];
                    crt.sizeDelta = size[i];
                }
            }
        }

        ContentSizeFitter[] allFitters = canvasGO.GetComponentsInChildren<ContentSizeFitter>(true);
        foreach (var csf in allFitters)
        {
            Object.DestroyImmediate(csf);
        }
    }

    public static void BuildFromCommandLine()
    {
        Debug.Log("[CreateMainMenuUI] Command line build started...");
        BuildAllMainMenuAssets(true);
        ApplyCanvasToScene("Assets/Scenes/MainScreen.unity");
        UnlockUIPositions();
        Debug.Log("[CreateMainMenuUI] Command line build and unlock completed successfully!");
    }

    public static void BuildAllMainMenuAssets(bool force)
    {
        Debug.Log("[CreateMainMenuUI] Starting Main Menu UI asset generation...");

        if (!Directory.Exists(UI_FOLDER))
        {
            Directory.CreateDirectory(UI_FOLDER);
        }

        // 1. Generate Sprites
        GeneratePawIconSprite();
        GenerateDiamondKnobSprite();
        GenerateSliderTrackSprite();
        GenerateKeyBadgeSprite();
        GenerateModalCardSprite();
        GenerateArrowSprites();
        GenerateReturnArrowSprite();

        AssetDatabase.Refresh();

        // 2. Build Prefab
        BuildMainMenuPrefab();

        Debug.Log("[CreateMainMenuUI] All Main Menu UI assets generated successfully!");
    }

    #region Sprite Generation

    private static void GeneratePawIconSprite()
    {
        string path = $"{UI_FOLDER}/PawIcon.png";

        int size = 128;
        Texture2D tex = new Texture2D(size, size, TextureFormat.RGBA32, false);

        Color clear = new Color(1f, 1f, 1f, 0f);
        Color white = Color.white;

        // Clear
        for (int y = 0; y < size; y++)
            for (int x = 0; x < size; x++)
                tex.SetPixel(x, y, clear);

        // Draw Paw: 1 large plush palm pad + 3 toes (left, center, right)
        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                float alpha = 0f;

                // Main palm pad: smooth union of 3 circles with bottom indent
                float dTop = Vector2.Distance(new Vector2(x, y), new Vector2(64f, 52f)) - 22f;
                float dLeft = Vector2.Distance(new Vector2(x, y), new Vector2(50f, 41f)) - 17.5f;
                float dRight = Vector2.Distance(new Vector2(x, y), new Vector2(78f, 41f)) - 17.5f;
                float distPalm = Mathf.Min(dTop, Mathf.Min(dLeft, dRight));

                float dIndent = Vector2.Distance(new Vector2(x, y), new Vector2(64f, 22f)) - 13.5f;
                float distMain = Mathf.Max(distPalm, -dIndent);

                if (distMain <= 0f)
                {
                    float a = Mathf.Clamp01(-distMain / 1.5f);
                    alpha = Mathf.Max(alpha, a);
                }

                // Center Toe: Center (64, 98), radius 12 x 15
                float dxC = (x - 64f) / 12.5f;
                float dyC = (y - 97f) / 15f;
                float distC = dxC * dxC + dyC * dyC;
                if (distC < 1.15f)
                {
                    float a = Mathf.Clamp01((1.15f - distC) / 0.18f);
                    alpha = Mathf.Max(alpha, a);
                }

                // Left Toe: Center (38, 85), rotated ~ -20 deg, radius 11 x 14
                float lx = x - 38f;
                float ly = y - 85f;
                float rotL = -20f * Mathf.Deg2Rad;
                float rlx = lx * Mathf.Cos(rotL) - ly * Mathf.Sin(rotL);
                float rly = lx * Mathf.Sin(rotL) + ly * Mathf.Cos(rotL);
                float dxL = rlx / 11.5f;
                float dyL = rly / 14f;
                float distL = dxL * dxL + dyL * dyL;
                if (distL < 1.15f)
                {
                    float a = Mathf.Clamp01((1.15f - distL) / 0.18f);
                    alpha = Mathf.Max(alpha, a);
                }

                // Right Toe: Center (90, 85), rotated ~ +20 deg, radius 11 x 14
                float rx = x - 90f;
                float ry = y - 85f;
                float rotR = 20f * Mathf.Deg2Rad;
                float rrx = rx * Mathf.Cos(rotR) - ry * Mathf.Sin(rotR);
                float rry = rx * Mathf.Sin(rotR) + ry * Mathf.Cos(rotR);
                float dxR = rrx / 11.5f;
                float dyR = rry / 14f;
                float distR = dxR * dxR + dyR * dyR;
                if (distR < 1.15f)
                {
                    float a = Mathf.Clamp01((1.15f - distR) / 0.18f);
                    alpha = Mathf.Max(alpha, a);
                }

                if (alpha > 0f)
                {
                    tex.SetPixel(x, y, new Color(white.r, white.g, white.b, alpha));
                }
            }
        }

        tex.Apply();
        SaveTextureAsSprite(tex, path, Vector4.zero);
    }

    private static void GenerateReturnArrowSprite()
    {
        string path = $"{UI_FOLDER}/ReturnArrow.png";
        int w = 48;
        int h = 32;
        Texture2D tex = new Texture2D(w, h, TextureFormat.RGBA32, false);

        Color clear = new Color(0, 0, 0, 0);
        Color iconColor = new Color(0.12f, 0.14f, 0.20f, 1f);

        for (int y = 0; y < h; y++)
            for (int x = 0; x < w; x++)
                tex.SetPixel(x, y, clear);

        // Draw Return Arrow (↵) icon
        for (int y = 0; y < h; y++)
        {
            for (int x = 0; x < w; x++)
            {
                bool filled = false;
                // Horizontal bar
                if (x >= 14 && x <= 36 && y >= 10 && y <= 15) filled = true;
                // Vertical bar on right
                if (x >= 31 && x <= 36 && y >= 10 && y <= 24) filled = true;
                // Arrow head on left pointing left
                if (x >= 8 && x <= 18)
                {
                    float distY = Mathf.Abs(y - 12.5f);
                    float maxDY = (x - 8) * 1.1f;
                    if (distY <= maxDY && distY >= maxDY - 4.5f) filled = true;
                }

                if (filled)
                {
                    tex.SetPixel(x, y, iconColor);
                }
            }
        }

        tex.Apply();
        SaveTextureAsSprite(tex, path, Vector4.zero);
    }

    private static void GenerateDiamondKnobSprite()
    {
        string path = $"{UI_FOLDER}/DiamondKnob.png";
        if (File.Exists(path)) return;

        int size = 64;
        Texture2D tex = new Texture2D(size, size, TextureFormat.RGBA32, false);

        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                float dx = Mathf.Abs(x - 31.5f);
                float dy = Mathf.Abs(y - 31.5f);
                float manhattan = dx + dy; // 45 degree diamond shape
                float alpha = Mathf.Clamp01((22.5f - manhattan) / 1.5f);
                tex.SetPixel(x, y, new Color(1f, 1f, 1f, alpha));
            }
        }

        tex.Apply();
        SaveTextureAsSprite(tex, path, Vector4.zero);
    }

    private static void GenerateSliderTrackSprite()
    {
        string path = $"{UI_FOLDER}/SliderTrack.png";
        if (File.Exists(path)) return;

        int w = 64;
        int h = 16;
        Texture2D tex = new Texture2D(w, h, TextureFormat.RGBA32, false);

        for (int y = 0; y < h; y++)
        {
            for (int x = 0; x < w; x++)
            {
                // Rounded capsule with radius 6
                float r = 6f;
                float dist = 0f;
                if (x < r)
                {
                    float dx = x - r;
                    float dy = y - (h * 0.5f);
                    dist = Mathf.Sqrt(dx * dx + dy * dy);
                }
                else if (x > w - r)
                {
                    float dx = x - (w - r);
                    float dy = y - (h * 0.5f);
                    dist = Mathf.Sqrt(dx * dx + dy * dy);
                }
                else
                {
                    dist = Mathf.Abs(y - (h * 0.5f));
                }

                float alpha = Mathf.Clamp01((r - dist) / 1.2f);
                tex.SetPixel(x, y, new Color(1f, 1f, 1f, alpha));
            }
        }

        tex.Apply();
        SaveTextureAsSprite(tex, path, new Vector4(8, 6, 8, 6));
    }

    private static void GenerateKeyBadgeSprite()
    {
        string path = $"{UI_FOLDER}/KeyBadgeBg.png";
        if (File.Exists(path)) return;

        int w = 128;
        int h = 64;
        int radius = 18;
        Texture2D tex = new Texture2D(w, h, TextureFormat.RGBA32, false);

        for (int y = 0; y < h; y++)
        {
            for (int x = 0; x < w; x++)
            {
                float dist = GetRoundedRectDist(x, y, w, h, radius);
                float alpha = Mathf.Clamp01((0f - dist) / 1.5f);
                tex.SetPixel(x, y, new Color(1f, 1f, 1f, alpha));
            }
        }

        tex.Apply();
        SaveTextureAsSprite(tex, path, new Vector4(radius + 2, radius + 2, radius + 2, radius + 2));
    }

    private static void GenerateModalCardSprite()
    {
        string path = $"{UI_FOLDER}/ModalCardBg.png";
        if (File.Exists(path)) return;

        int w = 256;
        int h = 256;
        int radius = 26;
        Texture2D tex = new Texture2D(w, h, TextureFormat.RGBA32, false);

        Color cardFill = new Color(0.05f, 0.07f, 0.11f, 0.96f); // Deep dark slate
        Color cardBorder = new Color(0.18f, 0.22f, 0.32f, 0.85f); // Subtle border

        for (int y = 0; y < h; y++)
        {
            for (int x = 0; x < w; x++)
            {
                float dist = GetRoundedRectDist(x, y, w, h, radius);
                if (dist < 0f)
                {
                    // Inside card
                    float borderT = Mathf.Clamp01((-dist) / 3f);
                    Color c = Color.Lerp(cardBorder, cardFill, borderT);
                    float edgeAlpha = Mathf.Clamp01((0f - dist) / 1.5f);
                    c.a *= edgeAlpha;
                    tex.SetPixel(x, y, c);
                }
                else
                {
                    tex.SetPixel(x, y, new Color(0f, 0f, 0f, 0f));
                }
            }
        }

        tex.Apply();
        SaveTextureAsSprite(tex, path, new Vector4(radius + 4, radius + 4, radius + 4, radius + 4));
    }

    private static void GenerateArrowSprites()
    {
        string leftPath = $"{UI_FOLDER}/ArrowLeft.png";
        string rightPath = $"{UI_FOLDER}/ArrowRight.png";

        if (!File.Exists(leftPath))
        {
            int s = 48;
            Texture2D tex = new Texture2D(s, s, TextureFormat.RGBA32, false);
            for (int y = 0; y < s; y++)
            {
                for (int x = 0; x < s; x++)
                {
                    // Triangle pointing left
                    // x from 38 down to 10
                    float progress = Mathf.Clamp01((38f - x) / 28f);
                    float halfH = progress * 18f;
                    float dy = Mathf.Abs(y - 23.5f);
                    float alpha = (x >= 10 && x <= 38 && dy <= halfH) ? Mathf.Clamp01((halfH - dy) / 1.2f) : 0f;
                    tex.SetPixel(x, y, new Color(1f, 1f, 1f, alpha));
                }
            }
            tex.Apply();
            SaveTextureAsSprite(tex, leftPath, Vector4.zero);
        }

        if (!File.Exists(rightPath))
        {
            int s = 48;
            Texture2D tex = new Texture2D(s, s, TextureFormat.RGBA32, false);
            for (int y = 0; y < s; y++)
            {
                for (int x = 0; x < s; x++)
                {
                    // Triangle pointing right
                    float progress = Mathf.Clamp01((x - 10f) / 28f);
                    float halfH = progress * 18f;
                    float dy = Mathf.Abs(y - 23.5f);
                    float alpha = (x >= 10 && x <= 38 && dy <= halfH) ? Mathf.Clamp01((halfH - dy) / 1.2f) : 0f;
                    tex.SetPixel(x, y, new Color(1f, 1f, 1f, alpha));
                }
            }
            tex.Apply();
            SaveTextureAsSprite(tex, rightPath, Vector4.zero);
        }
    }

    private static float GetRoundedRectDist(float x, float y, float w, float h, float r)
    {
        float cx = Mathf.Clamp(x, r, w - r);
        float cy = Mathf.Clamp(y, r, h - r);
        float dx = x - cx;
        float dy = y - cy;
        return Mathf.Sqrt(dx * dx + dy * dy) - r;
    }

    private static void SaveTextureAsSprite(Texture2D tex, string assetPath, Vector4 border)
    {
        byte[] bytes = tex.EncodeToPNG();
        File.WriteAllBytes(assetPath, bytes);
        AssetDatabase.ImportAsset(assetPath, ImportAssetOptions.ForceUpdate);

        TextureImporter importer = AssetImporter.GetAtPath(assetPath) as TextureImporter;
        if (importer != null)
        {
            importer.textureType = TextureImporterType.Sprite;
            importer.spriteImportMode = SpriteImportMode.Single;
            importer.spritePixelsPerUnit = 100;
            importer.alphaIsTransparency = true;
            importer.mipmapEnabled = false;
            importer.filterMode = FilterMode.Bilinear;
            if (border != Vector4.zero)
            {
                importer.spriteBorder = border;
            }
            importer.SaveAndReimport();
        }
    }

    #endregion

    #region Main Menu Prefab Construction

    private static void BuildMainMenuPrefab()
    {
        TMP_FontAsset font = LoadMenuFont();

        Sprite pawSprite = AssetDatabase.LoadAssetAtPath<Sprite>($"{UI_FOLDER}/PawIcon.png");
        Sprite diamondSprite = AssetDatabase.LoadAssetAtPath<Sprite>($"{UI_FOLDER}/DiamondKnob.png");
        Sprite trackSprite = AssetDatabase.LoadAssetAtPath<Sprite>($"{UI_FOLDER}/SliderTrack.png");
        Sprite keyBadgeSprite = AssetDatabase.LoadAssetAtPath<Sprite>($"{UI_FOLDER}/KeyBadgeBg.png");
        Sprite cardSprite = AssetDatabase.LoadAssetAtPath<Sprite>($"{UI_FOLDER}/ModalCardBg.png");
        Sprite arrowL = AssetDatabase.LoadAssetAtPath<Sprite>($"{UI_FOLDER}/ArrowLeft.png");
        Sprite arrowR = AssetDatabase.LoadAssetAtPath<Sprite>($"{UI_FOLDER}/ArrowRight.png");
        Sprite returnArrowSprite = AssetDatabase.LoadAssetAtPath<Sprite>($"{UI_FOLDER}/ReturnArrow.png");

        GameObject canvasGO = new GameObject("MainMenu_Canvas");
        Canvas canvas = canvasGO.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 50;

        CanvasScaler scaler = canvasGO.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);
        scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
        scaler.matchWidthOrHeight = 0.5f;

        canvasGO.AddComponent<GraphicRaycaster>();

        MainMenuUIController controller = canvasGO.AddComponent<MainMenuUIController>();

        // 1. Press Any Key Panel
        GameObject pressPanel = CreateFullScreenPanel(canvasGO.transform, "PressAnyKeyPanel");
        controller.pressAnyKeyPanel = pressPanel;

        TextMeshProUGUI pressText = CreateTMPText(pressPanel.transform, "PressAnyKeyText", "PRESS ANY KEY TO START", font, 54, FontStyles.Bold, TextAlignmentOptions.Center);
        RectTransform ptRect = pressText.GetComponent<RectTransform>();
        ptRect.anchorMin = new Vector2(0.5f, 0f);
        ptRect.anchorMax = new Vector2(0.5f, 0f);
        ptRect.pivot = new Vector2(0.5f, 0f);
        ptRect.anchoredPosition = new Vector2(0f, 95f);
        ptRect.sizeDelta = new Vector2(1200f, 90f);
        controller.pressAnyKeyText = pressText;

        // 2. Main Menu Panel (Start / Settings / Exit)
        GameObject mainPanel = CreateFullScreenPanel(canvasGO.transform, "MainMenuPanel");
        controller.mainMenuPanel = mainPanel;

        GameObject bottomRow = new GameObject("BottomButtonsRow");
        bottomRow.transform.SetParent(mainPanel.transform, false);
        RectTransform brRect = bottomRow.AddComponent<RectTransform>();
        brRect.anchorMin = new Vector2(0.5f, 0f);
        brRect.anchorMax = new Vector2(0.5f, 0f);
        brRect.pivot = new Vector2(0.5f, 0f);
        brRect.anchoredPosition = new Vector2(0f, 85f);
        brRect.sizeDelta = new Vector2(1400f, 100f);

        HorizontalLayoutGroup brLayout = bottomRow.AddComponent<HorizontalLayoutGroup>();
        brLayout.childAlignment = TextAnchor.MiddleCenter;
        brLayout.spacing = 110f;
        brLayout.childForceExpandWidth = false;
        brLayout.childForceExpandHeight = false;

        // START Button
        CreatePawButton(bottomRow.transform, "StartButton", "START", font, 54, pawSprite,
            out Button startBtn, out TextMeshProUGUI startTxt, out GameObject startPawsGO);
        controller.startButton = startBtn;
        controller.startText = startTxt;
        controller.startPaws = startPawsGO;

        // SETTINGS Button
        CreatePawButton(bottomRow.transform, "SettingsButton", "SETTINGS", font, 54, pawSprite,
            out Button settingsBtn, out TextMeshProUGUI settingsTxt, out GameObject settingsPawsGO);
        controller.settingsButton = settingsBtn;
        controller.settingsText = settingsTxt;
        controller.settingsPaws = settingsPawsGO;

        // EXIT Button
        CreatePawButton(bottomRow.transform, "ExitButton", "EXIT", font, 54, pawSprite,
            out Button exitBtn, out TextMeshProUGUI exitTxt, out GameObject exitPawsGO);
        controller.exitButton = exitBtn;
        controller.exitText = exitTxt;
        controller.exitPaws = exitPawsGO;

        // 3. Settings Panel
        GameObject settingsPanel = CreateFullScreenPanel(canvasGO.transform, "SettingsPanel");
        controller.settingsPanel = settingsPanel;

        GameObject settingsContainer = new GameObject("SettingsContent");
        settingsContainer.transform.SetParent(settingsPanel.transform, false);
        RectTransform scRect = settingsContainer.AddComponent<RectTransform>();
        scRect.anchorMin = new Vector2(0.5f, 0.5f);
        scRect.anchorMax = new Vector2(0.5f, 0.5f);
        scRect.pivot = new Vector2(0.5f, 0.5f);
        scRect.anchoredPosition = new Vector2(0f, 20f);
        scRect.sizeDelta = new Vector2(880f, 740f);

        VerticalLayoutGroup scLayout = settingsContainer.AddComponent<VerticalLayoutGroup>();
        scLayout.childAlignment = TextAnchor.MiddleCenter;
        scLayout.spacing = 16f;
        scLayout.childForceExpandWidth = false;
        scLayout.childForceExpandHeight = false;

        // Category: AUDIO
        CreateTMPText(settingsContainer.transform, "AudioHeader", "AUDIO", font, 44, FontStyles.Bold, TextAlignmentOptions.Center, 800, 55);

        // Row: MASTER
        CreateSliderRow(settingsContainer.transform, "MasterRow", "MASTER", font, trackSprite, diamondSprite, arrowL, arrowR, pawSprite,
            out Slider masterSlider, out TextMeshProUGUI masterLabel, out GameObject masterPawsGO, out GameObject masterAL, out GameObject masterAR);
        controller.masterVolumeSlider = masterSlider;
        controller.masterLabel = masterLabel;
        controller.masterArrowLeft = masterAL;
        controller.masterArrowRight = masterAR;

        // Row: BGM
        CreateSliderRow(settingsContainer.transform, "BgmRow", "BGM", font, trackSprite, diamondSprite, arrowL, arrowR, pawSprite,
            out Slider bgmSlider, out TextMeshProUGUI bgmLabel, out GameObject bgmPawsGO, out GameObject bgmAL, out GameObject bgmAR);
        controller.bgmVolumeSlider = bgmSlider;
        controller.bgmLabel = bgmLabel;
        controller.bgmArrowLeft = bgmAL;
        controller.bgmArrowRight = bgmAR;

        // Row: SFX
        CreateSliderRow(settingsContainer.transform, "SfxRow", "SFX", font, trackSprite, diamondSprite, arrowL, arrowR, pawSprite,
            out Slider sfxSlider, out TextMeshProUGUI sfxLabel, out GameObject sfxPawsGO, out GameObject sfxAL, out GameObject sfxAR);
        controller.sfxVolumeSlider = sfxSlider;
        controller.sfxLabel = sfxLabel;
        controller.sfxArrowLeft = sfxAL;
        controller.sfxArrowRight = sfxAR;

        // Spacer
        CreateSpacer(settingsContainer.transform, 14f);

        // Category: RESOLUTION
        CreateTMPText(settingsContainer.transform, "ResolutionHeader", "RESOLUTION", font, 44, FontStyles.Bold, TextAlignmentOptions.Center, 800, 55);

        // Row: Resolution Value
        CreateSelectorRow(settingsContainer.transform, "ResolutionRow", "1920 X 1080 (144HZ)", font, arrowL, arrowR, pawSprite,
            out TextMeshProUGUI resValueText, out GameObject resPawsGO, out GameObject resAL, out GameObject resAR);
        controller.resolutionValueText = resValueText;
        controller.resolutionArrowLeft = resAL;
        controller.resolutionArrowRight = resAR;

        // Spacer
        CreateSpacer(settingsContainer.transform, 14f);

        // Category: DISPLAY MODE
        CreateTMPText(settingsContainer.transform, "DisplayModeHeader", "DISPLAY MODE", font, 44, FontStyles.Bold, TextAlignmentOptions.Center, 800, 55);

        // Row: Display Mode Value
        CreateSelectorRow(settingsContainer.transform, "DisplayModeRow", "BORDERLESS", font, arrowL, arrowR, pawSprite,
            out TextMeshProUGUI dispValueText, out GameObject dispPawsGO, out GameObject dispAL, out GameObject dispAR);
        controller.displayModeValueText = dispValueText;
        controller.displayModeArrowLeft = dispAL;
        controller.displayModeArrowRight = dispAR;

        // Spacer
        CreateSpacer(settingsContainer.transform, 24f);

        // Back Button
        CreatePawButton(settingsContainer.transform, "BackButton", "BACK", font, 46, pawSprite,
            out Button backBtn, out TextMeshProUGUI backTxt, out GameObject backPawsGO);
        controller.settingsBackButton = backBtn;
        controller.settingsBackText = backTxt;
        controller.settingsBackPaws = backPawsGO;

        // 4. Exit Confirmation Modal
        GameObject exitModal = CreateFullScreenPanel(canvasGO.transform, "ExitConfirmModal");
        controller.exitConfirmModal = exitModal;

        // Dark dim backdrop
        GameObject dimGO = CreateFullScreenPanel(exitModal.transform, "DimBackdrop");
        Image dimImg = dimGO.AddComponent<Image>();
        dimImg.color = new Color(0f, 0f, 0f, 0.65f);
        controller.exitModalDimImage = dimImg;

        // Modal Card
        GameObject cardGO = new GameObject("ModalCard");
        cardGO.transform.SetParent(exitModal.transform, false);
        Image cardImg = cardGO.AddComponent<Image>();
        cardImg.sprite = cardSprite;
        cardImg.type = Image.Type.Sliced;
        RectTransform cardRect = cardGO.GetComponent<RectTransform>();
        cardRect.anchorMin = new Vector2(0.5f, 0.5f);
        cardRect.anchorMax = new Vector2(0.5f, 0.5f);
        cardRect.pivot = new Vector2(0.5f, 0.5f);
        cardRect.sizeDelta = new Vector2(640f, 350f);
        controller.exitModalCardRect = cardRect;


        // Modal Title: "EXIT THE\nGAME?"
        TextMeshProUGUI modalTitle = CreateTMPText(cardGO.transform, "TitleText", "EXIT THE\nGAME?", font, 54, FontStyles.Bold, TextAlignmentOptions.Center);
        RectTransform mtRect = modalTitle.GetComponent<RectTransform>();
        mtRect.anchorMin = new Vector2(0.5f, 1f);
        mtRect.anchorMax = new Vector2(0.5f, 1f);
        mtRect.pivot = new Vector2(0.5f, 1f);
        mtRect.anchoredPosition = new Vector2(0f, -40f);
        mtRect.sizeDelta = new Vector2(580f, 140f);

        // Modal Buttons Row
        GameObject modalBtnRow = new GameObject("ButtonsRow");
        modalBtnRow.transform.SetParent(cardGO.transform, false);
        RectTransform mbrRect = modalBtnRow.AddComponent<RectTransform>();
        mbrRect.anchorMin = new Vector2(0.5f, 0f);
        mbrRect.anchorMax = new Vector2(0.5f, 0f);
        mbrRect.pivot = new Vector2(0.5f, 0f);
        mbrRect.anchoredPosition = new Vector2(0f, 45f);
        mbrRect.sizeDelta = new Vector2(580f, 75f);

        HorizontalLayoutGroup mbrLayout = modalBtnRow.AddComponent<HorizontalLayoutGroup>();
        mbrLayout.childAlignment = TextAnchor.MiddleCenter;
        mbrLayout.spacing = 60f;
        mbrLayout.childForceExpandWidth = false;
        mbrLayout.childForceExpandHeight = false;

        // CONFIRM Button
        CreatePawButton(modalBtnRow.transform, "ConfirmButton", "CONFIRM", font, 42, pawSprite,
            out Button confirmBtn, out TextMeshProUGUI confirmTxt, out GameObject confirmPawsGO);
        controller.confirmExitButton = confirmBtn;
        controller.confirmExitText = confirmTxt;
        controller.confirmExitPaws = confirmPawsGO;

        // CANCEL Button
        CreatePawButton(modalBtnRow.transform, "CancelButton", "CANCEL", font, 42, pawSprite,
            out Button cancelBtn, out TextMeshProUGUI cancelTxt, out GameObject cancelPawsGO);
        controller.cancelExitButton = cancelBtn;
        controller.cancelExitText = cancelTxt;
        controller.cancelExitPaws = cancelPawsGO;

        // 5. Key Hints Panel (Top Right: [ENTER ↵] CONFIRM   [ESC] BACK)
        GameObject keyHints = new GameObject("KeyHintsPanel");
        keyHints.transform.SetParent(canvasGO.transform, false);
        RectTransform khRect = keyHints.AddComponent<RectTransform>();
        khRect.anchorMin = new Vector2(1f, 1f);
        khRect.anchorMax = new Vector2(1f, 1f);
        khRect.pivot = new Vector2(1f, 1f);
        khRect.anchoredPosition = new Vector2(-45f, -40f);
        khRect.sizeDelta = new Vector2(460f, 60f);

        HorizontalLayoutGroup khLayout = keyHints.AddComponent<HorizontalLayoutGroup>();
        khLayout.childAlignment = TextAnchor.MiddleRight;
        khLayout.spacing = 28f;
        khLayout.childForceExpandWidth = false;
        khLayout.childForceExpandHeight = false;

        // CONFIRM Hint: [ENTER ↵] CONFIRM
        CreateKeyHintItem(keyHints.transform, "ConfirmHint", "ENTER", "CONFIRM", font, keyBadgeSprite, 72f, 44f, returnArrowSprite, out RectTransform confBadge);
        controller.confirmBadgeRect = confBadge;

        // BACK Hint: [ESC] BACK
        CreateKeyHintItem(keyHints.transform, "BackHint", "ESC", "BACK", font, keyBadgeSprite, 62f, 44f, null, out RectTransform bkBadge);
        controller.backBadgeRect = bkBadge;

        controller.keyHintsPanel = keyHints;

        // Save as Prefab
        string dir = Path.GetDirectoryName(PREFAB_PATH);
        if (!Directory.Exists(dir)) Directory.CreateDirectory(dir);

        PrefabUtility.SaveAsPrefabAsset(canvasGO, PREFAB_PATH);
        Object.DestroyImmediate(canvasGO);

        AssetDatabase.Refresh();
        Debug.Log($"[CreateMainMenuUI] Saved Main Menu Prefab to: {PREFAB_PATH}");
    }

    private static void ApplyCanvasToScene(string scenePath)
    {
        if (string.IsNullOrEmpty(scenePath) || !File.Exists(scenePath))
        {
            Debug.LogWarning($"[CreateMainMenuUI] Scene not found: {scenePath}");
            return;
        }

        var scene = EditorSceneManager.OpenScene(scenePath, OpenSceneMode.Single);

        // Find old Canvas in scene
        Canvas[] existingCanvases = Object.FindObjectsOfType<Canvas>(true);
        foreach (Canvas c in existingCanvases)
        {
            if (c.gameObject.name.Contains("Canvas") && !c.gameObject.name.Contains("HUD"))
            {
                Debug.Log($"[CreateMainMenuUI] Replacing existing canvas in {scenePath}: {c.gameObject.name}");
                Object.DestroyImmediate(c.gameObject);
            }
        }

        // Instantiate new MainMenu_Canvas prefab
        GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(PREFAB_PATH);
        if (prefab != null)
        {
            GameObject instance = PrefabUtility.InstantiatePrefab(prefab) as GameObject;
            instance.name = "MainMenu_Canvas";
            Debug.Log($"[CreateMainMenuUI] Successfully applied MainMenu_Canvas into: {scenePath}");
        }

        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
    }

    #endregion

    #region UI Hierarchy Helpers

    private static GameObject CreateFullScreenPanel(Transform parent, string name)
    {
        GameObject go = new GameObject(name);
        go.transform.SetParent(parent, false);
        RectTransform rt = go.AddComponent<RectTransform>();
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.sizeDelta = Vector2.zero;
        return go;
    }

    private static TextMeshProUGUI CreateTMPText(Transform parent, string name, string text, TMP_FontAsset font, float size, FontStyles style, TextAlignmentOptions align, float width = 0, float height = 0)
    {
        GameObject go = new GameObject(name);
        go.transform.SetParent(parent, false);
        TextMeshProUGUI tmp = go.AddComponent<TextMeshProUGUI>();
        tmp.text = text;
        tmp.fontSize = size;
        tmp.fontStyle = style;
        tmp.alignment = align;
        tmp.color = Color.white;

        if (font != null)
        {
            tmp.font = font;
        }

        RectTransform rt = go.GetComponent<RectTransform>();
        if (width > 0 && height > 0)
        {
            rt.sizeDelta = new Vector2(width, height);
        }

        return tmp;
    }

    private static void CreatePawButton(Transform parent, string name, string text, TMP_FontAsset font, float fontSize, Sprite pawSprite,
        out Button btn, out TextMeshProUGUI label, out GameObject pawsGroup)
    {
        GameObject btnGO = new GameObject(name);
        btnGO.transform.SetParent(parent, false);
        btn = btnGO.AddComponent<Button>();

        HorizontalLayoutGroup hl = btnGO.AddComponent<HorizontalLayoutGroup>();
        hl.childAlignment = TextAnchor.MiddleCenter;
        hl.spacing = 14f;
        hl.childForceExpandWidth = false;
        hl.childForceExpandHeight = false;

        ContentSizeFitter csf = btnGO.AddComponent<ContentSizeFitter>();
        csf.horizontalFit = ContentSizeFitter.FitMode.PreferredSize;
        csf.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

        // Container for Left & Right Paws
        pawsGroup = new GameObject("PawsContainer");
        pawsGroup.transform.SetParent(btnGO.transform, false);
        // Note: we place paw elements directly
        Object.DestroyImmediate(pawsGroup);

        // Left Paw
        GameObject leftPaw = new GameObject("LeftPaw");
        leftPaw.transform.SetParent(btnGO.transform, false);
        Image lpImg = leftPaw.AddComponent<Image>();
        lpImg.sprite = pawSprite;
        lpImg.raycastTarget = false;
        RectTransform lpRect = leftPaw.GetComponent<RectTransform>();
        lpRect.sizeDelta = new Vector2(fontSize * 0.82f, fontSize * 0.82f);

        // Center Text
        label = CreateTMPText(btnGO.transform, "Label", text, font, fontSize, FontStyles.Bold, TextAlignmentOptions.Center);
        label.raycastTarget = false;

        // Right Paw
        GameObject rightPaw = new GameObject("RightPaw");
        rightPaw.transform.SetParent(btnGO.transform, false);
        Image rpImg = rightPaw.AddComponent<Image>();
        rpImg.sprite = pawSprite;
        rpImg.raycastTarget = false;
        RectTransform rpRect = rightPaw.GetComponent<RectTransform>();
        rpRect.sizeDelta = new Vector2(fontSize * 0.82f, fontSize * 0.82f);

        // Group together left and right paws under a single control GameObject
        // To keep layout order correct: [LeftPaw, Label, RightPaw]
        // We link a tiny helper component or toggle both paws via an empty container
        GameObject pawsTracker = new GameObject("PawsTracker");
        pawsTracker.transform.SetParent(btnGO.transform, false);
        pawsTracker.SetActive(false); // starts hidden
        pawsGroup = pawsTracker;

        // Set paws tracker to control left & right paw active state
        PawPairSync sync = pawsTracker.AddComponent<PawPairSync>();
        sync.leftPaw = leftPaw;
        sync.rightPaw = rightPaw;
    }

    private static void CreateSliderRow(Transform parent, string name, string labelText, TMP_FontAsset font, Sprite trackSprite, Sprite diamondSprite, Sprite arrowL, Sprite arrowR, Sprite pawSprite,
        out Slider slider, out TextMeshProUGUI label, out GameObject pawsGroup, out GameObject arrowLeftGO, out GameObject arrowRightGO)
    {
        GameObject rowGO = new GameObject(name);
        rowGO.transform.SetParent(parent, false);
        RectTransform rowRect = rowGO.AddComponent<RectTransform>();
        rowRect.sizeDelta = new Vector2(800f, 50f);

        HorizontalLayoutGroup hl = rowGO.AddComponent<HorizontalLayoutGroup>();
        hl.childAlignment = TextAnchor.MiddleCenter;
        hl.spacing = 20f;
        hl.childForceExpandWidth = false;
        hl.childForceExpandHeight = false;

        // Label on left
        label = CreateTMPText(rowGO.transform, "Label", labelText, font, 34, FontStyles.Bold, TextAlignmentOptions.Left, 220, 45);

        // Arrow Left (if present)
        arrowLeftGO = null;
        if (arrowL != null)
        {
            arrowLeftGO = new GameObject("ArrowLeft");
            arrowLeftGO.transform.SetParent(rowGO.transform, false);
            Image alImg = arrowLeftGO.AddComponent<Image>();
            alImg.sprite = arrowL;
            RectTransform alRect = arrowLeftGO.GetComponent<RectTransform>();
            alRect.sizeDelta = new Vector2(24, 24);
        }

        // Slider Container
        GameObject sliderGO = new GameObject("Slider");
        sliderGO.transform.SetParent(rowGO.transform, false);
        RectTransform sRect = sliderGO.AddComponent<RectTransform>();
        sRect.sizeDelta = new Vector2(340f, 30f);

        slider = sliderGO.AddComponent<Slider>();
        slider.minValue = 0f;
        slider.maxValue = 1f;
        slider.value = 1f;

        // Background Line
        GameObject bgLine = new GameObject("Background");
        bgLine.transform.SetParent(sliderGO.transform, false);
        Image bgImg = bgLine.AddComponent<Image>();
        bgImg.sprite = trackSprite;
        bgImg.type = Image.Type.Sliced;
        bgImg.color = new Color(1f, 1f, 1f, 0.45f);
        RectTransform bgRect = bgLine.GetComponent<RectTransform>();
        bgRect.anchorMin = new Vector2(0f, 0.5f);
        bgRect.anchorMax = new Vector2(1f, 0.5f);
        bgRect.sizeDelta = new Vector2(0f, 6f);

        // Fill Area
        GameObject fillArea = new GameObject("Fill Area");
        fillArea.transform.SetParent(sliderGO.transform, false);
        RectTransform faRect = fillArea.AddComponent<RectTransform>();
        faRect.anchorMin = new Vector2(0f, 0.5f);
        faRect.anchorMax = new Vector2(1f, 0.5f);
        faRect.sizeDelta = new Vector2(0f, 6f);

        GameObject fill = new GameObject("Fill");
        fill.transform.SetParent(fillArea.transform, false);
        Image fillImg = fill.AddComponent<Image>();
        fillImg.sprite = trackSprite;
        fillImg.type = Image.Type.Sliced;
        fillImg.color = Color.white;
        RectTransform fRect = fill.GetComponent<RectTransform>();
        fRect.sizeDelta = Vector2.zero;
        slider.fillRect = fRect;

        // Handle Area & Knob
        GameObject handleArea = new GameObject("Handle Slide Area");
        handleArea.transform.SetParent(sliderGO.transform, false);
        RectTransform haRect = handleArea.AddComponent<RectTransform>();
        haRect.anchorMin = Vector2.zero;
        haRect.anchorMax = Vector2.one;
        haRect.sizeDelta = Vector2.zero;

        GameObject handle = new GameObject("Handle");
        handle.transform.SetParent(handleArea.transform, false);
        Image handleImg = handle.AddComponent<Image>();
        handleImg.sprite = diamondSprite;
        handleImg.color = Color.white;
        RectTransform hRect = handle.GetComponent<RectTransform>();
        hRect.sizeDelta = new Vector2(26f, 26f);
        slider.handleRect = hRect;
        slider.targetGraphic = handleImg;

        // Arrow Right (if present)
        arrowRightGO = null;
        if (arrowR != null)
        {
            arrowRightGO = new GameObject("ArrowRight");
            arrowRightGO.transform.SetParent(rowGO.transform, false);
            Image arImg = arrowRightGO.AddComponent<Image>();
            arImg.sprite = arrowR;
            RectTransform arRect = arrowRightGO.GetComponent<RectTransform>();
            arRect.sizeDelta = new Vector2(24, 24);
        }

        // Paws tracker
        GameObject pawsTracker = new GameObject("PawsTracker");
        pawsTracker.transform.SetParent(rowGO.transform, false);
        pawsTracker.SetActive(false);
        pawsGroup = pawsTracker;
    }

    private static void CreateSelectorRow(Transform parent, string name, string initialValue, TMP_FontAsset font, Sprite arrowL, Sprite arrowR, Sprite pawSprite,
        out TextMeshProUGUI valueText, out GameObject pawsGroup, out GameObject arrowLeftGO, out GameObject arrowRightGO)
    {
        GameObject rowGO = new GameObject(name);
        rowGO.transform.SetParent(parent, false);
        RectTransform rowRect = rowGO.AddComponent<RectTransform>();
        rowRect.sizeDelta = new Vector2(700f, 50f);

        HorizontalLayoutGroup hl = rowGO.AddComponent<HorizontalLayoutGroup>();
        hl.childAlignment = TextAnchor.MiddleCenter;
        hl.spacing = 16f;
        hl.childForceExpandWidth = false;
        hl.childForceExpandHeight = false;

        // Arrow Left
        arrowLeftGO = null;
        if (arrowL != null)
        {
            arrowLeftGO = new GameObject("ArrowLeft");
            arrowLeftGO.transform.SetParent(rowGO.transform, false);
            Image alImg = arrowLeftGO.AddComponent<Image>();
            alImg.sprite = arrowL;
            RectTransform alRect = arrowLeftGO.GetComponent<RectTransform>();
            alRect.sizeDelta = new Vector2(26, 26);
        }

        // Value Text
        valueText = CreateTMPText(rowGO.transform, "ValueText", initialValue, font, 34, FontStyles.Bold, TextAlignmentOptions.Center, 460, 45);

        // Arrow Right
        arrowRightGO = null;
        if (arrowR != null)
        {
            arrowRightGO = new GameObject("ArrowRight");
            arrowRightGO.transform.SetParent(rowGO.transform, false);
            Image arImg = arrowRightGO.AddComponent<Image>();
            arImg.sprite = arrowR;
            RectTransform arRect = arrowRightGO.GetComponent<RectTransform>();
            arRect.sizeDelta = new Vector2(26, 26);
        }

        // Paws tracker
        GameObject pawsTracker = new GameObject("PawsTracker");
        pawsTracker.transform.SetParent(rowGO.transform, false);
        pawsTracker.SetActive(false);
        pawsGroup = pawsTracker;
    }

    private static void CreateKeyHintItem(Transform parent, string name, string keyText, string actionText, TMP_FontAsset font, Sprite badgeSprite, float badgeW, float badgeH, Sprite subIcon, out RectTransform badgeRect)
    {
        GameObject itemGO = new GameObject(name);
        itemGO.transform.SetParent(parent, false);

        HorizontalLayoutGroup hl = itemGO.AddComponent<HorizontalLayoutGroup>();
        hl.childAlignment = TextAnchor.MiddleCenter;
        hl.spacing = 10f;
        hl.childForceExpandWidth = false;
        hl.childForceExpandHeight = false;

        // Pill Badge
        GameObject badgeGO = new GameObject("Badge");
        badgeGO.transform.SetParent(itemGO.transform, false);
        Image badgeImg = badgeGO.AddComponent<Image>();
        badgeImg.sprite = badgeSprite;
        badgeImg.type = Image.Type.Sliced;
        badgeImg.color = Color.white;
        badgeRect = badgeGO.GetComponent<RectTransform>();
        badgeRect.sizeDelta = new Vector2(badgeW, badgeH);

        if (subIcon != null)
        {
            VerticalLayoutGroup vl = badgeGO.AddComponent<VerticalLayoutGroup>();
            vl.childAlignment = TextAnchor.MiddleCenter;
            vl.spacing = -2f;
            vl.childForceExpandWidth = false;
            vl.childForceExpandHeight = false;

            TextMeshProUGUI bTxt = CreateTMPText(badgeGO.transform, "KeyLabel", keyText, font, 14, FontStyles.Bold, TextAlignmentOptions.Center);
            bTxt.color = new Color(0.1f, 0.1f, 0.15f, 1f);

            GameObject iconGO = new GameObject("ReturnArrow");
            iconGO.transform.SetParent(badgeGO.transform, false);
            Image iImg = iconGO.AddComponent<Image>();
            iImg.sprite = subIcon;
            iImg.raycastTarget = false;
            RectTransform iRect = iconGO.GetComponent<RectTransform>();
            iRect.sizeDelta = new Vector2(24f, 16f);
        }
        else
        {
            TextMeshProUGUI bTxt = CreateTMPText(badgeGO.transform, "KeyLabel", keyText, font, 18, FontStyles.Bold, TextAlignmentOptions.Center);
            bTxt.color = new Color(0.1f, 0.1f, 0.15f, 1f);
            RectTransform btRect = bTxt.GetComponent<RectTransform>();
            btRect.anchorMin = Vector2.zero;
            btRect.anchorMax = Vector2.one;
            btRect.sizeDelta = Vector2.zero;
        }

        // Action Label (White)
        TextMeshProUGUI aTxt = CreateTMPText(itemGO.transform, "ActionLabel", actionText, font, 26, FontStyles.Bold, TextAlignmentOptions.Left);
        aTxt.color = Color.white;
    }

    private static void CreateSpacer(Transform parent, float height)
    {
        GameObject spacer = new GameObject("Spacer");
        spacer.transform.SetParent(parent, false);
        RectTransform rt = spacer.AddComponent<RectTransform>();
        rt.sizeDelta = new Vector2(10f, height);
    }

    private static TMP_FontAsset LoadMenuFont()
    {
        TMP_FontAsset font = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(FONT_PATH_PRIMARY);
        if (font == null) font = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(FONT_PATH_SECONDARY);
        if (font == null) font = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(FONT_PATH_FALLBACK);
        return font;
    }

    #endregion
}
#endif
