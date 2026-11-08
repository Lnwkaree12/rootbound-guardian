#if UNITY_EDITOR
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// SetupLanguageSwitcherUI — ติดตั้ง UI ระบบเปลี่ยนภาษา 3 ภาษาในหน้า MainScreen อัตโนมัติ
/// ทำงานอัตโนมัติเมื่อ Unity โหลด หรือคลิกเมนู Tools > SproutScout > Add Language Tab and Setting to MainScreen
/// </summary>
[InitializeOnLoad]
public static class SetupLanguageSwitcherUI
{
    private const string SCENE_PATH = "Assets/Scenes/MainScreen.unity";
    private const string PREFAB_PATH = "Assets/Prefabs/MainMenu_Canvas.prefab";
    private const string FONT_PRIMARY = "Assets/Main Screen/OneLittleFont-Full SDF.asset";
    private const string FONT_SECONDARY = "Assets/Fonts/Itim_TMP_Dynamic.asset";
    private const string FONT_FALLBACK = "Assets/Itim-Regular SDF.asset";
    private const string SPRITE_9SLICED = "Assets/Main Screen/UI/9Sliced.png";

    static SetupLanguageSwitcherUI()
    {
        EditorApplication.delayCall += EnsureLanguageUIReady;
    }

    [MenuItem("Tools/SproutScout/Add Language Tab and Setting to MainScreen")]
    public static void ForceBuildLanguageUI()
    {
        BuildLanguageUI(true);
    }

    private static void EnsureLanguageUIReady()
    {
        if (EditorApplication.isPlaying || Application.isPlaying) return;

        // Check if active scene is MainScreen or can be verified
        var activeScene = EditorSceneManager.GetActiveScene();
        if (activeScene.path == SCENE_PATH)
        {
            var controller = Object.FindFirstObjectByType<MainMenuUIController>();
            if (controller != null && controller.languageTabButton == null)
            {
                BuildLanguageUI(false);
            }
        }
    }

    public static void BuildLanguageUI(bool showDialog)
    {
        // 1. Ensure scene is open
        var currentScene = EditorSceneManager.GetActiveScene();
        if (currentScene.path != SCENE_PATH)
        {
            if (File.Exists(SCENE_PATH))
            {
                currentScene = EditorSceneManager.OpenScene(SCENE_PATH, OpenSceneMode.Single);
            }
        }

        // 2. Ensure LocalizationManager exists
        EnsureLocalizationManager();

        // 3. Find MainMenuUIController
        var controller = Object.FindFirstObjectByType<MainMenuUIController>();
        if (controller == null)
        {
            Debug.LogError("[SetupLanguageSwitcherUI] ไม่พบ MainMenuUIController ในฉาก!");
            if (showDialog) EditorUtility.DisplayDialog("Error", "ไม่พบ MainMenuUIController ในฉาก MainScreen", "OK");
            return;
        }

        Transform canvasT = controller.transform;
        TMP_FontAsset font = LoadFont();
        Sprite bgSprite = AssetDatabase.LoadAssetAtPath<Sprite>(SPRITE_9SLICED);

        // 4. Update CategoryTabsRow with 3rd Tab: LanguageTab
        SetupCategoryTabsRow(controller, font, bgSprite);

        // 5. Setup LanguageContent Panel under SettingsPanel
        SetupLanguageContentPanel(controller, font, bgSprite);

        // 6. Setup Corner Language Switcher Badge on Canvas
        SetupCornerBadge(canvasT, controller, font);

        // 7. Save Scene
        EditorSceneManager.MarkSceneDirty(currentScene);
        EditorSceneManager.SaveScene(currentScene);

        Debug.Log("[SetupLanguageSwitcherUI] ✅ ติดตั้ง Language Tab & Settings UI ใน MainScreen สำเร็จ 100%!");
        if (showDialog)
        {
            EditorUtility.DisplayDialog("สำเร็จ! ✅",
                "เพิ่มแท็บและระบบเลือกภาษาในหน้า MainScreen เรียบร้อยแล้ว!\n\n" +
                "1. แท็บ 'ภาษา' (LANGUAGE) ในหน้า Settings (กด E / Q สลับแท็บได้)\n" +
                "2. แผงเลือกภาษา 3 การ์ด: ภาษาไทย, English, 简体中文\n" +
                "3. วิดเจ็ตปุ่มสลับภาษาด่วนมุมขวาบน 🌐 [ไทย][EN][中文] (กด L วนได้)\n" +
                "4. รองรับ Mouse, Keyboard (1/2/3, W/A/S/D, Enter, ESC), และ Gamepad", "OK");
        }
    }

