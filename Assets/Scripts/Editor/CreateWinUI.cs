#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

[InitializeOnLoad]
public class CreateWinUI
{
    static CreateWinUI()
    {
        EditorApplication.delayCall += EnsureWinUIReady;
    }

    [MenuItem("Tools/SproutScout/Create Win UI")]
    public static void ForceCreateWinUI()
    {
        BuildWinUI(true);
    }

    [MenuItem("Tools/SproutScout/Setup Complete Win & Door Quest in Scene")]
    public static void SetupCompleteQuestInScene()
    {
        BuildWinUI(true);
        SetupSceneObjects(true);
    }

    private static void EnsureWinUIReady()
    {
        if (EditorApplication.isPlaying || Application.isPlaying) return;
        BuildWinUI(false);
    }

    public static void BuildWinUI(bool forceOverwrite)
    {
        string hudPrefabPath = "Assets/Prefabs/HUD_Canvas.prefab";
        string winCanvasPrefabPath = "Assets/Prefabs/Win_Canvas.prefab";

        GameObject canvasPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(hudPrefabPath);

        if (canvasPrefab == null)
        {
            Debug.LogWarning("[CreateWinUI] HUD_Canvas prefab not found. Building standalone Win_Canvas first...");
        }
        else
        {
            // Check if WinPanel already exists in the HUD prefab
            Transform existingPanel = canvasPrefab.transform.Find("WinPanel");
            if (existingPanel == null || forceOverwrite)
            {
                BuildWinUIIntoHUD(hudPrefabPath);
            }
        }

        // Also build standalone Win_Canvas prefab if needed
        if (forceOverwrite || AssetDatabase.LoadAssetAtPath<GameObject>(winCanvasPrefabPath) == null)
        {
            BuildStandaloneWinCanvas(winCanvasPrefabPath);
        }
    }

    private static void BuildWinUIIntoHUD(string prefabPath)
    {
        Debug.Log("[CreateWinUI] Integrating Win UI into HUD_Canvas prefab...");
        GameObject prefabRoot = PrefabUtility.LoadPrefabContents(prefabPath);

        // Remove old panel if present
        Transform oldPanel = prefabRoot.transform.Find("WinPanel");
        if (oldPanel != null)
        {
            Object.DestroyImmediate(oldPanel.gameObject);
        }

        Font customFont = LoadItimFont();

        // 1. Create WinPanel
        GameObject winPanelGO = BuildWinPanelHierarchy(prefabRoot.transform, customFont, out CanvasGroup canvasGroup, out RectTransform modalRect,
            out Text titleText, out Text subtitleText, out Text questStatusText, out Text clearTimeText, out Text hpText,
            out Button restartBtn, out Button menuBtn);

        // 2. Attach and configure WinUIManager
        WinUIManager winManager = prefabRoot.GetComponent<WinUIManager>();
        if (winManager == null)
        {
            winManager = prefabRoot.AddComponent<WinUIManager>();
        }

        winManager.winPanel = winPanelGO;
        winManager.winCanvasGroup = canvasGroup;
        winManager.modalContainer = modalRect;
        winManager.titleText = titleText;
        winManager.subtitleText = subtitleText;
        winManager.questStatusText = questStatusText;
        winManager.clearTimeText = clearTimeText;
        winManager.hpRemainingText = hpText;
        winManager.restartButton = restartBtn;
        winManager.mainMenuButton = menuBtn;

        // Start disabled
        winPanelGO.SetActive(false);

        // Save prefab
        PrefabUtility.SaveAsPrefabAsset(prefabRoot, prefabPath);
        PrefabUtility.UnloadPrefabContents(prefabRoot);

        AssetDatabase.Refresh();

        // Synchronize open scene instances
        Canvas[] sceneCanvases = Object.FindObjectsOfType<Canvas>(true);
        foreach (Canvas c in sceneCanvases)
        {
            if (c.gameObject.name.StartsWith("HUD Canvas") || c.gameObject.name.StartsWith("HUD_Canvas"))
            {
                if (PrefabUtility.IsPartOfPrefabInstance(c.gameObject))
                {
                    PrefabUtility.RevertPrefabInstance(c.gameObject, InteractionMode.AutomatedAction);
                    Debug.Log("[CreateWinUI] Synchronized HUD Canvas instance in active scene: " + c.gameObject.name);
                }
            }
        }

        Debug.Log($"[CreateWinUI] Successfully integrated Win UI into: {prefabPath}");
    }

