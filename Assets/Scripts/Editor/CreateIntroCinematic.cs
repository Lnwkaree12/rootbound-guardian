#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

[InitializeOnLoad]
public class CreateIntroCinematic
{
    static CreateIntroCinematic()
    {
        // Automatically run to configure sprites and create the prefab on compile
        EditorApplication.delayCall += EnsureIntroCinematicReady;
    }

    [MenuItem("Tools/SproutScout/Create Intro Cinematic Prefab")]
    public static void ForceCreateIntro()
    {
        CreateIntro(true);
    }

    [MenuItem("Tools/SproutScout/Create Original Intro Cinematic Prefab")]
    public static void ForceCreateOriginalIntro()
    {
        CreateOriginalIntro(true);
    }

    private static void EnsureIntroCinematicReady()
    {
        CreateIntro(false);
        CreateOriginalIntro(false);
    }

    private static void CreateIntro(bool forceOverwrite)
    {
        string scene1Path = "Assets/Image/Intro/Intro_Scene_1.jpg";
        string scene2Path = "Assets/Image/Intro/Intro_Scene_2.jpg";
        string scene3Path = "Assets/Image/Intro/Intro_Scene_3.jpg";
        string scene4Path = "Assets/Image/Intro/Intro_Scene_4.jpg";
        string scene5Path = "Assets/Image/Intro/Intro_Scene_5.jpg";
        string prefabPath = "Assets/Prefabs/Intro_Cinematic_Canvas.prefab";

        if (!forceOverwrite && AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath) != null)
        {
            return;
        }

        Debug.Log("[CreateIntroCinematic] Importing slides and creating Cinematic Prefab...");

        // 1. Configure all slides as Sprites (2D and UI)
        ConfigureAsSprite(scene1Path);
        ConfigureAsSprite(scene2Path);
        ConfigureAsSprite(scene3Path);
        ConfigureAsSprite(scene4Path);
        ConfigureAsSprite(scene5Path);
        AssetDatabase.Refresh();

        // Load the sprites
        Sprite s1 = AssetDatabase.LoadAssetAtPath<Sprite>(scene1Path);
        Sprite s2 = AssetDatabase.LoadAssetAtPath<Sprite>(scene2Path);
        Sprite s3 = AssetDatabase.LoadAssetAtPath<Sprite>(scene3Path);
        Sprite s4 = AssetDatabase.LoadAssetAtPath<Sprite>(scene4Path);
        Sprite s5 = AssetDatabase.LoadAssetAtPath<Sprite>(scene5Path);

        if (s1 == null || s2 == null || s3 == null || s4 == null || s5 == null)
        {
            Debug.LogWarning("[CreateIntroCinematic] One or more slide sprites could not be loaded yet. They will be linked once Unity finishes importing them.");
        }

