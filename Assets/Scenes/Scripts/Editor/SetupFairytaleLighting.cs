#if UNITY_EDITOR
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

public static class SetupFairytaleLighting
{
    private const string SETTINGS_DIR = "Assets/Settings";
    private const string MATERIALS_DIR = "Assets/Materials";
    private const string PROFILE_PATH = "Assets/Settings/CozyFairytaleVolumeProfile.asset";
    private const string SKYBOX_PATH = "Assets/Materials/CozyFairytaleSkybox.mat";

    [MenuItem("Tools/SproutScout/Apply Cozy Fairytale Lighting (Reference Style)")]
    public static void ApplyToActiveScene()
    {
        var scene = EditorSceneManager.GetActiveScene();
        if (!scene.IsValid())
        {
            Debug.LogError("[SetupFairytaleLighting] No valid active scene!");
            return;
        }

        Debug.Log($"[SetupFairytaleLighting] Applying Cozy Fairytale lighting style to scene: {scene.name}...");

        EnsureDirectories();

        // 1. Setup Procedural Skybox Material
        Material skyboxMat = SetupSkyboxMaterial();

        // 2. Setup Directional Sun
        SetupDirectionalSun();

        // 3. Setup RenderSettings (Trilight Ambient & Golden Fog)
        SetupEnvironmentSettings(skyboxMat);

        // 4. Setup URP Volume Profile (Color Grading, Bloom, White Balance, Shadows/Highlights)
        SetupPostProcessingVolume();

        // 5. Setup Torch & Lantern Point Lights with Warm Glow
        SetupTorchAndLanternLights();

        // 6. Save Scene and Assets
        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveOpenScenes();
        AssetDatabase.SaveAssets();

        Debug.Log($"[SetupFairytaleLighting] Successfully applied Cozy Fairytale lighting to {scene.name}!");
    }

    private static void EnsureDirectories()
    {
        if (!AssetDatabase.IsValidFolder(SETTINGS_DIR))
        {
            AssetDatabase.CreateFolder("Assets", "Settings");
        }
        if (!AssetDatabase.IsValidFolder(MATERIALS_DIR))
        {
            AssetDatabase.CreateFolder("Assets", "Materials");
        }
    }

    private static Material SetupSkyboxMaterial()
    {
        Material skybox = AssetDatabase.LoadAssetAtPath<Material>(SKYBOX_PATH);
        if (skybox == null)
        {
            Shader proceduralShader = Shader.Find("Skybox/Procedural");
            if (proceduralShader == null) proceduralShader = Shader.Find("Mobile/Skybox");
            if (proceduralShader == null) proceduralShader = Shader.Find("Universal Render Pipeline/Lit");

            skybox = new Material(proceduralShader);
            AssetDatabase.CreateAsset(skybox, SKYBOX_PATH);
        }

        if (skybox.shader.name == "Skybox/Procedural")
        {
            skybox.SetFloat("_SunSize", 0.04f);
            skybox.SetFloat("_SunSizeConvergence", 5.0f);
            skybox.SetFloat("_AtmosphereThickness", 0.88f);
            // Soft warm golden-peach sky fading into gentle pastel horizon
            skybox.SetColor("_SkyTint", new Color(0.96f, 0.86f, 0.72f, 1.0f));
            skybox.SetColor("_GroundColor", new Color(0.85f, 0.78f, 0.68f, 1.0f));
            skybox.SetFloat("_Exposure", 1.10f);
        }

        EditorUtility.SetDirty(skybox);
        return skybox;
    }

    private static void SetupDirectionalSun()
    {
        Light dirLight = null;
        var lights = Object.FindObjectsByType<Light>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        foreach (var l in lights)
        {
            if (l.type == LightType.Directional)
            {
                dirLight = l;
                break;
            }
        }

        if (dirLight == null)
        {
            GameObject sunGO = new GameObject("Directional Light");
            dirLight = sunGO.AddComponent<Light>();
            dirLight.type = LightType.Directional;
            Undo.RegisterCreatedObjectUndo(sunGO, "Create Directional Sun");
        }

        Undo.RecordObject(dirLight.gameObject, "Configure Directional Sun");
        Undo.RecordObject(dirLight.transform, "Position Directional Sun");
        Undo.RecordObject(dirLight, "Set Directional Sun Properties");

        // Angle: High-angled evening golden slant casting soft diagonal shadows
        dirLight.transform.rotation = Quaternion.Euler(42.0f, 138.0f, 0f);

        // Color: Soft Warm Buttery Honey Gold (gentle, painterly, not harsh yellow)
        dirLight.color = new Color(1.0f, 0.95f, 0.86f, 1.0f);
        dirLight.intensity = 1.25f;
        dirLight.shadows = LightShadows.Soft;
        dirLight.shadowStrength = 0.46f; // Transparent soft shadows so details stay visible in shade
        dirLight.shadowBias = 0.05f;
        dirLight.shadowNormalBias = 0.35f;

        EditorUtility.SetDirty(dirLight);
        EditorUtility.SetDirty(dirLight.transform);
    }

