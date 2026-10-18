#if UNITY_EDITOR
using System.IO;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

public static class CreateWaterVFX
{
    private const string TargetFolder = "Assets/Prefabs/VFX/Water_VFX";
    private const string TexturesFolder = "Assets/Prefabs/VFX/Water_VFX/Textures";

    private const string PrefabFarmWatering = TargetFolder + "/Watering_Can_Farm_Splash_VFX.prefab";
    private const string PrefabPondSpring = TargetFolder + "/Stylized_Water_Pond_Spring_VFX.prefab";
    private const string PrefabSplashBurst = TargetFolder + "/Water_Splash_Impact_Burst_VFX.prefab";
    private const string ScreenshotPath = "C:/Users/Artemis/Downloads/Water_VFX_Showcase.png";

    private const string PathStarSparkle = "Assets/Image/VFX/StarSparkle.png";
    private const string PathSoftGlow = "Assets/Image/VFX/SoftGlow.png";

    [MenuItem("Tools/Rootbound Guardian/Build All Water VFX")]
    public static void BuildAllWaterVFX()
    {
        EnsureFolderExists(TargetFolder);
        EnsureFolderExists(TexturesFolder);

        // 1. Generate Procedural Textures with generous padding
        Texture2D dropTex = GenerateWaterDropTexture();
        Texture2D rippleTex = GenerateWaterRippleTexture();
        Texture2D bubbleTex = GenerateWaterBubbleTexture();
        Texture2D splashTex = GenerateWaterSplashTexture();

        Texture2D starTex = AssetDatabase.LoadAssetAtPath<Texture2D>(PathStarSparkle);
        Texture2D glowTex = AssetDatabase.LoadAssetAtPath<Texture2D>(PathSoftGlow);

        // 2. Generate or Load Materials
        Material waterSurfaceMat = CreateOrUpdateWaterSurfaceMaterial();
        Material dropMat = CreateParticleMaterial("M_WaterDrop", dropTex, new Color(0.70f, 0.92f, 1.0f, 0.95f), false);
        Material rippleMat = CreateParticleMaterial("M_WaterRipple", rippleTex, new Color(0.75f, 0.95f, 1.0f, 0.85f), false);
        Material bubbleMat = CreateParticleMaterial("M_WaterBubble", bubbleTex, new Color(0.85f, 0.98f, 1.0f, 0.85f), false);
        Material splashMat = CreateParticleMaterial("M_WaterSplash", splashTex, new Color(0.80f, 0.96f, 1.0f, 0.95f), false);
        Material mistMat = CreateParticleMaterial("M_WaterMist", glowTex != null ? glowTex : rippleTex, new Color(0.45f, 0.85f, 1.0f, 0.28f), false);
        Material sparkleMat = CreateParticleMaterial("M_WaterSparkle", starTex != null ? starTex : bubbleTex, new Color(0.50f, 0.92f, 1.0f, 0.95f), true);
        Material growthSparkleMat = CreateParticleMaterial("M_GrowthSparkle", starTex != null ? starTex : dropTex, new Color(0.40f, 1.0f, 0.55f, 0.95f), true);

        // 3. Build Prefab 1: Watering_Can_Farm_Splash_VFX
        BuildFarmWateringPrefab(dropMat, rippleMat, splashMat, mistMat, growthSparkleMat);

        // 4. Build Prefab 2: Stylized_Water_Pond_Spring_VFX
        BuildPondSpringPrefab(waterSurfaceMat, rippleMat, bubbleMat, sparkleMat, mistMat);

        // 5. Build Prefab 3: Water_Splash_Impact_Burst_VFX
        BuildSplashBurstPrefab(splashMat, dropMat, rippleMat, sparkleMat, mistMat);

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        Debug.Log("[CreateWaterVFX] All Water VFX prefabs created and saved successfully!");
    }

    #region Prefab 1: Farm Watering Can Splash
    private static void BuildFarmWateringPrefab(Material dropMat, Material rippleMat, Material splashMat, Material mistMat, Material growthSparkleMat)
    {
        GameObject root = new GameObject("Watering_Can_Farm_Splash_VFX");

        // A. Streaming Water Arc (Cascading down from watering spout)
        GameObject streamGO = new GameObject("Water_Droplets_Stream");
        streamGO.transform.SetParent(root.transform, false);
        streamGO.transform.localPosition = new Vector3(0f, 1.8f, 0f);
        streamGO.transform.localRotation = Quaternion.Euler(75f, 0f, 0f);

        ParticleSystem psStream = streamGO.AddComponent<ParticleSystem>();
        ParticleSystemRenderer psrStream = streamGO.GetComponent<ParticleSystemRenderer>();
        psrStream.sharedMaterial = dropMat;
        psrStream.renderMode = ParticleSystemRenderMode.Stretch;
        psrStream.velocityScale = 0.08f;
        psrStream.lengthScale = 1.3f;

        var mainStream = psStream.main;
        mainStream.loop = true;
        mainStream.duration = 2.0f;
        mainStream.startLifetime = new ParticleSystem.MinMaxCurve(0.6f, 0.85f);
        mainStream.startSpeed = new ParticleSystem.MinMaxCurve(3.2f, 4.5f);
        mainStream.startSize = new ParticleSystem.MinMaxCurve(0.12f, 0.22f);
        mainStream.gravityModifier = 1.35f;
        mainStream.maxParticles = 120;
        mainStream.simulationSpace = ParticleSystemSimulationSpace.World;

        var emitStream = psStream.emission;
        emitStream.rateOverTime = 38f;

        var shapeStream = psStream.shape;
        shapeStream.shapeType = ParticleSystemShapeType.Cone;
        shapeStream.angle = 8f;
        shapeStream.radius = 0.08f;

        var colStream = psStream.colorOverLifetime;
        colStream.enabled = true;
        colStream.color = CreateGradient(
            new Color(0.85f, 0.96f, 1.0f), new Color(0.40f, 0.82f, 1.0f),
            0f, 0.95f, 0.85f, 0f);

        // B. Ground Splash Burst (Outward droplets hitting soil)
        GameObject splashGO = new GameObject("Ground_Impact_Splashes");
        splashGO.transform.SetParent(root.transform, false);
        splashGO.transform.localPosition = new Vector3(0f, 0.05f, 0.45f);

        ParticleSystem psSplash = splashGO.AddComponent<ParticleSystem>();
        ParticleSystemRenderer psrSplash = splashGO.GetComponent<ParticleSystemRenderer>();
        psrSplash.sharedMaterial = splashMat;
        psrSplash.renderMode = ParticleSystemRenderMode.Stretch;
        psrSplash.velocityScale = 0.06f;
        psrSplash.lengthScale = 1.2f;

        var mainSplash = psSplash.main;
        mainSplash.loop = true;
        mainSplash.duration = 2.0f;
        mainSplash.startLifetime = new ParticleSystem.MinMaxCurve(0.35f, 0.55f);
        mainSplash.startSpeed = new ParticleSystem.MinMaxCurve(1.5f, 2.8f);
        mainSplash.startSize = new ParticleSystem.MinMaxCurve(0.10f, 0.18f);
        mainSplash.gravityModifier = 1.8f;
        mainSplash.maxParticles = 80;

        var emitSplash = psSplash.emission;
        emitSplash.rateOverTime = 28f;

        var shapeSplash = psSplash.shape;
        shapeSplash.shapeType = ParticleSystemShapeType.Hemisphere;
        shapeSplash.radius = 0.25f;

        var colSplash = psSplash.colorOverLifetime;
        colSplash.enabled = true;
        colSplash.color = CreateGradient(
            new Color(0.95f, 1.0f, 1.0f), new Color(0.50f, 0.85f, 1.0f),
            0f, 0.95f, 0.8f, 0f);

        // C. Expanding Soil Ripples (Cartoon concentric rings)
        GameObject rippleGO = new GameObject("Soil_Water_Ripples");
        rippleGO.transform.SetParent(root.transform, false);
        rippleGO.transform.localPosition = new Vector3(0f, 0.02f, 0.45f);
        rippleGO.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);

