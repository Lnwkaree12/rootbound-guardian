using UnityEngine;
using UnityEditor;
using System.IO;

public static class CreateVoidBlackHoleVFX
{
    private const string TargetFolder = "Assets/Prefabs/VFX/Void_VFX";
    private const string PrefabPath = "Assets/Prefabs/VFX/Void_VFX/Void_BlackHole_Prominence_VFX.prefab";

    [MenuItem("Tools/SproutScout/Create Void Black Hole Prominence VFX")]
    public static GameObject CreateAndSaveVFX()
    {
        EnsureFolderExists(TargetFolder);

        // 1. Generate or load Materials
        Material lavaSurfaceMat = CreateOrUpdateLavaMaterial();
        Material prominenceMat = CreateOrUpdateParticleMaterial("M_DarkSolarProminence", new Color(0.05f, 0.01f, 0.12f, 0.95f), true);
        Material accretionMistMat = CreateOrUpdateParticleMaterial("M_AccretionMist", new Color(0.07f, 0.01f, 0.15f, 0.45f), false);
        Material sparklesMat = CreateOrUpdateParticleMaterial("M_SingularitySparkles", new Color(0.0f, 0.94f, 1.0f, 0.85f), true);

        // 2. Build Root GameObject
        GameObject root = new GameObject("Void_BlackHole_Prominence_VFX");

        // 3. Create Surface Void Plane
        GameObject planeObj = GameObject.CreatePrimitive(PrimitiveType.Plane);
        planeObj.name = "Void_Surface_Plane";
        planeObj.transform.SetParent(root.transform);
        planeObj.transform.localPosition = Vector3.zero;
        planeObj.transform.localRotation = Quaternion.identity;
        planeObj.transform.localScale = new Vector3(0.5f, 1f, 0.5f); // 5x5 meter plane

        MeshRenderer mr = planeObj.GetComponent<MeshRenderer>();
        if (mr != null)
        {
            mr.sharedMaterial = lavaSurfaceMat;
            mr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            mr.receiveShadows = true;
        }

        // Add DamageDealer if available in project
        System.Type damageDealerType = System.Type.GetType("DamageDealer, Assembly-CSharp");
        if (damageDealerType != null)
        {
            Component damageDealer = planeObj.AddComponent(damageDealerType);
            var field = damageDealerType.GetField("damageAmount");
            if (field != null) field.SetValue(damageDealer, 100);
        }

        // 4. Create Particle Systems
        // A) Dark Solar Prominences (Looping magnetic plasma arcs with trails)
        CreateDarkSolarProminences(root.transform, prominenceMat);

        // B) Accretion Disk Swirl (Orbital dark matter mist)
        CreateAccretionDiskSwirl(root.transform, accretionMistMat);

        // C) Singularity Sparkles (Inverted dark embers & cosmic sparks)
        CreateSingularitySparkles(root.transform, sparklesMat);

        // 5. Add Singularity Core Point Light
        GameObject lightObj = new GameObject("Singularity_Void_Light");
        lightObj.transform.SetParent(root.transform);
        lightObj.transform.localPosition = new Vector3(0f, 0.4f, 0f);
        Light pointLight = lightObj.AddComponent<Light>();
        pointLight.type = LightType.Point;
        pointLight.color = new Color(0.42f, 0.07f, 1.0f); // Cosmic purple
        pointLight.intensity = 2.4f;
        pointLight.range = 4.5f;
        pointLight.shadows = LightShadows.None;

        // 6. Save Prefab
        GameObject savedPrefab = PrefabUtility.SaveAsPrefabAsset(root, PrefabPath);
        Debug.Log("<color=#6b11ff><b>[Void VFX]</b> Successfully created and saved prefab to: " + PrefabPath + "</color>");

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        return root;
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

    private static Material CreateOrUpdateLavaMaterial()
    {
        string matPath = TargetFolder + "/M_VoidBlackHoleLava.mat";
        Material mat = AssetDatabase.LoadAssetAtPath<Material>(matPath);
        Shader shader = Shader.Find("Universal Render Pipeline/Custom/VoidBlackHoleLava");

        if (shader == null)
        {
            Debug.LogError("[Void VFX] Could not find shader Universal Render Pipeline/Custom/VoidBlackHoleLava");
            return null;
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

        // Apply colors and settings
        mat.SetColor("_CoreColor", new Color(0.020f, 0.0f, 0.039f, 1.0f)); // #05000a Singularity Pitch Black
        mat.SetColor("_PlasmaColor", new Color(0.071f, 0.012f, 0.149f, 1.0f)); // #120326 Dark Matter Swirls
        mat.SetColor("_CosmicPurple", new Color(0.420f, 0.067f, 1.0f, 1.0f)); // #6b11ff Cosmic Purple Arc
        mat.SetColor("_CosmicCyan", new Color(0.0f, 0.941f, 1.0f, 1.0f)); // #00f0ff Event Horizon Cyan
        mat.SetColor("_RimContactColor", new Color(0.420f, 0.067f, 1.0f, 1.0f));

        mat.SetFloat("_EmissionIntensity", 2.8f);
        mat.SetFloat("_PulseSpeed", 1.5f);
        mat.SetFloat("_PulseAmount", 0.22f);
        mat.SetFloat("_WorldScale", 0.16f);
        mat.SetFloat("_SwirlSpeed", 0.75f);
        mat.SetFloat("_DistortionStrength", 0.55f);
        mat.SetFloat("_SingularityRadius", 0.22f);
        mat.SetFloat("_EventHorizonWidth", 0.06f);
        mat.SetFloat("_WaveHeight", 0.05f);
        mat.SetFloat("_WaveFrequency", 1.4f);
        mat.SetFloat("_WaveSpeed", 1.2f);
        mat.SetFloat("_FoamDistance", 0.4f);
        mat.SetFloat("_FoamSharpness", 2.5f);

        EditorUtility.SetDirty(mat);
        return mat;
    }

    private static Material CreateOrUpdateParticleMaterial(string matName, Color baseColor, bool additive)
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

        // Load SoftGlow texture
        Texture2D glowTex = AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/Prefabs/VFX/Lava_VFX/SoftGlow.png");
        if (glowTex != null)
        {
            mat.SetTexture("_BaseMap", glowTex);
        }

        mat.SetColor("_BaseColor", baseColor);

        // Configure transparent rendering
        mat.SetFloat("_Surface", 1); // Transparent
        if (additive)
        {
            mat.SetFloat("_Blend", 1); // Additive
            mat.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
            mat.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.One);
        }
        else
        {
            mat.SetFloat("_Blend", 0); // Alpha
            mat.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
            mat.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
        }

        mat.SetInt("_ZWrite", 0);
        mat.renderQueue = (int)UnityEngine.Rendering.RenderQueue.Transparent;
        mat.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");

        EditorUtility.SetDirty(mat);
        return mat;
    }