    private static void EnsureLocalizationManager()
    {
        var lm = Object.FindFirstObjectByType<LocalizationManager>();
        if (lm == null)
        {
            GameObject lmGO = new GameObject("LocalizationManager");
            lmGO.AddComponent<LocalizationManager>();
            Debug.Log("[SetupLanguageSwitcherUI] สร้าง LocalizationManager ในฉาก");
        }
    }

    private static void SetupCategoryTabsRow(MainMenuUIController controller, TMP_FontAsset font, Sprite bgSprite)
    {
        if (controller.categoryTabsRow == null)
        {
            var tabsRowGO = GameObject.Find("CategoryTabsRow");
            if (tabsRowGO != null) controller.categoryTabsRow = tabsRowGO;
        }

        if (controller.categoryTabsRow == null) return;

        Transform rowT = controller.categoryTabsRow.transform;

        // Balance 3 Tabs widths and positions
        // Row is 920 wide. LeftHint at -420, RightHint at +420
        // Tab 1: AudioVideoTab at -265, size 240x46
        // Tab 2: ControllerTab at 0, size 240x46
        // Tab 3: LanguageTab at +265, size 240x46

        Transform leftHint = rowT.Find("LeftHint");
        if (leftHint != null)
        {
            var rt = leftHint.GetComponent<RectTransform>();
            if (rt != null) rt.anchoredPosition = new Vector2(-425f, 0f);
        }

        Transform rightHint = rowT.Find("RightHint");
        if (rightHint != null)
        {
            var rt = rightHint.GetComponent<RectTransform>();
            if (rt != null) rt.anchoredPosition = new Vector2(425f, 0f);
        }

        // AudioVideoTab
        Transform avTab = rowT.Find("AudioVideoTab");
        if (avTab != null)
        {
            var rt = avTab.GetComponent<RectTransform>();
            if (rt != null)
            {
                rt.anchoredPosition = new Vector2(-265f, 0f);
                rt.sizeDelta = new Vector2(240f, 46f);
            }
        }

        // ControllerTab
        Transform ctrlTab = rowT.Find("ControllerTab");
        if (ctrlTab != null)
        {
            var rt = ctrlTab.GetComponent<RectTransform>();
            if (rt != null)
            {
                rt.anchoredPosition = new Vector2(0f, 0f);
                rt.sizeDelta = new Vector2(240f, 46f);
            }
        }

        // LanguageTab (Create if not exists)
        Transform langTab = rowT.Find("LanguageTab");
        if (langTab == null)
        {
            GameObject langTabGO = new GameObject("LanguageTab");
            langTabGO.transform.SetParent(rowT, false);
            langTab = langTabGO.transform;

            var rt = langTabGO.AddComponent<RectTransform>();
            rt.anchorMin = new Vector2(0.5f, 0.5f);
            rt.anchorMax = new Vector2(0.5f, 0.5f);
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.anchoredPosition = new Vector2(265f, 0f);
            rt.sizeDelta = new Vector2(240f, 46f);

            var img = langTabGO.AddComponent<Image>();
            if (bgSprite != null)
            {
                img.sprite = bgSprite;
                img.type = Image.Type.Sliced;
            }
            img.color = new Color(0.12f, 0.18f, 0.26f, 0.60f);

            var btn = langTabGO.AddComponent<Button>();
            var colors = btn.colors;
            colors.normalColor = Color.clear;
            colors.highlightedColor = new Color(1f, 1f, 1f, 0.08f);
            colors.pressedColor = new Color(1f, 1f, 1f, 0.15f);
            btn.colors = colors;

            // Highlight bar at bottom
            GameObject hlGO = new GameObject("Highlight");
            hlGO.transform.SetParent(langTab, false);
            var hlRT = hlGO.AddComponent<RectTransform>();
            hlRT.anchorMin = new Vector2(0f, 0f);
            hlRT.anchorMax = new Vector2(1f, 0f);
            hlRT.pivot = new Vector2(0.5f, 0f);
            hlRT.anchoredPosition = Vector2.zero;
            hlRT.sizeDelta = new Vector2(0f, 4f);
            var hlImg = hlGO.AddComponent<Image>();
            hlImg.color = new Color(0.35f, 0.85f, 0.55f, 1.0f);
            hlGO.SetActive(false);

            // Tab Text
            GameObject txtGO = new GameObject("Text");
            txtGO.transform.SetParent(langTab, false);
            var txtRT = txtGO.AddComponent<RectTransform>();
            txtRT.anchorMin = Vector2.zero;
            txtRT.anchorMax = Vector2.one;
            txtRT.sizeDelta = Vector2.zero;
            txtRT.anchoredPosition = Vector2.zero;
            var tmp = txtGO.AddComponent<TextMeshProUGUI>();
            tmp.text = "ภาษา (LANGUAGE)";
            tmp.fontSize = 16f;
            tmp.fontStyle = FontStyles.Bold;
            tmp.alignment = TextAlignmentOptions.Center;
            tmp.color = new Color(0.70f, 0.75f, 0.85f, 0.65f);
            tmp.raycastTarget = false;
            if (font != null) tmp.font = font;

            controller.languageTabButton = btn;
            controller.languageTabText = tmp;
            controller.languageTabHighlight = hlImg;
        }
        else
        {
            var btn = langTab.GetComponent<Button>();
            if (btn == null) btn = langTab.gameObject.AddComponent<Button>();
            controller.languageTabButton = btn;

            var hl = langTab.Find("Highlight");
            if (hl != null) controller.languageTabHighlight = hl.GetComponent<Image>();

            var txt = langTab.Find("Text");
            if (txt != null) controller.languageTabText = txt.GetComponent<TextMeshProUGUI>();
        }

        // Adjust sibling index of RightHint to remain last
        if (rightHint != null) rightHint.SetAsLastSibling();
    }