    private static void SetupEnvironmentSettings(Material skyboxMat)
    {
        // 1. Skybox
        if (skyboxMat != null)
        {
            RenderSettings.skybox = skyboxMat;
        }

        // 2. Trilight Ambient Lighting (Lifts dark shadows into the fairytale slate-teal & sage tone of the reference)
        RenderSettings.ambientMode = AmbientMode.Trilight;
        RenderSettings.ambientSkyColor = new Color(0.72f, 0.80f, 0.86f, 1.0f);     // Soft Skylight Slate-Teal (lifts shadow darkness)
        RenderSettings.ambientEquatorColor = new Color(0.65f, 0.72f, 0.68f, 1.0f); // Soft Sage/Moss Horizon Fill
        RenderSettings.ambientGroundColor = new Color(0.52f, 0.48f, 0.42f, 1.0f);  // Warm Earth Ambient Bounce

        // 3. Dreamy Fairytale Atmospheric Fog
        RenderSettings.fog = true;
        RenderSettings.fogMode = FogMode.Linear;
        RenderSettings.fogColor = new Color(0.92f, 0.84f, 0.74f, 1.0f); // Soft Golden Peach Twilight Mist
        RenderSettings.fogStartDistance = 22.0f;
        RenderSettings.fogEndDistance = 110.0f;
    }

    private static void SetupPostProcessingVolume()
    {
        // Find existing global volume or create a new one
        Volume targetVolume = null;
        var volumes = Object.FindObjectsByType<Volume>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        foreach (var v in volumes)
        {
            if (v.isGlobal)
            {
                targetVolume = v;
                break;
            }
        }

        if (targetVolume == null)
        {
            GameObject volGO = new GameObject("Global Post Processing Volume");
            targetVolume = volGO.AddComponent<Volume>();
            targetVolume.isGlobal = true;
            Undo.RegisterCreatedObjectUndo(volGO, "Create Global Volume");
        }

        Undo.RecordObject(targetVolume, "Configure Global Volume");
        targetVolume.priority = 10.0f;
        targetVolume.weight = 1.0f;

        // Recreate Profile
        if (AssetDatabase.LoadAssetAtPath<VolumeProfile>(PROFILE_PATH) != null)
        {
            AssetDatabase.DeleteAsset(PROFILE_PATH);
        }

        VolumeProfile profile = ScriptableObject.CreateInstance<VolumeProfile>();
        AssetDatabase.CreateAsset(profile, PROFILE_PATH);

        // A. Tonemapping (Neutral: preserves delicate pastel shades without harsh contrast crush)
        Tonemapping tonemapping = profile.Add<Tonemapping>();
        tonemapping.active = true;
        tonemapping.mode.Override(TonemappingMode.Neutral);
        AssetDatabase.AddObjectToAsset(tonemapping, profile);

        // B. Color Adjustments (Soft painterly colors, balanced exposure)
        ColorAdjustments colorAdj = profile.Add<ColorAdjustments>();
        colorAdj.active = true;
        colorAdj.postExposure.Override(0.20f);
        colorAdj.contrast.Override(6.0f);
        colorAdj.colorFilter.Override(new Color(1.0f, 0.985f, 0.96f, 1.0f));
        colorAdj.saturation.Override(10.0f); // Gentle, cozy, charming saturation
        AssetDatabase.AddObjectToAsset(colorAdj, profile);

        // C. White Balance (Warm honey evening glow)
        WhiteBalance whiteBalance = profile.Add<WhiteBalance>();
        whiteBalance.active = true;
        whiteBalance.temperature.Override(12.0f); // Warm cozy atmosphere
        whiteBalance.tint.Override(1.5f);
        AssetDatabase.AddObjectToAsset(whiteBalance, profile);

        // D. Bloom (Dreamy Fairytale Radiance around lights & sky)
        Bloom bloom = profile.Add<Bloom>();
        bloom.active = true;
        bloom.intensity.Override(0.95f);
        bloom.threshold.Override(0.80f);
        bloom.scatter.Override(0.70f);
        bloom.tint.Override(new Color(1.0f, 0.94f, 0.82f, 1.0f)); // Warm golden bloom halo
        AssetDatabase.AddObjectToAsset(bloom, profile);

        // E. Shadows Midtones Highlights (Fairytale Teal-Slate shadows + Cream highlights)
        ShadowsMidtonesHighlights smh = profile.Add<ShadowsMidtonesHighlights>();
        smh.active = true;
        smh.shadows.Override(new Vector4(0.38f, 0.44f, 0.50f, 0.05f));  // Gentle slate-teal shadow lift (reference style)
        smh.midtones.Override(new Vector4(0.96f, 0.88f, 0.74f, 0.00f)); // Soft golden peach midtones
        smh.highlights.Override(new Vector4(1.0f, 0.97f, 0.92f, 0.00f)); // Soft buttery cream highlights
        AssetDatabase.AddObjectToAsset(smh, profile);

        // F. Vignette (Warm golden umber edge framing)
        Vignette vignette = profile.Add<Vignette>();
        vignette.active = true;
        vignette.intensity.Override(0.14f);
        vignette.smoothness.Override(0.48f);
        vignette.color.Override(new Color(0.12f, 0.09f, 0.06f, 1.0f)); // Subtle warm border
        AssetDatabase.AddObjectToAsset(vignette, profile);

        AssetDatabase.SaveAssets();

        targetVolume.sharedProfile = profile;
        EditorUtility.SetDirty(targetVolume);
    }

