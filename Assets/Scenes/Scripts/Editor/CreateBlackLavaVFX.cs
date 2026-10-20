using UnityEngine;
using UnityEditor;
using System.IO;

public static class CreateBlackLavaVFX
{
    private const string TargetFolder = "Assets/Prefabs/VFX/BlackLava_VFX";
    private const string PrefabPath = "Assets/Prefabs/VFX/BlackLava_VFX/Black_Lava_Prominence_Complete_VFX.prefab";

    [MenuItem("Tools/SproutScout/Create Black Lava with Solar Prominence VFX")]
    public static GameObject CreateAndSaveBlackLava()
    {
        EnsureFolderExists(TargetFolder);

        // 1. Generate or load Materials
        Material lavaSurfaceMat = CreateOrUpdateBlackLavaSurfaceMaterial();
        Material bubbleMat = CreateOrUpdateParticleMaterial("M_BlackLavaBubble", new Color(0.06f, 0.05f, 0.08f, 0.95f), false);
        Material prominenceMat = CreateOrUpdateParticleMaterial("M_BlackSolarProminence", new Color(0.04f, 0.03f, 0.06f, 0.98f), false);
        Material ashMat = CreateOrUpdateParticleMaterial("M_BlackLavaAsh", new Color(0.08f, 0.07f, 0.10f, 0.70f), false);
        Material splashMat = CreateOrUpdateParticleMaterial("M_BlackLavaSplash", new Color(0.05f, 0.04f, 0.07f, 0.92f), false);

        // 2. Build Root GameObject
        GameObject root = new GameObject("Black_Lava_Prominence_Complete_VFX");

        // 3. Create Surface Lava Plane
        GameObject planeObj = GameObject.CreatePrimitive(PrimitiveType.Plane);
        planeObj.name = "Black_Lava_Surface";
        planeObj.transform.SetParent(root.transform);
        planeObj.transform.localPosition = Vector3.zero;
        planeObj.transform.localRotation = Quaternion.identity;
        planeObj.transform.localScale = new Vector3(0.5f, 1f, 0.5f); // 5x5m surface

        MeshRenderer mr = planeObj.GetComponent<MeshRenderer>();
        if (mr != null)
        {
            mr.sharedMaterial = lavaSurfaceMat;
            mr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            mr.receiveShadows = true;
        }

        // Add DamageDealer component
        System.Type damageDealerType = System.Type.GetType("DamageDealer, Assembly-CSharp");
        if (damageDealerType != null)
        {
            Component damageDealer = planeObj.AddComponent(damageDealerType);
            var field = damageDealerType.GetField("damageAmount");
            if (field != null) field.SetValue(damageDealer, 100);
        }

        // 4. Create Particle Systems
        // A) Black Solar Prominences (Looping molten black arcs)
        CreateBlackSolarProminences(root.transform, prominenceMat);

        // B) Black Lava Bubbles (Popping bubbles on lava surface)
        CreateBlackLavaBubbles(root.transform, bubbleMat);

        // C) Black Lava Splashes (Droplets spraying from the liquid)
        CreateBlackLavaSplashes(root.transform, splashMat);

        // D) Volcanic Ash & Soot (Dark specks floating upward)
        CreateBlackLavaAsh(root.transform, ashMat);

        // 5. Add Subtle Dark Heat Point Light
        GameObject lightObj = new GameObject("Black_Lava_Warmth_Light");
        lightObj.transform.SetParent(root.transform);
        lightObj.transform.localPosition = new Vector3(0f, 0.35f, 0f);
        Light pointLight = lightObj.AddComponent<Light>();
        pointLight.type = LightType.Point;
        pointLight.color = new Color(0.28f, 0.16f, 0.38f); // Deep smoky obsidian violet warmth
        pointLight.intensity = 1.4f;
        pointLight.range = 4.0f;
        pointLight.shadows = LightShadows.None;

        // 6. Save Prefab
        GameObject savedPrefab = PrefabUtility.SaveAsPrefabAsset(root, PrefabPath);
        Debug.Log("<color=#7a38b3><b>[Black Lava VFX]</b> Successfully created and saved prefab to: " + PrefabPath + "</color>");

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

    private static Material CreateOrUpdateBlackLavaSurfaceMaterial()
    {
        string matPath = TargetFolder + "/M_StylizedBlackLava.mat";
        Material mat = AssetDatabase.LoadAssetAtPath<Material>(matPath);
        Shader shader = Shader.Find("Universal Render Pipeline/Custom/StylizedBlackLava");

        if (shader == null)
        {
            Debug.LogError("[Black Lava VFX] Could not find shader Universal Render Pipeline/Custom/StylizedBlackLava");
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

        // Apply authentic black lava palette
        mat.SetColor("_BaseColor", new Color(0.031f, 0.031f, 0.039f, 1.0f)); // #08080a Pitch Black Obsidian Crust
        mat.SetColor("_MidColor", new Color(0.102f, 0.078f, 0.129f, 1.0f));  // #1a1421 Dark Molten Charcoal
        mat.SetColor("_CoreColor", new Color(0.180f, 0.122f, 0.239f, 1.0f)); // #2e1f3d Molten Dark Core Blobs
        mat.SetColor("_HighlightColor", new Color(0.322f, 0.200f, 0.420f, 1.0f)); // #52336b Vein Glint
        mat.SetColor("_RimColor", new Color(0.141f, 0.122f, 0.180f, 1.0f));       // #241f2e Dark Lava Froth

        mat.SetFloat("_EmissionIntensity", 1.25f);
        mat.SetFloat("_PulseSpeed", 1.4f);
        mat.SetFloat("_PulseAmount", 0.15f);
        mat.SetFloat("_Glossiness", 24.0f);
        mat.SetFloat("_SpecularStrength", 0.85f);
        mat.SetFloat("_WorldScale", 0.18f);
        mat.SetFloat("_FlowSpeed", 0.22f);
        mat.SetVector("_FlowDirection", new Vector4(0.35f, 0.93f, 0, 0));
        mat.SetFloat("_CrustStep", 0.42f);
        mat.SetFloat("_OrangeStep", 0.62f);
        mat.SetFloat("_HotSpotStep", 0.78f);
        mat.SetFloat("_StepSmoothness", 0.05f);
        mat.SetFloat("_WaveHeight", 0.06f);
        mat.SetFloat("_WaveFrequency", 1.2f);
        mat.SetFloat("_WaveSpeed", 1.5f);
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

        Texture2D glowTex = AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/Image/VFX/SoftGlow.png");
        if (glowTex == null)
        {
            glowTex = AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/Prefabs/VFX/Lava_VFX/SoftGlow.png");
        }
        if (glowTex != null)
        {
            mat.SetTexture("_BaseMap", glowTex);
        }

        mat.SetColor("_BaseColor", baseColor);
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

    private static void CreateBlackSolarProminences(Transform parent, Material mat)
    {
        GameObject go = new GameObject("Black_Solar_Prominences");
        go.transform.SetParent(parent);
        go.transform.localPosition = new Vector3(0f, 0.05f, 0f);
        go.transform.localRotation = Quaternion.Euler(-90f, 0f, 0f);

        ParticleSystem ps = go.AddComponent<ParticleSystem>();
        ParticleSystemRenderer psr = go.GetComponent<ParticleSystemRenderer>();
        psr.sharedMaterial = mat;
        psr.trailMaterial = mat;

        var main = ps.main;
        main.loop = true;
        main.duration = 4.0f;
        main.startLifetime = new ParticleSystem.MinMaxCurve(2.0f, 3.2f);
        main.startSpeed = new ParticleSystem.MinMaxCurve(2.8f, 4.0f);
        main.startSize = new ParticleSystem.MinMaxCurve(0.2f, 0.4f);
        main.maxParticles = 60;
        main.simulationSpace = ParticleSystemSimulationSpace.World;

        var emission = ps.emission;
        emission.rateOverTime = 12f;

        // Shape: Emits from surface of the lava pool
        var shape = ps.shape;
        shape.shapeType = ParticleSystemShapeType.Box;
        shape.scale = new Vector3(3.5f, 3.5f, 0.1f);

        // Velocity Over Lifetime: Prominence Loop Arc (Erupts upward, arcs across, plunges back into lava)
        var vol = ps.velocityOverLifetime;
        vol.enabled = true;
        vol.space = ParticleSystemSimulationSpace.Local;

        AnimationCurve yArc = new AnimationCurve();
        yArc.AddKey(new Keyframe(0.0f, 1.0f, 0.0f, -0.6f));
        yArc.AddKey(new Keyframe(0.5f, 0.1f, -2.8f, -2.8f));
        yArc.AddKey(new Keyframe(1.0f, -1.2f, -1.2f, 0.0f));
        vol.y = new ParticleSystem.MinMaxCurve(3.2f, yArc);

        // X/Z curve for curved lateral looping arc
        vol.orbitalZ = new ParticleSystem.MinMaxCurve(1.2f);

        // Color Over Lifetime: Glossy black molten jet
        var col = ps.colorOverLifetime;
        col.enabled = true;
        Gradient grad = new Gradient();
        grad.SetKeys(
            new GradientColorKey[] {
                new GradientColorKey(new Color(0.12f, 0.08f, 0.16f), 0.0f),  // Dark molten base
                new GradientColorKey(new Color(0.03f, 0.02f, 0.04f), 0.4f),  // Pitch black prominence body
                new GradientColorKey(new Color(0.18f, 0.10f, 0.24f), 0.8f),  // Dark violet crest
                new GradientColorKey(new Color(0.03f, 0.02f, 0.04f), 1.0f)   // Splash back
            },
            new GradientAlphaKey[] {
                new GradientAlphaKey(0.0f, 0.0f),
                new GradientAlphaKey(0.95f, 0.15f),
                new GradientAlphaKey(0.90f, 0.75f),
                new GradientAlphaKey(0.0f, 1.0f)
            }
        );
        col.color = grad;

        // Size: Thick liquid arc
        var sol = ps.sizeOverLifetime;
        sol.enabled = true;
        AnimationCurve sizeCurve = new AnimationCurve();
        sizeCurve.AddKey(0.0f, 0.4f);
        sizeCurve.AddKey(0.45f, 1.1f);
        sizeCurve.AddKey(1.0f, 0.1f);
        sol.size = new ParticleSystem.MinMaxCurve(1.0f, sizeCurve);

        // Trails Module: Molten liquid ribbon trail
        var trails = ps.trails;
        trails.enabled = true;
        trails.ratio = 1.0f;
        trails.lifetime = new ParticleSystem.MinMaxCurve(0.6f);
        trails.minVertexDistance = 0.05f;
        trails.widthOverTrail = new ParticleSystem.MinMaxCurve(1.0f, new AnimationCurve(new Keyframe(0f, 1.0f), new Keyframe(1f, 0.1f)));
        trails.colorOverLifetime = grad;
    }

    private static void CreateBlackLavaBubbles(Transform parent, Material mat)
    {
        GameObject go = new GameObject("Black_Lava_Bubbles");
        go.transform.SetParent(parent);
        go.transform.localPosition = new Vector3(0f, 0.02f, 0f);
        go.transform.localRotation = Quaternion.Euler(-90f, 0f, 0f);

        ParticleSystem ps = go.AddComponent<ParticleSystem>();
        ParticleSystemRenderer psr = go.GetComponent<ParticleSystemRenderer>();
        psr.sharedMaterial = mat;

        var main = ps.main;
        main.loop = true;
        main.duration = 3.0f;
        main.startLifetime = new ParticleSystem.MinMaxCurve(1.2f, 2.2f);
        main.startSpeed = new ParticleSystem.MinMaxCurve(0.05f, 0.18f);
        main.startSize = new ParticleSystem.MinMaxCurve(0.2f, 0.5f);
        main.maxParticles = 40;
        main.simulationSpace = ParticleSystemSimulationSpace.World;

        var emission = ps.emission;
        emission.rateOverTime = 8f;

        var shape = ps.shape;
        shape.shapeType = ParticleSystemShapeType.Box;
        shape.scale = new Vector3(3.8f, 3.8f, 0.05f);

        // Size: Grows like a bubble swelling then pops!
        var sol = ps.sizeOverLifetime;
        sol.enabled = true;
        AnimationCurve bubbleCurve = new AnimationCurve();
        bubbleCurve.AddKey(0.0f, 0.1f);
        bubbleCurve.AddKey(0.7f, 0.85f);
        bubbleCurve.AddKey(0.92f, 1.1f);  // Swell
        bubbleCurve.AddKey(1.0f, 0.0f);   // Pop!
        sol.size = new ParticleSystem.MinMaxCurve(1.0f, bubbleCurve);

        // Color: Deep glossy black with slight shine
        var col = ps.colorOverLifetime;
        col.enabled = true;
        Gradient grad = new Gradient();
        grad.SetKeys(
            new GradientColorKey[] {
                new GradientColorKey(new Color(0.08f, 0.07f, 0.10f), 0.0f),
                new GradientColorKey(new Color(0.04f, 0.03f, 0.05f), 0.85f),
                new GradientColorKey(new Color(0.15f, 0.10f, 0.20f), 1.0f)
            },
            new GradientAlphaKey[] {
                new GradientAlphaKey(0.0f, 0.0f),
                new GradientAlphaKey(0.95f, 0.2f),
                new GradientAlphaKey(0.95f, 0.9f),
                new GradientAlphaKey(0.0f, 1.0f)
            }
        );
        col.color = grad;
    }

    private static void CreateBlackLavaSplashes(Transform parent, Material mat)
    {
        GameObject go = new GameObject("Black_Lava_Splashes");
        go.transform.SetParent(parent);
        go.transform.localPosition = new Vector3(0f, 0.04f, 0f);
        go.transform.localRotation = Quaternion.Euler(-90f, 0f, 0f);

        ParticleSystem ps = go.AddComponent<ParticleSystem>();
        ParticleSystemRenderer psr = go.GetComponent<ParticleSystemRenderer>();
        psr.sharedMaterial = mat;

        var main = ps.main;
        main.loop = true;
        main.duration = 2.0f;
        main.startLifetime = new ParticleSystem.MinMaxCurve(0.6f, 1.1f);
        main.startSpeed = new ParticleSystem.MinMaxCurve(1.2f, 2.5f);
        main.startSize = new ParticleSystem.MinMaxCurve(0.06f, 0.16f);
        main.gravityModifier = 1.6f; // Falls back down to lava surface
        main.maxParticles = 50;

        var emission = ps.emission;
        emission.rateOverTime = 15f;

        var shape = ps.shape;
        shape.shapeType = ParticleSystemShapeType.Box;
        shape.scale = new Vector3(3.6f, 3.6f, 0.05f);

        var col = ps.colorOverLifetime;
        col.enabled = true;
        Gradient grad = new Gradient();
        grad.SetKeys(
            new GradientColorKey[] {
                new GradientColorKey(new Color(0.05f, 0.04f, 0.07f), 0.0f),
                new GradientColorKey(new Color(0.02f, 0.02f, 0.03f), 1.0f)
            },
            new GradientAlphaKey[] {
                new GradientAlphaKey(0.95f, 0.0f),
                new GradientAlphaKey(0.0f, 1.0f)
            }
        );
        col.color = grad;
    }

    private static void CreateBlackLavaAsh(Transform parent, Material mat)
    {
        GameObject go = new GameObject("Black_Lava_Ash");
        go.transform.SetParent(parent);
        go.transform.localPosition = new Vector3(0f, 0.1f, 0f);
        go.transform.localRotation = Quaternion.Euler(-90f, 0f, 0f);

        ParticleSystem ps = go.AddComponent<ParticleSystem>();
        ParticleSystemRenderer psr = go.GetComponent<ParticleSystemRenderer>();
        psr.sharedMaterial = mat;

        var main = ps.main;
        main.loop = true;
        main.duration = 4.0f;
        main.startLifetime = new ParticleSystem.MinMaxCurve(2.5f, 4.0f);
        main.startSpeed = new ParticleSystem.MinMaxCurve(0.3f, 0.8f);
        main.startSize = new ParticleSystem.MinMaxCurve(0.04f, 0.12f);
        main.maxParticles = 60;
        main.simulationSpace = ParticleSystemSimulationSpace.World;

        var emission = ps.emission;
        emission.rateOverTime = 14f;

        var shape = ps.shape;
        shape.shapeType = ParticleSystemShapeType.Box;
        shape.scale = new Vector3(4.0f, 4.0f, 0.1f);

        // Organic volcanic drift
        var noise = ps.noise;
        noise.enabled = true;
        noise.strength = 0.35f;
        noise.frequency = 0.5f;

        var col = ps.colorOverLifetime;
        col.enabled = true;
        Gradient grad = new Gradient();
        grad.SetKeys(
            new GradientColorKey[] {
                new GradientColorKey(new Color(0.10f, 0.08f, 0.12f), 0.0f),
                new GradientColorKey(new Color(0.04f, 0.03f, 0.05f), 1.0f)
            },
            new GradientAlphaKey[] {
                new GradientAlphaKey(0.0f, 0.0f),
                new GradientAlphaKey(0.7f, 0.2f),
                new GradientAlphaKey(0.0f, 1.0f)
            }
        );
        col.color = grad;
    }
}