    private static void CreateDarkSolarProminences(Transform parent, Material mat)
    {
        GameObject go = new GameObject("Dark_Solar_Prominences");
        go.transform.SetParent(parent);
        go.transform.localPosition = new Vector3(0f, 0.05f, 0f);
        go.transform.localRotation = Quaternion.Euler(-90f, 0f, 0f); // Erupt upward

        ParticleSystem ps = go.AddComponent<ParticleSystem>();
        ParticleSystemRenderer psr = go.GetComponent<ParticleSystemRenderer>();
        psr.sharedMaterial = mat;
        psr.trailMaterial = mat;

        // Main Module
        var main = ps.main;
        main.loop = true;
        main.duration = 4.0f;
        main.startLifetime = new ParticleSystem.MinMaxCurve(2.2f, 3.5f);
        main.startSpeed = new ParticleSystem.MinMaxCurve(2.5f, 4.2f);
        main.startSize = new ParticleSystem.MinMaxCurve(0.18f, 0.35f);
        main.maxParticles = 80;
        main.simulationSpace = ParticleSystemSimulationSpace.World;
        main.gravityModifier = 0f; // Controlled via velocity curve for magnetic arc loop

        // Emission
        var emission = ps.emission;
        emission.rateOverTime = 16f;

        // Shape: Donut ring around event horizon boundary
        var shape = ps.shape;
        shape.shapeType = ParticleSystemShapeType.Donut;
        shape.radius = 1.4f;
        shape.donutRadius = 0.3f;
        shape.arc = 360f;

        // Velocity Over Lifetime: Loop Arc trajectory (Erupt up then curve back down into void)
        var vol = ps.velocityOverLifetime;
        vol.enabled = true;
        vol.space = ParticleSystemSimulationSpace.Local;

        // Y: Starts with strong upward thrust (+3.0), curves downward into negative (-3.5)
        AnimationCurve yCurve = new AnimationCurve();
        yCurve.AddKey(new Keyframe(0.0f, 1.0f, 0.0f, -0.5f));
        yCurve.AddKey(new Keyframe(0.45f, 0.1f, -2.5f, -2.5f));
        yCurve.AddKey(new Keyframe(1.0f, -1.2f, -1.0f, 0.0f));
        vol.y = new ParticleSystem.MinMaxCurve(2.8f, yCurve);

        // Orbital Velocity: Gravitational curl around singularity
        vol.orbitalZ = new ParticleSystem.MinMaxCurve(1.5f);
        vol.radial = new ParticleSystem.MinMaxCurve(-0.4f); // Magnetic pull back inward

        // Color Over Lifetime: Cosmic Cyan eruption -> Pitch-Black / Deep Violet Arc -> Cosmic Purple fade
        var col = ps.colorOverLifetime;
        col.enabled = true;
        Gradient grad = new Gradient();
        grad.SetKeys(
            new GradientColorKey[] {
                new GradientColorKey(new Color(0.0f, 0.94f, 1.0f), 0.0f),       // Bright cyan base
                new GradientColorKey(new Color(0.05f, 0.01f, 0.12f), 0.35f),    // Pitch black / dark plasma loop
                new GradientColorKey(new Color(0.42f, 0.07f, 1.0f), 0.75f),     // Cosmic purple arc tip
                new GradientColorKey(new Color(0.05f, 0.01f, 0.12f), 1.0f)      // Vanish into void
            },
            new GradientAlphaKey[] {
                new GradientAlphaKey(0.0f, 0.0f),
                new GradientAlphaKey(0.95f, 0.15f),
                new GradientAlphaKey(0.9f, 0.7f),
                new GradientAlphaKey(0.0f, 1.0f)
            }
        );
        col.color = grad;

        // Size Over Lifetime: Loop thickens in mid-arc and tapers at ends
        var sol = ps.sizeOverLifetime;
        sol.enabled = true;
        AnimationCurve sizeCurve = new AnimationCurve();
        sizeCurve.AddKey(0.0f, 0.4f);
        sizeCurve.AddKey(0.5f, 1.2f);
        sizeCurve.AddKey(1.0f, 0.1f);
        sol.size = new ParticleSystem.MinMaxCurve(1.0f, sizeCurve);

        // Noise Module: Magnetic turbulent wiggle
        var noise = ps.noise;
        noise.enabled = true;
        noise.strength = 0.28f;
        noise.frequency = 0.45f;
        noise.scrollSpeed = 0.6f;

        // Trails Module: Plasma tendril ribbon trails
        var trails = ps.trails;
        trails.enabled = true;
        trails.ratio = 1.0f;
        trails.lifetime = new ParticleSystem.MinMaxCurve(0.55f);
        trails.minVertexDistance = 0.05f;
        trails.widthOverTrail = new ParticleSystem.MinMaxCurve(1.0f, new AnimationCurve(new Keyframe(0f, 1f), new Keyframe(1f, 0.05f)));
        trails.colorOverLifetime = grad;
    }