    private static void SetupLanguageContentPanel(MainMenuUIController controller, TMP_FontAsset font, Sprite bgSprite)
    {
        // SettingsPanel is the father of ControllerContent
        Transform settingsPanel = null;
        if (controller.controllerContent != null)
        {
            settingsPanel = controller.controllerContent.transform.parent;
        }
        else if (controller.settingsPanel != null)
        {
            settingsPanel = controller.settingsPanel.transform;
        }

        if (settingsPanel == null) return;

        // Check if LanguageContent exists
        Transform langContentT = settingsPanel.Find("LanguageContent");
        GameObject langContentGO;
        if (langContentT == null)
        {
            langContentGO = new GameObject("LanguageContent");
            langContentGO.transform.SetParent(settingsPanel, false);
            langContentT = langContentGO.transform;

            var rt = langContentGO.AddComponent<RectTransform>();
            rt.anchorMin = new Vector2(0.5f, 0.5f);
            rt.anchorMax = new Vector2(0.5f, 0.5f);
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.anchoredPosition = new Vector2(0f, -30f);
            rt.sizeDelta = new Vector2(1580f, 840f);

            var cg = langContentGO.AddComponent<CanvasGroup>();
            cg.alpha = 1f;
            cg.interactable = true;
            cg.blocksRaycasts = true;

            // Start inactive
            langContentGO.SetActive(false);
        }
        else
        {
            langContentGO = langContentT.gameObject;
        }

        controller.languageContent = langContentGO;

        // Clean any old children to rebuild neatly
        while (langContentT.childCount > 0)
        {
            Object.DestroyImmediate(langContentT.GetChild(0).gameObject);
        }

        // 1. Header Section
        GameObject headerGO = new GameObject("HeaderSection");
        headerGO.transform.SetParent(langContentT, false);
        var headerRT = headerGO.AddComponent<RectTransform>();
        headerRT.anchorMin = new Vector2(0.5f, 1f);
        headerRT.anchorMax = new Vector2(0.5f, 1f);
        headerRT.pivot = new Vector2(0.5f, 1f);
        headerRT.anchoredPosition = new Vector2(0f, -40f);
        headerRT.sizeDelta = new Vector2(1000f, 90f);

        GameObject titleGO = new GameObject("Title");
        titleGO.transform.SetParent(headerGO.transform, false);
        var titleRT = titleGO.AddComponent<RectTransform>();
        titleRT.anchoredPosition = new Vector2(0f, 12f);
        titleRT.sizeDelta = new Vector2(1000f, 44f);
        var titleTMP = titleGO.AddComponent<TextMeshProUGUI>();
        titleTMP.text = "เลือกภาษา / SELECT LANGUAGE";
        titleTMP.fontSize = 30f;
        titleTMP.fontStyle = FontStyles.Bold;
        titleTMP.alignment = TextAlignmentOptions.Center;
        titleTMP.color = new Color(1.0f, 0.92f, 0.60f, 1.0f);
        if (font != null) titleTMP.font = font;
        controller.languageTitleText = titleTMP;

        GameObject subGO = new GameObject("Subtitle");
        subGO.transform.SetParent(headerGO.transform, false);
        var subRT = subGO.AddComponent<RectTransform>();
        subRT.anchoredPosition = new Vector2(0f, -24f);
        subRT.sizeDelta = new Vector2(1000f, 30f);
        var subTMP = subGO.AddComponent<TextMeshProUGUI>();
        subTMP.text = "เปลี่ยนภาษาคำบรรยายและเมนูทั้งหมดในเกม";
        subTMP.fontSize = 16f;
        subTMP.alignment = TextAlignmentOptions.Center;
        subTMP.color = new Color(0.70f, 0.80f, 0.75f, 0.85f);
        if (font != null) subTMP.font = font;
        controller.languageSubtitleText = subTMP;

        // 2. Three Language Cards (TH, EN, ZH)
        GameObject cardsContainer = new GameObject("CardsContainer");
        cardsContainer.transform.SetParent(langContentT, false);
        var cardsRT = cardsContainer.AddComponent<RectTransform>();
        cardsRT.anchorMin = new Vector2(0.5f, 0.5f);
        cardsRT.anchorMax = new Vector2(0.5f, 0.5f);
        cardsRT.pivot = new Vector2(0.5f, 0.5f);
        cardsRT.anchoredPosition = new Vector2(0f, 25f);
        cardsRT.sizeDelta = new Vector2(1060f, 380f);

        // Card 1: TH
        var thCard = CreateLanguageCard(cardsContainer.transform, -360f, "Card_TH", "🇹🇭", "ภาษาไทย", "Thai (ค่าเริ่มต้น)", font, bgSprite);
        controller.thLanguageButton = thCard.button;
        controller.thLanguageStatusText = thCard.statusText;
        controller.thCardBorder = thCard.borderImage;

        // Card 2: EN
        var enCard = CreateLanguageCard(cardsContainer.transform, 0f, "Card_EN", "🇬🇧", "English", "ภาษาอังกฤษ", font, bgSprite);
        controller.enLanguageButton = enCard.button;
        controller.enLanguageStatusText = enCard.statusText;
        controller.enCardBorder = enCard.borderImage;

        // Card 3: ZH
        var zhCard = CreateLanguageCard(cardsContainer.transform, 360f, "Card_ZH", "🇨🇳", "简体中文", "ภาษาจีนตัวย่อ", font, bgSprite);
        controller.zhLanguageButton = zhCard.button;
        controller.zhLanguageStatusText = zhCard.statusText;
        controller.zhCardBorder = zhCard.borderImage;

        // 3. Bottom Section: Tip & Back Button
        GameObject tipGO = new GameObject("TipText");
        tipGO.transform.SetParent(langContentT, false);
        var tipRT = tipGO.AddComponent<RectTransform>();
        tipRT.anchorMin = new Vector2(0.5f, 0.5f);
        tipRT.anchorMax = new Vector2(0.5f, 0.5f);
        tipRT.pivot = new Vector2(0.5f, 0.5f);
        tipRT.anchoredPosition = new Vector2(0f, -220f);
        tipRT.sizeDelta = new Vector2(800f, 32f);
        var tipTMP = tipGO.AddComponent<TextMeshProUGUI>();
        tipTMP.text = "💡 กด [1] [2] [3] หรือ [L] เพื่อสลับภาษาได้ทันที (Press 1/2/3 or L to switch anytime)";
        tipTMP.fontSize = 15f;
        tipTMP.alignment = TextAlignmentOptions.Center;
        tipTMP.color = new Color(0.65f, 0.75f, 0.70f, 0.75f);
        if (font != null) tipTMP.font = font;
        controller.languageTipText = tipTMP;

        // Back Button
        GameObject backBtnGO = new GameObject("LanguageBackButton");
        backBtnGO.transform.SetParent(langContentT, false);
        var backRT = backBtnGO.AddComponent<RectTransform>();
        backRT.anchorMin = new Vector2(0.5f, 0.5f);
        backRT.anchorMax = new Vector2(0.5f, 0.5f);
        backRT.pivot = new Vector2(0.5f, 0.5f);
        backRT.anchoredPosition = new Vector2(0f, -280f);
        backRT.sizeDelta = new Vector2(220f, 48f);

        var backImg = backBtnGO.AddComponent<Image>();
        if (bgSprite != null)
        {
            backImg.sprite = bgSprite;
            backImg.type = Image.Type.Sliced;
        }
        backImg.color = new Color(0.12f, 0.18f, 0.15f, 0.90f);

        var backBtn = backBtnGO.AddComponent<Button>();
        var bColors = backBtn.colors;
        bColors.normalColor = Color.clear;
        bColors.highlightedColor = new Color(1f, 1f, 1f, 0.12f);
        bColors.pressedColor = new Color(1f, 1f, 1f, 0.25f);
        backBtn.colors = bColors;

        GameObject backTxtGO = new GameObject("Text");
        backTxtGO.transform.SetParent(backBtnGO.transform, false);
        var backTxtRT = backTxtGO.AddComponent<RectTransform>();
        backTxtRT.anchorMin = Vector2.zero;
        backTxtRT.anchorMax = Vector2.one;
        backTxtRT.sizeDelta = Vector2.zero;
        var backTMP = backTxtGO.AddComponent<TextMeshProUGUI>();
        backTMP.text = "◀ กลับ (BACK)";
        backTMP.fontSize = 16f;
        backTMP.fontStyle = FontStyles.Bold;
        backTMP.alignment = TextAlignmentOptions.Center;
        backTMP.color = new Color(1f, 0.93f, 0.70f, 1f);
        if (font != null) backTMP.font = font;

        controller.languageBackButton = backBtn;
    }