        // 2. Create UI Hierarchy for the Intro Cinematic
        GameObject canvasGO = new GameObject("Intro Cinematic Canvas");
        Canvas canvas = canvasGO.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        
        // Configure CanvasScaler to scale with screen size (responsive UI)
        CanvasScaler scaler = canvasGO.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);
        scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
        scaler.matchWidthOrHeight = 0.5f;

        canvasGO.AddComponent<GraphicRaycaster>();

        // Add CanvasGroup for fading the whole system
        CanvasGroup fadeGroup = canvasGO.AddComponent<CanvasGroup>();
        fadeGroup.alpha = 1f; // Set to 1f so it is visible and editable in the Scene View

        // Background Black Panel
        GameObject bgGO = new GameObject("BlackBackground");
        RectTransform bgRect = bgGO.AddComponent<RectTransform>();
        bgRect.SetParent(canvasGO.transform, false); // Crucial: false to prevent scaling bugs
        Image bgImg = bgGO.AddComponent<Image>();
        bgImg.color = Color.black;
        bgRect.anchorMin = Vector2.zero;
        bgRect.anchorMax = Vector2.one;
        bgRect.sizeDelta = Vector2.zero;

        // Slide Image (Aspect ratio fitted)
        GameObject imgGO = new GameObject("SlideImage");
        RectTransform imgRect = imgGO.AddComponent<RectTransform>();
        imgRect.SetParent(canvasGO.transform, false);
        Image slideImgComponent = imgGO.AddComponent<Image>();
        if (s1 != null) slideImgComponent.sprite = s1;
        slideImgComponent.preserveAspect = true;
        imgRect.anchorMin = new Vector2(0.1f, 0.25f);
        imgRect.anchorMax = new Vector2(0.9f, 0.9f);
        imgRect.sizeDelta = Vector2.zero;

        // Dialogue / Narrative Text Container
        GameObject boxGO = new GameObject("DialogueBox");
        RectTransform boxRect = boxGO.AddComponent<RectTransform>();
        boxRect.SetParent(canvasGO.transform, false);
        Image boxImg = boxGO.AddComponent<Image>();
        boxImg.color = new Color(0.12f, 0.08f, 0.06f, 0.85f); // Soft wood brown transparent backing
        boxRect.anchorMin = new Vector2(0.1f, 0.05f);
        boxRect.anchorMax = new Vector2(0.9f, 0.22f);
        boxRect.sizeDelta = Vector2.zero;

        // Text Component
        GameObject textGO = new GameObject("NarrativeText");
        RectTransform textRect = textGO.AddComponent<RectTransform>();
        textRect.SetParent(boxRect, false);
        Text dialogueText = textGO.AddComponent<Text>();
        
        // Load custom font Itim-Regular.ttf with legacy fallbacks
        Font customFont = AssetDatabase.LoadAssetAtPath<Font>("Assets/Itim-Regular.ttf");
        if (customFont == null)
        {
            try { customFont = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf"); }
            catch { try { customFont = Resources.GetBuiltinResource<Font>("Arial.ttf"); } catch { } }
        }
        dialogueText.font = customFont;
        dialogueText.fontSize = 28; // Slightly larger for better readability on 1080p
        dialogueText.color = Color.white;
        dialogueText.alignment = TextAnchor.MiddleLeft;
        textRect.anchorMin = new Vector2(0.04f, 0.1f);
        textRect.anchorMax = new Vector2(0.96f, 0.9f);
        textRect.sizeDelta = Vector2.zero;

        // Skip / Next Prompt Text
        GameObject promptGO = new GameObject("NextPrompt");
        RectTransform promptRect = promptGO.AddComponent<RectTransform>();
        promptRect.SetParent(boxRect, false);
        Text promptText = promptGO.AddComponent<Text>();
        promptText.font = dialogueText.font;
        promptText.fontSize = 18;
        promptText.color = new Color(0.85f, 0.65f, 0.25f); // Gold color
        promptText.text = "กด [Space] หรือคลิกเพื่อไปต่อ...";
        promptText.alignment = TextAnchor.LowerRight;
        promptRect.anchorMin = new Vector2(0.5f, 0.05f);
        promptRect.anchorMax = new Vector2(0.98f, 0.4f);
        promptRect.sizeDelta = Vector2.zero;

        // 3. Attach and configure the Manager Script
        IntroCinematicManager manager = canvasGO.AddComponent<IntroCinematicManager>();
        manager.slideImage = slideImgComponent;
        manager.dialogueText = dialogueText;
        manager.fadeGroup = fadeGroup;
        manager.pressSpacePrompt = promptGO;
        manager.nextSceneName = "map"; // Target gameplay scene name

        // Configure 5 slides data
        manager.slides = new IntroCinematicManager.Slide[5];
        
        manager.slides[0].sprite = s1;
        manager.slides[0].narrativeText = "ป่าเวทมนตร์อันเขียวขะอุ่มและอุดมสมบูรณ์ ฝูงนกโบยบินอย่างเป็นอิสระภายใต้ผืนฟ้าครามที่แจ่มใส ทุกชีวิตดำเนินไปอย่างสงบสุข...";
        
        manager.slides[1].sprite = s2;
        manager.slides[1].narrativeText = "แต่แล้วความมืดมิดก็คืบคลานเข้ามา... ท้องฟ้ากลายเป็นสีเทาหม่น ลมพายุพัดพาเอาความแห้งแล้งมาเยือน ต้นไม้ใหญ่น้อยเหี่ยวเฉากลายเป็นสีน้ำตาลแห้งกรอบ...";
        
        manager.slides[2].sprite = s3;
        manager.slides[2].narrativeText = "ณ ใจกลางป่า ต้นไม้โลกศักดิ์สิทธิ์อันสูงใหญ่ตระหง่าน แม้จะยังคงมีออร่าพลังสีเขียวเรืองแสงปกป้องอยู่รอบๆ ทว่ารากและกิ่งก้านของมันก็กำลังค่อยๆ แห้งเหี่ยวและตายลงอย่างรวดเร็ว...";
        
        manager.slides[3].sprite = s4;
        manager.slides[3].narrativeText = "เด็กสาวเอลฟ์โบราณผู้พิทักษ์ป่า คุกเข่าลงต่อหน้าเอลฟ์ผู้เฒ่าเพื่อรับคำสั่งภารกิจสุดท้าย... 'จงเดินทางลงสู่รากลึกใต้พิภพ และช่วยชีวิตต้นไม้โลกก่อนที่ป่าแห่งนี้จะสูญสิ้น!'";
        
        manager.slides[4].sprite = s5;
        manager.slides[4].narrativeText = "สายลมโบกสะบัด... เด็กสาวเอลฟ์มองไปยังอุโมงค์ถ้ำอันมืดมิดที่เป็นทางลงสู่รากแก้วของต้นไม้โลก เธอสูดหายใจเข้าลึกๆ แล้วเริ่มก้าวเท้าเดินมุ่งหน้าเข้าสู่ดันเจี้ยนหินแห่งการทดสอบ...";

        // 4. Save as Prefab
        string finalPath = AssetDatabase.GenerateUniqueAssetPath(prefabPath);
        if (!forceOverwrite && AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath) == null)
        {
            finalPath = prefabPath;
        }
        else if (forceOverwrite)
        {
            finalPath = prefabPath;
        }

        PrefabUtility.SaveAsPrefabAssetAndConnect(canvasGO, finalPath, InteractionMode.AutomatedAction);
        Object.DestroyImmediate(canvasGO);

        AssetDatabase.Refresh();
        Debug.Log($"[CreateIntroCinematic] Successfully created Intro Cinematic Canvas Prefab at: {finalPath}");
    }

    private static void CreateOriginalIntro(bool forceOverwrite)
    {
        string scene1Path = "Assets/Image/Intro/Original_Intro_Scene_1.jpg";
        string scene2Path = "Assets/Image/Intro/Original_Intro_Scene_2.jpg";
        string scene3Path = "Assets/Image/Intro/Original_Intro_Scene_3.jpg";
        string scene4Path = "Assets/Image/Intro/Original_Intro_Scene_4.jpg";
        string prefabPath = "Assets/Prefabs/Original_Intro_Cinematic_Canvas.prefab";

        if (!forceOverwrite && AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath) != null)
        {
            return;
        }

        Debug.Log("[CreateIntroCinematic] Creating Original Intro Cinematic Prefab...");

        ConfigureAsSprite(scene1Path);
        ConfigureAsSprite(scene2Path);
        ConfigureAsSprite(scene3Path);
        ConfigureAsSprite(scene4Path);
        AssetDatabase.Refresh();

        Sprite s1 = AssetDatabase.LoadAssetAtPath<Sprite>(scene1Path);
        Sprite s2 = AssetDatabase.LoadAssetAtPath<Sprite>(scene2Path);
        Sprite s3 = AssetDatabase.LoadAssetAtPath<Sprite>(scene3Path);
        Sprite s4 = AssetDatabase.LoadAssetAtPath<Sprite>(scene4Path);

        if (s1 == null || s2 == null || s3 == null || s4 == null)
        {
            Debug.LogWarning("[CreateIntroCinematic] One or more original slide sprites could not be loaded yet.");
        }

        GameObject canvasGO = new GameObject("Original Intro Cinematic Canvas");
        Canvas canvas = canvasGO.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        
        CanvasScaler scaler = canvasGO.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);
        scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
        scaler.matchWidthOrHeight = 0.5f;

        canvasGO.AddComponent<GraphicRaycaster>();
        CanvasGroup fadeGroup = canvasGO.AddComponent<CanvasGroup>();
        fadeGroup.alpha = 1f;

        GameObject bgGO = new GameObject("BlackBackground");
        RectTransform bgRect = bgGO.AddComponent<RectTransform>();
        bgRect.SetParent(canvasGO.transform, false);
        Image bgImg = bgGO.AddComponent<Image>();
        bgImg.color = Color.black;
        bgRect.anchorMin = Vector2.zero;
        bgRect.anchorMax = Vector2.one;
        bgRect.sizeDelta = Vector2.zero;

        GameObject imgGO = new GameObject("SlideImage");
        RectTransform imgRect = imgGO.AddComponent<RectTransform>();
        imgRect.SetParent(canvasGO.transform, false);
        Image slideImgComponent = imgGO.AddComponent<Image>();
        if (s1 != null) slideImgComponent.sprite = s1;
        slideImgComponent.preserveAspect = true;
        imgRect.anchorMin = new Vector2(0.1f, 0.25f);
        imgRect.anchorMax = new Vector2(0.9f, 0.9f);
        imgRect.sizeDelta = Vector2.zero;

        GameObject boxGO = new GameObject("DialogueBox");
        RectTransform boxRect = boxGO.AddComponent<RectTransform>();
        boxRect.SetParent(canvasGO.transform, false);
        Image boxImg = boxGO.AddComponent<Image>();
        boxImg.color = new Color(0.12f, 0.08f, 0.06f, 0.85f);
        boxRect.anchorMin = new Vector2(0.1f, 0.05f);
        boxRect.anchorMax = new Vector2(0.9f, 0.22f);
        boxRect.sizeDelta = Vector2.zero;

        GameObject textGO = new GameObject("NarrativeText");
        RectTransform textRect = textGO.AddComponent<RectTransform>();
        textRect.SetParent(boxRect, false);
        Text dialogueText = textGO.AddComponent<Text>();
        
        Font customFont = AssetDatabase.LoadAssetAtPath<Font>("Assets/Itim-Regular.ttf");
        if (customFont == null)
        {
            try { customFont = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf"); }
            catch { try { customFont = Resources.GetBuiltinResource<Font>("Arial.ttf"); } catch { } }
        }
        dialogueText.font = customFont;
        dialogueText.fontSize = 28;
        dialogueText.color = Color.white;
        dialogueText.alignment = TextAnchor.MiddleLeft;
        textRect.anchorMin = new Vector2(0.04f, 0.1f);
        textRect.anchorMax = new Vector2(0.96f, 0.9f);
        textRect.sizeDelta = Vector2.zero;

        GameObject promptGO = new GameObject("NextPrompt");
        RectTransform promptRect = promptGO.AddComponent<RectTransform>();
        promptRect.SetParent(boxRect, false);
        Text promptText = promptGO.AddComponent<Text>();
        promptText.font = dialogueText.font;
        promptText.fontSize = 18;
        promptText.color = new Color(0.85f, 0.65f, 0.25f);
        promptText.text = "กด [Space] หรือคลิกเพื่อไปต่อ...";
        promptText.alignment = TextAnchor.LowerRight;
        promptRect.anchorMin = new Vector2(0.5f, 0.05f);
        promptRect.anchorMax = new Vector2(0.98f, 0.4f);
        promptRect.sizeDelta = Vector2.zero;

        IntroCinematicManager manager = canvasGO.AddComponent<IntroCinematicManager>();
        manager.slideImage = slideImgComponent;
        manager.dialogueText = dialogueText;
        manager.fadeGroup = fadeGroup;
        manager.pressSpacePrompt = promptGO;
        manager.nextSceneName = "map";

        // Original 4 slides data
        manager.slides = new IntroCinematicManager.Slide[4];
        
        manager.slides[0].sprite = s1;
        manager.slides[0].narrativeText = "ท่ามกลางค่ำคืนอันเงียบสงัด... เด็กสาวเอลฟ์โบราณผู้พิทักษ์ป่าได้นั่งคุกเข่าวิงวอนอธิษฐานต่อดวงดาวบนฟากฟ้า หวังให้มีปาฏิหาริย์เกิดขึ้นกับโลกใบนี้เพื่อต่อสู้กับความชั่วร้าย...";
        
        manager.slides[1].sprite = s2;
        manager.slides[1].narrativeText = "ทันใดนั้น! ลำแสงเจิดจรัสพุ่งวาบผ่านหมู่เมฆ ดาวตกสีทองดวงใหญ่ตกลงมายังเบื้องหน้าของเธออย่างรวดเร็ว เกิดแรงสั่นสะเทือนและละอองเวทมนตร์กระจายไปทั่วทุ่งหญ้า...";
        
        manager.slides[2].sprite = s3;
        manager.slides[2].narrativeText = "แต่ทว่า... สิ่งที่ปรากฏต่อหน้าเธอกลับไม่ใช่ก้อนอุกกาบาตธรรมดา แต่เป็น 'ภูตแห่งแสง' ลอยตัวส่งยิ้มอย่างอ่อนโยน แท้จริงแล้วมันคือผู้พิทักษ์ที่ตกลงมาเพื่อช่วยปกป้องโลกจากเหล่ามอนสเตอร์!";
        
        manager.slides[3].sprite = s4;
        manager.slides[3].narrativeText = "ภูตแห่งแสงได้ยื่นเข็มทิศโบราณให้ พร้อมมอบภารกิจสำคัญ... 'จงเดินทางเข้าสู่ดันเจี้ยนหินโบราณแห่งนั้น และตามหากุญแจเวทมนตร์เพื่อขับไล่ความมืดมิด!' การผจญภัยของเธอได้เริ่มต้นขึ้นแล้ว...";

        string finalPath = prefabPath;
        PrefabUtility.SaveAsPrefabAssetAndConnect(canvasGO, finalPath, InteractionMode.AutomatedAction);
        Object.DestroyImmediate(canvasGO);

        AssetDatabase.Refresh();
        Debug.Log($"[CreateIntroCinematic] Successfully created Original Intro Cinematic Canvas Prefab at: {finalPath}");
    }

    private static void ConfigureAsSprite(string assetPath)
    {
        TextureImporter importer = AssetImporter.GetAtPath(assetPath) as TextureImporter;
        if (importer != null && importer.textureType != TextureImporterType.Sprite)
        {
            importer.textureType = TextureImporterType.Sprite;
            importer.spriteImportMode = SpriteImportMode.Single;
            importer.mipmapEnabled = false;
            importer.filterMode = FilterMode.Bilinear;
            importer.alphaIsTransparency = true;
            importer.SaveAndReimport();
        }
    }
}
#endif
