using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public static class BuildNewLoadingScreen
{
    [MenuItem("Rootbound/Build New Loading Screen")]
    public static void Execute()
    {
        string scenePath = "Assets/LoadingScreenNew.unity";
        var scene = EditorSceneManager.OpenScene(scenePath, OpenSceneMode.Single);

        // 1. Setup Main Camera
        Camera mainCam = Camera.main;
        if (mainCam == null)
        {
            GameObject camObj = new GameObject("Main Camera");
            mainCam = camObj.AddComponent<Camera>();
            camObj.tag = "MainCamera";
        }
        mainCam.transform.position = new Vector3(0f, 0f, -10f);
        mainCam.transform.rotation = Quaternion.identity;
        mainCam.clearFlags = CameraClearFlags.SolidColor;
        mainCam.backgroundColor = new Color(0.04f, 0.05f, 0.08f, 1f); // Deep ethereal obsidian
        mainCam.orthographic = false;
        mainCam.fieldOfView = 50f;
        mainCam.nearClipPlane = 0.3f;
        mainCam.farClipPlane = 100f;

        // Ensure AudioListener
        if (mainCam.GetComponent<AudioListener>() == null)
        {
            mainCam.gameObject.AddComponent<AudioListener>();
        }

        // 2. Load Fonts & Resources
        TMP_FontAsset thaiFont = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>("Assets/Fonts/Itim_Thai_SDF.asset");
        if (thaiFont == null)
        {
            thaiFont = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>("Assets/Fonts/Itim_TMP_Dynamic.asset");
        }

        Sprite cardBg = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Image/UI/Tutorial_Card_Bg.png");
        Sprite keycapBg = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Image/UI/Keycap_Bg.png");
        Sprite trapIcon = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Image/UI/Trap_Icon.png");
        Sprite ladderIcon = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Image/UI/Ladder_Icon.png");
        Sprite treeBadge = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Image/UI/Tree_Badge_Icon.png");
        Sprite potionIcon = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Image/UI/HUD_Potion_Blue.png");
        Sprite oxygenIcon = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Image/UI/Fairytale_Oxygen_Medallion.png");
        Sprite chestIcon = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Image/UI/Cute_Chest_Badge.png");
        Sprite barTrough = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Image/UI/Fairytale_Bar_Trough.png");
        Sprite barFill = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Image/UI/Fairytale_HP_Fill.png");
        Sprite cutsceneBorder = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Image/Cutscene/Cutscene_Frame_Border.png");

        AudioClip bgmClip = AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Audio/SFX/Dungeon_BGM.mp3");
        AudioClip confirmClip = AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Audio/SFX/Key.mp3");

        // 3. Remove old canvases/managers if existing
        GameObject oldCanvas = GameObject.Find("Loading_Canvas");
        if (oldCanvas != null) Object.DestroyImmediate(oldCanvas);
        GameObject oldBgCanvas = GameObject.Find("Background_Canvas");
        if (oldBgCanvas != null) Object.DestroyImmediate(oldBgCanvas);
        GameObject oldMgr = GameObject.Find("LoadingManager");
        if (oldMgr != null) Object.DestroyImmediate(oldMgr);

        // 4. Background Canvas (Placed in World Space behind Lumi)
        GameObject bgCanvasObj = new GameObject("Background_Canvas");
        Canvas bgCanvas = bgCanvasObj.AddComponent<Canvas>();
        bgCanvas.renderMode = RenderMode.ScreenSpaceCamera;
        bgCanvas.worldCamera = mainCam;
        bgCanvas.planeDistance = 25f; // Behind Lumi
        bgCanvas.sortingOrder = -10;

        CanvasScaler bgScaler = bgCanvasObj.AddComponent<CanvasScaler>();
        bgScaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        bgScaler.referenceResolution = new Vector2(1920, 1080);
        bgScaler.matchWidthOrHeight = 0.5f;

        // Dark Vignette / Backdrop
        GameObject bgImageObj = new GameObject("Backdrop_Dark");
        bgImageObj.transform.SetParent(bgCanvasObj.transform, false);
        Image bgImg = bgImageObj.AddComponent<Image>();
        bgImg.color = new Color(0.04f, 0.06f, 0.09f, 1f);
        RectTransform bgRect = bgImageObj.GetComponent<RectTransform>();
        bgRect.anchorMin = Vector2.zero;
        bgRect.anchorMax = Vector2.one;
        bgRect.sizeDelta = Vector2.zero;

        // Decorative subtle fantasy border on Background
        if (cutsceneBorder != null)
        {
            GameObject borderObj = new GameObject("Decorative_Border");
            borderObj.transform.SetParent(bgCanvasObj.transform, false);
            Image borderImg = borderObj.AddComponent<Image>();
            borderImg.sprite = cutsceneBorder;
            borderImg.type = Image.Type.Sliced;
            borderImg.color = new Color(1f, 0.85f, 0.5f, 0.25f);
            RectTransform borderRect = borderObj.GetComponent<RectTransform>();
            borderRect.anchorMin = Vector2.zero;
            borderRect.anchorMax = Vector2.one;
            borderRect.sizeDelta = new Vector2(-40, -40);
        }

        // 5. Setup 3D Lumi Stage (Hero sized, lovingly framed)
        GameObject lumiStage = GameObject.Find("Lumi_Companion_Stage");
        if (lumiStage == null)
        {
            lumiStage = new GameObject("Lumi_Companion_Stage");
        }
        lumiStage.transform.position = new Vector3(-2.6f, -0.05f, -4.5f);
        lumiStage.transform.rotation = Quaternion.Euler(0f, 155f, 0f);
        lumiStage.transform.localScale = new Vector3(2.1f, 2.1f, 2.1f);

        // Ensure 3D Model inside Lumi Stage
        Transform modelChild = lumiStage.transform.Find("Lumi_Model");
        if (modelChild == null)
        {
            GameObject lumiPrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Lumi_Companion.prefab");
            if (lumiPrefab != null)
            {
                GameObject instantiated = (GameObject)PrefabUtility.InstantiatePrefab(lumiPrefab, lumiStage.transform);
                instantiated.name = "Lumi_Model";
                instantiated.transform.localPosition = Vector3.zero;
                instantiated.transform.localRotation = Quaternion.identity;
            }
        }

        // Point Light on Lumi
        Transform lightChild = lumiStage.transform.Find("Lumi_Light");
        Light lumiLight;
        if (lightChild == null)
        {
            GameObject lObj = new GameObject("Lumi_Light");
            lObj.transform.SetParent(lumiStage.transform, false);
            lObj.transform.localPosition = new Vector3(0f, 0.8f, -1.2f);
            lumiLight = lObj.AddComponent<Light>();
        }
        else
        {
            lumiLight = lightChild.GetComponent<Light>();
        }
        lumiLight.type = LightType.Point;
        lumiLight.range = 9f;
        lumiLight.intensity = 4.2f;
        lumiLight.color = new Color(1f, 0.94f, 0.78f);
        lumiLight.shadows = LightShadows.Soft;

        // 6. Foreground UI Canvas (ScreenSpaceCamera in front of Lumi, planeDistance = 4.0f)
        GameObject fgCanvasObj = new GameObject("Loading_Canvas");
        Canvas fgCanvas = fgCanvasObj.AddComponent<Canvas>();
        fgCanvas.renderMode = RenderMode.ScreenSpaceCamera;
        fgCanvas.worldCamera = mainCam;
        fgCanvas.planeDistance = 4.0f; // Directly in front of camera
        fgCanvas.sortingOrder = 10;

        CanvasScaler fgScaler = fgCanvasObj.AddComponent<CanvasScaler>();
        fgScaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        fgScaler.referenceResolution = new Vector2(1920, 1080);
        fgScaler.matchWidthOrHeight = 0.5f;
        fgCanvasObj.AddComponent<GraphicRaycaster>();

        // ------------------ LEFT SIDE: LUMI INFO & SPEECH BUBBLE ------------------
        GameObject leftSection = new GameObject("Lumi_Left_Section");
        leftSection.transform.SetParent(fgCanvasObj.transform, false);
        RectTransform leftRect = leftSection.AddComponent<RectTransform>();
        leftRect.anchorMin = new Vector2(0f, 0.12f);
        leftRect.anchorMax = new Vector2(0.40f, 0.96f);
        leftRect.sizeDelta = Vector2.zero;

        // Top Nameplate Card
        GameObject nameCardObj = new GameObject("Lumi_Nameplate");
        nameCardObj.transform.SetParent(leftSection.transform, false);
        Image nameCardImg = nameCardObj.AddComponent<Image>();
        if (cardBg != null) { nameCardImg.sprite = cardBg; nameCardImg.type = Image.Type.Sliced; }
        nameCardImg.color = new Color(0.08f, 0.11f, 0.16f, 0.95f);
        RectTransform nameCardRect = nameCardObj.GetComponent<RectTransform>();
        nameCardRect.anchorMin = new Vector2(0.12f, 0.82f);
        nameCardRect.anchorMax = new Vector2(0.88f, 0.95f);
        nameCardRect.sizeDelta = Vector2.zero;

        GameObject nameTextObj = new GameObject("Name_Text");
        nameTextObj.transform.SetParent(nameCardObj.transform, false);
        TextMeshProUGUI nameText = nameTextObj.AddComponent<TextMeshProUGUI>();
        if (thaiFont != null) nameText.font = thaiFont;
        nameText.fontSize = 25;
        nameText.alignment = TextAlignmentOptions.Center;
        nameText.text = "~ <color=#FFEAA7><b>LUMI (ลูมิ)</b></color> ~\n<size=17><color=#9AE6B4>ภูติแห่งแสงนำทาง (Guide Spirit)</color></size>";
        RectTransform nameTextRect = nameTextObj.GetComponent<RectTransform>();
        nameTextRect.anchorMin = Vector2.zero;
        nameTextRect.anchorMax = Vector2.one;
        nameTextRect.sizeDelta = Vector2.zero;

        // Speech Bubble (Anchored below Lumi)
        GameObject speechBubbleObj = new GameObject("Lumi_Speech_Bubble");
        speechBubbleObj.transform.SetParent(leftSection.transform, false);
        CanvasGroup speechBubbleGroup = speechBubbleObj.AddComponent<CanvasGroup>();
        Image speechImg = speechBubbleObj.AddComponent<Image>();
        if (cardBg != null) { speechImg.sprite = cardBg; speechImg.type = Image.Type.Sliced; }
        speechImg.color = new Color(0.09f, 0.13f, 0.19f, 0.96f);
        RectTransform speechRect = speechBubbleObj.GetComponent<RectTransform>();
        speechRect.anchorMin = new Vector2(0.06f, 0.04f);
        speechRect.anchorMax = new Vector2(0.94f, 0.28f);
        speechRect.sizeDelta = Vector2.zero;

        // Speech bubble header tag
        GameObject tagObj = new GameObject("Tag_Text");
        tagObj.transform.SetParent(speechBubbleObj.transform, false);
        TextMeshProUGUI tagText = tagObj.AddComponent<TextMeshProUGUI>();
        if (thaiFont != null) tagText.font = thaiFont;
        tagText.fontSize = 16;
        tagText.fontStyle = FontStyles.Bold;
        tagText.color = new Color(1f, 0.88f, 0.50f);
        tagText.text = "คำแนะนำจากลูมิ :";
        RectTransform tagRect = tagObj.GetComponent<RectTransform>();
        tagRect.anchorMin = new Vector2(0.06f, 0.74f);
        tagRect.anchorMax = new Vector2(0.94f, 0.96f);
        tagRect.sizeDelta = Vector2.zero;

        // Dialogue text inside speech bubble
        GameObject dialogueTextObj = new GameObject("Dialogue_Text");
        dialogueTextObj.transform.SetParent(speechBubbleObj.transform, false);
        TextMeshProUGUI dialogueTMP = dialogueTextObj.AddComponent<TextMeshProUGUI>();
        if (thaiFont != null) dialogueTMP.font = thaiFont;
        dialogueTMP.fontSize = 18;
        dialogueTMP.color = Color.white;
        dialogueTMP.textWrappingMode = TextWrappingModes.Normal;
        dialogueTMP.lineSpacing = 10f;
        dialogueTMP.alignment = TextAlignmentOptions.TopLeft;
        dialogueTMP.text = "จำปุ่มควบคุมพวกนี้ไว้ให้ดีนะเจ้ามนุษย์! ข้าจะคอยส่องทางให้เจ้าในดันเจี้ยนเอง ฮิๆ~";
        RectTransform dialogueRect = dialogueTextObj.GetComponent<RectTransform>();
        dialogueRect.anchorMin = new Vector2(0.06f, 0.08f);
        dialogueRect.anchorMax = new Vector2(0.94f, 0.74f);
        dialogueRect.sizeDelta = Vector2.zero;

        // ------------------ RIGHT SIDE: TUTORIAL CODEX ------------------
        GameObject rightSection = new GameObject("Tutorial_Right_Section");
        rightSection.transform.SetParent(fgCanvasObj.transform, false);
        RectTransform rightRect = rightSection.AddComponent<RectTransform>();
        rightRect.anchorMin = new Vector2(0.40f, 0.12f);
        rightRect.anchorMax = new Vector2(0.98f, 0.96f);
        rightRect.sizeDelta = Vector2.zero;

        // Tutorial Header Card
        GameObject headerCardObj = new GameObject("Tutorial_Header_Card");
        headerCardObj.transform.SetParent(rightSection.transform, false);
        Image headerImg = headerCardObj.AddComponent<Image>();
        if (cardBg != null) { headerImg.sprite = cardBg; headerImg.type = Image.Type.Sliced; }
        headerImg.color = new Color(0.11f, 0.15f, 0.22f, 0.98f);
        RectTransform headerRect = headerCardObj.GetComponent<RectTransform>();
        headerRect.anchorMin = new Vector2(0f, 0.89f);
        headerRect.anchorMax = new Vector2(1f, 1f);
        headerRect.sizeDelta = Vector2.zero;

        GameObject headerTextObj = new GameObject("Header_Text");
        headerTextObj.transform.SetParent(headerCardObj.transform, false);
        TextMeshProUGUI headerTMP = headerTextObj.AddComponent<TextMeshProUGUI>();
        if (thaiFont != null) headerTMP.font = thaiFont;
        headerTMP.fontSize = 23;
        headerTMP.alignment = TextAlignmentOptions.Center;
        headerTMP.text = "~ <color=#FFE8A3><b>คู่มือการผจญภัย & การควบคุม (CONTROLS & SURVIVAL)</b></color> ~\n<size=15><color=#94A3B8>เรียนรู้ทักษะและวิธีเอาชีวิตรอดก่อนก้าวเข้าสู่ดันเจี้ยนโบราณ</color></size>";
        RectTransform headerTextRect = headerTextObj.GetComponent<RectTransform>();
        headerTextRect.anchorMin = Vector2.zero;
        headerTextRect.anchorMax = Vector2.one;
        headerTextRect.sizeDelta = Vector2.zero;

        // Tutorial Rows Container
        GameObject rowsContainer = new GameObject("Tutorial_Rows_Container");
        rowsContainer.transform.SetParent(rightSection.transform, false);
        RectTransform rowsRect = rowsContainer.AddComponent<RectTransform>();
        rowsRect.anchorMin = new Vector2(0f, 0f);
        rowsRect.anchorMax = new Vector2(1f, 0.87f);
        rowsRect.sizeDelta = Vector2.zero;

        VerticalLayoutGroup vlg = rowsContainer.AddComponent<VerticalLayoutGroup>();
        vlg.spacing = 8f;
        vlg.padding = new RectOffset(4, 4, 4, 4);
        vlg.childControlWidth = true;
        vlg.childControlHeight = false;
        vlg.childForceExpandWidth = true;
        vlg.childForceExpandHeight = false;

        // Data structure for the 7 tutorial items
        var tutorialItems = new[]
        {
            new {
                ExactUserString = "W A S D เดิน",
                Category = "การเคลื่อนที่ (Movement)",
                CategoryColor = "#FFE8A3",
                Description = "<b>W A S D เดิน</b> — ใช้บังคับทิศทางตัวละครเพื่อสำรวจดันเจี้ยนและหลบหลีกสิ่งกีดขวาง",
                BadgeType = 1, // 4-key wasd
                CustomSprite = (Sprite)null
            },
            new {
                ExactUserString = "Space Bar แดช",
                Category = "พุ่งตัวหลบหลีก (Dash)",
                CategoryColor = "#FFE8A3",
                Description = "<b>Space Bar แดช</b> — พุ่งตัวเคลื่อนที่อย่างรวดเร็วเพื่อหลบหลีกการโจมตีหรือข้ามช่องว่าง",
                BadgeType = 2, // Space bar
                CustomSprite = (Sprite)null
            },
            new {
                ExactUserString = "ระวังกับดัก",
                Category = "อันตรายถึงตาย (Hazard Alert)",
                CategoryColor = "#FF6B6B",
                Description = "<b>ระวังกับดัก</b> — สังเกตหนามแหลมและหลุมพรางบนพื้น หากสัมผัสจะเสียพลังชีวิตมหาศาล",
                BadgeType = 0, // Sprite only
                CustomSprite = trapIcon
            },
            new {
                ExactUserString = "กด E เพื่อเปิดกล่อง",
                Category = "เปิดหีบสมบัติ (Chest & Loot)",
                CategoryColor = "#FFE8A3",
                Description = "<b>กด E เพื่อเปิดกล่อง</b> — เดินเข้าใกล้หีบสมบัติแล้วกดปุ่มเพื่อค้นหากุญแจและไอเทมสำคัญ",
                BadgeType = 3, // Key E + Chest
                CustomSprite = chestIcon
            },
            new {
                ExactUserString = "ใช้โพชั่นรักษาต้นไม้เล็กเพื่อ Save",
                Category = "ฟื้นฟู & บันทึกจุดเกิด (Save Point)",
                CategoryColor = "#6EE7B7",
                Description = "<b>ใช้โพชั่นรักษาต้นไม้เล็กเพื่อ Save</b> — มอบพลังโพชั่นฟื้นฟูต้นไม้เพื่อบันทึกจุดเกิดและฟื้นพลัง",
                BadgeType = 4, // Potion + Tree
                CustomSprite = treeBadge
            },
            new {
                ExactUserString = "ระวัง Oxygen หมดไม่งั้นจะแย่แน่",
                Category = "รักษาระดับออกซิเจน (Oxygen Limit)",
                CategoryColor = "#38BDF8",
                Description = "<b>ระวัง Oxygen หมดไม่งั้นจะแย่แน่</b> — หมั่นสังเกตเกจออกซิเจน อย่าปล่อยให้หมด รีบกลับมาพักใกล้ต้นไม้",
                BadgeType = 0, // Sprite only
                CustomSprite = oxygenIcon
            },
            new {
                ExactUserString = "กด W A S D ไปหาบันไดค้างเพื่อปีน",
                Category = "การปีนป่ายบันได (Ladder Climbing)",
                CategoryColor = "#FCD34D",
                Description = "<b>กด W A S D ไปหาบันไดค้างเพื่อปีน</b> — เดินเข้าหาบันไดและกดปุ่มทิศทางค้างไว้เพื่อปีนขึ้น-ลง",
                BadgeType = 5, // WASD badge + Ladder
                CustomSprite = ladderIcon
            }
        };

        for (int i = 0; i < tutorialItems.Length; i++)
        {
            var item = tutorialItems[i];
            GameObject rowObj = new GameObject($"Tutorial_Row_{i + 1}");
            rowObj.transform.SetParent(rowsContainer.transform, false);

            LayoutElement le = rowObj.AddComponent<LayoutElement>();
            le.minHeight = 82f;
            le.preferredHeight = 82f;

            Image rowBg = rowObj.AddComponent<Image>();
            if (cardBg != null) { rowBg.sprite = cardBg; rowBg.type = Image.Type.Sliced; }
            rowBg.color = new Color(0.08f, 0.11f, 0.17f, 0.94f);

            // Left Badge Container
            GameObject badgeContainer = new GameObject("Badge_Container");
            badgeContainer.transform.SetParent(rowObj.transform, false);
            RectTransform badgeRect = badgeContainer.AddComponent<RectTransform>();
            badgeRect.anchorMin = new Vector2(0f, 0f);
            badgeRect.anchorMax = new Vector2(0.20f, 1f);
            badgeRect.offsetMin = new Vector2(10f, 6f);
            badgeRect.offsetMax = new Vector2(-6f, -6f);

            // Create badges based on item.BadgeType
            BuildRowBadge(badgeContainer, item.BadgeType, item.CustomSprite, keycapBg, potionIcon, chestIcon, thaiFont);

            // Right Text Container
            GameObject textContainer = new GameObject("Text_Container");
            textContainer.transform.SetParent(rowObj.transform, false);
            RectTransform textRect = textContainer.AddComponent<RectTransform>();
            textRect.anchorMin = new Vector2(0.20f, 0f);
            textRect.anchorMax = new Vector2(1f, 1f);
            textRect.offsetMin = new Vector2(10f, 6f);
            textRect.offsetMax = new Vector2(-12f, -6f);

            TextMeshProUGUI rowTMP = textContainer.AddComponent<TextMeshProUGUI>();
            if (thaiFont != null) rowTMP.font = thaiFont;
            rowTMP.fontSize = 17f;
            rowTMP.textWrappingMode = TextWrappingModes.Normal;
            rowTMP.lineSpacing = 6f;
            rowTMP.alignment = TextAlignmentOptions.MidlineLeft;
            rowTMP.text = $"<size=19><color={item.CategoryColor}><b>{item.Category}</b></color></size>\n<color=#F1F5F9>{item.Description}</color>";
        }

        // ------------------ BOTTOM SECTION: PROGRESS BAR & PROMPT ------------------
        GameObject bottomSection = new GameObject("Loading_Bottom_Section");
        bottomSection.transform.SetParent(fgCanvasObj.transform, false);
        RectTransform bottomRect = bottomSection.AddComponent<RectTransform>();
        bottomRect.anchorMin = new Vector2(0.05f, 0.015f);
        bottomRect.anchorMax = new Vector2(0.95f, 0.115f);
        bottomRect.sizeDelta = Vector2.zero;

        // Status & Percent Row (Top of bottom bar)
        GameObject statusTextObj = new GameObject("Status_Text");
        statusTextObj.transform.SetParent(bottomSection.transform, false);
        TextMeshProUGUI statusTMP = statusTextObj.AddComponent<TextMeshProUGUI>();
        if (thaiFont != null) statusTMP.font = thaiFont;
        statusTMP.fontSize = 18f;
        statusTMP.color = new Color(0.85f, 0.90f, 0.98f);
        statusTMP.alignment = TextAlignmentOptions.Left;
        statusTMP.text = "กำลังเตรียมพร้อมและรวบรวมพลังเวทมนตร์เข้าสู่ดันเจี้ยน...";
        RectTransform statusRect = statusTextObj.GetComponent<RectTransform>();
        statusRect.anchorMin = new Vector2(0f, 0.58f);
        statusRect.anchorMax = new Vector2(0.75f, 0.98f);
        statusRect.sizeDelta = Vector2.zero;

        GameObject percentTextObj = new GameObject("Percent_Text");
        percentTextObj.transform.SetParent(bottomSection.transform, false);
        TextMeshProUGUI percentTMP = percentTextObj.AddComponent<TextMeshProUGUI>();
        if (thaiFont != null) percentTMP.font = thaiFont;
        percentTMP.fontSize = 20f;
        percentTMP.color = new Color(1f, 0.92f, 0.65f);
        percentTMP.alignment = TextAlignmentOptions.Right;
        percentTMP.text = "100%"; // Set to 100% for preview
        RectTransform percentRect = percentTextObj.GetComponent<RectTransform>();
        percentRect.anchorMin = new Vector2(0.75f, 0.58f);
        percentRect.anchorMax = new Vector2(1f, 0.98f);
        percentRect.sizeDelta = Vector2.zero;

        // Progress Bar Slider (Height 28px)
        GameObject sliderObj = new GameObject("Loading_Slider");
        sliderObj.transform.SetParent(bottomSection.transform, false);
        RectTransform sliderRect = sliderObj.AddComponent<RectTransform>();
        sliderRect.anchorMin = new Vector2(0f, 0.06f);
        sliderRect.anchorMax = new Vector2(1f, 0.54f);
        sliderRect.sizeDelta = Vector2.zero;

        Slider slider = sliderObj.AddComponent<Slider>();
        slider.minValue = 0f;
        slider.maxValue = 1f;
        slider.value = 1f; // 1.0 for preview
        slider.interactable = false;

        // Slider Background (Trough)
        GameObject sliderBg = new GameObject("Background");
        sliderBg.transform.SetParent(sliderObj.transform, false);
        Image sliderBgImg = sliderBg.AddComponent<Image>();
        if (barTrough != null) { sliderBgImg.sprite = barTrough; sliderBgImg.type = Image.Type.Sliced; }
        else sliderBgImg.color = new Color(0.12f, 0.12f, 0.15f, 0.95f);
        RectTransform sliderBgRect = sliderBg.GetComponent<RectTransform>();
        sliderBgRect.anchorMin = Vector2.zero;
        sliderBgRect.anchorMax = Vector2.one;
        sliderBgRect.sizeDelta = Vector2.zero;

        // Fill Area
        GameObject fillArea = new GameObject("Fill Area");
        fillArea.transform.SetParent(sliderObj.transform, false);
        RectTransform fillAreaRect = fillArea.AddComponent<RectTransform>();
        fillAreaRect.anchorMin = new Vector2(0.005f, 0.12f);
        fillAreaRect.anchorMax = new Vector2(0.995f, 0.88f);
        fillAreaRect.sizeDelta = Vector2.zero;

        // Fill
        GameObject fillObj = new GameObject("Fill");
        fillObj.transform.SetParent(fillArea.transform, false);
        Image fillImg = fillObj.AddComponent<Image>();
        if (barFill != null) { fillImg.sprite = barFill; fillImg.type = Image.Type.Sliced; }
        fillImg.color = new Color(0.22f, 0.90f, 0.78f, 1f); // Glowing emerald cyan
        RectTransform fillRect = fillObj.GetComponent<RectTransform>();
        fillRect.anchorMin = Vector2.zero;
        fillRect.anchorMax = Vector2.one;
        fillRect.sizeDelta = Vector2.zero;

        slider.fillRect = fillRect;
        slider.targetGraphic = fillImg;

        // Ready Prompt (Overlay on bar)
        GameObject promptObj = new GameObject("Ready_Prompt");
        promptObj.transform.SetParent(bottomSection.transform, false);
        promptObj.SetActive(false); // Initially hidden in gameplay
        RectTransform promptRect = promptObj.AddComponent<RectTransform>();
        promptRect.anchorMin = new Vector2(0f, 0f);
        promptRect.anchorMax = new Vector2(1f, 1f);
        promptRect.sizeDelta = Vector2.zero;

        GameObject promptTextObj = new GameObject("Prompt_Text");
        promptTextObj.transform.SetParent(promptObj.transform, false);
        TextMeshProUGUI promptTMP = promptTextObj.AddComponent<TextMeshProUGUI>();
        if (thaiFont != null) promptTMP.font = thaiFont;
        promptTMP.fontSize = 24f;
        promptTMP.alignment = TextAlignmentOptions.Center;
        promptTMP.text = ">> <color=#FFE082><b>กด [SPACE] หรือ [คลิกเมาส์] เพื่อเข้าสู่ดันเจี้ยน</b></color> <<";
        RectTransform promptTextRect = promptTextObj.GetComponent<RectTransform>();
        promptTextRect.anchorMin = Vector2.zero;
        promptTextRect.anchorMax = Vector2.one;
        promptTextRect.sizeDelta = Vector2.zero;

        // ------------------ BLACK FADER OVERLAY ------------------
        GameObject faderObj = new GameObject("Black_Fader");
        faderObj.transform.SetParent(fgCanvasObj.transform, false);
        CanvasGroup faderGroup = faderObj.AddComponent<CanvasGroup>();
        faderGroup.alpha = 0f; // Alpha 0 so scene is visible in editor / preview! (Awake sets to 1 in runtime)
        faderGroup.blocksRaycasts = false;
        Image faderImg = faderObj.AddComponent<Image>();
        faderImg.color = Color.black;
        RectTransform faderRect = faderObj.GetComponent<RectTransform>();
        faderRect.anchorMin = Vector2.zero;
        faderRect.anchorMax = Vector2.one;
        faderRect.sizeDelta = Vector2.zero;

        // ------------------ MANAGER COMPONENT SETUP ------------------
        GameObject mgrObj = new GameObject("LoadingManager");
        NewLoadingScreenManager mgr = mgrObj.AddComponent<NewLoadingScreenManager>();

        SerializedObject so = new SerializedObject(mgr);
        so.FindProperty("targetSceneName").stringValue = "MainGameplay";
        so.FindProperty("minimumDisplayTime").floatValue = 4.5f;
        so.FindProperty("progressBar").objectReferenceValue = slider;
        so.FindProperty("progressFillImage").objectReferenceValue = fillImg;
        so.FindProperty("statusText").objectReferenceValue = statusTMP;
        so.FindProperty("percentText").objectReferenceValue = percentTMP;
        so.FindProperty("readyPromptObj").objectReferenceValue = promptObj;
        so.FindProperty("readyPromptText").objectReferenceValue = promptTMP;
        so.FindProperty("blackFader").objectReferenceValue = faderGroup;
        so.FindProperty("lumiTransform").objectReferenceValue = lumiStage.transform;
        so.FindProperty("lumiDialogueTMP").objectReferenceValue = dialogueTMP;
        so.FindProperty("lumiSpeechBubbleGroup").objectReferenceValue = speechBubbleGroup;
        so.FindProperty("lumiHoverSpeed").floatValue = 2.4f;
        so.FindProperty("lumiHoverHeight").floatValue = 0.12f;

        if (bgmClip != null) so.FindProperty("bgmClip").objectReferenceValue = bgmClip;
        if (confirmClip != null) so.FindProperty("startConfirmSFX").objectReferenceValue = confirmClip;
        so.ApplyModifiedProperties();

        // 7. Save Scene
        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);

        // 8. Update EditorBuildSettings.scenes
        EditorBuildSettingsScene[] newScenes = new EditorBuildSettingsScene[]
        {
            new EditorBuildSettingsScene("Assets/Scenes/MainScreen.unity", true),
            new EditorBuildSettingsScene("Assets/Scenes/Cutscene.unity", true),
            new EditorBuildSettingsScene("Assets/LoadingScreenNew.unity", true),
            new EditorBuildSettingsScene("Assets/Scenes/MainGameplay.unity", true),
            new EditorBuildSettingsScene("Assets/EndScene.unity", true),
        };
        EditorBuildSettings.scenes = newScenes;

        Debug.Log("[BuildNewLoadingScreen] LoadingScreenNew scene successfully built, saved, and registered in Build Settings!");

        // 9. Capture Render Preview to disk
        RenderPreview(mainCam);
    }

    public static void RenderPreview(Camera cam)
    {
        if (cam == null) return;
        int width = 1920;
        int height = 1080;
        RenderTexture rt = new RenderTexture(width, height, 24, RenderTextureFormat.ARGB32);
        RenderTexture oldRt = cam.targetTexture;
        cam.targetTexture = rt;
        cam.Render();

        RenderTexture.active = rt;
        Texture2D tex = new Texture2D(width, height, TextureFormat.RGB24, false);
        tex.ReadPixels(new Rect(0, 0, width, height), 0, 0);
        tex.Apply();

        cam.targetTexture = oldRt;
        RenderTexture.active = null;
        Object.DestroyImmediate(rt);

        byte[] bytes = tex.EncodeToPNG();
        Object.DestroyImmediate(tex);

        string outPath = "C:/Users/Artemis/Downloads/LoadingScreen_Preview.png";
        File.WriteAllBytes(outPath, bytes);
        Debug.Log("[BuildNewLoadingScreen] Saved preview screenshot to: " + outPath);
    }

    private static void BuildRowBadge(GameObject parent, int badgeType, Sprite customSprite, Sprite keycapBg, Sprite potionIcon, Sprite chestIcon, TMP_FontAsset font)
    {
        switch (badgeType)
        {
            case 1: // WASD keys
                CreateKeycap(parent, "W", new Vector2(0.5f, 0.72f), new Vector2(30, 26), keycapBg, font);
                CreateKeycap(parent, "A", new Vector2(0.20f, 0.28f), new Vector2(30, 26), keycapBg, font);
                CreateKeycap(parent, "S", new Vector2(0.5f, 0.28f), new Vector2(30, 26), keycapBg, font);
                CreateKeycap(parent, "D", new Vector2(0.80f, 0.28f), new Vector2(30, 26), keycapBg, font);
                break;
            case 2: // Space Bar
                CreateKeycap(parent, "SPACE BAR", new Vector2(0.5f, 0.5f), new Vector2(130, 36), keycapBg, font, 14);
                break;
            case 3: // Key E + Chest
                CreateKeycap(parent, "E", new Vector2(0.28f, 0.5f), new Vector2(38, 36), keycapBg, font, 18);
                if (chestIcon != null)
                {
                    GameObject icoObj = new GameObject("Icon");
                    icoObj.transform.SetParent(parent.transform, false);
                    Image img = icoObj.AddComponent<Image>();
                    img.sprite = chestIcon;
                    img.preserveAspect = true;
                    RectTransform rt = icoObj.GetComponent<RectTransform>();
                    rt.anchorMin = new Vector2(0.55f, 0.12f);
                    rt.anchorMax = new Vector2(0.95f, 0.88f);
                    rt.sizeDelta = Vector2.zero;
                }
                break;
            case 4: // Potion + Tree
                if (potionIcon != null)
                {
                    GameObject pObj = new GameObject("PotionIcon");
                    pObj.transform.SetParent(parent.transform, false);
                    Image pImg = pObj.AddComponent<Image>();
                    pImg.sprite = potionIcon;
                    pImg.preserveAspect = true;
                    RectTransform prt = pObj.GetComponent<RectTransform>();
                    prt.anchorMin = new Vector2(0.08f, 0.15f);
                    prt.anchorMax = new Vector2(0.48f, 0.85f);
                    prt.sizeDelta = Vector2.zero;
                }
                if (customSprite != null)
                {
                    GameObject tObj = new GameObject("TreeIcon");
                    tObj.transform.SetParent(parent.transform, false);
                    Image tImg = tObj.AddComponent<Image>();
                    tImg.sprite = customSprite;
                    tImg.preserveAspect = true;
                    RectTransform trt = tObj.GetComponent<RectTransform>();
                    trt.anchorMin = new Vector2(0.52f, 0.12f);
                    trt.anchorMax = new Vector2(0.95f, 0.88f);
                    trt.sizeDelta = Vector2.zero;
                }
                break;
            case 5: // WASD badge + Ladder
                CreateKeycap(parent, "WASD", new Vector2(0.26f, 0.5f), new Vector2(58, 36), keycapBg, font, 14);
                if (customSprite != null)
                {
                    GameObject lObj = new GameObject("LadderIcon");
                    lObj.transform.SetParent(parent.transform, false);
                    Image lImg = lObj.AddComponent<Image>();
                    lImg.sprite = customSprite;
                    lImg.preserveAspect = true;
                    RectTransform lrt = lObj.GetComponent<RectTransform>();
                    lrt.anchorMin = new Vector2(0.58f, 0.12f);
                    lrt.anchorMax = new Vector2(0.94f, 0.88f);
                    lrt.sizeDelta = Vector2.zero;
                }
                break;
            default: // Sprite only (e.g. Trap, Oxygen)
                if (customSprite != null)
                {
                    GameObject icoObj = new GameObject("SpriteIcon");
                    icoObj.transform.SetParent(parent.transform, false);
                    Image img = icoObj.AddComponent<Image>();
                    img.sprite = customSprite;
                    img.preserveAspect = true;
                    RectTransform rt = icoObj.GetComponent<RectTransform>();
                    rt.anchorMin = new Vector2(0.18f, 0.08f);
                    rt.anchorMax = new Vector2(0.82f, 0.92f);
                    rt.sizeDelta = Vector2.zero;
                }
                break;
        }
    }

    private static void CreateKeycap(GameObject parent, string label, Vector2 normalizedPos, Vector2 size, Sprite keycapBg, TMP_FontAsset font, float fontSize = 16)
    {
        GameObject keyObj = new GameObject($"Key_{label}");
        keyObj.transform.SetParent(parent.transform, false);

        Image img = keyObj.AddComponent<Image>();
        if (keycapBg != null) { img.sprite = keycapBg; img.type = Image.Type.Sliced; }
        else img.color = new Color(0.2f, 0.25f, 0.32f);

        RectTransform rt = keyObj.GetComponent<RectTransform>();
        rt.anchorMin = normalizedPos;
        rt.anchorMax = normalizedPos;
        rt.sizeDelta = size;
        rt.anchoredPosition = Vector2.zero;

        GameObject textObj = new GameObject("Label");
        textObj.transform.SetParent(keyObj.transform, false);
        TextMeshProUGUI tmp = textObj.AddComponent<TextMeshProUGUI>();
        if (font != null) tmp.font = font;
        tmp.fontSize = fontSize;
        tmp.fontStyle = FontStyles.Bold;
        tmp.color = new Color(1f, 0.92f, 0.75f);
        tmp.alignment = TextAlignmentOptions.Center;
        tmp.text = label;

        RectTransform textRt = textObj.GetComponent<RectTransform>();
        textRt.anchorMin = Vector2.zero;
        textRt.anchorMax = Vector2.one;
        textRt.sizeDelta = Vector2.zero;
    }
}