    private static void CreateAccretionDiskSwirl(Transform parent, Material mat)
    {
        GameObject go = new GameObject("Accretion_Disk_Swirl");
        go.transform.SetParent(parent);
        go.transform.localPosition = new Vector3(0f, 0.08f, 0f);
        go.transform.localRotation = Quaternion.Euler(-90f, 0f, 0f);

        ParticleSystem ps = go.AddComponent<ParticleSystem>();
        ParticleSystemRenderer psr = go.GetComponent<ParticleSystemRenderer>();
        psr.sharedMaterial = mat;

        var main = ps.main;
        main.loop = true;
        main.duration = 5.0f;
        main.startLifetime = new ParticleSystem.MinMaxCurve(3.0f, 4.5f);
        main.startSpeed = new ParticleSystem.MinMaxCurve(0.1f, 0.35f);
        main.startSize = new ParticleSystem.MinMaxCurve(1.2f, 2.4f);
        main.startRotation = new ParticleSystem.MinMaxCurve(0f, 360f * Mathf.Deg2Rad);
        main.maxParticles = 50;
        main.simulationSpace = ParticleSystemSimulationSpace.World;

        var emission = ps.emission;
        emission.rateOverTime = 10f;

        var shape = ps.shape;
        shape.shapeType = ParticleSystemShapeType.Donut;
        shape.radius = 2.0f;
        shape.donutRadius = 0.9f;

        // Orbital Swirl: Inward spiral toward singularity center
        var vol = ps.velocityOverLifetime;
        vol.enabled = true;
        vol.space = ParticleSystemSimulationSpace.Local;
        vol.orbitalZ = new ParticleSystem.MinMaxCurve(2.2f);
        vol.radial = new ParticleSystem.MinMaxCurve(-0.35f); // Inward suction

        // Color Over Lifetime: Soft dark mist fade in/out
        var col = ps.colorOverLifetime;
        col.enabled = true;
        Gradient grad = new Gradient();
        grad.SetKeys(
            new GradientColorKey[] {
                new GradientColorKey(new Color(0.07f, 0.01f, 0.15f), 0.0f),
                new GradientColorKey(new Color(0.02f, 0.0f, 0.04f), 0.6f),
                new GradientColorKey(new Color(0.42f, 0.07f, 1.0f), 1.0f)
            },
            new GradientAlphaKey[] {
                new GradientAlphaKey(0.0f, 0.0f),
                new GradientAlphaKey(0.45f, 0.3f),
                new GradientAlphaKey(0.35f, 0.7f),
                new GradientAlphaKey(0.0f, 1.0f)
            }
        );
        col.color = grad;

        // Rotation Over Lifetime
        var rot = ps.rotationOverLifetime;
        rot.enabled = true;
        rot.z = new ParticleSystem.MinMaxCurve(25f * Mathf.Deg2Rad);
    }

