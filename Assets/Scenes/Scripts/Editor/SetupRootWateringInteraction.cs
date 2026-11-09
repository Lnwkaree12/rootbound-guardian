using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public static class SetupRootWateringInteraction
{
    [MenuItem("Rootbound/Setup Root Watering Interaction")]
    public static void Execute()
    {
        string scenePath = "Assets/Scenes/MainGameplay.unity";
        var scene = EditorSceneManager.OpenScene(scenePath, OpenSceneMode.Single);

        GameObject rootObj = GameObject.Find("root");
        if (rootObj == null)
        {
            Debug.LogError("[SetupRootWateringInteraction] 'root' GameObject not found in MainGameplay!");
            return;
        }

        // 1. Remove previous interaction point if any to ensure clean build
        Transform oldPoint = rootObj.transform.Find("Root_Interaction_Point");
        if (oldPoint != null) Object.DestroyImmediate(oldPoint.gameObject);

        // 2. Create Root_Interaction_Point
        GameObject interactPoint = new GameObject("Root_Interaction_Point");
        interactPoint.transform.SetParent(rootObj.transform, true);
        interactPoint.transform.position = new Vector3(832f, 10.0f, -37.0f);
        interactPoint.transform.rotation = Quaternion.identity;

        SphereCollider sc = interactPoint.AddComponent<SphereCollider>();
        sc.isTrigger = true;
        sc.radius = 8.5f;

        WorldTreeRootInteraction interaction = interactPoint.AddComponent<WorldTreeRootInteraction>();

        // 3. Load Fonts & Sprites
        TMP_FontAsset thaiFont = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>("Assets/Fonts/Itim_Thai_SDF.asset");
        if (thaiFont == null) thaiFont = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>("Assets/Fonts/Itim_TMP_Dynamic.asset");

        Sprite cardBg = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Image/UI/Tutorial_Card_Bg.png");
        Sprite keycapBg = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Image/UI/Keycap_Bg.png");
        Sprite treeBadge = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Image/UI/Tree_Badge_Icon.png");
        if (treeBadge == null) treeBadge = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Image/UI/Cute_Sprout_Badge.png");
        Sprite potionIcon = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Image/UI/HUD_Potion_Blue.png");

        GameObject waterVFXPrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/VFX/Water_VFX/Watering_Can_Farm_Splash_VFX.prefab");
        if (waterVFXPrefab == null) waterVFXPrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/VFX/Water_VFX/Water_Splash_Impact_Burst_VFX.prefab");

        AudioClip waterClip = AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Audio/SFX/whoosh-effect.mp3");
        AudioClip keyClip = AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Audio/SFX/Key.mp3");

        // 4. Create Floating WorldSpace Canvas
        GameObject canvasObj = new GameObject("Root_Prompt_Canvas");
        canvasObj.transform.SetParent(interactPoint.transform, false);
        canvasObj.transform.localPosition = new Vector3(0f, 3.2f, 0f); // 3.2m above floor
        canvasObj.transform.localRotation = Quaternion.identity;
        canvasObj.transform.localScale = new Vector3(0.012f, 0.012f, 0.012f);

        Canvas canvas = canvasObj.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.WorldSpace;
        canvas.sortingOrder = 50;

        CanvasScaler scaler = canvasObj.AddComponent<CanvasScaler>();
        scaler.dynamicPixelsPerUnit = 3f;

        CanvasGroup cg = canvasObj.AddComponent<CanvasGroup>();
        cg.alpha = 0f;
        cg.blocksRaycasts = false;

        RectTransform canvasRect = canvasObj.GetComponent<RectTransform>();
        canvasRect.sizeDelta = new Vector2(380f, 80f);

        // Background Image
        GameObject bgObj = new GameObject("Background");
        bgObj.transform.SetParent(canvasObj.transform, false);
        Image bgImg = bgObj.AddComponent<Image>();
        if (cardBg != null) { bgImg.sprite = cardBg; bgImg.type = Image.Type.Sliced; }
        bgImg.color = new Color(0.06f, 0.09f, 0.14f, 0.95f);
        RectTransform bgRect = bgObj.GetComponent<RectTransform>();
        bgRect.anchorMin = Vector2.zero;
        bgRect.anchorMax = Vector2.one;
        bgRect.sizeDelta = Vector2.zero;

        // Keycap [E] Badge
        GameObject keyObj = new GameObject("Keycap_E");
        keyObj.transform.SetParent(canvasObj.transform, false);
        Image keyImg = keyObj.AddComponent<Image>();
        if (keycapBg != null) { keyImg.sprite = keycapBg; keyImg.type = Image.Type.Sliced; }
        else keyImg.color = new Color(0.2f, 0.25f, 0.35f);
        RectTransform keyRect = keyObj.GetComponent<RectTransform>();
        keyRect.anchorMin = new Vector2(0f, 0.5f);
        keyRect.anchorMax = new Vector2(0f, 0.5f);
        keyRect.sizeDelta = new Vector2(46f, 46f);
        keyRect.anchoredPosition = new Vector2(38f, 0f);

        GameObject keyTextObj = new GameObject("Label");
        keyTextObj.transform.SetParent(keyObj.transform, false);
        TextMeshProUGUI keyTMP = keyTextObj.AddComponent<TextMeshProUGUI>();
        if (thaiFont != null) keyTMP.font = thaiFont;
        keyTMP.fontSize = 22f;
        keyTMP.fontStyle = FontStyles.Bold;
        keyTMP.color = new Color(1f, 0.92f, 0.72f);
        keyTMP.alignment = TextAlignmentOptions.Center;
        keyTMP.text = "E";
        RectTransform keyTextRect = keyTextObj.GetComponent<RectTransform>();
        keyTextRect.anchorMin = Vector2.zero;
        keyTextRect.anchorMax = Vector2.one;
        keyTextRect.sizeDelta = Vector2.zero;

        // Sprout / Water Badge Icon
        GameObject sproutObj = new GameObject("SproutBadge");
        sproutObj.transform.SetParent(canvasObj.transform, false);
        Image sproutImg = sproutObj.AddComponent<Image>();
        if (treeBadge != null) sproutImg.sprite = treeBadge;
        else if (potionIcon != null) sproutImg.sprite = potionIcon;
        sproutImg.preserveAspect = true;
        RectTransform sproutRect = sproutObj.GetComponent<RectTransform>();
        sproutRect.anchorMin = new Vector2(0f, 0.5f);
        sproutRect.anchorMax = new Vector2(0f, 0.5f);
        sproutRect.sizeDelta = new Vector2(50f, 50f);
        sproutRect.anchoredPosition = new Vector2(96f, 0f);

        // Prompt Text: "กด E รดน้ำต้นไม้"
        GameObject textObj = new GameObject("PromptText");
        textObj.transform.SetParent(canvasObj.transform, false);
        TextMeshProUGUI promptTMP = textObj.AddComponent<TextMeshProUGUI>();
        if (thaiFont != null) promptTMP.font = thaiFont;
        promptTMP.fontSize = 26f;
        promptTMP.fontStyle = FontStyles.Bold;
        promptTMP.color = new Color(1f, 0.93f, 0.76f);
        promptTMP.alignment = TextAlignmentOptions.MidlineLeft;
        promptTMP.text = "กด E รดน้ำต้นไม้";
        RectTransform textRect = textObj.GetComponent<RectTransform>();
        textRect.anchorMin = new Vector2(0f, 0f);
        textRect.anchorMax = new Vector2(1f, 1f);
        textRect.offsetMin = new Vector2(136f, 4f);
        textRect.offsetMax = new Vector2(-16f, -4f);

        // 5. Setup Camera & VFX References
        Transform rootsCamT = rootObj.transform.Find("Roots_Camera");
        Camera rootsCamera = null;
        if (rootsCamT != null)
        {
            rootsCamera = rootsCamT.GetComponent<Camera>();
            rootsCamT.gameObject.SetActive(false); // Initially inactive until watered
        }

        GameObject vfxSpawnObj = new GameObject("Water_Spawn_Point");
        vfxSpawnObj.transform.SetParent(interactPoint.transform, false);
        vfxSpawnObj.transform.localPosition = new Vector3(6f, 1.5f, 0f); // Right on the root surface

        // 6. Serialize fields on WorldTreeRootInteraction
        SerializedObject so = new SerializedObject(interaction);
        so.FindProperty("interactRadius").floatValue = 8.5f;
        so.FindProperty("interactKey").enumValueIndex = (int)KeyCode.E;
        so.FindProperty("allowKeyFBackup").boolValue = true;
        so.FindProperty("promptCanvasObject").objectReferenceValue = canvasObj;
        so.FindProperty("promptCanvasGroup").objectReferenceValue = cg;
        so.FindProperty("promptTextTMP").objectReferenceValue = promptTMP;
        so.FindProperty("promptKeyIcon").objectReferenceValue = keyImg;
        so.FindProperty("promptSproutIcon").objectReferenceValue = sproutImg;
        so.FindProperty("promptBg").objectReferenceValue = bgImg;
        if (rootsCamera != null) so.FindProperty("rootsCamera").objectReferenceValue = rootsCamera;
        if (waterVFXPrefab != null) so.FindProperty("waterSplashVFXPrefab").objectReferenceValue = waterVFXPrefab;
        so.FindProperty("waterSplashSpawnPoint").objectReferenceValue = vfxSpawnObj.transform;
        if (waterClip != null) so.FindProperty("waterSFX").objectReferenceValue = waterClip;
        if (keyClip != null) so.FindProperty("successChimeSFX").objectReferenceValue = keyClip;
        so.FindProperty("transitionToEndScene").boolValue = true;
        so.FindProperty("endSceneName").stringValue = "EndScene";
        so.FindProperty("cinematicDelay").floatValue = 3.2f;
        so.ApplyModifiedProperties();

        // 7. Save Scene
        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
        Debug.Log("[SetupRootWateringInteraction] Successfully set up 'กด E รดน้ำต้นไม้' interaction on 'root'!");
    }
}