        ParticleSystem psRipple = rippleGO.AddComponent<ParticleSystem>();
        ParticleSystemRenderer psrRipple = rippleGO.GetComponent<ParticleSystemRenderer>();
        psrRipple.sharedMaterial = rippleMat;
        psrRipple.renderMode = ParticleSystemRenderMode.Billboard;

        var mainRipple = psRipple.main;
        mainRipple.loop = true;
        mainRipple.duration = 2.0f;
        mainRipple.startLifetime = new ParticleSystem.MinMaxCurve(0.6f, 0.9f);
        mainRipple.startSpeed = 0f;
        mainRipple.startSize = new ParticleSystem.MinMaxCurve(0.2f, 0.35f);
        mainRipple.maxParticles = 30;

        var emitRipple = psRipple.emission;
        emitRipple.rateOverTime = 8f;

        var shapeRipple = psRipple.shape;
        shapeRipple.shapeType = ParticleSystemShapeType.Circle;
        shapeRipple.radius = 0.25f;

        var sizeRipple = psRipple.sizeOverLifetime;
        sizeRipple.enabled = true;
        AnimationCurve rCurve = new AnimationCurve();
        rCurve.AddKey(0f, 0.4f);
        rCurve.AddKey(1f, 1.6f);
        sizeRipple.size = new ParticleSystem.MinMaxCurve(1f, rCurve);

        var colRipple = psRipple.colorOverLifetime;
        colRipple.enabled = true;
        colRipple.color = CreateGradient(
            new Color(0.70f, 0.92f, 1.0f), new Color(0.45f, 0.80f, 0.98f),
            0f, 0.75f, 0.5f, 0f);

        // D. Moisture Dew Mist (Dewy, fresh atmospheric mist)
        GameObject mistGO = new GameObject("Moisture_Dew_Mist");
        mistGO.transform.SetParent(root.transform, false);
        mistGO.transform.localPosition = new Vector3(0f, 0.15f, 0.45f);

        ParticleSystem psMist = mistGO.AddComponent<ParticleSystem>();
        ParticleSystemRenderer psrMist = mistGO.GetComponent<ParticleSystemRenderer>();
        psrMist.sharedMaterial = mistMat;

        var mainMist = psMist.main;
        mainMist.loop = true;
        mainMist.duration = 3.0f;
        mainMist.startLifetime = new ParticleSystem.MinMaxCurve(1.2f, 2.0f);
        mainMist.startSpeed = new ParticleSystem.MinMaxCurve(0.15f, 0.35f);
        mainMist.startSize = new ParticleSystem.MinMaxCurve(0.45f, 0.85f);
        mainMist.maxParticles = 25;

        var emitMist = psMist.emission;
        emitMist.rateOverTime = 6f;

        var shapeMist = psMist.shape;
        shapeMist.shapeType = ParticleSystemShapeType.Circle;
        shapeMist.radius = 0.4f;

        var colMist = psMist.colorOverLifetime;
        colMist.enabled = true;
        colMist.color = CreateGradient(
            new Color(0.65f, 0.90f, 1.0f), new Color(0.50f, 0.82f, 0.95f),
            0f, 0.35f, 0.25f, 0f);

        // E. Magical Plant Growth Sparkles (Sprouting life energy with random star rotation)
        GameObject sparkGO = new GameObject("Plant_Growth_Sparkles");
        sparkGO.transform.SetParent(root.transform, false);
        sparkGO.transform.localPosition = new Vector3(0f, 0.2f, 0.45f);

        ParticleSystem psSpark = sparkGO.AddComponent<ParticleSystem>();
        ParticleSystemRenderer psrSpark = sparkGO.GetComponent<ParticleSystemRenderer>();
        psrSpark.sharedMaterial = growthSparkleMat;

        var mainSpark = psSpark.main;
        mainSpark.loop = true;
        mainSpark.duration = 2.0f;
        mainSpark.startLifetime = new ParticleSystem.MinMaxCurve(0.8f, 1.4f);
        mainSpark.startSpeed = new ParticleSystem.MinMaxCurve(0.2f, 0.6f);
        mainSpark.startSize = new ParticleSystem.MinMaxCurve(0.12f, 0.24f);
        mainSpark.startRotation = new ParticleSystem.MinMaxCurve(0f, Mathf.PI * 2f);
        mainSpark.maxParticles = 30;

        var rotSpark = psSpark.rotationOverLifetime;
        rotSpark.enabled = true;
        rotSpark.z = new ParticleSystem.MinMaxCurve(-1.8f, 1.8f);

        var emitSpark = psSpark.emission;
        emitSpark.rateOverTime = 8f;

        var shapeSpark = psSpark.shape;
        shapeSpark.shapeType = ParticleSystemShapeType.Box;
        shapeSpark.scale = new Vector3(0.6f, 0.1f, 0.6f);

        var colSpark = psSpark.colorOverLifetime;
        colSpark.enabled = true;
        colSpark.color = CreateGradient(
            new Color(0.50f, 1.0f, 0.60f), new Color(1.0f, 0.95f, 0.50f),
            0f, 0.95f, 0.8f, 0f);

        // F. Soft Hydration Point Light
        GameObject lightGO = new GameObject("Water_Glow_Light");
        lightGO.transform.SetParent(root.transform, false);
        lightGO.transform.localPosition = new Vector3(0f, 0.5f, 0.45f);
        Light pl = lightGO.AddComponent<Light>();
        pl.type = LightType.Point;
        pl.color = new Color(0.35f, 0.85f, 1.0f);
        pl.intensity = 1.2f;
        pl.range = 3.0f;