    private struct CardElements
    {
        public Button button;
        public TextMeshProUGUI statusText;
        public Image borderImage;
    }

    private static CardElements CreateLanguageCard(Transform parent, float posX, string name, string flag, string primaryTitle, string subTitle, TMP_FontAsset font, Sprite bgSprite)
    {
        CardElements card = new CardElements();

        GameObject root = new GameObject(name);
        root.transform.SetParent(parent, false);
        var rt = root.AddComponent<RectTransform>();
        rt.anchorMin = new Vector2(0.5f, 0.5f);
        rt.anchorMax = new Vector2(0.5f, 0.5f);
        rt.pivot = new Vector2(0.5f, 0.5f);
        rt.anchoredPosition = new Vector2(posX, 0f);
        rt.sizeDelta = new Vector2(310f, 350f);

        // Border
        var borderImg = root.AddComponent<Image>();
        if (bgSprite != null)
        {
            borderImg.sprite = bgSprite;
            borderImg.type = Image.Type.Sliced;
        }
        borderImg.color = new Color(0.30f, 0.40f, 0.35f, 0.50f);
        card.borderImage = borderImg;

        // Inner Card Panel
        GameObject inner = new GameObject("Inner");
        inner.transform.SetParent(root.transform, false);
        var innerRT = inner.AddComponent<RectTransform>();
        innerRT.anchorMin = Vector2.zero;
        innerRT.anchorMax = Vector2.one;
        innerRT.sizeDelta = new Vector2(-6f, -6f);
        innerRT.anchoredPosition = Vector2.zero;
        var innerImg = inner.AddComponent<Image>();
        if (bgSprite != null)
        {
            innerImg.sprite = bgSprite;
            innerImg.type = Image.Type.Sliced;
        }
        innerImg.color = new Color(0.08f, 0.12f, 0.10f, 0.95f);

        // Flag Icon
        GameObject flagGO = new GameObject("Flag");
        flagGO.transform.SetParent(inner.transform, false);
        var flagRT = flagGO.AddComponent<RectTransform>();
        flagRT.anchoredPosition = new Vector2(0f, 85f);
        flagRT.sizeDelta = new Vector2(100f, 60f);
        var flagTMP = flagGO.AddComponent<TextMeshProUGUI>();
        flagTMP.text = flag;
        flagTMP.fontSize = 44f;
        flagTMP.alignment = TextAlignmentOptions.Center;

        // Primary Title
        GameObject titleGO = new GameObject("Title");
        titleGO.transform.SetParent(inner.transform, false);
        var titleRT = titleGO.AddComponent<RectTransform>();
        titleRT.anchoredPosition = new Vector2(0f, 20f);
        titleRT.sizeDelta = new Vector2(280f, 40f);
        var titleTMP = titleGO.AddComponent<TextMeshProUGUI>();
        titleTMP.text = primaryTitle;
        titleTMP.fontSize = 24f;
        titleTMP.fontStyle = FontStyles.Bold;
        titleTMP.alignment = TextAlignmentOptions.Center;
        titleTMP.color = new Color(1.0f, 0.95f, 0.70f, 1f);
        if (font != null) titleTMP.font = font;

        // Sub Title
        GameObject subGO = new GameObject("Sub");
        subGO.transform.SetParent(inner.transform, false);
        var subRT = subGO.AddComponent<RectTransform>();
        subRT.anchoredPosition = new Vector2(0f, -18f);
        subRT.sizeDelta = new Vector2(280f, 30f);
        var subTMP = subGO.AddComponent<TextMeshProUGUI>();
        subTMP.text = subTitle;
        subTMP.fontSize = 15f;
        subTMP.alignment = TextAlignmentOptions.Center;
        subTMP.color = new Color(0.70f, 0.80f, 0.75f, 0.70f);
        if (font != null) subTMP.font = font;

        // Status Badge Pill
        GameObject statusPill = new GameObject("StatusPill");
        statusPill.transform.SetParent(inner.transform, false);
        var pillRT = statusPill.AddComponent<RectTransform>();
        pillRT.anchoredPosition = new Vector2(0f, -85f);
        pillRT.sizeDelta = new Vector2(200f, 40f);
        var pillImg = statusPill.AddComponent<Image>();
        if (bgSprite != null)
        {
            pillImg.sprite = bgSprite;
            pillImg.type = Image.Type.Sliced;
        }
        pillImg.color = new Color(0.12f, 0.18f, 0.15f, 0.85f);

        GameObject statusTxtGO = new GameObject("StatusText");
        statusTxtGO.transform.SetParent(statusPill.transform, false);
        var statusTxtRT = statusTxtGO.AddComponent<RectTransform>();
        statusTxtRT.anchorMin = Vector2.zero;
        statusTxtRT.anchorMax = Vector2.one;
        statusTxtRT.sizeDelta = Vector2.zero;
        var statusTMP = statusTxtGO.AddComponent<TextMeshProUGUI>();
        statusTMP.text = "เลือกภาษานี้";
        statusTMP.fontSize = 15f;
        statusTMP.fontStyle = FontStyles.Bold;
        statusTMP.alignment = TextAlignmentOptions.Center;
        statusTMP.color = new Color(0.65f, 0.75f, 0.70f, 0.80f);
        if (font != null) statusTMP.font = font;
        card.statusText = statusTMP;

        // Button component on root
        var btn = root.AddComponent<Button>();
        var bColors = btn.colors;
        bColors.normalColor = Color.clear;
        bColors.highlightedColor = new Color(1f, 1f, 1f, 0.08f);
        bColors.pressedColor = new Color(1f, 1f, 1f, 0.18f);
        btn.colors = bColors;
        card.button = btn;

        return card;
    }

