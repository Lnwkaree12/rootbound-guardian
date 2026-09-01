#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

[InitializeOnLoad]
public class CreateInventoryHUD
{
    static CreateInventoryHUD()
    {
        // Run once when Unity compiles or launches to ensure the Inventory HUD is integrated
        EditorApplication.delayCall += EnsureInventoryHUDReady;
    }

    [MenuItem("Tools/SproutScout/Create Inventory and Potion UI")]
    public static void ForceCreateInventoryHUD()
    {
        CreateInventory(true);
    }

    private static void EnsureInventoryHUDReady()
    {
        if (EditorApplication.isPlaying || Application.isPlaying)
        {
            return;
        }
        CreateInventory(false);
    }

    private static void CreateInventory(bool forceOverwrite)
    {
        string hpPath = "Assets/Image/HUD_HP_Bar.png";
        string staminaPath = "Assets/Image/HUD_Stamina_Bar.png";
        string avatarPath = "Assets/Image/HUD_Avatar_Portrait.png";
        string potionPath = "Assets/Image/HUD_Potion.png";
        string prefabPath = "Assets/Prefabs/HUD_Canvas.prefab";

        // If we are not forcing, only run if the prefab is missing or we need to update it
        GameObject existingPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);
        if (!forceOverwrite && existingPrefab != null)
        {
            // Check if already has InventoryManager (means it has been configured)
            if (existingPrefab.GetComponent<InventoryManager>() != null)
            {
                return;
            }
        }

        Debug.Log("[CreateInventoryHUD] Configuring HUD & Potion sprites and building Inventory Canvas...");

        // 1. Configure Potion sprite settings
        ConfigureAsSprite(potionPath);
        AssetDatabase.Refresh();

        // Load Sprites
        Sprite hpSprite = AssetDatabase.LoadAssetAtPath<Sprite>(hpPath);
        Sprite staminaSprite = AssetDatabase.LoadAssetAtPath<Sprite>(staminaPath);
        Sprite avatarSprite = AssetDatabase.LoadAssetAtPath<Sprite>(avatarPath);
        Sprite potionSprite = AssetDatabase.LoadAssetAtPath<Sprite>(potionPath);

        if (hpSprite == null || staminaSprite == null || avatarSprite == null || potionSprite == null)
        {
            Debug.LogError("[CreateInventoryHUD] Failed to load one or more sprites. Make sure all PNGs exist.");
            return;
        }

