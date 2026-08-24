#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

[InitializeOnLoad]
public class CreateHUDUI
{
    static CreateHUDUI()
    {
        // Run once when Unity compiles or launches to ensure the HUD is ready
        EditorApplication.delayCall += EnsureHUDAssetsReady;
    }

    [MenuItem("Tools/SproutScout/Create HUD UI Prefab")]
    public static void ForceCreateHUD()
    {
        CreateHUD(true);
    }

    private static void EnsureHUDAssetsReady()
    {
        CreateHUD(false);
    }

    private static void CreateHUD(bool forceOverwrite)
    {
        string hpPath = "Assets/Image/HUD_HP_Bar.png";
        string staminaPath = "Assets/Image/HUD_Stamina_Bar.png";
        string avatarPath = "Assets/Image/HUD_Avatar_Portrait.png";
        string prefabPath = "Assets/Prefabs/HUD_Canvas.prefab";

        // Skip if already exists and we are not forcing overwrite
        if (!forceOverwrite && AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath) != null)
        {
            return;
        }

        Debug.Log("[CreateHUDUI] Configuring HUD sprites and creating Prefab...");

        // 1. Configure Texture Import Settings as Sprite (2D and UI)
        ConfigureAsSprite(hpPath);
        ConfigureAsSprite(staminaPath);
        ConfigureAsSprite(avatarPath);
        AssetDatabase.Refresh();

        // Load the sprites
        Sprite hpSprite = AssetDatabase.LoadAssetAtPath<Sprite>(hpPath);
        Sprite staminaSprite = AssetDatabase.LoadAssetAtPath<Sprite>(staminaPath);
        Sprite avatarSprite = AssetDatabase.LoadAssetAtPath<Sprite>(avatarPath);

        if (hpSprite == null || staminaSprite == null || avatarSprite == null)
        {
            Debug.LogError("[CreateHUDUI] Failed to load one or more sprites. Make sure the PNGs exist.");
            return;
        }

        // 2. Create UI Canvas Hierarchy
        GameObject canvasGO = new GameObject("HUD Canvas");
        Canvas canvas = canvasGO.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvasGO.AddComponent<CanvasScaler>();
        canvasGO.AddComponent<GraphicRaycaster>();

        // Create Container Panel (anchored at Top-Left)
        GameObject containerGO = new GameObject("HUD Container");
        containerGO.transform.SetParent(canvasGO.transform);
        RectTransform containerRect = containerGO.AddComponent<RectTransform>();
        containerRect.anchorMin = new Vector2(0f, 1f); // Top-left anchor
        containerRect.anchorMax = new Vector2(0f, 1f);
        containerRect.pivot = new Vector2(0f, 1f);
        containerRect.anchoredPosition = new Vector2(25f, -25f);
        containerRect.sizeDelta = new Vector2(450f, 120f);

        // A. Avatar Portrait UI
        GameObject avatarGO = new GameObject("AvatarPortrait");
        avatarGO.transform.SetParent(containerGO.transform);
        Image avatarImg = avatarGO.AddComponent<Image>();
        avatarImg.sprite = avatarSprite;
        RectTransform avatarRect = avatarGO.GetComponent<RectTransform>();
        avatarRect.anchorMin = new Vector2(0f, 0.5f); // Centered vertically in container
        avatarRect.anchorMax = new Vector2(0f, 0.5f);
        avatarRect.pivot = new Vector2(0f, 0.5f);
        avatarRect.anchoredPosition = new Vector2(0f, 0f);
        avatarRect.sizeDelta = new Vector2(100f, 100f);

        // B. Health Bar UI (Green Leaf themed)
        GameObject hpGO = new GameObject("HPBar");
        hpGO.transform.SetParent(containerGO.transform);
        Image hpImg = hpGO.AddComponent<Image>();
        hpImg.sprite = hpSprite;
        RectTransform hpRect = hpGO.GetComponent<RectTransform>();
        hpRect.anchorMin = new Vector2(0f, 0.5f);
        hpRect.anchorMax = new Vector2(0f, 0.5f);
        hpRect.pivot = new Vector2(0f, 0.5f);
        hpRect.anchoredPosition = new Vector2(110f, 22f); // Placed next to avatar, upper row
        hpRect.sizeDelta = new Vector2(300f, 40f);

        // C. Stamina Bar UI (Yellow Energy themed)
        GameObject staminaGO = new GameObject("StaminaBar");
        staminaGO.transform.SetParent(containerGO.transform);
        Image staminaImg = staminaGO.AddComponent<Image>();
        staminaImg.sprite = staminaSprite;
        RectTransform staminaRect = staminaGO.GetComponent<RectTransform>();
        staminaRect.anchorMin = new Vector2(0f, 0.5f);
        staminaRect.anchorMax = new Vector2(0f, 0.5f);
        staminaRect.pivot = new Vector2(0f, 0.5f);
        staminaRect.anchoredPosition = new Vector2(110f, -22f); // Placed next to avatar, lower row
        staminaRect.sizeDelta = new Vector2(300f, 40f);

        // 3. Save as Prefab
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
        Debug.Log($"[CreateHUDUI] Successfully created HUD Canvas Prefab at: {finalPath}");
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