    private static void SetupCornerBadge(Transform canvasT, MainMenuUIController controller, TMP_FontAsset font)
    {
        var oldBadge = canvasT.Find("LanguageSwitcher_Badge");
        if (oldBadge != null) Object.DestroyImmediate(oldBadge.gameObject);

        // Root badge anchored top-right
        GameObject badge = new GameObject("LanguageSwitcher_Badge");
        badge.transform.SetParent(canvasT, false);
        var bRT = badge.AddComponent<RectTransform>();
        bRT.anchorMin = new Vector2(1f, 1f);
        bRT.anchorMax = new Vector2(1f, 1f);
        bRT.pivot = new Vector2(1f, 1f);
        bRT.anchoredPosition = new Vector2(-24f, -24f);
        bRT.sizeDelta = new Vector2(210f, 42f);

        var bgImg = badge.AddComponent<Image>();
        bgImg.color = new Color(0.08f, 0.12f, 0.10f, 0.90f);

        var hLayout = badge.AddComponent<HorizontalLayoutGroup>();
        hLayout.spacing = 3f;
        hLayout.padding = new RectOffset(6, 6, 4, 4);
        hLayout.childAlignment = TextAnchor.MiddleCenter;
        hLayout.childControlWidth = false;
        hLayout.childControlHeight = false;

        // Prompt icon
        GameObject pGO = new GameObject("Icon");
        pGO.transform.SetParent(badge.transform, false);
        var pRT = pGO.AddComponent<RectTransform>();
        pRT.sizeDelta = new Vector2(28f, 34f);
        var pTMP = pGO.AddComponent<TextMeshProUGUI>();
        pTMP.text = "🌐";
        pTMP.fontSize = 16f;
        pTMP.alignment = TextAlignmentOptions.Center;

        string[] labels = { "ไทย", "EN", "中文" };
        Button[] btns = new Button[3];
        TextMeshProUGUI[] lbls = new TextMeshProUGUI[3];
        Image[] hls = new Image[3];

        for (int i = 0; i < 3; i++)
        {
            GameObject bGO = new GameObject($"Btn_{labels[i]}");
            bGO.transform.SetParent(badge.transform, false);
            var btnRT = bGO.AddComponent<RectTransform>();
            btnRT.sizeDelta = new Vector2(52f, 34f);

            GameObject hl = new GameObject("Hl");
            hl.transform.SetParent(bGO.transform, false);
            var hlRT = hl.AddComponent<RectTransform>();
            hlRT.anchorMin = Vector2.zero;
            hlRT.anchorMax = Vector2.one;
            hlRT.sizeDelta = Vector2.zero;
            var hlImg = hl.AddComponent<Image>();
            hlImg.color = new Color(0.35f, 0.85f, 0.55f, 0.35f);
            hl.SetActive(false);
            hls[i] = hlImg;

            GameObject lGO = new GameObject("Label");
            lGO.transform.SetParent(bGO.transform, false);
            var lRT = lGO.AddComponent<RectTransform>();
            lRT.anchorMin = Vector2.zero;
            lRT.anchorMax = Vector2.one;
            lRT.sizeDelta = Vector2.zero;
            var lTMP = lGO.AddComponent<TextMeshProUGUI>();
            lTMP.text = labels[i];
            lTMP.fontSize = 14f;
            lTMP.fontStyle = FontStyles.Bold;
            lTMP.alignment = TextAlignmentOptions.Center;
            lTMP.color = new Color(0.70f, 0.78f, 0.74f, 0.75f);
            if (font != null) lTMP.font = font;
            lbls[i] = lTMP;

            var btn = bGO.AddComponent<Button>();
            btns[i] = btn;
        }

        var switcher = badge.AddComponent<LanguageSwitcherUI>();
        switcher.thButton = btns[0];
        switcher.enButton = btns[1];
        switcher.zhButton = btns[2];
        switcher.thLabel = lbls[0];
        switcher.enLabel = lbls[1];
        switcher.zhLabel = lbls[2];
        switcher.thHighlight = hls[0];
        switcher.enHighlight = hls[1];
        switcher.zhHighlight = hls[2];

        controller.languageSwitcher = switcher;
        badge.transform.SetAsLastSibling();
    }

    private static TMP_FontAsset LoadFont()
    {
        TMP_FontAsset font = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(FONT_PRIMARY);
        if (font == null) font = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(FONT_SECONDARY);
        if (font == null) font = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(FONT_FALLBACK);
        return font;
    }
}
#endif