        // 2. Create or Open Canvas GameObject
        GameObject canvasGO = new GameObject("HUD Canvas");
        Canvas canvas = canvasGO.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        
        CanvasScaler scaler = canvasGO.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);
        scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
        scaler.matchWidthOrHeight = 0.5f;
        scaler.dynamicPixelsPerUnit = 3f; // Fix pixelated/blurry fonts by increasing dynamic resolution

        canvasGO.AddComponent<GraphicRaycaster>();

        // Create Container Panel (anchored at Top-Left)
        GameObject containerGO = new GameObject("HUD Container");
        containerGO.transform.SetParent(canvasGO.transform, false);
        RectTransform containerRect = containerGO.AddComponent<RectTransform>();
        containerRect.anchorMin = new Vector2(0f, 1f); // Top-left anchor
        containerRect.anchorMax = new Vector2(0f, 1f);
        containerRect.pivot = new Vector2(0f, 1f);
        containerRect.anchoredPosition = new Vector2(25f, -25f);
        containerRect.sizeDelta = new Vector2(450f, 200f); // Height increased to fit Potion Slot

        // A. Avatar Portrait UI
        GameObject avatarGO = new GameObject("AvatarPortrait");
        avatarGO.transform.SetParent(containerGO.transform, false);
        Image avatarImg = avatarGO.AddComponent<Image>();
        avatarImg.sprite = avatarSprite;
        RectTransform avatarRect = avatarGO.GetComponent<RectTransform>();
        avatarRect.anchorMin = new Vector2(0f, 1f); 
        avatarRect.anchorMax = new Vector2(0f, 1f);
        avatarRect.pivot = new Vector2(0f, 1f);
        avatarRect.anchoredPosition = new Vector2(0f, -10f);
        avatarRect.sizeDelta = new Vector2(100f, 100f);

        // B. Health Bar UI (Green Leaf themed)
        GameObject hpGO = new GameObject("HPBar");
        hpGO.transform.SetParent(containerGO.transform, false);
        Image hpImg = hpGO.AddComponent<Image>();
        hpImg.sprite = hpSprite;
        RectTransform hpRect = hpGO.GetComponent<RectTransform>();
        hpRect.anchorMin = new Vector2(0f, 1f);
        hpRect.anchorMax = new Vector2(0f, 1f);
        hpRect.pivot = new Vector2(0f, 1f);
        hpRect.anchoredPosition = new Vector2(110f, -20f); // Placed next to avatar, upper row
        hpRect.sizeDelta = new Vector2(300f, 40f);

        // C. Stamina Bar UI (Yellow Energy themed)
        GameObject staminaGO = new GameObject("StaminaBar");
        staminaGO.transform.SetParent(containerGO.transform, false);
        Image staminaImg = staminaGO.AddComponent<Image>();
        staminaImg.sprite = staminaSprite;
        RectTransform staminaRect = staminaGO.GetComponent<RectTransform>();
        staminaRect.anchorMin = new Vector2(0f, 1f);
        staminaRect.anchorMax = new Vector2(0f, 1f);
        staminaRect.pivot = new Vector2(0f, 1f);
        staminaRect.anchoredPosition = new Vector2(110f, -65f); // Placed next to avatar, lower row
        staminaRect.sizeDelta = new Vector2(300f, 40f);

        // D. Quick Slot HUD Potion UI
        GameObject quickSlotGO = new GameObject("HUD_PotionQuickSlot");
        quickSlotGO.transform.SetParent(containerGO.transform, false);
        Image quickSlotBg = quickSlotGO.AddComponent<Image>();
        quickSlotBg.color = new Color(0.12f, 0.08f, 0.06f, 0.85f); // wood brown transparent slot background
        RectTransform quickSlotRect = quickSlotGO.GetComponent<RectTransform>();
        quickSlotRect.anchorMin = new Vector2(0f, 1f);
        quickSlotRect.anchorMax = new Vector2(0f, 1f);
        quickSlotRect.pivot = new Vector2(0f, 1f);
        quickSlotRect.anchoredPosition = new Vector2(110f, -115f); // Below stamina bar
        quickSlotRect.sizeDelta = new Vector2(150f, 50f);

        // Quick Slot Icon
        GameObject quickIconGO = new GameObject("Icon");
        quickIconGO.transform.SetParent(quickSlotGO.transform, false);
        Image quickIconImg = quickIconGO.AddComponent<Image>();
        quickIconImg.sprite = potionSprite;
        quickIconImg.preserveAspect = true;
        RectTransform quickIconRect = quickIconGO.GetComponent<RectTransform>();
        quickIconRect.anchorMin = new Vector2(0f, 0.5f);
        quickIconRect.anchorMax = new Vector2(0f, 0.5f);
        quickIconRect.pivot = new Vector2(0f, 0.5f);
        quickIconRect.anchoredPosition = new Vector2(8f, 0f);
        quickIconRect.sizeDelta = new Vector2(36f, 36f);

        // Quick Slot Count Text
        GameObject quickQtyGO = new GameObject("QtyText");
        quickQtyGO.transform.SetParent(quickSlotGO.transform, false);
        Text quickQtyText = quickQtyGO.AddComponent<Text>();
        
        // Load custom font Itim-Regular.ttf with legacy fallbacks
        Font customFont = AssetDatabase.LoadAssetAtPath<Font>("Assets/Itim-Regular.ttf");
        if (customFont == null)
        {
            try { customFont = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf"); }
            catch { try { customFont = Resources.GetBuiltinResource<Font>("Arial.ttf"); } catch { } }
        }
        quickQtyText.font = customFont;
        quickQtyText.fontSize = 20;
        quickQtyText.color = Color.white;
        quickQtyText.text = "x3";
        quickQtyText.alignment = TextAnchor.MiddleLeft;
        RectTransform quickQtyRect = quickQtyGO.GetComponent<RectTransform>();
        quickQtyRect.anchorMin = new Vector2(0f, 0.5f);
        quickQtyRect.anchorMax = new Vector2(0f, 0.5f);
        quickQtyRect.pivot = new Vector2(0f, 0.5f);
        quickQtyRect.anchoredPosition = new Vector2(50f, 6f);
        quickQtyRect.sizeDelta = new Vector2(60f, 24f);

        // Quick Slot Input Hint Label
        GameObject quickHintGO = new GameObject("HintText");
        quickHintGO.transform.SetParent(quickSlotGO.transform, false);
        Text quickHintText = quickHintGO.AddComponent<Text>();
        quickHintText.font = quickQtyText.font;
        quickHintText.fontSize = 11;
        quickHintText.color = new Color(0.85f, 0.65f, 0.25f); // Gold
        quickHintText.text = "กด [1] หรือ [H] เพื่อใช้";
        quickHintText.alignment = TextAnchor.MiddleLeft;
        RectTransform quickHintRect = quickHintGO.GetComponent<RectTransform>();
        quickHintRect.anchorMin = new Vector2(0f, 0.5f);
        quickHintRect.anchorMax = new Vector2(0f, 0.5f);
        quickHintRect.pivot = new Vector2(0f, 0.5f);
        quickHintRect.anchoredPosition = new Vector2(50f, -12f);
        quickHintRect.sizeDelta = new Vector2(90f, 16f);

        // E. Backpack Inventory Panel (กระเป๋า)
        GameObject backpackGO = new GameObject("BackpackPanel");
        backpackGO.transform.SetParent(canvasGO.transform, false);
        Image backpackBg = backpackGO.AddComponent<Image>();
        backpackBg.color = new Color(0.12f, 0.08f, 0.06f, 0.95f); // Rich warm dark wood brown panel background
        RectTransform backpackRect = backpackGO.GetComponent<RectTransform>();
        backpackRect.anchorMin = new Vector2(0.5f, 0.5f); // Center of screen
        backpackRect.anchorMax = new Vector2(0.5f, 0.5f);
        backpackRect.pivot = new Vector2(0.5f, 0.5f);
        backpackRect.anchoredPosition = new Vector2(0f, 0f);
        backpackRect.sizeDelta = new Vector2(500f, 320f);

        // Backpack Title
        GameObject titleGO = new GameObject("TitleText");
        titleGO.transform.SetParent(backpackGO.transform, false);
        Text titleText = titleGO.AddComponent<Text>();
        titleText.font = quickQtyText.font;
        titleText.fontSize = 24;
        titleText.color = new Color(0.85f, 0.65f, 0.25f); // Gold
        titleText.text = "BACKPACK (กระเป๋าเป้สะพาย)";
        titleText.alignment = TextAnchor.MiddleCenter;
        RectTransform titleRect = titleGO.GetComponent<RectTransform>();
        titleRect.anchorMin = new Vector2(0.5f, 1f);
        titleRect.anchorMax = new Vector2(0.5f, 1f);
        titleRect.pivot = new Vector2(0.5f, 1f);
        titleRect.anchoredPosition = new Vector2(0f, -20f);
        titleRect.sizeDelta = new Vector2(400f, 40f);

        // 5 Item Slots container
        GameObject slotsContainerGO = new GameObject("SlotsContainer");
        slotsContainerGO.transform.SetParent(backpackGO.transform, false);
        RectTransform slotsContainerRect = slotsContainerGO.AddComponent<RectTransform>();
        slotsContainerRect.anchorMin = new Vector2(0.5f, 0.5f);
        slotsContainerRect.anchorMax = new Vector2(0.5f, 0.5f);
        slotsContainerRect.pivot = new Vector2(0.5f, 0.5f);
        slotsContainerRect.anchoredPosition = new Vector2(0f, 10f);
        slotsContainerRect.sizeDelta = new Vector2(400f, 80f);

        Image[] slotImagesArray = new Image[5];
        Text[] slotTextsArray = new Text[5];

        float startX = -150f;
        float spacingX = 75f;

        for (int i = 0; i < 5; i++)
        {
            // Slot Background box
            GameObject slotGO = new GameObject($"Slot_{i}");
            slotGO.transform.SetParent(slotsContainerGO.transform, false);
            Image slotBg = slotGO.AddComponent<Image>();
            slotBg.color = new Color(0.25f, 0.18f, 0.15f, 1f); // lighter wood slot color
            RectTransform slotRect = slotGO.GetComponent<RectTransform>();
            slotRect.anchorMin = new Vector2(0.5f, 0.5f);
            slotRect.anchorMax = new Vector2(0.5f, 0.5f);
            slotRect.pivot = new Vector2(0.5f, 0.5f);
            slotRect.anchoredPosition = new Vector2(startX + i * spacingX, 0f);
            slotRect.sizeDelta = new Vector2(65f, 65f);

            // Slot Item Image
            GameObject slotItemGO = new GameObject("ItemImage");
            slotItemGO.transform.SetParent(slotGO.transform, false);
            Image slotItemImg = slotItemGO.AddComponent<Image>();
            slotItemImg.sprite = potionSprite;
            slotItemImg.preserveAspect = true;
            slotItemImg.enabled = false; // disabled when empty
            RectTransform slotItemRect = slotItemGO.GetComponent<RectTransform>();
            slotItemRect.anchorMin = new Vector2(0.5f, 0.5f);
            slotItemRect.anchorMax = new Vector2(0.5f, 0.5f);
            slotItemRect.pivot = new Vector2(0.5f, 0.5f);
            slotItemRect.sizeDelta = new Vector2(50f, 50f);
            slotImagesArray[i] = slotItemImg;

            // Slot Item Qty text
            GameObject slotQtyGO = new GameObject("QtyText");
            slotQtyGO.transform.SetParent(slotGO.transform, false);
            Text slotQtyText = slotQtyGO.AddComponent<Text>();
            slotQtyText.font = quickQtyText.font;
            slotQtyText.fontSize = 12;
            slotQtyText.color = Color.white;
            slotQtyText.text = "1";
            slotQtyText.alignment = TextAnchor.LowerRight;
            slotQtyText.enabled = false;
            RectTransform slotQtyRect = slotQtyGO.GetComponent<RectTransform>();
            slotQtyRect.anchorMin = new Vector2(1f, 0f); // Bottom-right corner
            slotQtyRect.anchorMax = new Vector2(1f, 0f);
            slotQtyRect.pivot = new Vector2(1f, 0f);
            slotQtyRect.anchoredPosition = new Vector2(-4f, 4f);
            slotQtyRect.sizeDelta = new Vector2(24f, 16f);
            slotTextsArray[i] = slotQtyText;
        }

        // Backpack Instructions
        GameObject instructionsGO = new GameObject("InstructionsText");
        instructionsGO.transform.SetParent(backpackGO.transform, false);
        Text instructionsText = instructionsGO.AddComponent<Text>();
        instructionsText.font = quickQtyText.font;
        instructionsText.fontSize = 13;
        instructionsText.color = Color.lightGray;
        instructionsText.text = "กด [Tab] หรือ [I] เพื่อเปิด/ปิดกระเป๋า\nกด [1] หรือ [H] เพื่อกินโพชั่นฟื้นพลังเลือด (HP)";
        instructionsText.alignment = TextAnchor.MiddleCenter;
        RectTransform instructionsRect = instructionsGO.GetComponent<RectTransform>();
        instructionsRect.anchorMin = new Vector2(0.5f, 0f);
        instructionsRect.anchorMax = new Vector2(0.5f, 0f);
        instructionsRect.pivot = new Vector2(0.5f, 0f);
        instructionsRect.anchoredPosition = new Vector2(0f, 20f);
        instructionsRect.sizeDelta = new Vector2(400f, 40f);

        // 3. Attach Managers and Link References
        PlayerStats statsComp = canvasGO.AddComponent<PlayerStats>();
        statsComp.hpBarImage = hpImg;
        statsComp.staminaBarImage = staminaImg;

        InventoryManager invComp = canvasGO.AddComponent<InventoryManager>();
        invComp.inventoryPanel = backpackGO;
        invComp.slotImages = slotImagesArray;
        invComp.slotQtyTexts = slotTextsArray;
        invComp.hudPotionQtyText = quickQtyText;
        invComp.potionSprite = potionSprite;

        // 4. Save as Prefab
        string finalPath = prefabPath;
        PrefabUtility.SaveAsPrefabAssetAndConnect(canvasGO, finalPath, InteractionMode.AutomatedAction);
        Object.DestroyImmediate(canvasGO);

        AssetDatabase.Refresh();

        // Synchronize HUD Canvas prefab instances in the active scene to show the updated HUD immediately
        Canvas[] sceneCanvases = Object.FindObjectsOfType<Canvas>(true);
        foreach (Canvas c in sceneCanvases)
        {
            if (c.gameObject.name.StartsWith("HUD Canvas") || c.gameObject.name.StartsWith("HUD_Canvas"))
            {
                if (PrefabUtility.IsPartOfPrefabInstance(c.gameObject))
                {
                    PrefabUtility.RevertPrefabInstance(c.gameObject, InteractionMode.AutomatedAction);
                    Debug.Log("[CreateInventoryHUD] Synchronized and updated prefab instance in active scene: " + c.gameObject.name);
                }
            }
        }

        Debug.Log($"[CreateInventoryHUD] Successfully created fully integrated HUD Canvas Prefab at: {finalPath}");
    }

    private static void ConfigureAsSprite(string assetPath)
    {
        TextureImporter importer = AssetImporter.GetAtPath(assetPath) as TextureImporter;
        if (importer != null)
        {
            importer.textureType = TextureImporterType.Sprite;
            importer.spriteImportMode = SpriteImportMode.Single;
            importer.mipmapEnabled = false;
            importer.filterMode = FilterMode.Point; // Set to Point for consistency
            importer.alphaIsTransparency = true;
            importer.spritePixelsPerUnit = 100; // UI default
            importer.SaveAndReimport();
        }
    }
}
#endif
