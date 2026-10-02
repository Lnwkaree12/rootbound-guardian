#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

public class CreateHUDAdditions
{
    [MenuItem("Tools/SproutScout/Setup Dash and Quest UI")]
    public static void SetupAll()
    {
        BuildHUDAdditions(true);
    }

    [MenuItem("Tools/SproutScout/Create Dash Cooldown UI")]
    public static void SetupDashUI()
    {
        BuildHUDAdditions(true);
    }

    [MenuItem("Tools/SproutScout/Create Redesigned Quest UI")]
    public static void SetupQuestUI()
    {
        BuildHUDAdditions(true);
    }

    public static void BuildHUDAdditions(bool forceOverwrite)
    {
        string hudPrefabPath = "Assets/Prefabs/HUD_Canvas.prefab";
        GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(hudPrefabPath);
        if (prefab == null)
        {
            Debug.LogError("[CreateHUDAdditions] HUD_Canvas.prefab not found at " + hudPrefabPath);
            return;
        }

        Debug.Log("[CreateHUDAdditions] Updating HUD_Canvas prefab with Dash Cooldown UI and Redesigned Quest UI...");
        GameObject prefabRoot = PrefabUtility.LoadPrefabContents(hudPrefabPath);

        Font font = LoadItimFont();

        // 1. Build or replace Redesigned Quest UI
        BuildQuestUI(prefabRoot, font);

        // 2. Build or replace Dash Cooldown UI
        BuildDashUI(prefabRoot, font);

        // Save Prefab
        PrefabUtility.SaveAsPrefabAsset(prefabRoot, hudPrefabPath);
        PrefabUtility.UnloadPrefabContents(prefabRoot);

        AssetDatabase.Refresh();

        // 3. Synchronize open scene instances
        var sceneCanvases = Object.FindObjectsByType<Canvas>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        foreach (Canvas c in sceneCanvases)
        {
            if (c.gameObject.name.StartsWith("HUD Canvas") || c.gameObject.name.StartsWith("HUD_Canvas"))
            {
                if (PrefabUtility.IsPartOfPrefabInstance(c.gameObject))
                {
                    PrefabUtility.RevertPrefabInstance(c.gameObject, InteractionMode.AutomatedAction);
                    Debug.Log("[CreateHUDAdditions] Synchronized HUD Canvas instance: " + c.gameObject.name);
                }
                else
                {
                    // If it's the non-prefab HUD Canvas in scene, also add the Dash Cooldown widget so the player gets it in all views
                    var existingDash = c.transform.Find("DashCooldownUI");
                    if (existingDash == null)
                    {
                        BuildDashUI(c.gameObject, font);
                        Debug.Log("[CreateHUDAdditions] Added Dash Cooldown UI to scene canvas: " + c.gameObject.name);
                    }
                }
            }
        }

        Debug.Log("[CreateHUDAdditions] Successfully integrated Dash Cooldown UI and Redesigned Quest UI!");
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

    private static void BuildQuestUI(GameObject root, Font font)
    {
        Transform oldQuest = root.transform.Find("QuestPanel");
        if (oldQuest != null)
        {
            Object.DestroyImmediate(oldQuest.gameObject);
        }

        Sprite bannerSprite = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Image/UI/Quest_Banner.png");
        Sprite badgeSprite = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Image/UI/Quest_Badge.png");

        // Quest Panel Root
        GameObject questGO = new GameObject("QuestPanel");
        questGO.transform.SetParent(root.transform, false);

        var bgImg = questGO.AddComponent<Image>();
        if (bannerSprite != null)
        {
            bgImg.sprite = bannerSprite;
            bgImg.type = Image.Type.Simple;
        }
        else
        {
            bgImg.color = new Color(0.12f, 0.10f, 0.12f, 0.90f);
        }

        RectTransform questRect = questGO.GetComponent<RectTransform>();
        questRect.anchorMin = new Vector2(0f, 0.5f); // Left-middle anchor
        questRect.anchorMax = new Vector2(0f, 0.5f);
        questRect.pivot = new Vector2(0f, 0.5f);
        questRect.anchoredPosition = new Vector2(25f, 40f);
        questRect.sizeDelta = new Vector2(360f, 110f);

        // Badge Icon
        if (badgeSprite != null)
        {
            GameObject badgeGO = new GameObject("BadgeIcon");
            badgeGO.transform.SetParent(questGO.transform, false);
            var badgeImg = badgeGO.AddComponent<Image>();
            badgeImg.sprite = badgeSprite;
            badgeImg.raycastTarget = false;

            RectTransform badgeRect = badgeGO.GetComponent<RectTransform>();
            badgeRect.anchorMin = new Vector2(0f, 1f);
            badgeRect.anchorMax = new Vector2(0f, 1f);
            badgeRect.pivot = new Vector2(0.5f, 0.5f);
            badgeRect.anchoredPosition = new Vector2(32f, -22f);
            badgeRect.sizeDelta = new Vector2(24f, 24f);
        }

        // Category Text
        GameObject catGO = new GameObject("CategoryText");
        catGO.transform.SetParent(questGO.transform, false);
        Text catText = catGO.AddComponent<Text>();
        catText.font = font;
        catText.fontSize = 13;
        catText.fontStyle = FontStyle.Bold;
        catText.color = new Color(1f, 0.82f, 0.35f); // Warm Gold
        catText.text = "✦  ภารกิจหลัก (MAIN QUEST)  ✦";
        catText.alignment = TextAnchor.MiddleLeft;

        var catShadow = catGO.AddComponent<Shadow>();
        catShadow.effectColor = new Color(0f, 0f, 0f, 0.8f);
        catShadow.effectDistance = new Vector2(1f, -1f);

        RectTransform catRect = catGO.GetComponent<RectTransform>();
        catRect.anchorMin = new Vector2(0f, 1f);
        catRect.anchorMax = new Vector2(1f, 1f);
        catRect.pivot = new Vector2(0f, 1f);
        catRect.anchoredPosition = new Vector2(48f, -13f);
        catRect.sizeDelta = new Vector2(300f, 20f);

        // Title Text
        GameObject titleGO = new GameObject("TitleText");
        titleGO.transform.SetParent(questGO.transform, false);
        Text titleText = titleGO.AddComponent<Text>();
        titleText.font = font;
        titleText.fontSize = 16;
        titleText.fontStyle = FontStyle.Bold;
        titleText.color = new Color(1f, 0.98f, 0.92f);
        titleText.text = "ค้นหากุญแจและเปิดประตูดันเจี้ยน";
        titleText.alignment = TextAnchor.MiddleLeft;

        var titleShadow = titleGO.AddComponent<Shadow>();
        titleShadow.effectColor = new Color(0f, 0f, 0f, 0.8f);
        titleShadow.effectDistance = new Vector2(1f, -1f);

        RectTransform titleRect = titleGO.GetComponent<RectTransform>();
        titleRect.anchorMin = new Vector2(0f, 1f);
        titleRect.anchorMax = new Vector2(1f, 1f);
        titleRect.pivot = new Vector2(0f, 1f);
        titleRect.anchoredPosition = new Vector2(25f, -44f);
        titleRect.sizeDelta = new Vector2(320f, 24f);

        // Inset Objective Card
        GameObject objCardGO = new GameObject("ObjectiveCard");
        objCardGO.transform.SetParent(questGO.transform, false);
        var cardBg = objCardGO.AddComponent<Image>();
        cardBg.color = new Color(0.10f, 0.08f, 0.10f, 0.90f);

        RectTransform cardRect = objCardGO.GetComponent<RectTransform>();
        cardRect.anchorMin = new Vector2(0f, 0f);
        cardRect.anchorMax = new Vector2(1f, 0f);
        cardRect.pivot = new Vector2(0.5f, 0f);
        cardRect.anchoredPosition = new Vector2(0f, 8f);
        cardRect.sizeDelta = new Vector2(-40f, 30f);

        // Objective Card border
        GameObject cardBorderGO = new GameObject("Border");
        cardBorderGO.transform.SetParent(objCardGO.transform, false);
        var borderImg = cardBorderGO.AddComponent<Image>();
        borderImg.color = new Color(0.65f, 0.50f, 0.25f, 0.6f);
        borderImg.raycastTarget = false;
        RectTransform borderRect = cardBorderGO.GetComponent<RectTransform>();
        borderRect.anchorMin = Vector2.zero;
        borderRect.anchorMax = Vector2.one;
        borderRect.sizeDelta = Vector2.zero;
        cardBorderGO.transform.SetAsFirstSibling();

        // Objective Text (DescText)
        GameObject descGO = new GameObject("DescText");
        descGO.transform.SetParent(objCardGO.transform, false);
        Text descText = descGO.AddComponent<Text>();
        descText.font = font;
        descText.fontSize = 15;
        descText.color = Color.white;
        descText.text = "🔑 เก็บกุญแจสำคัญ (0/1)";
        descText.alignment = TextAnchor.MiddleLeft;

        RectTransform descRect = descGO.GetComponent<RectTransform>();
        descRect.anchorMin = Vector2.zero;
        descRect.anchorMax = Vector2.one;
        descRect.offsetMin = new Vector2(12f, 0f);
        descRect.offsetMax = new Vector2(-12f, 0f);

        // Configure QuestManager on root
        QuestManager questComp = root.GetComponent<QuestManager>();
        if (questComp == null)
        {
            questComp = root.AddComponent<QuestManager>();
        }
        questComp.questPanel = questGO;
        questComp.questCategoryText = catText;
        questComp.questTitleText = titleText;
        questComp.questDescText = descText;
    }

    private static void BuildDashUI(GameObject root, Font font)
    {
        Transform oldDash = root.transform.Find("DashCooldownUI");
        if (oldDash != null)
        {
            Object.DestroyImmediate(oldDash.gameObject);
        }

        Sprite ringSprite = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Image/UI/Dash_Ring.png");
        Sprite maskSprite = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Image/UI/Dash_Cooldown_Mask.png");
        Sprite iconSprite = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Image/UI/Dash_Icon.png");

        // 1. DashCooldownUI Root
        GameObject dashRootGO = new GameObject("DashCooldownUI");
        dashRootGO.transform.SetParent(root.transform, false);

        RectTransform rootRect = dashRootGO.AddComponent<RectTransform>();
        rootRect.anchorMin = new Vector2(1f, 0f); // Bottom-Right anchor
        rootRect.anchorMax = new Vector2(1f, 0f);
        rootRect.pivot = new Vector2(1f, 0f);
        rootRect.anchoredPosition = new Vector2(-35f, 35f);
        rootRect.sizeDelta = new Vector2(100f, 100f);

        // 2. Ready Glow (Pulses when off cooldown)
        GameObject glowGO = new GameObject("ReadyGlow");
        glowGO.transform.SetParent(dashRootGO.transform, false);
        var glowImg = glowGO.AddComponent<Image>();
        if (ringSprite != null) glowImg.sprite = ringSprite;
        glowImg.color = new Color(0.3f, 1f, 0.85f, 0f);
        glowImg.raycastTarget = false;

        RectTransform glowRect = glowGO.GetComponent<RectTransform>();
        glowRect.anchorMin = new Vector2(0.5f, 0.5f);
        glowRect.anchorMax = new Vector2(0.5f, 0.5f);
        glowRect.pivot = new Vector2(0.5f, 0.5f);
        glowRect.anchoredPosition = Vector2.zero;
        glowRect.sizeDelta = new Vector2(110f, 110f);

        // 3. Ring Frame (Outer metallic circular frame)
        GameObject ringGO = new GameObject("RingFrame");
        ringGO.transform.SetParent(dashRootGO.transform, false);
        var ringImg = ringGO.AddComponent<Image>();
        if (ringSprite != null) ringImg.sprite = ringSprite;
        else ringImg.color = new Color(0.2f, 0.25f, 0.3f, 1f);

        RectTransform ringRect = ringGO.GetComponent<RectTransform>();
        ringRect.anchorMin = Vector2.zero;
        ringRect.anchorMax = Vector2.one;
        ringRect.sizeDelta = Vector2.zero;

        // 4. Dash Icon
        GameObject iconGO = new GameObject("DashIcon");
        iconGO.transform.SetParent(dashRootGO.transform, false);
        var iconImg = iconGO.AddComponent<Image>();
        if (iconSprite != null) iconImg.sprite = iconSprite;
        iconImg.color = Color.white;

        RectTransform iconRect = iconGO.GetComponent<RectTransform>();
        iconRect.anchorMin = new Vector2(0.5f, 0.5f);
        iconRect.anchorMax = new Vector2(0.5f, 0.5f);
        iconRect.pivot = new Vector2(0.5f, 0.5f);
        iconRect.anchoredPosition = Vector2.zero;
        iconRect.sizeDelta = new Vector2(68f, 68f);

        // 5. Cooldown Radial Fill Overlay
        GameObject overlayGO = new GameObject("CooldownOverlay");
        overlayGO.transform.SetParent(dashRootGO.transform, false);
        var overlayImg = overlayGO.AddComponent<Image>();
        if (maskSprite != null) overlayImg.sprite = maskSprite;
        overlayImg.color = new Color(0.06f, 0.06f, 0.10f, 0.82f);
        overlayImg.type = Image.Type.Filled;
        overlayImg.fillMethod = Image.FillMethod.Radial360;
        overlayImg.fillOrigin = (int)Image.Origin360.Top;
        overlayImg.fillClockwise = false;
        overlayImg.fillAmount = 0f;

        RectTransform overlayRect = overlayGO.GetComponent<RectTransform>();
        overlayRect.anchorMin = new Vector2(0.5f, 0.5f);
        overlayRect.anchorMax = new Vector2(0.5f, 0.5f);
        overlayRect.pivot = new Vector2(0.5f, 0.5f);
        overlayRect.anchoredPosition = Vector2.zero;
        overlayRect.sizeDelta = new Vector2(80f, 80f);

        // 6. Countdown Text
        GameObject cdTextGO = new GameObject("CooldownText");
        cdTextGO.transform.SetParent(dashRootGO.transform, false);
        Text cdText = cdTextGO.AddComponent<Text>();
        cdText.font = font;
        cdText.fontSize = 22;
        cdText.fontStyle = FontStyle.Bold;
        cdText.color = new Color(1f, 0.95f, 0.65f);
        cdText.alignment = TextAnchor.MiddleCenter;
        cdText.text = "";

        var cdShadow = cdTextGO.AddComponent<Shadow>();
        cdShadow.effectColor = new Color(0f, 0f, 0f, 0.95f);
        cdShadow.effectDistance = new Vector2(1.5f, -1.5f);

        RectTransform cdRect = cdTextGO.GetComponent<RectTransform>();
        cdRect.anchorMin = new Vector2(0.5f, 0.5f);
        cdRect.anchorMax = new Vector2(0.5f, 0.5f);
        cdRect.pivot = new Vector2(0.5f, 0.5f);
        cdRect.anchoredPosition = Vector2.zero;
        cdRect.sizeDelta = new Vector2(80f, 32f);

        // 7. Keybind Pill Badge (at bottom of circle)
        GameObject pillGO = new GameObject("KeybindBadge");
        pillGO.transform.SetParent(dashRootGO.transform, false);
        var pillBg = pillGO.AddComponent<Image>();
        pillBg.color = new Color(0.12f, 0.10f, 0.08f, 0.95f);

        RectTransform pillRect = pillGO.GetComponent<RectTransform>();
        pillRect.anchorMin = new Vector2(0.5f, 0f);
        pillRect.anchorMax = new Vector2(0.5f, 0f);
        pillRect.pivot = new Vector2(0.5f, 0.5f);
        pillRect.anchoredPosition = new Vector2(0f, -4f);
        pillRect.sizeDelta = new Vector2(56f, 18f);

        GameObject pillBorder = new GameObject("Border");
        pillBorder.transform.SetParent(pillGO.transform, false);
        var pbImg = pillBorder.AddComponent<Image>();
        pbImg.color = new Color(0.85f, 0.65f, 0.25f, 0.8f);
        pbImg.raycastTarget = false;
        RectTransform pbRect = pillBorder.GetComponent<RectTransform>();
        pbRect.anchorMin = Vector2.zero;
        pbRect.anchorMax = Vector2.one;
        pbRect.sizeDelta = Vector2.zero;
        pillBorder.transform.SetAsFirstSibling();

        GameObject pillTextGO = new GameObject("Text");
        pillTextGO.transform.SetParent(pillGO.transform, false);
        Text keyText = pillTextGO.AddComponent<Text>();
        keyText.font = font;
        keyText.fontSize = 11;
        keyText.fontStyle = FontStyle.Bold;
        keyText.color = new Color(1f, 0.88f, 0.45f);
        keyText.alignment = TextAnchor.MiddleCenter;
        keyText.text = "SHIFT";

        RectTransform keyRect = pillTextGO.GetComponent<RectTransform>();
        keyRect.anchorMin = Vector2.zero;
        keyRect.anchorMax = Vector2.one;
        keyRect.sizeDelta = Vector2.zero;

        // 8. Cost Pill Badge (at top of circle: -15 O2)
        GameObject costGO = new GameObject("CostBadge");
        costGO.transform.SetParent(dashRootGO.transform, false);
        var costBg = costGO.AddComponent<Image>();
        costBg.color = new Color(0.08f, 0.14f, 0.18f, 0.92f);

        RectTransform costRect = costGO.GetComponent<RectTransform>();
        costRect.anchorMin = new Vector2(0.5f, 1f);
        costRect.anchorMax = new Vector2(0.5f, 1f);
        costRect.pivot = new Vector2(0.5f, 0.5f);
        costRect.anchoredPosition = new Vector2(0f, 4f);
        costRect.sizeDelta = new Vector2(56f, 18f);

        GameObject costBorder = new GameObject("Border");
        costBorder.transform.SetParent(costGO.transform, false);
        var cbImg = costBorder.AddComponent<Image>();
        cbImg.color = new Color(0.35f, 0.80f, 0.95f, 0.8f);
        cbImg.raycastTarget = false;
        RectTransform cbRect = costBorder.GetComponent<RectTransform>();
        cbRect.anchorMin = Vector2.zero;
        cbRect.anchorMax = Vector2.one;
        cbRect.sizeDelta = Vector2.zero;
        costBorder.transform.SetAsFirstSibling();

        GameObject costTextGO = new GameObject("Text");
        costTextGO.transform.SetParent(costGO.transform, false);
        Text costText = costTextGO.AddComponent<Text>();
        costText.font = font;
        costText.fontSize = 11;
        costText.fontStyle = FontStyle.Bold;
        costText.color = new Color(0.45f, 0.92f, 1f);
        costText.alignment = TextAnchor.MiddleCenter;
        costText.text = "-15 O₂";

        RectTransform ctRect = costTextGO.GetComponent<RectTransform>();
        ctRect.anchorMin = Vector2.zero;
        ctRect.anchorMax = Vector2.one;
        ctRect.sizeDelta = Vector2.zero;

        // 9. Attach and wire DashCooldownUI component
        var dashComp = dashRootGO.AddComponent<DashCooldownUI>();
        dashComp.Setup(null, overlayImg, iconImg, ringImg, glowImg, cdText, keyText);
    }

    [MenuItem("Tools/SproutScout/Setup Tree Save Point VFX")]
    public static void SetupTreeVFXInScene()
    {
        var sceneTrees = Object.FindObjectsByType<TreeSavePoint>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        Debug.Log("[CreateHUDAdditions] Found " + sceneTrees.Length + " TreeSavePoint objects in active scene.");

        Material softGlowMat = AssetDatabase.LoadAssetAtPath<Material>("Assets/Materials/VFX/Mat_SoftGlow_Additive.mat");
        Material starMat = AssetDatabase.LoadAssetAtPath<Material>("Assets/Materials/VFX/Mat_StarSparkle_Additive.mat");
        Material ringMat = AssetDatabase.LoadAssetAtPath<Material>("Assets/Materials/VFX/Mat_RingShockwave_Additive.mat");
        Material leafMat = AssetDatabase.LoadAssetAtPath<Material>("Assets/Materials/VFX/Mat_LeafParticle_Alpha.mat");
        AudioClip sound = AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Audio/SFX/Key.mp3");

        foreach (var tree in sceneTrees)
        {
            BuildTreeVFXHierarchy(tree.gameObject, softGlowMat, starMat, ringMat, leafMat, sound);
            Debug.Log("[CreateHUDAdditions] Configured VFX on: " + tree.gameObject.name);
        }

        // Save as prefab
        if (sceneTrees.Length > 0)
        {
            string prefabPath = "Assets/Prefabs/Tree/TreeSavePoint.prefab";
            PrefabUtility.SaveAsPrefabAssetAndConnect(sceneTrees[0].gameObject, prefabPath, InteractionMode.AutomatedAction);
            Debug.Log("[CreateHUDAdditions] Saved TreeSavePoint prefab at: " + prefabPath);
        }

        var scene = UnityEditor.SceneManagement.EditorSceneManager.GetActiveScene();
        UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(scene);
        UnityEditor.SceneManagement.EditorSceneManager.SaveScene(scene);
        Debug.Log("[CreateHUDAdditions] Saved active scene with TreeSavePoint VFX!");
    }

    private static void BuildTreeVFXHierarchy(GameObject root, Material softGlowMat, Material starMat, Material ringMat, Material leafMat, AudioClip sound)
    {
        Transform oldBurst = root.transform.Find("TreeRestoreVFX");
        if (oldBurst != null) Object.DestroyImmediate(oldBurst.gameObject);

        Transform oldAmbient = root.transform.Find("AmbientFireflies");
        if (oldAmbient != null) Object.DestroyImmediate(oldAmbient.gameObject);

        // 1. Build TreeRestoreVFX Root (Life Energy Burst)
        GameObject burstGO = new GameObject("TreeRestoreVFX");
        burstGO.transform.SetParent(root.transform, false);
        burstGO.transform.localPosition = new Vector3(0f, 1.5f, 0f);

        ParticleSystem rootPS = burstGO.AddComponent<ParticleSystem>();
        ParticleSystemRenderer rootPSR = burstGO.GetComponent<ParticleSystemRenderer>();
        if (softGlowMat != null) rootPSR.material = softGlowMat;

        var main = rootPS.main;
        main.duration = 2.0f;
        main.loop = false;
        main.playOnAwake = false;
        main.startLifetime = new ParticleSystem.MinMaxCurve(1.5f, 2.5f);
        main.startSpeed = new ParticleSystem.MinMaxCurve(3.0f, 6.0f);
        main.startSize = new ParticleSystem.MinMaxCurve(0.25f, 0.45f);
        main.gravityModifier = -0.15f;

        var emission = rootPS.emission;
        emission.rateOverTime = 0f;
        emission.SetBursts(new ParticleSystem.Burst[] { new ParticleSystem.Burst(0.0f, 45) });

        var shape = rootPS.shape;
        shape.shapeType = ParticleSystemShapeType.Sphere;
        shape.radius = 0.6f;

        var col = rootPS.colorOverLifetime;
        col.enabled = true;
        Gradient grad = new Gradient();
        grad.SetKeys(
            new GradientColorKey[] {
                new GradientColorKey(new Color(0.18f, 0.90f, 0.45f), 0.0f),
                new GradientColorKey(new Color(1.0f, 0.85f, 0.25f), 0.4f),
                new GradientColorKey(new Color(0.55f, 0.95f, 0.85f), 0.8f)
            },
            new GradientAlphaKey[] {
                new GradientAlphaKey(1.0f, 0.0f),
                new GradientAlphaKey(0.85f, 0.6f),
                new GradientAlphaKey(0.0f, 1.0f)
            }
        );
        col.color = grad;

        var sol = rootPS.sizeOverLifetime;
        sol.enabled = true;
        AnimationCurve sizeCurve = new AnimationCurve();
        sizeCurve.AddKey(0.0f, 0.2f);
        sizeCurve.AddKey(0.25f, 1.0f);
        sizeCurve.AddKey(1.0f, 0.0f);
        sol.size = new ParticleSystem.MinMaxCurve(1.0f, sizeCurve);

        // 2. Child 1: Magical Star Sparkles
        GameObject sparkGO = new GameObject("Sparkles");
        sparkGO.transform.SetParent(burstGO.transform, false);
        ParticleSystem sparkPS = sparkGO.AddComponent<ParticleSystem>();
        ParticleSystemRenderer sparkPSR = sparkGO.GetComponent<ParticleSystemRenderer>();
        if (starMat != null) sparkPSR.material = starMat;

        var sparkMain = sparkPS.main;
        sparkMain.duration = 2.0f;
        sparkMain.loop = false;
        sparkMain.playOnAwake = false;
        sparkMain.startLifetime = new ParticleSystem.MinMaxCurve(1.8f, 2.8f);
        sparkMain.startSpeed = new ParticleSystem.MinMaxCurve(3.5f, 7.5f);
        sparkMain.startSize = new ParticleSystem.MinMaxCurve(0.15f, 0.32f);
        sparkMain.gravityModifier = -0.1f;

        var sparkEm = sparkPS.emission;
        sparkEm.rateOverTime = 0f;
        sparkEm.SetBursts(new ParticleSystem.Burst[] { new ParticleSystem.Burst(0.05f, 55) });

        var sparkShape = sparkPS.shape;
        sparkShape.shapeType = ParticleSystemShapeType.Sphere;
        sparkShape.radius = 0.8f;

        var sparkCol = sparkPS.colorOverLifetime;
        sparkCol.enabled = true;
        Gradient sparkGrad = new Gradient();
        sparkGrad.SetKeys(
            new GradientColorKey[] {
                new GradientColorKey(new Color(1.0f, 0.92f, 0.40f), 0.0f),
                new GradientColorKey(new Color(1.0f, 0.75f, 0.20f), 0.7f)
            },
            new GradientAlphaKey[] {
                new GradientAlphaKey(1.0f, 0.0f),
                new GradientAlphaKey(0.9f, 0.5f),
                new GradientAlphaKey(0.0f, 1.0f)
            }
        );
        sparkCol.color = sparkGrad;

        var sparkNoise = sparkPS.noise;
        sparkNoise.enabled = true;
        sparkNoise.strength = 0.6f;
        sparkNoise.frequency = 0.5f;

        // 3. Child 2: Shockwave Ring
        GameObject ringGO = new GameObject("ShockwaveRing");
        ringGO.transform.SetParent(burstGO.transform, false);
        ringGO.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);

        ParticleSystem ringPS = ringGO.AddComponent<ParticleSystem>();
        ParticleSystemRenderer ringPSR = ringGO.GetComponent<ParticleSystemRenderer>();
        if (ringMat != null) ringPSR.material = ringMat;
        ringPSR.alignment = ParticleSystemRenderSpace.Local;

        var ringMain = ringPS.main;
        ringMain.duration = 1.0f;
        ringMain.loop = false;
        ringMain.playOnAwake = false;
        ringMain.startLifetime = 0.75f;
        ringMain.startSpeed = 0f;
        ringMain.startSize = 0.5f;

        var ringEm = ringPS.emission;
        ringEm.rateOverTime = 0f;
        ringEm.SetBursts(new ParticleSystem.Burst[] { new ParticleSystem.Burst(0.0f, 1) });

        var ringSol = ringPS.sizeOverLifetime;
        ringSol.enabled = true;
        AnimationCurve ringCurve = new AnimationCurve();
        ringCurve.AddKey(0.0f, 1.0f);
        ringCurve.AddKey(1.0f, 12.0f);
        ringSol.size = new ParticleSystem.MinMaxCurve(1.0f, ringCurve);

        var ringCol = ringPS.colorOverLifetime;
        ringCol.enabled = true;
        Gradient ringGrad = new Gradient();
        ringGrad.SetKeys(
            new GradientColorKey[] { new GradientColorKey(new Color(0.1f, 1.0f, 0.75f), 0.0f) },
            new GradientAlphaKey[] { new GradientAlphaKey(1.0f, 0.0f), new GradientAlphaKey(0.0f, 1.0f) }
        );
        ringCol.color = ringGrad;

        // 4. Child 3: Rising Nature Leaves
        GameObject leafGO = new GameObject("RisingLeaves");
        leafGO.transform.SetParent(burstGO.transform, false);

        ParticleSystem leafPS = leafGO.AddComponent<ParticleSystem>();
        ParticleSystemRenderer leafPSR = leafGO.GetComponent<ParticleSystemRenderer>();
        if (leafMat != null) leafPSR.material = leafMat;

        var leafMain = leafPS.main;
        leafMain.duration = 2.0f;
        leafMain.loop = false;
        leafMain.playOnAwake = false;
        leafMain.startLifetime = new ParticleSystem.MinMaxCurve(2.0f, 3.2f);
        leafMain.startSpeed = new ParticleSystem.MinMaxCurve(2.0f, 4.5f);
        leafMain.startSize = new ParticleSystem.MinMaxCurve(0.2f, 0.38f);
        leafMain.startRotation = new ParticleSystem.MinMaxCurve(0f, Mathf.PI * 2f);
        leafMain.gravityModifier = -0.2f;

        var leafEm = leafPS.emission;
        leafEm.rateOverTime = 0f;
        leafEm.SetBursts(new ParticleSystem.Burst[] { new ParticleSystem.Burst(0.08f, 25) });

        var leafShape = leafPS.shape;
        leafShape.shapeType = ParticleSystemShapeType.Cone;
        leafShape.angle = 15f;
        leafShape.radius = 0.8f;

        var leafRot = leafPS.rotationOverLifetime;
        leafRot.enabled = true;
        leafRot.z = new ParticleSystem.MinMaxCurve(1.5f, 3.0f);

        var leafCol = leafPS.colorOverLifetime;
        leafCol.enabled = true;
        Gradient leafGrad = new Gradient();
        leafGrad.SetKeys(
            new GradientColorKey[] {
                new GradientColorKey(new Color(0.2f, 0.85f, 0.35f), 0.0f),
                new GradientColorKey(new Color(0.95f, 0.80f, 0.20f), 0.7f)
            },
            new GradientAlphaKey[] {
                new GradientAlphaKey(1.0f, 0.0f),
                new GradientAlphaKey(0.9f, 0.7f),
                new GradientAlphaKey(0.0f, 1.0f)
            }
        );
        leafCol.color = leafGrad;

        // 5. Ambient Fireflies (Gentle persistent aura around restored tree)
        GameObject ambientGO = new GameObject("AmbientFireflies");
        ambientGO.transform.SetParent(root.transform, false);
        ambientGO.transform.localPosition = new Vector3(0f, 2.0f, 0f);

        ParticleSystem ambPS = ambientGO.AddComponent<ParticleSystem>();
        ParticleSystemRenderer ambPSR = ambientGO.GetComponent<ParticleSystemRenderer>();
        if (softGlowMat != null) ambPSR.material = softGlowMat;

        var ambMain = ambPS.main;
        ambMain.duration = 4.0f;
        ambMain.loop = true;
        ambMain.playOnAwake = false;
        ambMain.startLifetime = new ParticleSystem.MinMaxCurve(3.0f, 5.0f);
        ambMain.startSpeed = new ParticleSystem.MinMaxCurve(0.15f, 0.45f);
        ambMain.startSize = new ParticleSystem.MinMaxCurve(0.12f, 0.22f);

        var ambEm = ambPS.emission;
        ambEm.rateOverTime = 10f;

        var ambShape = ambPS.shape;
        ambShape.shapeType = ParticleSystemShapeType.Sphere;
        ambShape.radius = 2.2f;

        var ambNoise = ambPS.noise;
        ambNoise.enabled = true;
        ambNoise.strength = 0.35f;
        ambNoise.frequency = 0.3f;

        var ambCol = ambPS.colorOverLifetime;
        ambCol.enabled = true;
        Gradient ambGrad = new Gradient();
        ambGrad.SetKeys(
            new GradientColorKey[] {
                new GradientColorKey(new Color(0.35f, 0.95f, 0.65f), 0.0f),
                new GradientColorKey(new Color(0.95f, 0.88f, 0.35f), 0.5f),
                new GradientColorKey(new Color(0.35f, 0.95f, 0.85f), 1.0f)
            },
            new GradientAlphaKey[] {
                new GradientAlphaKey(0.0f, 0.0f),
                new GradientAlphaKey(0.85f, 0.5f),
                new GradientAlphaKey(0.0f, 1.0f)
            }
        );
        ambCol.color = ambGrad;

        // Configure TreeSavePoint script
        var treeScript = root.GetComponent<TreeSavePoint>();
        if (treeScript != null)
        {
            treeScript.ConfigureVFX(rootPS, ambPS, sound);
        }
    }
}
#endif
