#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;

[InitializeOnLoad]
public class CreateTorchFirePrefab
{
    static CreateTorchFirePrefab()
    {
        // Run once when Unity compiles or launches to ensure the prefab is ready
        EditorApplication.delayCall += EnsurePrefabExists;
    }

    [MenuItem("Tools/SproutScout/Create Torch Fire Prefab")]
    public static void ForceCreatePrefab()
    {
        CreatePrefab(true);
    }

    private static void EnsurePrefabExists()
    {
        CreatePrefab(false);
    }

    private static void CreatePrefab(bool forceOverwrite)
    {
        string prefabPath = "Assets/Prefabs/TorchFire.prefab";
        string matPath = "Assets/Materials/TorchFireMaterial.mat";

        // Skip if already exists and we are not forcing overwrite
        if (!forceOverwrite && AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath) != null)
        {
            return;
        }

        Debug.Log("[CreateTorchFirePrefab] Generating Torch Fire assets...");

        // 1. Ensure directories exist
        if (!AssetDatabase.IsValidFolder("Assets/Materials"))
        {
            AssetDatabase.CreateFolder("Assets", "Materials");
        }
        if (!AssetDatabase.IsValidFolder("Assets/Prefabs"))
        {
            AssetDatabase.CreateFolder("Assets", "Prefabs");
        }

        // 2. Create and configure the Fire Material (URP Particle Unlit Additive)
        Material fireMat = AssetDatabase.LoadAssetAtPath<Material>(matPath);
        if (fireMat == null || forceOverwrite)
        {
            Shader urpParticleShader = Shader.Find("Universal Render Pipeline/Particles/Unlit");
            if (urpParticleShader == null)
            {
                // Fallback to mobile particles if URP shader isn't found
                urpParticleShader = Shader.Find("Mobile/Particles/Additive");
            }
            if (urpParticleShader == null)
            {
                // Standard fallback
                urpParticleShader = Shader.Find("Particles/Additive");
            }

            fireMat = new Material(urpParticleShader);
            
            // Configure URP Particle Unlit shader settings for Additive Blending
            if (fireMat.HasProperty("_Blend"))
            {
                fireMat.SetFloat("_Blend", 1f); // 1 = Additive blending in URP Particles
            }
            if (fireMat.HasProperty("_Surface"))
            {
                fireMat.SetFloat("_Surface", 1f); // Transparent surface type
            }
            
            // Set render states
            fireMat.SetOverrideTag("RenderType", "Transparent");
            fireMat.SetInt("_SrcBlend", (int)BlendMode.SrcAlpha);
            fireMat.SetInt("_DstBlend", (int)BlendMode.One);
            fireMat.SetInt("_ZWrite", 0);
            fireMat.DisableKeyword("_ALPHATEST_ON");
            fireMat.EnableKeyword("_ALPHABLEND_ON");
            fireMat.EnableKeyword("_ALPHAPREMULTIPLY_ON");
            fireMat.renderQueue = (int)RenderQueue.Transparent;

            // Load default soft particle texture if available
            Texture2D defaultGlow = AssetDatabase.GetBuiltinExtraResource<Texture2D>("Default-Particle.png");
            if (defaultGlow == null)
            {
                defaultGlow = AssetDatabase.GetBuiltinExtraResource<Texture2D>("Default-Particle.psd");
            }
            if (defaultGlow != null)
            {
                fireMat.mainTexture = defaultGlow;
            }

            AssetDatabase.CreateAsset(fireMat, matPath);
            AssetDatabase.SaveAssets();
            Debug.Log($"[CreateTorchFirePrefab] Created Fire Material at: {matPath}");
        }

        // 3. Create Particle System GameObject
        GameObject fireGO = new GameObject("TorchFireEffect");
        ParticleSystem ps = fireGO.AddComponent<ParticleSystem>();
        