    private static void BuildStandaloneWinCanvas(string prefabPath)
    {
        Debug.Log("[CreateWinUI] Building standalone Win_Canvas prefab...");

        GameObject canvasGO = new GameObject("Win Canvas");
        Canvas canvas = canvasGO.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 100; // Always top

        CanvasScaler scaler = canvasGO.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);
        scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
        scaler.matchWidthOrHeight = 0.5f;

        canvasGO.AddComponent<GraphicRaycaster>();

        Font customFont = LoadItimFont();

        GameObject winPanelGO = BuildWinPanelHierarchy(canvasGO.transform, customFont, out CanvasGroup canvasGroup, out RectTransform modalRect,
            out Text titleText, out Text subtitleText, out Text questStatusText, out Text clearTimeText, out Text hpText,
            out Button restartBtn, out Button menuBtn);

        WinUIManager winManager = canvasGO.AddComponent<WinUIManager>();
        winManager.winPanel = winPanelGO;
        winManager.winCanvasGroup = canvasGroup;
        winManager.modalContainer = modalRect;
        winManager.titleText = titleText;
        winManager.subtitleText = subtitleText;
        winManager.questStatusText = questStatusText;
        winManager.clearTimeText = clearTimeText;
        winManager.hpRemainingText = hpText;
        winManager.restartButton = restartBtn;
        winManager.mainMenuButton = menuBtn;

        winPanelGO.SetActive(false);

        PrefabUtility.SaveAsPrefabAsset(canvasGO, prefabPath);
        Object.DestroyImmediate(canvasGO);

