#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

[InitializeOnLoad]
public class CreatePlantWithLantern
{
    static CreatePlantWithLantern()
    {
        EditorApplication.delayCall += EnsurePlantPrefabExists;
    }

    [MenuItem("Tools/SproutScout/Create Plant with Lantern")]
    public static void ForceCreatePlantPrefab()
    {
        CreatePrefab(true);
    }

    private static void EnsurePlantPrefabExists()
    {
        if (EditorApplication.isPlaying || Application.isPlaying) return;
        CreatePrefab(false);
    }

    private static void CreatePrefab(bool forceOverwrite)
    {
        string plantPngPath = "Assets/Character/Plant/long_bean2.1.png";
        string lanternPngPath = "Assets/Character/Plant/hanging_lantern.png";
        string prefabPath = "Assets/Character/Plant/long_bean2.1.prefab";

        if (System.IO.File.Exists(prefabPath) && !forceOverwrite)
        {
            return;
        }

        // Configure Sprite Settings for the plant if needed
        ConfigureSpriteSettings(plantPngPath, 1024, SpriteAlignment.Center);
        // Configure Sprite Settings for the lantern (Custom pivot at top of chain)
        ConfigureSpriteSettings(lanternPngPath, 1024, SpriteAlignment.Custom, new Vector2(0.5f, 0.95f));

        Sprite plantSprite = AssetDatabase.LoadAssetAtPath<Sprite>(plantPngPath);
        Sprite lanternSprite = AssetDatabase.LoadAssetAtPath<Sprite>(lanternPngPath);

        if (plantSprite == null)
        {
            Debug.LogError($"[CreatePlantWithLantern] Plant sprite not found at: {plantPngPath}");
            return;
        }

        if (lanternSprite == null)
        {
            Debug.LogError($"[CreatePlantWithLantern] Lantern sprite not found at: {lanternPngPath}");
            return;
        }

        Debug.Log("[CreatePlantWithLantern] Creating Plant with Lantern Prefab...");

        // 1. Create main GameObject
        GameObject plantGO = new GameObject("long_bean2.1");
        SpriteRenderer plantSR = plantGO.AddComponent<SpriteRenderer>();
        plantSR.sprite = plantSprite;
        ConfigureSpritesLighting.ConfigureSpriteLighting(plantSR);

        // 2. Create Lantern child GameObject
        GameObject lanternGO = new GameObject("HangingLantern");
        lanternGO.transform.SetParent(plantGO.transform);
        
        // Position lantern. Based on 1x1 plant, let's place it hanging from a branch
        // e.g., X = 0.2f, Y = 0.15f, Z = -0.05f (slightly in front)
        lanternGO.transform.localPosition = new Vector3(0.2f, 0.15f, -0.05f);
        lanternGO.transform.localScale = new Vector3(0.35f, 0.35f, 0.35f); // Scale down lantern to fit nicely

        SpriteRenderer lanternSR = lanternGO.AddComponent<SpriteRenderer>();
        lanternSR.sprite = lanternSprite;
        ConfigureSpritesLighting.ConfigureSpriteLighting(lanternSR);

        // Add swing animation script
        lanternGO.AddComponent<LanternSwing>();

        // Add Light component (URP Point Light)
        Light light = lanternGO.AddComponent<Light>();
        light.type = LightType.Point;
        light.color = new Color(1.0f, 0.7f, 0.2f); // Warm flame orange/yellow
        light.intensity = 2.0f;
        light.range = 5.0f;

        // Add LightFlicker script if it exists in the project
        LightFlicker flickerScript = lanternGO.AddComponent<LightFlicker>();
        if (flickerScript != null)
        {
            flickerScript.minIntensity = 1.2f;
            flickerScript.maxIntensity = 2.4f;
            flickerScript.flickerSpeed = 0.08f;
            flickerScript.jitterPosition = true;
            flickerScript.jitterRange = 0.03f;
        }

        // 3. Save as Prefab
        PrefabUtility.SaveAsPrefabAssetAndConnect(plantGO, prefabPath, InteractionMode.AutomatedAction);

        // 4. Destroy temporary instance
        Object.DestroyImmediate(plantGO);

        Debug.Log($"[CreatePlantWithLantern] Successfully created and saved prefab at: {prefabPath}");
        AssetDatabase.Refresh();
    }

    private static void ConfigureSpriteSettings(string assetPath, float pixelsPerUnit, SpriteAlignment alignment, Vector2? customPivot = null)
    {
        TextureImporter importer = AssetImporter.GetAtPath(assetPath) as TextureImporter;
        if (importer != null)
        {
            bool modified = false;

            if (importer.textureType != TextureImporterType.Sprite)
            {
                importer.textureType = TextureImporterType.Sprite;
                modified = true;
            }
            if (importer.spriteImportMode != SpriteImportMode.Single)
            {
                importer.spriteImportMode = SpriteImportMode.Single;
                modified = true;
            }
            if (importer.spritePixelsPerUnit != pixelsPerUnit)
            {
                importer.spritePixelsPerUnit = pixelsPerUnit;
                modified = true;
            }
            if (importer.filterMode != FilterMode.Point)
            {
                importer.filterMode = FilterMode.Point;
                modified = true;
            }
            if (!importer.alphaIsTransparency)
            {
                importer.alphaIsTransparency = true;
                modified = true;
            }

            TextureImporterSettings settings = new TextureImporterSettings();
            importer.ReadTextureSettings(settings);

            if (settings.spriteAlignment != (int)alignment)
            {
                settings.spriteAlignment = (int)alignment;
                modified = true;
            }
            if (customPivot.HasValue && settings.spritePivot != customPivot.Value)
            {
                settings.spritePivot = customPivot.Value;
                modified = true;
            }

            if (modified)
            {
                importer.SetTextureSettings(settings);
                importer.mipmapEnabled = false;
                importer.SaveAndReimport();
                AssetDatabase.ImportAsset(assetPath, ImportAssetOptions.ForceUpdate);
                Debug.Log($"[CreatePlantWithLantern] Configured sprite settings for: {assetPath}");
            }
        }
    }
}
#endif
