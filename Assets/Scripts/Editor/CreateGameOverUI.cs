#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

public class CreateGameOverUI
{
    [MenuItem("Tools/SproutScout/Create Game Over UI")]
    public static void ForceCreateGameOverUI()
    {
        BuildGameOverUI(true);
    }

    public static void BuildGameOverUI(bool forceOverwrite)
    {
        string hudPrefabPath = "Assets/Prefabs/HUD_Canvas.prefab";
        GameObject canvasPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(hudPrefabPath);

        if (canvasPrefab == null)
        {
            Debug.LogError("[CreateGameOverUI] HUD_Canvas prefab not found at " + hudPrefabPath);
            return;
        }

        Transform existingPanel = canvasPrefab.transform.Find("GameOverPanel");
        if (existingPanel != null && !forceOverwrite)
        {
            Debug.Log("[CreateGameOverUI] GameOverPanel already exists in HUD_Canvas.");
            return;
        }

        Debug.Log("[CreateGameOverUI] Integrating Game Over UI into HUD_Canvas prefab...");
        GameObject prefabRoot = PrefabUtility.LoadPrefabContents(hudPrefabPath);

        // Remove old panel if present
        Transform oldPanel = prefabRoot.transform.Find("GameOverPanel");
        if (oldPanel != null)
        {
            Object.DestroyImmediate(oldPanel.gameObject);
        }

        Font customFont = LoadItimFont();

        // 1. Create GameOverPanel
        GameObject gameOverPanelGO = BuildGameOverPanelHierarchy(prefabRoot.transform, customFont,
            out CanvasGroup canvasGroup, out RectTransform modalRect,
            out Text titleText, out Text reasonText,
            out Button restartBtn, out Button menuBtn);

        // 2. Clean duplicate GameOverUIManager components
        var managers = prefabRoot.GetComponents<GameOverUIManager>();
        GameOverUIManager manager = null;
        if (managers.Length > 0)
        {
            manager = managers[0];
            for (int i = 1; i < managers.Length; i++)
            {
                Object.DestroyImmediate(managers[i]);
            }
        }
        else
        {
            manager = prefabRoot.AddComponent<GameOverUIManager>();
        }

        // 3. Attach and configure GameOverUIManager
        manager.gameOverPanel = gameOverPanelGO;
        manager.gameOverCanvasGroup = canvasGroup;
        manager.titleText = titleText;
        manager.reasonText = reasonText;
        manager.restartButton = restartBtn;
        manager.mainMenuButton = menuBtn;

        // Start disabled
        gameOverPanelGO.SetActive(false);

        // Save prefab
        PrefabUtility.SaveAsPrefabAsset(prefabRoot, hudPrefabPath);
        PrefabUtility.UnloadPrefabContents(prefabRoot);

        AssetDatabase.Refresh();

        // 4. Synchronize open scene instances
        var sceneCanvases = Object.FindObjectsByType<Canvas>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        foreach (Canvas c in sceneCanvases)
        {
            if (c.gameObject.name.StartsWith("HUD Canvas") || c.gameObject.name.StartsWith("HUD_Canvas"))
            {
                if (PrefabUtility.IsPartOfPrefabInstance(c.gameObject))
                {
                    PrefabUtility.RevertPrefabInstance(c.gameObject, InteractionMode.AutomatedAction);
                    Debug.Log("[CreateGameOverUI] Synchronized HUD Canvas instance in active scene: " + c.gameObject.name);
                }
            }
        }

        Debug.Log($"[CreateGameOverUI] Successfully integrated Game Over UI into: {hudPrefabPath}");
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

    private static GameObject BuildGameOverPanelHierarchy(Transform parent, Font font,
        out CanvasGroup canvasGroup, out RectTransform modalRect,
        out Text titleText, out Text reasonText,
        out Button restartBtn, out Button menuBtn)
    {
        // 1. Root Panel
        GameObject panelGO = new GameObject("GameOverPanel");
        panelGO.transform.SetParent(parent, false);

        var overlayBg = panelGO.AddComponent<Image>();
        overlayBg.color = new Color(0.08f, 0.02f, 0.02f, 0.88f); // Dark crimson tint

        RectTransform panelRect = panelGO.GetComponent<RectTransform>();
        panelRect.anchorMin = Vector2.zero;
        panelRect.anchorMax = Vector2.one;
        panelRect.sizeDelta = Vector2.zero;

        canvasGroup = panelGO.AddComponent<CanvasGroup>();
        canvasGroup.alpha = 1f;
        canvasGroup.interactable = true;
        canvasGroup.blocksRaycasts = true;

        // 2. ModalContainer (Crimson border)
        GameObject modalBorderGO = new GameObject("ModalContainer");
        modalBorderGO.transform.SetParent(panelGO.transform, false);

        var borderImg = modalBorderGO.AddComponent<Image>();
        borderImg.color = new Color(0.80f, 0.20f, 0.18f, 1f);

        modalRect = modalBorderGO.GetComponent<RectTransform>();
        modalRect.anchorMin = new Vector2(0.5f, 0.5f);
        modalRect.anchorMax = new Vector2(0.5f, 0.5f);
        modalRect.pivot = new Vector2(0.5f, 0.5f);
        modalRect.sizeDelta = new Vector2(580f, 440f);

        // 3. InnerCard (Dark obsidian core)
        GameObject innerCardGO = new GameObject("InnerCard");
        innerCardGO.transform.SetParent(modalBorderGO.transform, false);

        var cardImg = innerCardGO.AddComponent<Image>();
        cardImg.color = new Color(0.10f, 0.06f, 0.07f, 0.98f);

        RectTransform cardRect = innerCardGO.GetComponent<RectTransform>();
        cardRect.anchorMin = Vector2.zero;
        cardRect.anchorMax = Vector2.one;
        cardRect.offsetMin = new Vector2(4f, 4f);
        cardRect.offsetMax = new Vector2(-4f, -4f);

        // 4. Badge Text
        GameObject badgeGO = new GameObject("BadgeText");
        badgeGO.transform.SetParent(innerCardGO.transform, false);
        Text badge = badgeGO.AddComponent<Text>();
        badge.font = font;
        badge.fontSize = 18;
        badge.color = new Color(1f, 0.40f, 0.35f);
        badge.alignment = TextAnchor.MiddleCenter;
        badge.text = "☠  G A M E   O V E R  ☠";

        RectTransform badgeRect = badgeGO.GetComponent<RectTransform>();
        badgeRect.anchorMin = new Vector2(0.5f, 1f);
        badgeRect.anchorMax = new Vector2(0.5f, 1f);
        badgeRect.pivot = new Vector2(0.5f, 1f);
        badgeRect.anchoredPosition = new Vector2(0f, -22f);
        badgeRect.sizeDelta = new Vector2(400f, 26f);

        // 5. Title Text
        GameObject titleGO = new GameObject("TitleText");
        titleGO.transform.SetParent(innerCardGO.transform, false);
        titleText = titleGO.AddComponent<Text>();
        titleText.font = font;
        titleText.fontSize = 38;
        titleText.fontStyle = FontStyle.Bold;
        titleText.color = new Color(1f, 0.25f, 0.25f);
        titleText.alignment = TextAnchor.MiddleCenter;
        titleText.text = "ภารกิจล้มเหลว!";

        var titleShadow = titleGO.AddComponent<Shadow>();
        titleShadow.effectColor = new Color(0f, 0f, 0f, 0.85f);
        titleShadow.effectDistance = new Vector2(2f, -2f);

        RectTransform titleRect = titleGO.GetComponent<RectTransform>();
        titleRect.anchorMin = new Vector2(0.5f, 1f);
        titleRect.anchorMax = new Vector2(0.5f, 1f);
        titleRect.pivot = new Vector2(0.5f, 1f);
        titleRect.anchoredPosition = new Vector2(0f, -50f);
        titleRect.sizeDelta = new Vector2(460f, 48f);

        // 6. Reason Text
        GameObject reasonGO = new GameObject("ReasonText");
        reasonGO.transform.SetParent(innerCardGO.transform, false);
        reasonText = reasonGO.AddComponent<Text>();
        reasonText.font = font;
        reasonText.fontSize = 20;
        reasonText.color = new Color(1.0f, 0.88f, 0.88f);
        reasonText.alignment = TextAnchor.MiddleCenter;
        reasonText.text = "พลังชีวิตของคุณหมดลงแล้ว!";

        RectTransform reasonRect = reasonGO.GetComponent<RectTransform>();
        reasonRect.anchorMin = new Vector2(0.5f, 1f);
        reasonRect.anchorMax = new Vector2(0.5f, 1f);
        reasonRect.pivot = new Vector2(0.5f, 1f);
        reasonRect.anchoredPosition = new Vector2(0f, -102f);
        reasonRect.sizeDelta = new Vector2(500f, 32f);

        // 7. Info / Tip Box
        GameObject tipBoxGO = new GameObject("TipBox");
        tipBoxGO.transform.SetParent(innerCardGO.transform, false);

        var tipBg = tipBoxGO.AddComponent<Image>();
        tipBg.color = new Color(0.18f, 0.10f, 0.11f, 0.92f);

        RectTransform tipBoxRect = tipBoxGO.GetComponent<RectTransform>();
        tipBoxRect.anchorMin = new Vector2(0.5f, 0.5f);
        tipBoxRect.anchorMax = new Vector2(0.5f, 0.5f);
        tipBoxRect.pivot = new Vector2(0.5f, 0.5f);
        tipBoxRect.anchoredPosition = new Vector2(0f, -15f);
        tipBoxRect.sizeDelta = new Vector2(500f, 120f);

        GameObject tipTextGO = new GameObject("TipText");
        tipTextGO.transform.SetParent(tipBoxGO.transform, false);
        Text tipText = tipTextGO.AddComponent<Text>();
        tipText.font = font;
        tipText.fontSize = 16;
        tipText.color = new Color(0.92f, 0.85f, 0.80f);
        tipText.alignment = TextAnchor.MiddleCenter;
        tipText.text = "💡 คำแนะนำในการเอาชีวิตรอด\n\n• ระวังการโจมตีจากศัตรูในดันเจี้ยน\n• เมื่อออกนอก Safe Zone ออกซิเจนจะลดลงเรื่อยๆ\n• หมั่นกลับเข้า Safe Zone เพื่อฟื้นฟูออกซิเจน!";

        RectTransform tipTextRect = tipTextGO.GetComponent<RectTransform>();
        tipTextRect.anchorMin = Vector2.zero;
        tipTextRect.anchorMax = Vector2.one;
        tipTextRect.offsetMin = new Vector2(10f, 8f);
        tipTextRect.offsetMax = new Vector2(-10f, -8f);

        // 8. Buttons Row
        GameObject buttonRowGO = new GameObject("ButtonRow");
        buttonRowGO.transform.SetParent(innerCardGO.transform, false);
        RectTransform rowRect = buttonRowGO.AddComponent<RectTransform>();
        rowRect.anchorMin = new Vector2(0.5f, 0f);
        rowRect.anchorMax = new Vector2(0.5f, 0f);
        rowRect.pivot = new Vector2(0.5f, 0f);
        rowRect.anchoredPosition = new Vector2(0f, 25f);
        rowRect.sizeDelta = new Vector2(500f, 52f);

        // 8a. Restart Button
        GameObject restartBtnGO = new GameObject("RestartButton");
        restartBtnGO.transform.SetParent(buttonRowGO.transform, false);
        var restartBg = restartBtnGO.AddComponent<Image>();
        restartBg.color = new Color(0.24f, 0.14f, 0.12f, 1f);

        restartBtn = restartBtnGO.AddComponent<Button>();
        ConfigureButtonColors(restartBtn, new Color(0.24f, 0.14f, 0.12f), new Color(0.42f, 0.20f, 0.18f), new Color(0.16f, 0.08f, 0.06f));

        RectTransform restartRect = restartBtnGO.GetComponent<RectTransform>();
        restartRect.anchorMin = new Vector2(0f, 0.5f);
        restartRect.anchorMax = new Vector2(0f, 0.5f);
        restartRect.pivot = new Vector2(0f, 0.5f);
        restartRect.anchoredPosition = new Vector2(15f, 0f);
        restartRect.sizeDelta = new Vector2(220f, 48f);

        GameObject rBorder = new GameObject("Border");
        rBorder.transform.SetParent(restartBtnGO.transform, false);
        var rbImg = rBorder.AddComponent<Image>();
        rbImg.color = new Color(0.85f, 0.35f, 0.30f, 0.85f);
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
        rText.color = new Color(1f, 0.9f, 0.85f);
        rText.alignment = TextAnchor.MiddleCenter;
        rText.text = "🔄 เล่นใหม่อีกครั้ง";
        RectTransform rtRect = rTextGO.GetComponent<RectTransform>();
        rtRect.anchorMin = Vector2.zero;
        rtRect.anchorMax = Vector2.one;
        rtRect.sizeDelta = Vector2.zero;

        // 8b. Main Menu Button
        GameObject menuBtnGO = new GameObject("MainMenuButton");
        menuBtnGO.transform.SetParent(buttonRowGO.transform, false);
        var menuBg = menuBtnGO.AddComponent<Image>();
        menuBg.color = new Color(0.20f, 0.16f, 0.14f, 1f);

        menuBtn = menuBtnGO.AddComponent<Button>();
        ConfigureButtonColors(menuBtn, new Color(0.20f, 0.16f, 0.14f), new Color(0.35f, 0.28f, 0.22f), new Color(0.14f, 0.10f, 0.08f));

        RectTransform menuRect = menuBtnGO.GetComponent<RectTransform>();
        menuRect.anchorMin = new Vector2(1f, 0.5f);
        menuRect.anchorMax = new Vector2(1f, 0.5f);
        menuRect.pivot = new Vector2(1f, 0.5f);
        menuRect.anchoredPosition = new Vector2(-15f, 0f);
        menuRect.sizeDelta = new Vector2(220f, 48f);

        GameObject mBorder = new GameObject("Border");
        mBorder.transform.SetParent(menuBtnGO.transform, false);
        var mbImg = mBorder.AddComponent<Image>();
        mbImg.color = new Color(0.65f, 0.55f, 0.45f, 0.85f);
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
        mText.color = new Color(0.95f, 0.90f, 0.85f);
        mText.alignment = TextAnchor.MiddleCenter;
        mText.text = "🏠 กลับหน้าหลัก";
        RectTransform mtRect = mTextGO.GetComponent<RectTransform>();
        mtRect.anchorMin = Vector2.zero;
        mtRect.anchorMax = Vector2.one;
        mtRect.sizeDelta = Vector2.zero;

        return panelGO;
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
}
#endif
