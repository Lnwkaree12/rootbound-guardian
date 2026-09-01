#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

public class DungeonSceneConfigurator
{
    [MenuItem("Tools/SproutScout/Configure Dungeon Lighting and Post-Processing")]
    public static void ConfigureDungeonScene()
    {
        var scene = EditorSceneManager.GetActiveScene();
        if (!scene.IsValid())
        {
            Debug.LogError("[DungeonConfigurator] No active scene found!");
            return;
        }

        string scenePath = scene.path;
        Debug.Log($"[DungeonConfigurator] Setting up dungeon environment for active scene: {scene.name} ({scenePath})...");

        // Configure Ambient Lighting (make environment pitch black / very dark blue-indigo)
        RenderSettings.ambientMode = AmbientMode.Flat;
        RenderSettings.ambientLight = new Color(0.015f, 0.015f, 0.03f, 1f); // Cool dark blue ambient

        // 2. Configure Directional Light (make it dim, dark blue moonlit ambient)
        Light dirLight = null;
        var lights = Object.FindObjectsOfType<Light>();
        foreach (var l in lights)
        {
            if (l.type == LightType.Directional)
            {
                dirLight = l;
                break;
            }
        }

        if (dirLight != null)
        {
            dirLight.intensity = 0.03f; // Very dim
            dirLight.color = new Color(0.12f, 0.15f, 0.25f); // Cool dark blue
            dirLight.shadows = LightShadows.Soft;
            EditorUtility.SetDirty(dirLight);
            Debug.Log("[DungeonConfigurator] Dimmed Directional Light for dark dungeon atmosphere.");
        }

        // Enable soft shadows on all Point and Spot lights in the scene (lanterns, torches)
        int updatedLightsCount = 0;
        foreach (var l in lights)
        {
            if (l.type == LightType.Point || l.type == LightType.Spot)
            {
                l.shadows = LightShadows.Soft;
                EditorUtility.SetDirty(l);
                updatedLightsCount++;
            }
        }
        Debug.Log($"[DungeonConfigurator] Configured soft shadows on {updatedLightsCount} scene lights.");

        // 3. Spawn point lights with flickering at key coordinates (only if we are in the main "map" scene)
        if (scene.name == "map")
        {
            Vector3[] lightPositions = new Vector3[]
            {
                new Vector3(0.0f, 1.8f, 0.0f),       // Near Starting Area / Camera
                new Vector3(18.14f, 1.8f, 10.74f),   // Near Cube (1)
                new Vector3(14.23f, 1.8f, 19.55f),   // Near Cube
                new Vector3(209.7f, 2.2f, 61.82f),   // Near corridor-wide-corner
                new Vector3(221.7f, 1.8f, 30.29f)    // Near Cube (2)
            };

            // Clear any existing point lights we spawned before to avoid duplicates
            foreach (var l in Object.FindObjectsOfType<Light>())
            {
                if (l.type == LightType.Point && l.gameObject.name.StartsWith("DungeonPointLight"))
                {
                    Object.DestroyImmediate(l.gameObject);
                }
            }

            for (int i = 0; i < lightPositions.Length; i++)
            {
                GameObject lightGO = new GameObject($"DungeonPointLight_{i + 1}");
                lightGO.transform.position = lightPositions[i];

                Light pLight = lightGO.AddComponent<Light>();
                pLight.type = LightType.Point;
                pLight.color = new Color(1.0f, 0.62f, 0.23f); // Warm torch-like orange
                pLight.intensity = 2.2f;
                pLight.range = i == 4 ? 10f : 8f; // Cube (2) area gets a slightly larger range
                pLight.shadows = LightShadows.Soft; // Enable soft shadows for beautiful visuals

                // Attach LightFlicker script
                LightFlicker flicker = lightGO.AddComponent<LightFlicker>();
                flicker.minIntensity = 1.6f;
                flicker.maxIntensity = 2.6f;
                flicker.flickerSpeed = 0.08f;
                flicker.jitterPosition = true;
                flicker.jitterRange = 0.04f;

                Debug.Log($"[DungeonConfigurator] Spawned Flickering Point Light {i + 1} at: {lightPositions[i]}");
            }
        }

        // 4. Configure Post-Processing
        Volume globalVolume = Object.FindObjectOfType<Volume>();
        if (globalVolume == null)
        {
            GameObject volGO = new GameObject("Dungeon Global Volume");
            globalVolume = volGO.AddComponent<Volume>();
            globalVolume.isGlobal = true;
            globalVolume.weight = 1f;
        }

        // Generate or load VolumeProfile
        VolumeProfile profile = globalVolume.sharedProfile;
        string profileFolder = "Assets/Settings";
        string profilePath = $"{profileFolder}/DungeonVolumeProfile.asset";

        if (!AssetDatabase.IsValidFolder(profileFolder))
        {
            AssetDatabase.CreateFolder("Assets", "Settings");
        }

        if (profile == null || profile.name != "DungeonVolumeProfile")
        {
            profile = ScriptableObject.CreateInstance<VolumeProfile>();
            AssetDatabase.CreateAsset(profile, profilePath);
            globalVolume.sharedProfile = profile;
            Debug.Log($"[DungeonConfigurator] Created new Dungeon Volume Profile at: {profilePath}");
        }

        // --- Post-Processing Effects ---
        // A. Vignette (Creates dark border / claustrophobic dungeon feel)
        Vignette vignette;
        if (!profile.TryGet(out vignette)) vignette = profile.Add<Vignette>();
        vignette.active = true;
        vignette.intensity.Override(0.42f);
        vignette.smoothness.Override(0.55f);
        vignette.color.Override(Color.black);

        // B. Bloom (Makes torch fires and glowing lights pop out beautifully)
        Bloom bloom;
        if (!profile.TryGet(out bloom)) bloom = profile.Add<Bloom>();
        bloom.active = true;
        bloom.intensity.Override(1.35f);
        bloom.threshold.Override(0.85f);
        bloom.scatter.Override(0.7f);

        // C. Color Adjustments (Improves contrast, tints scene cool)
        ColorAdjustments colorAdjustments;
        if (!profile.TryGet(out colorAdjustments)) colorAdjustments = profile.Add<ColorAdjustments>();
        colorAdjustments.active = true;
        colorAdjustments.contrast.Override(16f); // Make shadows darker, highlights pop
        colorAdjustments.saturation.Override(-6f); // Subtle desaturation for grit
        colorAdjustments.postExposure.Override(0.15f);

        // D. Shadows Midtones Highlights (Teal & Orange cinematic dungeon color grading)
        ShadowsMidtonesHighlights smh;
        if (!profile.TryGet(out smh)) smh = profile.Add<ShadowsMidtonesHighlights>();
        smh.active = true;
        
        // Slightly cool/blue shadows
        smh.shadows.Override(new Vector4(0.85f, 0.92f, 1.05f, 1.0f)); 
        // Balanced midtones
        smh.midtones.Override(new Vector4(1.0f, 0.98f, 0.95f, 1.0f));
        // Warm orange highlights for light sources
        smh.highlights.Override(new Vector4(1.15f, 1.05f, 0.90f, 1.0f));

        EditorUtility.SetDirty(globalVolume);
        AssetDatabase.SaveAssets();

        // 5. Save the scene changes
        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
        
        Debug.Log("[DungeonConfigurator] Dungeon lighting and post-processing setup complete and saved successfully!");
    }
}
#endif