    private static void SetupTorchAndLanternLights()
    {
        // 1. Search for torch GameObjects and attach warm point lights
        var allGOs = Object.FindObjectsByType<GameObject>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        int torchLightCount = 0;
        foreach (var go in allGOs)
        {
            if (go.name.ToLower().StartsWith("torch"))
            {
                Light pLight = go.GetComponentInChildren<Light>();
                if (pLight == null)
                {
                    GameObject lightChild = new GameObject("TorchPointLight");
                    lightChild.transform.SetParent(go.transform, false);
                    lightChild.transform.localPosition = new Vector3(0f, 0.85f, 0f);

                    pLight = lightChild.AddComponent<Light>();
                    pLight.type = LightType.Point;
                    pLight.color = new Color(1.0f, 0.74f, 0.38f, 1.0f); // Warm glowing amber fire
                    pLight.intensity = 1.6f;
                    pLight.range = 6.5f;
                    pLight.shadows = LightShadows.None;

                    LightFlicker flicker = lightChild.AddComponent<LightFlicker>();
                    flicker.minIntensity = 1.3f;
                    flicker.maxIntensity = 1.8f;
                    flicker.flickerSpeed = 0.07f;
                    flicker.jitterPosition = true;
                    flicker.jitterRange = 0.03f;

                    EditorUtility.SetDirty(lightChild);
                    torchLightCount++;
                }
                else
                {
                    pLight.color = new Color(1.0f, 0.74f, 0.38f, 1.0f);
                    pLight.intensity = 1.6f;
                    pLight.range = 6.5f;
                    EditorUtility.SetDirty(pLight);
                    torchLightCount++;
                }
            }
        }
        Debug.Log($"[SetupFairytaleLighting] Configured warm point lights on {torchLightCount} torches.");

        // 2. Tree Save Point ambient glow
        var treeSaves = Object.FindObjectsByType<TreeSavePoint>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        foreach (var tree in treeSaves)
        {
            Light treeLight = tree.GetComponentInChildren<Light>();
            if (treeLight == null)
            {
                GameObject treeLightGO = new GameObject("TreeMagicLight");
                treeLightGO.transform.SetParent(tree.transform, false);
                treeLightGO.transform.localPosition = new Vector3(0f, 1.8f, 0f);

                treeLight = treeLightGO.AddComponent<Light>();
                treeLight.type = LightType.Point;
                treeLight.color = new Color(0.90f, 0.96f, 0.72f, 1.0f); // Warm lime-gold magical glow
                treeLight.intensity = 1.4f;
                treeLight.range = 8.5f;
                treeLight.shadows = LightShadows.None;
                EditorUtility.SetDirty(treeLightGO);
            }
        }
    }
}
#endif