        AssetDatabase.Refresh();
        Debug.Log($"[CreateWinUI] Successfully created standalone Win_Canvas prefab at: {prefabPath}");
    }

    private static GameObject BuildWinPanelHierarchy(Transform parent, Font font,
        out CanvasGroup canvasGroup, out RectTransform modalRect,
        out Text titleText, out Text subtitleText, out Text questStatusText, out Text clearTimeText, out Text hpText,
        out Button restartBtn, out Button menuBtn)
    {
        // WinPanel: Full screen overlay
        GameObject winPanelGO = new GameObject("WinPanel");
        winPanelGO.transform.SetParent(parent, false);
        Image overlayBg = winPanelGO.AddComponent<Image>();
        overlayBg.color = new Color(0f, 0f, 0f, 0.80f);
        RectTransform winRect = winPanelGO.GetComponent<RectTransform>();
        winRect.anchorMin = Vector2.zero;
        winRect.anchorMax = Vector2.one;
        winRect.sizeDelta = Vector2.zero;

        canvasGroup = winPanelGO.AddComponent<CanvasGroup>();
        canvasGroup.alpha = 1f;
        canvasGroup.interactable = true;
        canvasGroup.blocksRaycasts = true;

        // Modal Container (Gold Frame)
        GameObject modalBorderGO = new GameObject("ModalContainer");
        modalBorderGO.transform.SetParent(winPanelGO.transform, false);
        Image borderImg = modalBorderGO.AddComponent<Image>();
        borderImg.color = new Color(0.85f, 0.65f, 0.25f, 1f); // Gold border
        modalRect = modalBorderGO.GetComponent<RectTransform>();
        modalRect.anchorMin = new Vector2(0.5f, 0.5f);
        modalRect.anchorMax = new Vector2(0.5f, 0.5f);
        modalRect.pivot = new Vector2(0.5f, 0.5f);
        modalRect.sizeDelta = new Vector2(580f, 440f);

        // Inner Card (Dark Wood)
        GameObject innerCardGO = new GameObject("InnerCard");
        innerCardGO.transform.SetParent(modalBorderGO.transform, false);
        Image cardImg = innerCardGO.AddComponent<Image>();
        cardImg.color = new Color(0.12f, 0.08f, 0.06f, 0.98f);
        RectTransform cardRect = innerCardGO.GetComponent<RectTransform>();
        cardRect.anchorMin = Vector2.zero;
        cardRect.anchorMax = Vector2.one;
        cardRect.offsetMin = new Vector2(4f, 4f);
        cardRect.offsetMax = new Vector2(-4f, -4f);

        // Badge Text
        GameObject badgeGO = new GameObject("BadgeText");
        badgeGO.transform.SetParent(innerCardGO.transform, false);
        Text badge = badgeGO.AddComponent<Text>();
        badge.font = font;
        badge.fontSize = 18;
        badge.color = new Color(1f, 0.85f, 0.35f);
        badge.alignment = TextAnchor.MiddleCenter;
        badge.text = "★ ✦  VICTORY  ✦ ★";
        RectTransform badgeRect = badgeGO.GetComponent<RectTransform>();
        badgeRect.anchorMin = new Vector2(0.5f, 1f);
        badgeRect.anchorMax = new Vector2(0.5f, 1f);
        badgeRect.pivot = new Vector2(0.5f, 1f);
        badgeRect.anchoredPosition = new Vector2(0f, -22f);
        badgeRect.sizeDelta = new Vector2(400f, 26f);

        // Title Text
        GameObject titleGO = new GameObject("TitleText");
        titleGO.transform.SetParent(innerCardGO.transform, false);
        titleText = titleGO.AddComponent<Text>();
        titleText.font = font;
        titleText.fontSize = 36;
        titleText.fontStyle = FontStyle.Bold;
        titleText.color = new Color(1f, 0.82f, 0.2f);
        titleText.alignment = TextAnchor.MiddleCenter;
        titleText.text = "ภารกิจสำเร็จ!";
        RectTransform titleRect = titleGO.GetComponent<RectTransform>();
        titleRect.anchorMin = new Vector2(0.5f, 1f);
        titleRect.anchorMax = new Vector2(0.5f, 1f);
        titleRect.pivot = new Vector2(0.5f, 1f);
        titleRect.anchoredPosition = new Vector2(0f, -50f);
        titleRect.sizeDelta = new Vector2(460f, 48f);

        // Subtitle Text
        GameObject subGO = new GameObject("SubtitleText");
        subGO.transform.SetParent(innerCardGO.transform, false);
        subtitleText = subGO.AddComponent<Text>();
        subtitleText.font = font;
        subtitleText.fontSize = 16;
        subtitleText.color = new Color(0.9f, 0.86f, 0.8f);
        subtitleText.alignment = TextAnchor.MiddleCenter;
        subtitleText.text = "ยินดีด้วย! คุณใช้กุญแจไขประตูดันเจี้ยนและผ่านด่านได้สำเร็จ";
        RectTransform subRect = subGO.GetComponent<RectTransform>();
        subRect.anchorMin = new Vector2(0.5f, 1f);
        subRect.anchorMax = new Vector2(0.5f, 1f);
        subRect.pivot = new Vector2(0.5f, 1f);
        subRect.anchoredPosition = new Vector2(0f, -100f);
        subRect.sizeDelta = new Vector2(500f, 28f);

        // Stats Box
        GameObject statsGO = new GameObject("StatsPanel");
        statsGO.transform.SetParent(innerCardGO.transform, false);
        Image statsBg = statsGO.AddComponent<Image>();
        statsBg.color = new Color(0.08f, 0.05f, 0.04f, 0.95f);
        RectTransform statsRect = statsGO.GetComponent<RectTransform>();
        statsRect.anchorMin = new Vector2(0.5f, 0.5f);
        statsRect.anchorMax = new Vector2(0.5f, 0.5f);
        statsRect.pivot = new Vector2(0.5f, 0.5f);
        statsRect.anchoredPosition = new Vector2(0f, -15f);
        statsRect.sizeDelta = new Vector2(500f, 130f);

        // Quest line
        GameObject questStatGO = new GameObject("QuestStatusText");
        questStatGO.transform.SetParent(statsGO.transform, false);
        questStatusText = questStatGO.AddComponent<Text>();
        questStatusText.font = font;
        questStatusText.fontSize = 18;
        questStatusText.color = new Color(1f, 0.85f, 0.3f);
        questStatusText.alignment = TextAnchor.MiddleCenter;
        questStatusText.text = "🔑 ภารกิจ: ใช้กุญแจไขประตูสำเร็จ! (1/1)";
        RectTransform questStatRect = questStatGO.GetComponent<RectTransform>();
        questStatRect.anchorMin = new Vector2(0.5f, 1f);
        questStatRect.anchorMax = new Vector2(0.5f, 1f);
        questStatRect.pivot = new Vector2(0.5f, 1f);
        questStatRect.anchoredPosition = new Vector2(0f, -16f);
        questStatRect.sizeDelta = new Vector2(460f, 28f);

        // Time line
        GameObject timeGO = new GameObject("TimeText");
        timeGO.transform.SetParent(statsGO.transform, false);
        clearTimeText = timeGO.AddComponent<Text>();
        clearTimeText.font = font;
        clearTimeText.fontSize = 17;
        clearTimeText.color = Color.white;
        clearTimeText.alignment = TextAnchor.MiddleCenter;
        clearTimeText.text = "⏱️ เวลาที่ใช้: 00:00";
        RectTransform timeRect = timeGO.GetComponent<RectTransform>();
        timeRect.anchorMin = new Vector2(0.5f, 1f);
        timeRect.anchorMax = new Vector2(0.5f, 1f);
        timeRect.pivot = new Vector2(0.5f, 1f);
        timeRect.anchoredPosition = new Vector2(0f, -50f);
        timeRect.sizeDelta = new Vector2(460f, 26f);

        // HP line
        GameObject hpStatGO = new GameObject("HPText");
        hpStatGO.transform.SetParent(statsGO.transform, false);
        hpText = hpStatGO.AddComponent<Text>();
        hpText.font = font;
        hpText.fontSize = 16;
        hpText.color = new Color(0.45f, 0.9f, 0.45f);
        hpText.alignment = TextAnchor.MiddleCenter;
        hpText.text = "❤️ พลังชีวิตคงเหลือ: 100 / 100 HP";
        RectTransform hpStatRect = hpStatGO.GetComponent<RectTransform>();
        hpStatRect.anchorMin = new Vector2(0.5f, 1f);
        hpStatRect.anchorMax = new Vector2(0.5f, 1f);
        hpStatRect.pivot = new Vector2(0.5f, 1f);
        hpStatRect.anchoredPosition = new Vector2(0f, -84f);
        hpStatRect.sizeDelta = new Vector2(460f, 26f);

        // Buttons Row
        GameObject buttonRowGO = new GameObject("ButtonRow");
        buttonRowGO.transform.SetParent(innerCardGO.transform, false);
        RectTransform rowRect = buttonRowGO.AddComponent<RectTransform>();
        rowRect.anchorMin = new Vector2(0.5f, 0f);
        rowRect.anchorMax = new Vector2(0.5f, 0f);
        rowRect.pivot = new Vector2(0.5f, 0f);
        rowRect.anchoredPosition = new Vector2(0f, 25f);
        rowRect.sizeDelta = new Vector2(500f, 52f);

        // Restart Button
        GameObject restartBtnGO = new GameObject("RestartButton");
        restartBtnGO.transform.SetParent(buttonRowGO.transform, false);
        Image restartBg = restartBtnGO.AddComponent<Image>();
        restartBg.color = new Color(0.24f, 0.16f, 0.12f, 1f);
        restartBtn = restartBtnGO.AddComponent<Button>();
        ConfigureButtonColors(restartBtn, new Color(0.24f, 0.16f, 0.12f), new Color(0.38f, 0.26f, 0.18f), new Color(0.16f, 0.10f, 0.06f));
        RectTransform restartRect = restartBtnGO.GetComponent<RectTransform>();
        restartRect.anchorMin = new Vector2(0f, 0.5f);
        restartRect.anchorMax = new Vector2(0f, 0.5f);
        restartRect.pivot = new Vector2(0f, 0.5f);
        restartRect.anchoredPosition = new Vector2(15f, 0f);
        restartRect.sizeDelta = new Vector2(220f, 48f);

        GameObject rBorder = new GameObject("Border");
        rBorder.transform.SetParent(restartBtnGO.transform, false);
        Image rbImg = rBorder.AddComponent<Image>();
        rbImg.color = new Color(0.85f, 0.65f, 0.25f, 0.8f);
        rbImg.raycastTarget = false;
        RectTransform rbRect = rBorder.GetComponent<RectTransform>();
        rbRect.anchorMin = Vector2.zero;
        rbRect.anchorMax = Vector2.one;
        rbRect.sizeDelta = Vector2.zero;
        rBorder.transform.SetAsFirstSibling();

        GameObject rTextGO = new GameObject("Text");
        rTextGO.transform.SetParent(restartBtnGO.transform, false);
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

        // Main Menu Button
        GameObject menuBtnGO = new GameObject("MainMenuButton");
        menuBtnGO.transform.SetParent(buttonRowGO.transform, false);
        Image menuBg = menuBtnGO.AddComponent<Image>();
        menuBg.color = new Color(0.24f, 0.16f, 0.12f, 1f);
        menuBtn = menuBtnGO.AddComponent<Button>();
        ConfigureButtonColors(menuBtn, new Color(0.24f, 0.16f, 0.12f), new Color(0.38f, 0.26f, 0.18f), new Color(0.16f, 0.10f, 0.06f));
        RectTransform menuRect = menuBtnGO.GetComponent<RectTransform>();
        menuRect.anchorMin = new Vector2(1f, 0.5f);
        menuRect.anchorMax = new Vector2(1f, 0.5f);
        menuRect.pivot = new Vector2(1f, 0.5f);
        menuRect.anchoredPosition = new Vector2(-15f, 0f);
        menuRect.sizeDelta = new Vector2(220f, 48f);

        GameObject mBorder = new GameObject("Border");
        mBorder.transform.SetParent(menuBtnGO.transform, false);
        Image mbImg = mBorder.AddComponent<Image>();
        mbImg.color = new Color(0.7f, 0.65f, 0.6f, 0.8f);
        mbImg.raycastTarget = false;
        RectTransform mbRect = mBorder.GetComponent<RectTransform>();
        mbRect.anchorMin = Vector2.zero;
        mbRect.anchorMax = Vector2.one;
        mbRect.sizeDelta = Vector2.zero;
        mBorder.transform.SetAsFirstSibling();

        GameObject mTextGO = new GameObject("Text");
        mTextGO.transform.SetParent(menuBtnGO.transform, false);
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

        return winPanelGO;
    }

    private static Font LoadItimFont()
    {
        Font customFont = AssetDatabase.LoadAssetAtPath<Font>("Assets/Itim-Regular.ttf");
        if (customFont == null)
        {
            try { customFont = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf"); }
            catch { try { customFont = Resources.GetBuiltinResource<Font>("Arial.ttf"); } catch { } }
        }
        return customFont;
    }

    private static void ConfigureButtonColors(Button btn, Color normal, Color highlighted, Color pressed)
    {
        ColorBlock colors = btn.colors;
        colors.normalColor = normal;
        colors.highlightedColor = highlighted;
        colors.pressedColor = pressed;
        colors.selectedColor = highlighted;
        colors.fadeDuration = 0.1f;
        btn.colors = colors;
    }

    public static void SetupSceneObjects(bool force)
    {
        var activeScene = EditorSceneManager.GetActiveScene();
        if (!activeScene.IsValid()) return;

        Debug.Log($"[CreateWinUI] Ensuring Door, Key, and HUD Canvas exist in active scene: {activeScene.name}...");

        // 1. Ensure HUD Canvas instance exists
        GameObject hudInstance = GameObject.Find("HUD Canvas");
        if (hudInstance == null)
        {
            string hudPrefabPath = "Assets/Prefabs/HUD_Canvas.prefab";
            GameObject hudPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(hudPrefabPath);
            if (hudPrefab != null)
            {
                hudInstance = (GameObject)PrefabUtility.InstantiatePrefab(hudPrefab);
                hudInstance.name = "HUD Canvas";
                Debug.Log("[CreateWinUI] Spawned HUD Canvas in scene.");
            }
        }

        // 2. Ensure Door exists
        GameObject doorInstance = GameObject.Find("Door");
        if (doorInstance == null || force)
        {
            if (doorInstance != null && force) Object.DestroyImmediate(doorInstance);

            string doorPrefabPath = "Assets/Prefabs/Door.prefab";
            GameObject doorPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(doorPrefabPath);
            if (doorPrefab != null)
            {
                doorInstance = (GameObject)PrefabUtility.InstantiatePrefab(doorPrefab);
                doorInstance.name = "Door";
                doorInstance.transform.position = new Vector3(8.0f, 0.0f, 0.0f);
                doorInstance.transform.rotation = Quaternion.Euler(0f, 90f, 0f);
                Debug.Log("[CreateWinUI] Spawned Door in scene at (8.0, 0.0, 0.0).");
            }
        }

        // 3. Ensure Key exists
        GameObject keyInstance = GameObject.Find("Key");
        if (keyInstance == null || force)
        {
            if (keyInstance != null && force) Object.DestroyImmediate(keyInstance);

            string keyPrefabPath = "Assets/Prefabs/Key.prefab";
            GameObject keyPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(keyPrefabPath);
            if (keyPrefab != null)
            {
                keyInstance = (GameObject)PrefabUtility.InstantiatePrefab(keyPrefab);
                keyInstance.name = "Key";
                keyInstance.transform.position = new Vector3(18.14f, 1.2f, 10.74f);
                Debug.Log("[CreateWinUI] Spawned Key in scene at (18.14, 1.2, 10.74).");
            }
        }

        EditorSceneManager.MarkSceneDirty(activeScene);
        EditorSceneManager.SaveScene(activeScene);
        Debug.Log("[CreateWinUI] Scene saved with Door, Key, and HUD Canvas!");
    }
}
#endif