        PrefabUtility.SaveAsPrefabAsset(root, PrefabFarmWatering);
        Object.DestroyImmediate(root);
        Debug.Log("[CreateWaterVFX] Saved: " + PrefabFarmWatering);
    }
    #endregion

    #region Prefab 2: Stylized Pond / Spring VFX
    private static void BuildPondSpringPrefab(Material waterSurfaceMat, Material rippleMat, Material bubbleMat, Material sparkleMat, Material mistMat)
    {
        GameObject root = new GameObject("Stylized_Water_Pond_Spring_VFX");

        // 1. Water Surface Plane (4x4m)
        GameObject planeGO = GameObject.CreatePrimitive(PrimitiveType.Plane);
        planeGO.name = "Water_Surface_Plane";
        planeGO.transform.SetParent(root.transform, false);
        planeGO.transform.localScale = new Vector3(0.4f, 1f, 0.4f); // 4x4m plane
        MeshRenderer mr = planeGO.GetComponent<MeshRenderer>();
        if (mr != null)
        {
            mr.sharedMaterial = waterSurfaceMat;
            mr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            mr.receiveShadows = false;
        }

        // 2. Rising Spring Bubbles
        GameObject bubbleGO = new GameObject("Spring_Bubbles");
        bubbleGO.transform.SetParent(root.transform, false);
        bubbleGO.transform.localPosition = new Vector3(0f, -0.1f, 0f);

        ParticleSystem psBubble = bubbleGO.AddComponent<ParticleSystem>();
        ParticleSystemRenderer psrBubble = bubbleGO.GetComponent<ParticleSystemRenderer>();
        psrBubble.sharedMaterial = bubbleMat;

        var mainB = psBubble.main;
        mainB.loop = true;
        mainB.duration = 3.0f;
        mainB.startLifetime = new ParticleSystem.MinMaxCurve(1.5f, 2.5f);
        mainB.startSpeed = new ParticleSystem.MinMaxCurve(0.25f, 0.55f);
        mainB.startSize = new ParticleSystem.MinMaxCurve(0.10f, 0.18f);
        mainB.maxParticles = 50;

        var emitB = psBubble.emission;
        emitB.rateOverTime = 12f;

        var shapeB = psBubble.shape;
        shapeB.shapeType = ParticleSystemShapeType.Box;
        shapeB.scale = new Vector3(3.0f, 0.1f, 3.0f);

        var colB = psBubble.colorOverLifetime;
        colB.enabled = true;
        colB.color = CreateGradient(
            new Color(0.85f, 0.98f, 1.0f), new Color(0.50f, 0.88f, 1.0f),
            0f, 0.85f, 0.75f, 0f);

        // 3. Surface Ripples
        GameObject ripGO = new GameObject("Surface_Water_Ripples");
        ripGO.transform.SetParent(root.transform, false);
        ripGO.transform.localPosition = new Vector3(0f, 0.03f, 0f);
        ripGO.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);

        ParticleSystem psRip = ripGO.AddComponent<ParticleSystem>();
        ParticleSystemRenderer psrRip = ripGO.GetComponent<ParticleSystemRenderer>();
        psrRip.sharedMaterial = rippleMat;
        psrRip.renderMode = ParticleSystemRenderMode.Billboard;

        var mainRip = psRip.main;
        mainRip.loop = true;
        mainRip.duration = 4.0f;
        mainRip.startLifetime = new ParticleSystem.MinMaxCurve(1.0f, 1.8f);
        mainRip.startSpeed = 0f;
        mainRip.startSize = new ParticleSystem.MinMaxCurve(0.35f, 0.65f);
        mainRip.maxParticles = 40;

        var emitRip = psRip.emission;
        emitRip.rateOverTime = 7f;

        var shapeRip = psRip.shape;
        shapeRip.shapeType = ParticleSystemShapeType.Box;
        shapeRip.scale = new Vector3(2.8f, 2.8f, 0.1f);

        var sizeRip = psRip.sizeOverLifetime;
        sizeRip.enabled = true;
        AnimationCurve ripCurve = new AnimationCurve();
        ripCurve.AddKey(0f, 0.35f);
        ripCurve.AddKey(1f, 1.75f);
        sizeRip.size = new ParticleSystem.MinMaxCurve(1f, ripCurve);

        var colRip = psRip.colorOverLifetime;
        colRip.enabled = true;
        colRip.color = CreateGradient(
            new Color(0.80f, 0.95f, 1.0f), new Color(0.60f, 0.90f, 1.0f),
            0f, 0.65f, 0.45f, 0f);

        // 4. Water Sparkles (Floating Starlight over Water with smooth rotation)
        GameObject sparkGO = new GameObject("Water_Glint_Sparkles");
        sparkGO.transform.SetParent(root.transform, false);
        sparkGO.transform.localPosition = new Vector3(0f, 0.15f, 0f);

        ParticleSystem psSpark = sparkGO.AddComponent<ParticleSystem>();
        ParticleSystemRenderer psrSpark = sparkGO.GetComponent<ParticleSystemRenderer>();
        psrSpark.sharedMaterial = sparkleMat;

        var mainS = psSpark.main;
        mainS.loop = true;
        mainS.duration = 3.0f;
        mainS.startLifetime = new ParticleSystem.MinMaxCurve(1.0f, 1.8f);
        mainS.startSpeed = new ParticleSystem.MinMaxCurve(0.1f, 0.25f);
        mainS.startSize = new ParticleSystem.MinMaxCurve(0.14f, 0.25f);
        mainS.startRotation = new ParticleSystem.MinMaxCurve(0f, Mathf.PI * 2f);
        mainS.maxParticles = 35;

        var rotS = psSpark.rotationOverLifetime;
        rotS.enabled = true;
        rotS.z = new ParticleSystem.MinMaxCurve(-1.5f, 1.5f);

        var emitS = psSpark.emission;
        emitS.rateOverTime = 10f;

        var shapeS = psSpark.shape;
        shapeS.shapeType = ParticleSystemShapeType.Box;
        shapeS.scale = new Vector3(2.8f, 0.3f, 2.8f);

        var colS = psSpark.colorOverLifetime;
        colS.enabled = true;
        colS.color = CreateGradient(
            new Color(0.85f, 1.0f, 1.0f), new Color(0.40f, 0.85f, 1.0f),
            0f, 0.95f, 0.75f, 0f);

        // 5. Ambient Low Mist
        GameObject mistGO = new GameObject("Ambient_Water_Mist");
        mistGO.transform.SetParent(root.transform, false);
        mistGO.transform.localPosition = new Vector3(0f, 0.1f, 0f);

        ParticleSystem psMist = mistGO.AddComponent<ParticleSystem>();
        ParticleSystemRenderer psrMist = mistGO.GetComponent<ParticleSystemRenderer>();
        psrMist.sharedMaterial = mistMat;

        var mainM = psMist.main;
        mainM.loop = true;
        mainM.duration = 4.0f;
        mainM.startLifetime = new ParticleSystem.MinMaxCurve(2.5f, 3.8f);
        mainM.startSpeed = new ParticleSystem.MinMaxCurve(0.08f, 0.2f);
        mainM.startSize = new ParticleSystem.MinMaxCurve(1.2f, 2.0f);
        mainM.maxParticles = 18;

        var emitM = psMist.emission;
        emitM.rateOverTime = 4f;

        var shapeM = psMist.shape;
        shapeM.shapeType = ParticleSystemShapeType.Box;
        shapeM.scale = new Vector3(2.6f, 0.1f, 2.6f);

        var colM = psMist.colorOverLifetime;
        colM.enabled = true;
        colM.color = CreateGradient(
            new Color(0.60f, 0.88f, 1.0f), new Color(0.45f, 0.80f, 0.95f),
            0f, 0.25f, 0.20f, 0f);

        // 6. Spring Luminescence Light
        GameObject lightGO = new GameObject("Spring_Light");
        lightGO.transform.SetParent(root.transform, false);
        lightGO.transform.localPosition = new Vector3(0f, 0.4f, 0f);
        Light pl = lightGO.AddComponent<Light>();
        pl.type = LightType.Point;
        pl.color = new Color(0.32f, 0.82f, 0.98f);
        pl.intensity = 1.4f;
        pl.range = 5.0f;

        PrefabUtility.SaveAsPrefabAsset(root, PrefabPondSpring);
        Object.DestroyImmediate(root);
        Debug.Log("[CreateWaterVFX] Saved: " + PrefabPondSpring);
    }
    #endregion

    #region Prefab 3: Water Splash Impact Burst
    private static void BuildSplashBurstPrefab(Material splashMat, Material dropMat, Material rippleMat, Material sparkleMat, Material mistMat)
    {
        GameObject root = new GameObject("Water_Splash_Impact_Burst_VFX");

        // 1. Upward Geyser Column (Water crown exploding upwards)
        GameObject geyserGO = new GameObject("Water_Geyser_Crown");
        geyserGO.transform.SetParent(root.transform, false);

        ParticleSystem psGeyser = geyserGO.AddComponent<ParticleSystem>();
        ParticleSystemRenderer psrGeyser = geyserGO.GetComponent<ParticleSystemRenderer>();
        psrGeyser.sharedMaterial = splashMat;
        psrGeyser.renderMode = ParticleSystemRenderMode.Stretch;
        psrGeyser.velocityScale = 0.08f;
        psrGeyser.lengthScale = 1.3f;

        var mainG = psGeyser.main;
        mainG.loop = true;
        mainG.duration = 1.5f;
        mainG.startLifetime = new ParticleSystem.MinMaxCurve(0.45f, 0.75f);
        mainG.startSpeed = new ParticleSystem.MinMaxCurve(4.2f, 7.0f);
        mainG.startSize = new ParticleSystem.MinMaxCurve(0.20f, 0.38f);
        mainG.gravityModifier = 2.0f;
        mainG.maxParticles = 80;

        var emitG = psGeyser.emission;
        emitG.rateOverTime = 0f;
        emitG.SetBursts(new ParticleSystem.Burst[] { new ParticleSystem.Burst(0.0f, 36) });

        var shapeG = psGeyser.shape;
        shapeG.shapeType = ParticleSystemShapeType.Cone;
        shapeG.angle = 16f;
        shapeG.radius = 0.15f;

        var colG = psGeyser.colorOverLifetime;
        colG.enabled = true;
        colG.color = CreateGradient(
            new Color(0.95f, 1.0f, 1.0f), new Color(0.55f, 0.88f, 1.0f),
            0f, 0.95f, 0.85f, 0f);

        // 2. Radial Droplets Ring (Flying outwards in wide arc)
        GameObject radGO = new GameObject("Radial_Splash_Droplets");
        radGO.transform.SetParent(root.transform, false);

        ParticleSystem psRad = radGO.AddComponent<ParticleSystem>();
        ParticleSystemRenderer psrRad = radGO.GetComponent<ParticleSystemRenderer>();
        psrRad.sharedMaterial = dropMat;
        psrRad.renderMode = ParticleSystemRenderMode.Stretch;
        psrRad.velocityScale = 0.06f;
        psrRad.lengthScale = 1.2f;

        var mainRad = psRad.main;
        mainRad.loop = true;
        mainRad.duration = 1.5f;
        mainRad.startLifetime = new ParticleSystem.MinMaxCurve(0.5f, 0.85f);
        mainRad.startSpeed = new ParticleSystem.MinMaxCurve(3.2f, 5.8f);
        mainRad.startSize = new ParticleSystem.MinMaxCurve(0.12f, 0.22f);
        mainRad.gravityModifier = 2.4f;
        mainRad.maxParticles = 60;

        var emitRad = psRad.emission;
        emitRad.rateOverTime = 0f;
        emitRad.SetBursts(new ParticleSystem.Burst[] { new ParticleSystem.Burst(0.02f, 30) });

        var shapeRad = psRad.shape;
        shapeRad.shapeType = ParticleSystemShapeType.Hemisphere;
        shapeRad.radius = 0.25f;

        var colRad = psRad.colorOverLifetime;
        colRad.enabled = true;
        colRad.color = CreateGradient(
            new Color(0.90f, 0.98f, 1.0f), new Color(0.40f, 0.80f, 0.98f),
            0f, 0.95f, 0.75f, 0f);

        // 3. Impact Concentric Shockwave Ripples
        GameObject ripGO = new GameObject("Impact_Shockwave_Ripples");
        ripGO.transform.SetParent(root.transform, false);
        ripGO.transform.localPosition = new Vector3(0f, 0.02f, 0f);
        ripGO.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);

        ParticleSystem psRip = ripGO.AddComponent<ParticleSystem>();
        ParticleSystemRenderer psrRip = ripGO.GetComponent<ParticleSystemRenderer>();
        psrRip.sharedMaterial = rippleMat;
        psrRip.renderMode = ParticleSystemRenderMode.Billboard;

        var mainRip = psRip.main;
        mainRip.loop = true;
        mainRip.duration = 1.5f;
        mainRip.startLifetime = 0.85f;
        mainRip.startSpeed = 0f;
        mainRip.startSize = 0.45f;
        mainRip.maxParticles = 10;

        var emitRip = psRip.emission;
        emitRip.rateOverTime = 0f;
        emitRip.SetBursts(new ParticleSystem.Burst[] {
            new ParticleSystem.Burst(0.0f, 1),
            new ParticleSystem.Burst(0.10f, 1)
        });

        var sizeRip = psRip.sizeOverLifetime;
        sizeRip.enabled = true;
        AnimationCurve c = new AnimationCurve();
        c.AddKey(0f, 0.3f);
        c.AddKey(1f, 3.2f);
        sizeRip.size = new ParticleSystem.MinMaxCurve(1f, c);

        var colRip = psRip.colorOverLifetime;
        colRip.enabled = true;
        colRip.color = CreateGradient(
            new Color(0.85f, 0.98f, 1.0f), new Color(0.50f, 0.85f, 1.0f),
            0f, 0.85f, 0.6f, 0f);

        // 4. Splash Flash Light
        GameObject lightGO = new GameObject("Burst_Light");
        lightGO.transform.SetParent(root.transform, false);
        lightGO.transform.localPosition = new Vector3(0f, 0.5f, 0f);
        Light pl = lightGO.AddComponent<Light>();
        pl.type = LightType.Point;
        pl.color = new Color(0.40f, 0.88f, 1.0f);
        pl.intensity = 2.0f;
        pl.range = 4.0f;

        PrefabUtility.SaveAsPrefabAsset(root, PrefabSplashBurst);
        Object.DestroyImmediate(root);
        Debug.Log("[CreateWaterVFX] Saved: " + PrefabSplashBurst);
    }
    #endregion

    #region Material & Texture Helpers
    private static Material CreateOrUpdateWaterSurfaceMaterial()
    {
        string matPath = TargetFolder + "/M_CuteStylizedWater.mat";
        Material mat = AssetDatabase.LoadAssetAtPath<Material>(matPath);
        Shader shader = Shader.Find("Universal Render Pipeline/Custom/CuteStylizedWater");

        if (shader == null)
        {
            shader = Shader.Find("Universal Render Pipeline/Lit");
        }

        if (mat == null)
        {
            mat = new Material(shader);
            AssetDatabase.CreateAsset(mat, matPath);
        }
        else
        {
            mat.shader = shader;
        }

        mat.SetColor("_ShallowColor", new Color(0.22f, 0.82f, 0.92f, 0.85f)); // Bright radiant turquoise
        mat.SetColor("_DeepColor", new Color(0.05f, 0.32f, 0.65f, 0.95f));    // Royal sapphire blue
        mat.SetColor("_SunHighlight", new Color(0.50f, 0.95f, 1.0f, 1.0f));
        mat.SetColor("_CausticColor", new Color(0.85f, 1.0f, 1.0f, 0.90f));   // Crisp sun glare
        mat.SetColor("_FoamColor", new Color(1.0f, 1.0f, 1.0f, 0.95f));       // Soft white foam
        mat.SetFloat("_CausticScale", 0.95f);
        mat.SetFloat("_CausticSpeed", 0.35f);
        mat.SetFloat("_CausticStrength", 0.65f);
        mat.SetFloat("_FoamDistance", 0.12f);
        mat.SetFloat("_MeshEdgeFoam", 0.035f);
        mat.SetFloat("_WaveHeight", 0.025f);
        mat.SetFloat("_WaveFrequency", 1.5f);
        mat.SetFloat("_WaveSpeed", 1.3f);
        mat.renderQueue = (int)UnityEngine.Rendering.RenderQueue.Transparent;

        EditorUtility.SetDirty(mat);
        return mat;
    }

    private static Material CreateParticleMaterial(string matName, Texture2D texture, Color baseColor, bool additive)
    {
        string matPath = TargetFolder + "/" + matName + ".mat";
        Material mat = AssetDatabase.LoadAssetAtPath<Material>(matPath);
        Shader shader = Shader.Find("Universal Render Pipeline/Particles/Unlit");

        if (mat == null)
        {
            mat = new Material(shader);
            AssetDatabase.CreateAsset(mat, matPath);
        }
        else
        {
            mat.shader = shader;
        }

        if (texture != null)
        {
            mat.SetTexture("_BaseMap", texture);
        }

        mat.SetColor("_BaseColor", baseColor);
        mat.SetFloat("_Surface", 1); // 1 = Transparent

        if (additive)
        {
            mat.SetFloat("_Blend", 1); // Additive
            mat.SetFloat("_SrcBlend", (float)UnityEngine.Rendering.BlendMode.SrcAlpha);
            mat.SetFloat("_DstBlend", (float)UnityEngine.Rendering.BlendMode.One);
            mat.EnableKeyword("_BLENDMODE_ADD");
            mat.DisableKeyword("_BLENDMODE_ALPHA");
        }
        else
        {
            mat.SetFloat("_Blend", 0); // Alpha
            mat.SetFloat("_SrcBlend", (float)UnityEngine.Rendering.BlendMode.SrcAlpha);
            mat.SetFloat("_DstBlend", (float)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
            mat.EnableKeyword("_BLENDMODE_ALPHA");
            mat.DisableKeyword("_BLENDMODE_ADD");
        }

        mat.SetFloat("_ZWrite", 0);
        mat.SetOverrideTag("RenderType", "Transparent");
        mat.renderQueue = 3000;
        mat.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");

        EditorUtility.SetDirty(mat);
        return mat;
    }

    private static Texture2D GenerateWaterDropTexture()
    {
        string path = TexturesFolder + "/WaterDrop_Stylized.png";
        int size = 256;
        Texture2D tex = new Texture2D(size, size, TextureFormat.RGBA32, false);

        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                float nx = (x / (float)size) * 2f - 1f;
                float ny = (y / (float)size) * 2f - 1f;

                // Seamless organic teardrop centered with plenty of padding:
                float d = 2.0f;
                if (ny < -0.15f)
                {
                    float r = Mathf.Sqrt(nx * nx + (ny + 0.15f) * (ny + 0.15f));
                    d = r / 0.40f;
                }
                else if (ny <= 0.65f)
                {
                    float t = (ny + 0.15f) / 0.80f; // 0 to 1
                    float width = 0.40f * Mathf.Pow(1.0f - t, 1.25f);
                    d = Mathf.Abs(nx) / (width + 0.001f);
                }

                float alpha = Mathf.Clamp01((1.0f - d) / 0.12f);

                // Specular gleam in upper-left
                float specDist = Mathf.Sqrt((nx + 0.10f) * (nx + 0.10f) + (ny - 0.05f) * (ny - 0.05f));
                float spec = Mathf.Clamp01((0.12f - specDist) / 0.07f) * 0.95f;

                Color c = Color.Lerp(new Color(0.60f, 0.90f, 1.0f, alpha), Color.white, spec);
                c.a = Mathf.Max(alpha * 0.95f, spec);
                tex.SetPixel(x, y, c);
            }
        }

        tex.Apply();
        SaveTextureAndImport(tex, path);
        return AssetDatabase.LoadAssetAtPath<Texture2D>(path);
    }

    private static Texture2D GenerateWaterRippleTexture()
    {
        string path = TexturesFolder + "/WaterRipple_Caustic.png";
        int size = 256;
        Texture2D tex = new Texture2D(size, size, TextureFormat.RGBA32, false);

        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                float nx = (x / (float)size) * 2f - 1f;
                float ny = (y / (float)size) * 2f - 1f;
                float dist = Mathf.Sqrt(nx * nx + ny * ny);

                // Concentric ring between 0.45 and 0.72 with generous padding to edge
                float ring = 0f;
                if (dist >= 0.35f && dist <= 0.75f)
                {
                    ring = 1.0f - Mathf.Abs(dist - 0.55f) / 0.20f;
                    ring = Mathf.SmoothStep(0f, 1f, ring);
                }

                Color c = new Color(0.80f, 0.95f, 1.0f, ring * 0.95f);
                tex.SetPixel(x, y, c);
            }
        }

        tex.Apply();
        SaveTextureAndImport(tex, path);
        return AssetDatabase.LoadAssetAtPath<Texture2D>(path);
    }

    private static Texture2D GenerateWaterBubbleTexture()
    {
        string path = TexturesFolder + "/WaterBubble_Gloss.png";
        int size = 256;
        Texture2D tex = new Texture2D(size, size, TextureFormat.RGBA32, false);

        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                float nx = (x / (float)size) * 2f - 1f;
                float ny = (y / (float)size) * 2f - 1f;
                float dist = Mathf.Sqrt(nx * nx + ny * ny);

                // Bubble radius = 0.65 with generous margin
                if (dist > 0.75f)
                {
                    tex.SetPixel(x, y, Color.clear);
                    continue;
                }

                float normDist = dist / 0.65f;
                float outerFade = Mathf.Clamp01((0.75f - dist) / 0.10f);

                // Spherical rim highlight
                float rim = Mathf.Pow(Mathf.Clamp01(normDist), 3.0f) * 0.90f;
                float body = Mathf.Clamp01(1.0f - normDist) * 0.18f;

                // Glossy crescent reflection
                float specDist = Mathf.Sqrt((nx + 0.22f) * (nx + 0.22f) + (ny - 0.22f) * (ny - 0.22f));
                float spec = Mathf.Clamp01((0.18f - specDist) / 0.10f) * 0.95f;

                float alpha = Mathf.Clamp01((rim + body + spec) * outerFade);
                Color c = Color.Lerp(new Color(0.65f, 0.92f, 1.0f, alpha), Color.white, spec);
                c.a = alpha;
                tex.SetPixel(x, y, c);
            }
        }

        tex.Apply();
        SaveTextureAndImport(tex, path);
        return AssetDatabase.LoadAssetAtPath<Texture2D>(path);
    }

    private static Texture2D GenerateWaterSplashTexture()
    {
        string path = TexturesFolder + "/WaterSplash_Arc.png";
        int size = 256;
        Texture2D tex = new Texture2D(size, size, TextureFormat.RGBA32, false);

        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                float nx = (x / (float)size) * 2f - 1f;
                float ny = (y / (float)size) * 2f - 1f;

                // Tapered diamond droplet with generous 25% padding
                float taper = 1.0f - (Mathf.Abs(ny) / 0.75f);
                if (taper <= 0f)
                {
                    tex.SetPixel(x, y, Color.clear);
                    continue;
                }

                float width = 0.35f * Mathf.Pow(Mathf.Clamp01(taper), 0.7f);
                float d = Mathf.Abs(nx) / (width + 0.001f);

                float alpha = Mathf.Clamp01((1.0f - d) / 0.15f) * Mathf.Clamp01(taper / 0.15f);
                float spec = (1.0f - Mathf.Abs(nx) / 0.12f) * 0.85f;
                spec = Mathf.Clamp01(spec) * alpha;

                Color c = Color.Lerp(new Color(0.70f, 0.92f, 1.0f, alpha), Color.white, spec);
                c.a = alpha;
                tex.SetPixel(x, y, c);
            }
        }

        tex.Apply();
        SaveTextureAndImport(tex, path);
        return AssetDatabase.LoadAssetAtPath<Texture2D>(path);
    }

    private static void SaveTextureAndImport(Texture2D tex, string path)
    {
        byte[] bytes = tex.EncodeToPNG();
        File.WriteAllBytes(path, bytes);
        AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceSynchronousImport);

        TextureImporter ti = AssetImporter.GetAtPath(path) as TextureImporter;
        if (ti != null)
        {
            ti.textureType = TextureImporterType.Default;
            ti.alphaIsTransparency = true;
            ti.mipmapEnabled = false;
            ti.wrapMode = TextureWrapMode.Clamp;
            ti.SaveAndReimport();
        }
    }

    private static ParticleSystem.MinMaxGradient CreateGradient(Color c1, Color c2, float a0, float a1, float a2, float a3)
    {
        Gradient g = new Gradient();
        g.SetKeys(
            new GradientColorKey[] { new GradientColorKey(c1, 0f), new GradientColorKey(c2, 1f) },
            new GradientAlphaKey[] {
                new GradientAlphaKey(a0, 0f),
                new GradientAlphaKey(a1, 0.2f),
                new GradientAlphaKey(a2, 0.7f),
                new GradientAlphaKey(a3, 1f)
            }
        );
        return new ParticleSystem.MinMaxGradient(g);
    }

    private static void EnsureFolderExists(string folderPath)
    {
        if (!AssetDatabase.IsValidFolder(folderPath))
        {
            string parent = Path.GetDirectoryName(folderPath).Replace('\\', '/');
            string folderName = Path.GetFileName(folderPath);
            if (!AssetDatabase.IsValidFolder(parent))
            {
                EnsureFolderExists(parent);
            }
            AssetDatabase.CreateFolder(parent, folderName);
        }
    }
    #endregion

    #region Screenshot Showcase
    [MenuItem("Tools/Rootbound Guardian/Capture Water VFX Showcase")]
    public static void CaptureWaterVFXScreenshot()
    {
        GameObject showcaseRoot = new GameObject("TempWaterShowcase");
        showcaseRoot.transform.position = new Vector3(300f, 300f, 300f); // Isolated from any scene objects

        Vector3 origin = showcaseRoot.transform.position;

        // 1. Warm Garden Deck / Dark Soil Floor (Generously sized 60x60m)
        GameObject floor = GameObject.CreatePrimitive(PrimitiveType.Plane);
        floor.name = "Showcase_Floor";
        floor.transform.SetParent(showcaseRoot.transform);
        floor.transform.position = origin;
        floor.transform.localScale = new Vector3(6f, 1f, 6f);

        Material floorMat = new Material(Shader.Find("Universal Render Pipeline/Lit"));
        floorMat.color = new Color(0.12f, 0.09f, 0.07f); // Rich warm soil
        floorMat.SetFloat("_Smoothness", 0.08f);
        floor.GetComponent<MeshRenderer>().sharedMaterial = floorMat;

        // 2. Spawn Prefabs Side by Side
        // Zone 1 (Left): Farm Watering Can Splash
        GameObject p1 = PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(PrefabFarmWatering)) as GameObject;
        p1.transform.SetParent(showcaseRoot.transform);
        p1.transform.position = origin + new Vector3(-4.2f, 0f, 0f);

        // Zone 2 (Center): Stylized Water Pond / Spring (elevated slightly above floor)
        GameObject p2 = PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPondSpring)) as GameObject;
        p2.transform.SetParent(showcaseRoot.transform);
        p2.transform.position = origin + new Vector3(0f, 0.08f, 0f);
        p2.transform.localScale = new Vector3(0.75f, 1f, 0.75f);

        // Zone 3 (Right): Water Splash Impact Burst
        GameObject p3 = PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(PrefabSplashBurst)) as GameObject;
        p3.transform.SetParent(showcaseRoot.transform);
        p3.transform.position = origin + new Vector3(4.2f, 0f, 0f);

        // 3. Simulate particle systems
        ParticleSystem[] ps1 = p1.GetComponentsInChildren<ParticleSystem>();
        foreach (var ps in ps1) { ps.Play(); ps.Simulate(1.2f, true, true); }

        ParticleSystem[] ps2 = p2.GetComponentsInChildren<ParticleSystem>();
        foreach (var ps in ps2) { ps.Play(); ps.Simulate(1.5f, true, true); }

        ParticleSystem[] ps3 = p3.GetComponentsInChildren<ParticleSystem>();
        foreach (var ps in ps3) { ps.Play(); ps.Simulate(0.24f, true, true); }

        // 4. Studio Lighting
        GameObject dirLightGO = new GameObject("Showcase_SunLight");
        dirLightGO.transform.SetParent(showcaseRoot.transform);
        dirLightGO.transform.rotation = Quaternion.Euler(42f, -35f, 0f);
        Light dirL = dirLightGO.AddComponent<Light>();
        dirL.type = LightType.Directional;
        dirL.color = new Color(1.0f, 0.96f, 0.90f);
        dirL.intensity = 1.25f;

        GameObject ambLightGO = new GameObject("Showcase_SkyFillLight");
        ambLightGO.transform.SetParent(showcaseRoot.transform);
        ambLightGO.transform.position = origin + new Vector3(0f, 5.0f, -3.0f);
        Light ambL = ambLightGO.AddComponent<Light>();
        ambL.type = LightType.Point;
        ambL.color = new Color(0.45f, 0.85f, 1.0f);
        ambL.intensity = 1.6f;
        ambL.range = 20f;

        // 5. Cinematic Camera
        GameObject camGO = new GameObject("Showcase_Cam");
        camGO.transform.SetParent(showcaseRoot.transform);
        camGO.transform.position = origin + new Vector3(0f, 4.2f, -7.5f);
        camGO.transform.rotation = Quaternion.Euler(24f, 0f, 0f);

        Camera cam = camGO.AddComponent<Camera>();
        cam.clearFlags = CameraClearFlags.SolidColor;
        cam.backgroundColor = new Color(0.06f, 0.05f, 0.04f, 1f); // Deep cozy night
        cam.fieldOfView = 50f;

        // 6. Showcase Overlay Canvas (Title and Presentation Cards)
        GameObject canvasGO = new GameObject("ShowcaseCanvas");
        canvasGO.transform.SetParent(showcaseRoot.transform, false);
        Canvas canvas = canvasGO.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceCamera;
        canvas.worldCamera = cam;
        canvas.planeDistance = 2.0f; // Directly in front of camera lens so it's never occluded by 3D objects!
        canvas.sortingOrder = 50;

        CanvasScaler scaler = canvasGO.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);
        scaler.matchWidthOrHeight = 0.5f;

        Font font = AssetDatabase.LoadAssetAtPath<Font>("Assets/Itim-Regular.ttf");
        if (font == null) font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");

        // Header Title Card
        GameObject headerCardGO = new GameObject("HeaderCard");
        headerCardGO.transform.SetParent(canvasGO.transform, false);
        Image hcImg = headerCardGO.AddComponent<Image>();
        hcImg.color = new Color(0.12f, 0.09f, 0.07f, 0.95f);
        RectTransform hcRT = headerCardGO.GetComponent<RectTransform>();
        hcRT.anchorMin = new Vector2(0.5f, 1f);
        hcRT.anchorMax = new Vector2(0.5f, 1f);
        hcRT.pivot = new Vector2(0.5f, 1f);
        hcRT.anchoredPosition = new Vector2(0f, -25f);
        hcRT.sizeDelta = new Vector2(1000f, 85f);

        GameObject hcBorderGO = new GameObject("Border");
        hcBorderGO.transform.SetParent(headerCardGO.transform, false);
        Image hcbImg = hcBorderGO.AddComponent<Image>();
        hcbImg.color = new Color(0.85f, 0.68f, 0.32f, 0.90f);
        RectTransform hcbRT = hcBorderGO.GetComponent<RectTransform>();
        hcbRT.anchorMin = Vector2.zero;
        hcbRT.anchorMax = Vector2.one;
        hcbRT.sizeDelta = new Vector2(4f, 4f);
        hcBorderGO.transform.SetAsFirstSibling();

        GameObject titleGO = new GameObject("Title");
        titleGO.transform.SetParent(headerCardGO.transform, false);
        Text titleTxt = titleGO.AddComponent<Text>();
        titleTxt.font = font;
        titleTxt.fontSize = 26;
        titleTxt.fontStyle = FontStyle.Bold;
        titleTxt.alignment = TextAnchor.MiddleCenter;
        titleTxt.color = new Color(1.0f, 0.88f, 0.45f);
        titleTxt.text = "ROOTBOUND GUARDIAN • WATER VFX SUITE";
        var tOut = titleGO.AddComponent<Outline>();
        tOut.effectColor = new Color(0.08f, 0.05f, 0.02f, 0.95f);
        tOut.effectDistance = new Vector2(1.5f, -1.5f);
        RectTransform tRT = titleGO.GetComponent<RectTransform>();
        tRT.anchorMin = Vector2.zero;
        tRT.anchorMax = Vector2.one;
        tRT.anchoredPosition = new Vector2(0f, 12f);

        GameObject subGO = new GameObject("Subtitle");
        subGO.transform.SetParent(headerCardGO.transform, false);
        Text subTxt = subGO.AddComponent<Text>();
        subTxt.font = font;
        subTxt.fontSize = 15;
        subTxt.alignment = TextAnchor.MiddleCenter;
        subTxt.color = new Color(0.85f, 0.80f, 0.72f);
        subTxt.text = "✦ สไตล์ Cozy Farm & Fairytale RPG • ละอองรดน้ำแปลงผัก • สระน้ำมหาพฤกษา • เอฟเฟกต์น้ำกระเซ็น ✦";
        RectTransform sRT = subGO.GetComponent<RectTransform>();
        sRT.anchorMin = Vector2.zero;
        sRT.anchorMax = Vector2.one;
        sRT.anchoredPosition = new Vector2(0f, -16f);

        // 3 Zone Presentation Cards
        CreateZoneBadge(canvasGO.transform, font, "1. Farm Watering Can Splash", "ละอองรดน้ำแปลงผัก & ประกายพฤกษาชุ่มฉ่ำ", new Vector2(-550f, 230f));
        CreateZoneBadge(canvasGO.transform, font, "2. Stylized Spring Pond", "สระน้ำใสคริสตัล & ฟองปุดผิวน้ำพร้อมระลอกคลื่น", new Vector2(0f, 230f));
        CreateZoneBadge(canvasGO.transform, font, "3. Water Splash Impact Burst", "น้ำพุกระจายตัว & คลื่นกระแทก Radial Shockwave", new Vector2(550f, 230f));

        // Footer Card
        GameObject footCardGO = new GameObject("FooterCard");
        footCardGO.transform.SetParent(canvasGO.transform, false);
        Image fcImg = footCardGO.AddComponent<Image>();
        fcImg.color = new Color(0.12f, 0.09f, 0.07f, 0.95f);
        RectTransform fcRT = footCardGO.GetComponent<RectTransform>();
        fcRT.anchorMin = new Vector2(0.5f, 0f);
        fcRT.anchorMax = new Vector2(0.5f, 0f);
        fcRT.pivot = new Vector2(0.5f, 0f);
        fcRT.anchoredPosition = new Vector2(0f, 25f);
        fcRT.sizeDelta = new Vector2(1000f, 65f);

        GameObject fcbGO = new GameObject("Border");
        fcbGO.transform.SetParent(footCardGO.transform, false);
        Image fcbImg = fcbGO.AddComponent<Image>();
        fcbImg.color = new Color(0.55f, 0.42f, 0.20f, 0.8f);
        RectTransform fcbRT = fcbGO.GetComponent<RectTransform>();
        fcbRT.anchorMin = Vector2.zero;
        fcbRT.anchorMax = Vector2.one;
        fcbRT.sizeDelta = new Vector2(2f, 2f);
        fcbGO.transform.SetAsFirstSibling();

        GameObject fTxtGO = new GameObject("Text");
        fTxtGO.transform.SetParent(footCardGO.transform, false);
        Text fTxt = fTxtGO.AddComponent<Text>();
        fTxt.font = font;
        fTxt.fontSize = 14;
        fTxt.alignment = TextAnchor.MiddleCenter;
        fTxt.color = new Color(0.95f, 0.90f, 0.80f);
        fTxt.text = "✦ URP Translucent Water Shader (Voronoi Animated Caustics & Rim Foam) • 100% Procedural Alpha Textures ✦\n✦ Prefab พร้อมใช้งานใน Assets/Prefabs/VFX/Water_VFX/ รองรับทั้งทำฟาร์ม สระน้ำ และการต่อสู้ ✦";
        RectTransform fRT = fTxtGO.GetComponent<RectTransform>();
        fRT.anchorMin = Vector2.zero;
        fRT.anchorMax = Vector2.one;

        Canvas.ForceUpdateCanvases();

        // Render to Texture
        int w = 1920;
        int h = 1080;
        RenderTexture rt = new RenderTexture(w, h, 24, RenderTextureFormat.ARGB32);
        rt.Create();
        cam.targetTexture = rt;
        cam.Render();

        RenderTexture.active = rt;
        Texture2D tex = new Texture2D(w, h, TextureFormat.RGB24, false);
        tex.ReadPixels(new Rect(0, 0, w, h), 0, 0);
        tex.Apply();

        byte[] bytes = tex.EncodeToPNG();
        File.WriteAllBytes(ScreenshotPath, bytes);

        // Cleanup
        RenderTexture.active = null;
        cam.targetTexture = null;
        rt.Release();
        Object.DestroyImmediate(rt);
        Object.DestroyImmediate(tex);
        Object.DestroyImmediate(showcaseRoot);

        Debug.Log("[CreateWaterVFX] Captured refined Water VFX Showcase to: " + ScreenshotPath);
    }

    private static void CreateZoneBadge(Transform parent, Font font, string title, string subtitle, Vector2 pos)
    {
        GameObject cardGO = new GameObject("Badge_" + pos.x);
        cardGO.transform.SetParent(parent, false);

        Image img = cardGO.AddComponent<Image>();
        img.color = new Color(0.12f, 0.09f, 0.07f, 0.95f);

        RectTransform rt = cardGO.GetComponent<RectTransform>();
        rt.anchorMin = new Vector2(0.5f, 0.5f);
        rt.anchorMax = new Vector2(0.5f, 0.5f);
        rt.pivot = new Vector2(0.5f, 0.5f);
        rt.anchoredPosition = pos;
        rt.sizeDelta = new Vector2(380f, 65f);

        GameObject borderGO = new GameObject("Border");
        borderGO.transform.SetParent(cardGO.transform, false);
        Image bImg = borderGO.AddComponent<Image>();
        bImg.color = new Color(0.70f, 0.55f, 0.25f, 0.85f);
        RectTransform bRT = borderGO.GetComponent<RectTransform>();
        bRT.anchorMin = Vector2.zero;
        bRT.anchorMax = Vector2.one;
        bRT.sizeDelta = new Vector2(3f, 3f);
        borderGO.transform.SetAsFirstSibling();

        GameObject titleGO = new GameObject("Title");
        titleGO.transform.SetParent(cardGO.transform, false);
        Text t = titleGO.AddComponent<Text>();
        t.font = font;
        t.fontSize = 15;
        t.fontStyle = FontStyle.Bold;
        t.alignment = TextAnchor.MiddleCenter;
        t.color = new Color(0.50f, 0.92f, 1.0f);
        t.text = title;
        RectTransform tRT = titleGO.GetComponent<RectTransform>();
        tRT.anchorMin = Vector2.zero;
        tRT.anchorMax = Vector2.one;
        tRT.anchoredPosition = new Vector2(0f, 10f);

        GameObject subGO = new GameObject("Sub");
        subGO.transform.SetParent(cardGO.transform, false);
        Text s = subGO.AddComponent<Text>();
        s.font = font;
        s.fontSize = 13;
        s.alignment = TextAnchor.MiddleCenter;
        s.color = new Color(0.85f, 0.80f, 0.72f);
        s.text = subtitle;
        RectTransform sRT = subGO.GetComponent<RectTransform>();
        sRT.anchorMin = Vector2.zero;
        sRT.anchorMax = Vector2.one;
        sRT.anchoredPosition = new Vector2(0f, -12f);
    }
    #endregion
}
#endif