        // 4. Configure Particle System - Main Module
        var main = ps.main;
        main.duration = 1.0f;
        main.loop = true;
        main.prewarm = true;
        main.startLifetime = new ParticleSystem.MinMaxCurve(0.3f, 0.6f);
        main.startSpeed = new ParticleSystem.MinMaxCurve(0.6f, 1.2f);
        main.startSize = new ParticleSystem.MinMaxCurve(0.12f, 0.25f);
        main.startColor = new Color(1.0f, 0.45f, 0.05f, 1.0f); // Intense glowing orange
        main.gravityModifier = -0.15f; // Pull particles upward (float like hot air)
        main.simulationSpace = ParticleSystemSimulationSpace.World;
        main.playOnAwake = true;

        // 5. Configure Emission Module
        var emission = ps.emission;
        emission.enabled = true;
        emission.rateOverTime = 30f; // Dense flame

        // 6. Configure Shape Module (point upward cone)
        var shape = ps.shape;
        shape.enabled = true;
        shape.shapeType = ParticleSystemShapeType.Cone;
        shape.angle = 6f; // Tight column
        shape.radius = 0.03f; // Narrow base suitable for a torch tip
        shape.rotation = new Vector3(-90f, 0f, 0f); // Rotate to point upward in Unity's coords

        // 7. Configure Color Over Lifetime Module (Yellow core -> Orange body -> Dark Red tip -> Fade out)
        var colorOverLifetime = ps.colorOverLifetime;
        colorOverLifetime.enabled = true;
        
        Gradient gradient = new Gradient();
        gradient.SetKeys(
            new GradientColorKey[] { 
                new GradientColorKey(new Color(1f, 0.95f, 0.3f), 0.0f), // Yellow flame center
                new GradientColorKey(new Color(1f, 0.4f, 0.0f), 0.4f),  // Warm orange body
                new GradientColorKey(new Color(0.85f, 0.1f, 0.0f), 0.75f), // Reddish outer flame
                new GradientColorKey(new Color(0.15f, 0.15f, 0.15f), 1.0f) // Dark smoke fade
            },
            new GradientAlphaKey[] { 
                new GradientAlphaKey(1.0f, 0.0f),
                new GradientAlphaKey(0.85f, 0.5f),
                new GradientAlphaKey(0.2f, 0.8f),
                new GradientAlphaKey(0.0f, 1.0f) // Fade to invisible
            }
        );
        colorOverLifetime.color = gradient;

        // 8. Configure Size Over Lifetime Module (Fades to point at top)
        var sizeOverLifetime = ps.sizeOverLifetime;
        sizeOverLifetime.enabled = true;
        AnimationCurve sizeCurve = new AnimationCurve();
        sizeCurve.AddKey(0.0f, 0.8f); // Start slightly smaller
        sizeCurve.AddKey(0.2f, 1.0f); // Expand quickly
        sizeCurve.AddKey(1.0f, 0.15f); // Shrink to point at the end
        sizeOverLifetime.size = new ParticleSystem.MinMaxCurve(1.0f, sizeCurve);

        // 9. Configure Velocity Over Lifetime Module (Add minor wind sway / noise)
        var velocityOverLifetime = ps.velocityOverLifetime;
        velocityOverLifetime.enabled = true;
        AnimationCurve swayX = new AnimationCurve();
        swayX.AddKey(0f, 0f);
        swayX.AddKey(0.5f, 0.15f);
        swayX.AddKey(1.0f, -0.15f);
        velocityOverLifetime.x = new ParticleSystem.MinMaxCurve(0.2f, swayX);

        // 10. Configure Renderer Module (Apply Material)
        ParticleSystemRenderer psr = fireGO.GetComponent<ParticleSystemRenderer>();
        if (fireMat != null)
        {
            psr.material = fireMat;
        }

        // 11. Save as Prefab
        string localPath = AssetDatabase.GenerateUniqueAssetPath(prefabPath);
        if (!forceOverwrite && AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath) == null)
        {
            localPath = prefabPath;
        }
        else if (forceOverwrite)
        {
            localPath = prefabPath;
        }

        PrefabUtility.SaveAsPrefabAssetAndConnect(fireGO, localPath, InteractionMode.AutomatedAction);
        
        // Clean up scene temp object
        Object.DestroyImmediate(fireGO);
        
        AssetDatabase.Refresh();
        Debug.Log($"[CreateTorchFirePrefab] Created and saved TorchFire Prefab at: {localPath}");
    }
}
#endif