    private static void CreateSingularitySparkles(Transform parent, Material mat)
    {
        GameObject go = new GameObject("Singularity_Sparkles");
        go.transform.SetParent(parent);
        go.transform.localPosition = new Vector3(0f, 0.06f, 0f);
        go.transform.localRotation = Quaternion.Euler(-90f, 0f, 0f);

        ParticleSystem ps = go.AddComponent<ParticleSystem>();
        ParticleSystemRenderer psr = go.GetComponent<ParticleSystemRenderer>();
        psr.sharedMaterial = mat;

        var main = ps.main;
        main.loop = true;
        main.duration = 3.0f;
        main.startLifetime = new ParticleSystem.MinMaxCurve(1.0f, 2.2f);
        main.startSpeed = new ParticleSystem.MinMaxCurve(0.4f, 1.2f);
        main.startSize = new ParticleSystem.MinMaxCurve(0.08f, 0.22f);
        main.maxParticles = 60;
        main.simulationSpace = ParticleSystemSimulationSpace.World;

        var emission = ps.emission;
        emission.rateOverTime = 22f;

        var shape = ps.shape;
        shape.shapeType = ParticleSystemShapeType.Circle;
        shape.radius = 1.2f;

        var vol = ps.velocityOverLifetime;
        vol.enabled = true;
        vol.space = ParticleSystemSimulationSpace.Local;
        vol.orbitalZ = new ParticleSystem.MinMaxCurve(3.0f);
        vol.radial = new ParticleSystem.MinMaxCurve(-0.55f); // Accelerated inward fall

        var col = ps.colorOverLifetime;
        col.enabled = true;
        Gradient grad = new Gradient();
        grad.SetKeys(
            new GradientColorKey[] {
                new GradientColorKey(new Color(0.0f, 0.94f, 1.0f), 0.0f),    // Cosmic cyan spark
                new GradientColorKey(new Color(0.65f, 0.25f, 1.0f), 0.5f),   // Electric violet
                new GradientColorKey(new Color(0.02f, 0.0f, 0.04f), 1.0f)    // Inverted dark ember
            },
            new GradientAlphaKey[] {
                new GradientAlphaKey(0.0f, 0.0f),
                new GradientAlphaKey(1.0f, 0.2f),
                new GradientAlphaKey(0.8f, 0.6f),
                new GradientAlphaKey(0.0f, 1.0f)
            }
        );
        col.color = grad;

        var sol = ps.sizeOverLifetime;
        sol.enabled = true;
        AnimationCurve sCurve = new AnimationCurve();
        sCurve.AddKey(0.0f, 0.2f);
        sCurve.AddKey(0.25f, 1.0f);
        sCurve.AddKey(0.8f, 0.6f);
        sCurve.AddKey(1.0f, 0.0f);
        sol.size = new ParticleSystem.MinMaxCurve(1.0f, sCurve);
    }
}
