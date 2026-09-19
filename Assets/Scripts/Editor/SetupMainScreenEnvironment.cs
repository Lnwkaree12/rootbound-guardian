#if UNITY_EDITOR
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

/// <summary>
/// Sets up the complete 3D stylized cave entrance environment for MainScreen.unity
/// based on the reference screenshot:
/// - Low-poly cave cavern with skylight roof opening
/// - Center wooden mine timber gate/archway
/// - Stepped mossy rock terraces inside the gate
/// - Volumetric sunbeam / god ray pouring down from the ceiling opening
/// - Right wooden fence with warm glowing lantern and leaning pickaxe
/// - Left rustic wooden fence
/// - Foreground ground path lined with stylized grass clumps
/// - Glowing dust motes and lantern embers particles
/// - Cinematic URP lighting & post-processing (Bloom, Vignette, Color Grading)
/// </summary>
[InitializeOnLoad]
public static class SetupMainScreenEnvironment
{
    private const string SCENE_PATH = "Assets/Scenes/MainScreen.unity";
    private const string MODELS_DIR = "Assets/Models/MainScreen";
    private const string MATERIALS_DIR = "Assets/Materials/MainScreen";
    private const string SETTINGS_DIR = "Assets/Settings";

    static SetupMainScreenEnvironment()
    {
        EditorApplication.delayCall += AutoRunIfApplicable;
    }

    private static void AutoRunIfApplicable()
    {
        if (EditorApplication.isPlaying || Application.isPlaying) return;

        if (!SessionState.GetBool("SetupMainScreen_RanOnce_v2", false))
        {
            SessionState.SetBool("SetupMainScreen_RanOnce_v2", true);
            ExecuteSetup();
        }
    }

    [MenuItem("Tools/SproutScout/Setup Main Screen 3D Environment")]
    public static void ExecuteSetup()
    {
        Debug.Log("[SetupMainScreenEnvironment] Starting 3D Cave Environment creation...");

        EnsureDirectories();

        // 1. Open MainScreen scene
        var scene = EditorSceneManager.OpenScene(SCENE_PATH, OpenSceneMode.Single);
        if (!scene.IsValid())
        {
            Debug.LogError($"[SetupMainScreenEnvironment] Could not open scene at {SCENE_PATH}!");
            return;
        }

        // 2. Setup Shaders & Materials
        var materials = SetupMaterials();

        // 3. Setup Meshes
        var meshes = SetupMeshes();

        // 4. Configure Camera & Background
        SetupCamera();

        // 5. Clean up old 3D environment instances if any
        CleanOldEnvironment();

        // 6. Build Environment Hierarchy
        GameObject rootEnv = new GameObject("Cave_3D_Environment");
        rootEnv.transform.position = Vector3.zero;

        // A. Cave Cavern (Walls & Roof)
        BuildCaveCavern(rootEnv.transform, meshes, materials);

        // B. Ground Path
        BuildGround(rootEnv.transform, meshes, materials);

        // C. Center Mine Timber Gate
        BuildMineGate(rootEnv.transform, meshes, materials);

        // D. Stepped Mossy Rocks
        BuildSteppedRocks(rootEnv.transform, meshes, materials);

        // E. Left & Right Fences
        BuildFences(rootEnv.transform, meshes, materials);

        // F. Lantern & Warm Point Light
        BuildLantern(rootEnv.transform, meshes, materials);

        // G. Pickaxe
        BuildPickaxe(rootEnv.transform, meshes, materials);

        // H. Grass Clumps
        BuildGrassTufts(rootEnv.transform, meshes, materials);

        // I. Sun Shaft / God Ray & Directional Sunlight
        BuildSunShaftAndLighting(rootEnv.transform, meshes, materials);

        // J. Particle Systems (Dust Motes in Sunbeam & Lantern Embers)
        BuildParticleSystems(rootEnv.transform, materials);

        // K. Post-Processing Volume
        SetupPostProcessing();

        // 7. Ensure MainMenu_Canvas is positioned cleanly with Camera
        EnsureCanvasCompatibility();

        // 8. Save Scene
        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        Debug.Log("[SetupMainScreenEnvironment] MainScreen 3D Environment setup completed and saved successfully!");
    }

    private static void EnsureDirectories()
    {
        string[] dirs = { MODELS_DIR, MATERIALS_DIR, SETTINGS_DIR };
        foreach (var dir in dirs)
        {
            string full = Path.Combine(Application.dataPath, "..", dir);
            if (!Directory.Exists(full))
            {
                Directory.CreateDirectory(full);
            }
        }
    }

    // ==========================================
    // 1. MATERIALS SETUP
    // ==========================================
    private class EnvMaterials
    {
        public Material timber;
        public Material caveStone;
        public Material steppedRock;
        public Material moss;
        public Material steel;
        public Material lanternFrame;
        public Material lanternGlow;
        public Material ground;
        public Material grass;
        public Material sunShaft;
        public Material particles;
    }

    private static EnvMaterials SetupMaterials()
    {
        var mats = new EnvMaterials();
        Shader urpLit = Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard");
        Shader urpUnlit = Shader.Find("Universal Render Pipeline/Unlit");
        Shader additiveShader = Shader.Find("MagicalRocksAndStones/AdditiveParticles") ?? Shader.Find("Particles/Standard Unlit") ?? urpUnlit;

        // Timber / Wood (Warm pale low-poly pine)
        mats.timber = GetOrCreateMaterial($"{MATERIALS_DIR}/Wood_Timber.mat", urpLit, m =>
        {
            m.color = new Color(0.83f, 0.74f, 0.60f); // Warm natural pine
            SetProp(m, "_Smoothness", 0.1f);
            SetProp(m, "_Metallic", 0.0f);
        });

        // Cave Stone (Deep moody blue-slate)
        mats.caveStone = GetOrCreateMaterial($"{MATERIALS_DIR}/Cave_Stone.mat", urpLit, m =>
        {
            m.color = new Color(0.16f, 0.21f, 0.28f); // Deep cool slate
            SetProp(m, "_Smoothness", 0.05f);
            SetProp(m, "_Metallic", 0.0f);
        });

        // Stepped Rock (Neutral stone)
        mats.steppedRock = GetOrCreateMaterial($"{MATERIALS_DIR}/Rock_Stepped.mat", urpLit, m =>
        {
            m.color = new Color(0.68f, 0.72f, 0.75f); // Stylized light slate stone
            SetProp(m, "_Smoothness", 0.15f);
            SetProp(m, "_Metallic", 0.0f);
        });

        // Moss Top (Vibrant fresh moss green)
        mats.moss = GetOrCreateMaterial($"{MATERIALS_DIR}/Moss_Top.mat", urpLit, m =>
        {
            m.color = new Color(0.48f, 0.66f, 0.22f); // Fresh vibrant olive/lime moss
            SetProp(m, "_Smoothness", 0.08f);
            SetProp(m, "_Metallic", 0.0f);
        });

        // Pickaxe Steel (Stylized cyan-blue steel)
        mats.steel = GetOrCreateMaterial($"{MATERIALS_DIR}/Pickaxe_Steel.mat", urpLit, m =>
        {
            m.color = new Color(0.45f, 0.66f, 0.82f); // Stylized cyan-blue metal
            SetProp(m, "_Smoothness", 0.55f);
            SetProp(m, "_Metallic", 0.65f);
        });

        // Lantern Frame (Dark rustic iron/timber)
        mats.lanternFrame = GetOrCreateMaterial($"{MATERIALS_DIR}/Lantern_Frame.mat", urpLit, m =>
        {
            m.color = new Color(0.72f, 0.62f, 0.50f);
            SetProp(m, "_Smoothness", 0.2f);
            SetProp(m, "_Metallic", 0.1f);
        });

        // Lantern Glow (Emissive Warm Amber Core)
        mats.lanternGlow = GetOrCreateMaterial($"{MATERIALS_DIR}/Lantern_Glow.mat", urpLit, m =>
        {
            m.color = new Color(1.0f, 0.88f, 0.60f);
            m.EnableKeyword("_EMISSION");
            m.SetColor("_EmissionColor", new Color(1.0f, 0.75f, 0.35f) * 2.8f);
            SetProp(m, "_Smoothness", 0.0f);
        });

        // Ground Dirt / Sand Path
        mats.ground = GetOrCreateMaterial($"{MATERIALS_DIR}/Ground_Dirt.mat", urpLit, m =>
        {
            m.color = new Color(0.76f, 0.72f, 0.65f); // Pale dirt/sand tone
            SetProp(m, "_Smoothness", 0.08f);
            SetProp(m, "_Metallic", 0.0f);
        });

        // Grass Stylized Clump
        mats.grass = GetOrCreateMaterial($"{MATERIALS_DIR}/Grass_Stylized.mat", urpLit, m =>
        {
            m.color = new Color(0.52f, 0.70f, 0.25f);
            SetProp(m, "_Smoothness", 0.1f);
            SetProp(m, "_Metallic", 0.0f);
        });

        // Sun Shaft (Transparent Additive Beam)
        mats.sunShaft = GetOrCreateMaterial($"{MATERIALS_DIR}/SunShaft_Volumetric.mat", additiveShader, m =>
        {
            m.color = new Color(1.0f, 0.90f, 0.60f, 0.38f);
            if (m.HasProperty("_Color")) m.SetColor("_Color", new Color(1.0f, 0.90f, 0.60f, 0.38f));
        });

        // Dust Particle Material
        mats.particles = GetOrCreateMaterial($"{MATERIALS_DIR}/Particle_Dust.mat", additiveShader, m =>
        {
            m.color = new Color(1.0f, 0.92f, 0.65f, 0.85f);
            if (m.HasProperty("_Color")) m.SetColor("_Color", new Color(1.0f, 0.92f, 0.65f, 0.85f));
        });

        return mats;
    }

    private static Material GetOrCreateMaterial(string path, Shader shader, System.Action<Material> configure)
    {
        Material mat = AssetDatabase.LoadAssetAtPath<Material>(path);
        if (mat == null)
        {
            mat = new Material(shader);
            configure?.Invoke(mat);
            AssetDatabase.CreateAsset(mat, path);
        }
        else
        {
            configure?.Invoke(mat);
            EditorUtility.SetDirty(mat);
        }
        return mat;
    }

    private static void SetProp(Material m, string prop, float val)
    {
        if (m.HasProperty(prop)) m.SetFloat(prop, val);
    }

    // ==========================================
    // 2. MESHES SETUP
    // ==========================================
    private class EnvMeshes
    {
        public Mesh mineGate;
        public Mesh caveCavern;
        public Mesh steppedRocks;
        public Mesh steppedMoss;
        public Mesh leftFence;
        public Mesh rightFence;
        public Mesh lantern;
        public Mesh pickaxe;
        public Mesh grassClump;
        public Mesh sunShaft;
        public Mesh groundPath;
    }

    private static EnvMeshes SetupMeshes()
    {
        var meshes = new EnvMeshes();
        meshes.mineGate = GetOrCreateMesh($"{MODELS_DIR}/MineGateMesh.asset", GenerateMineGateMesh);
        meshes.caveCavern = GetOrCreateMesh($"{MODELS_DIR}/CaveCavernMesh.asset", GenerateCaveCavernMesh);
        meshes.steppedRocks = GetOrCreateMesh($"{MODELS_DIR}/SteppedRocksMesh.asset", GenerateSteppedRocksMesh);
        meshes.steppedMoss = GetOrCreateMesh($"{MODELS_DIR}/SteppedMossMesh.asset", GenerateSteppedMossMesh);
        meshes.leftFence = GetOrCreateMesh($"{MODELS_DIR}/LeftFenceMesh.asset", GenerateLeftFenceMesh);
        meshes.rightFence = GetOrCreateMesh($"{MODELS_DIR}/RightFenceMesh.asset", GenerateRightFenceMesh);
        meshes.lantern = GetOrCreateMesh($"{MODELS_DIR}/LanternMesh.asset", GenerateLanternMesh);
        meshes.pickaxe = GetOrCreateMesh($"{MODELS_DIR}/PickaxeMesh.asset", GeneratePickaxeMesh);
        meshes.grassClump = GetOrCreateMesh($"{MODELS_DIR}/GrassClumpMesh.asset", GenerateGrassClumpMesh);
        meshes.sunShaft = GetOrCreateMesh($"{MODELS_DIR}/SunShaftMesh.asset", GenerateSunShaftMesh);
        meshes.groundPath = GetOrCreateMesh($"{MODELS_DIR}/GroundPathMesh.asset", GenerateGroundPathMesh);
        return meshes;
    }

    private static Mesh GetOrCreateMesh(string path, System.Func<Mesh> generator)
    {
        Mesh mesh = AssetDatabase.LoadAssetAtPath<Mesh>(path);
        if (mesh == null)
        {
            mesh = generator();
            AssetDatabase.CreateAsset(mesh, path);
        }
        else
        {
            Mesh newMesh = generator();
            mesh.Clear();
            mesh.vertices = newMesh.vertices;
            mesh.triangles = newMesh.triangles;
            mesh.normals = newMesh.normals;
            mesh.uv = newMesh.uv;
            mesh.colors = newMesh.colors;
            mesh.RecalculateBounds();
            mesh.RecalculateTangents();
            EditorUtility.SetDirty(mesh);
        }
        return mesh;
    }

    // ==========================================
    // PROCEDURAL MESH GENERATORS
    // ==========================================

    private static Mesh GenerateMineGateMesh()
    {
        MeshBuilder mb = new MeshBuilder();

        // Dimensions matching reference screenshot:
        // Left upright post (angled slightly outward at bottom)
        mb.AddOrientedBox(new Vector3(-1.95f, 1.45f, 0f), new Vector3(0.32f, 3.1f, 0.32f), Quaternion.Euler(0, 0, -4f));
        // Left outer angled brace leg
        mb.AddOrientedBox(new Vector3(-2.3f, 1.1f, 0f), new Vector3(0.24f, 2.4f, 0.24f), Quaternion.Euler(0, 0, 16f));

        // Right upright post
        mb.AddOrientedBox(new Vector3(1.95f, 1.45f, 0f), new Vector3(0.32f, 3.1f, 0.32f), Quaternion.Euler(0, 0, 4f));
        // Right outer angled brace leg
        mb.AddOrientedBox(new Vector3(2.3f, 1.1f, 0f), new Vector3(0.24f, 2.4f, 0.24f), Quaternion.Euler(0, 0, -16f));

        // Top horizontal beam / lintel (overhangs on both ends)
        mb.AddOrientedBox(new Vector3(0f, 2.95f, 0f), new Vector3(4.8f, 0.38f, 0.38f), Quaternion.identity);

        // Corner diagonal braces
        mb.AddOrientedBox(new Vector3(-1.45f, 2.55f, 0f), new Vector3(0.2f, 0.95f, 0.22f), Quaternion.Euler(0, 0, -45f));
        mb.AddOrientedBox(new Vector3(1.45f, 2.55f, 0f), new Vector3(0.2f, 0.95f, 0.22f), Quaternion.Euler(0, 0, 45f));

        // Pegs / bolt detail blocks on the top beam ends
        mb.AddOrientedBox(new Vector3(-2.2f, 2.95f, 0f), new Vector3(0.12f, 0.2f, 0.44f), Quaternion.identity);
        mb.AddOrientedBox(new Vector3(2.2f, 2.95f, 0f), new Vector3(0.12f, 0.2f, 0.44f), Quaternion.identity);

        return mb.Build("MineGateMesh");
    }

    private static Mesh GenerateCaveCavernMesh()
    {
        MeshBuilder mb = new MeshBuilder();

        // Cavern consists of faceted low-poly rock walls surrounding the camera and opening overhead
        // Left Cave Wall facets
        Vector3[] leftWallProfile = new Vector3[]
        {
            new Vector3(-6.5f, -0.2f, -5f),
            new Vector3(-5.5f, 1.8f, -4f),
            new Vector3(-4.8f, 3.8f, -3f),
            new Vector3(-4.0f, 5.5f, -1f),
            new Vector3(-3.2f, 6.2f, 2f),
            new Vector3(-2.8f, 6.5f, 5f)
        };

        // Create inward-facing rock faces for left wall
        BuildCaveWallRibs(mb, -1f);
        BuildCaveWallRibs(mb, 1f);

        // Arched ceiling with center skylight opening at (0, 6.2, 5.0)
        BuildCaveCeilingWithSkylight(mb);

        return mb.Build("CaveCavernMesh");
    }

    private static void BuildCaveWallRibs(MeshBuilder mb, float side)
    {
        int zSlices = 7;
        float[] zCoords = { -5f, -2.5f, 0f, 2.5f, 5.0f, 7.5f, 10f };

        List<Vector3[]> slices = new List<Vector3[]>();
        Random.InitState((int)(side * 1000) + 77);

        for (int i = 0; i < zSlices; i++)
        {
            float z = zCoords[i];
            float depthT = (float)i / (zSlices - 1);
            float xBase = (3.6f + depthT * 0.8f + Random.Range(-0.25f, 0.25f)) * side;

            Vector3[] pts = new Vector3[5];
            pts[0] = new Vector3(xBase * 1.5f, -0.2f, z);
            pts[1] = new Vector3(xBase * 1.25f, 1.6f + Random.Range(-0.3f, 0.3f), z);
            pts[2] = new Vector3(xBase * 1.05f, 3.4f + Random.Range(-0.3f, 0.3f), z);
            pts[3] = new Vector3(xBase * 0.85f, 5.0f + Random.Range(-0.2f, 0.2f), z);
            pts[4] = new Vector3(xBase * 0.65f, 6.2f + Random.Range(-0.2f, 0.2f), z);
            slices.Add(pts);
        }

        // Bridge slices with quads/triangles
        for (int i = 0; i < slices.Count - 1; i++)
        {
            var sA = slices[i];
            var sB = slices[i + 1];
            for (int j = 0; j < 4; j++)
            {
                if (side < 0)
                {
                    mb.AddQuad(sA[j], sB[j], sB[j + 1], sA[j + 1]);
                }
                else
                {
                    mb.AddQuad(sB[j], sA[j], sA[j + 1], sB[j + 1]);
                }
            }
        }
    }

    private static void BuildCaveCeilingWithSkylight(MeshBuilder mb)
    {
        // Ceiling arch over the cave, leaving an opening at center
        int segments = 12;
        float radiusInner = 1.15f; // Skylight hole radius
        float radiusOuter = 4.8f;
        Vector3 holeCenter = new Vector3(0.1f, 6.4f, 5.2f);

        for (int i = 0; i < segments; i++)
        {
            float angleA = (i / (float)segments) * Mathf.PI * 2f;
            float angleB = ((i + 1) / (float)segments) * Mathf.PI * 2f;

            Vector3 innerA = holeCenter + new Vector3(Mathf.Cos(angleA) * radiusInner, 0f, Mathf.Sin(angleA) * radiusInner * 0.85f);
            Vector3 innerB = holeCenter + new Vector3(Mathf.Cos(angleB) * radiusInner, 0f, Mathf.Sin(angleB) * radiusInner * 0.85f);

            Vector3 outerA = holeCenter + new Vector3(Mathf.Cos(angleA) * radiusOuter, -0.6f, Mathf.Sin(angleA) * radiusOuter * 1.1f);
            Vector3 outerB = holeCenter + new Vector3(Mathf.Cos(angleB) * radiusOuter, -0.6f, Mathf.Sin(angleB) * radiusOuter * 1.1f);

            // Facing down into cave
            mb.AddQuad(innerA, innerB, outerB, outerA);
        }
    }

    private static Mesh GenerateSteppedRocksMesh()
    {
        MeshBuilder mb = new MeshBuilder();

        // 4 ascending tiers of stepped cave rocks inside and behind the gate
        // Tier 1 (Lowest, closest behind gate)
        AddRockTier(mb, new Vector3(0f, 0.35f, 2.2f), new Vector3(3.4f, 0.7f, 1.8f), 0.15f);

        // Tier 2 (Middle)
        AddRockTier(mb, new Vector3(0.15f, 1.15f, 3.8f), new Vector3(3.8f, 0.9f, 1.9f), 0.18f);

        // Tier 3 (Upper tier directly under skylight)
        AddRockTier(mb, new Vector3(-0.1f, 2.1f, 5.4f), new Vector3(4.2f, 1.0f, 2.0f), 0.22f);

        // Tier 4 (High back rock wall tier)
        AddRockTier(mb, new Vector3(0f, 3.2f, 7.0f), new Vector3(4.6f, 1.2f, 2.2f), 0.25f);

        return mb.Build("SteppedRocksMesh");
    }

    private static Mesh GenerateSteppedMossMesh()
    {
        MeshBuilder mb = new MeshBuilder();

        // Moss caps covering top surfaces of each rock tier, offset slightly upward (+0.03) to prevent z-fighting
        AddMossCap(mb, new Vector3(0f, 0.72f, 2.2f), new Vector2(3.2f, 1.6f));
        AddMossCap(mb, new Vector3(0.15f, 1.62f, 3.8f), new Vector2(3.6f, 1.7f));
        AddMossCap(mb, new Vector3(-0.1f, 2.62f, 5.4f), new Vector2(4.0f, 1.8f));
        AddMossCap(mb, new Vector3(0f, 3.82f, 7.0f), new Vector2(4.4f, 2.0f));

        return mb.Build("SteppedMossMesh");
    }

    private static void AddRockTier(MeshBuilder mb, Vector3 center, Vector3 size, float bevel)
    {
        float hx = size.x * 0.5f;
        float hy = size.y * 0.5f;
        float hz = size.z * 0.5f;

        // Faceted rock tier box with sloped/beveled sides
        Vector3 b00 = center + new Vector3(-hx - bevel, -hy, -hz - bevel);
        Vector3 b10 = center + new Vector3( hx + bevel, -hy, -hz - bevel);
        Vector3 b11 = center + new Vector3( hx + bevel, -hy,  hz + bevel);
        Vector3 b01 = center + new Vector3(-hx - bevel, -hy,  hz + bevel);

        Vector3 t00 = center + new Vector3(-hx, hy, -hz);
        Vector3 t10 = center + new Vector3( hx, hy, -hz);
        Vector3 t11 = center + new Vector3( hx, hy,  hz);
        Vector3 t01 = center + new Vector3(-hx, hy,  hz);

        // Sides
        mb.AddQuad(b00, b10, t10, t00); // Front
        mb.AddQuad(b10, b11, t11, t10); // Right
        mb.AddQuad(b11, b01, t01, t11); // Back
        mb.AddQuad(b01, b00, t00, t01); // Left
        // Top
        mb.AddQuad(t00, t10, t11, t01);
    }

    private static void AddMossCap(MeshBuilder mb, Vector3 topCenter, Vector2 size)
    {
        float hx = size.x * 0.5f;
        float hz = size.y * 0.5f;

        // Subdivided low-poly rounded moss pad
        int segX = 4;
        int segZ = 3;
        Vector3[,] grid = new Vector3[segX + 1, segZ + 1];

        for (int i = 0; i <= segX; i++)
        {
            float u = (float)i / segX;
            float x = Mathf.Lerp(-hx, hx, u);
            for (int j = 0; j <= segZ; j++)
            {
                float v = (float)j / segZ;
                float z = Mathf.Lerp(-hz, hz, v);
                // Slight organic mound curve
                float dome = Mathf.Sin(u * Mathf.PI) * Mathf.Sin(v * Mathf.PI) * 0.12f;
                grid[i, j] = topCenter + new Vector3(x, dome, z);
            }
        }

        for (int i = 0; i < segX; i++)
        {
            for (int j = 0; j < segZ; j++)
            {
                mb.AddQuad(grid[i, j], grid[i + 1, j], grid[i + 1, j + 1], grid[i, j + 1]);
            }
        }
    }

    private static Mesh GenerateLeftFenceMesh()
    {
        MeshBuilder mb = new MeshBuilder();

        // 4 rustic wooden vertical stakes/planks, tilted naturally
        Vector3[] postPos = {
            new Vector3(-2.2f, 0.45f, -2.1f),
            new Vector3(-2.8f, 0.50f, -1.8f),
            new Vector3(-3.4f, 0.48f, -1.4f),
            new Vector3(-4.0f, 0.52f, -1.0f)
        };
        float[] postAngles = { 5f, -3f, 7f, -4f };

        for (int i = 0; i < postPos.Length; i++)
        {
            mb.AddOrientedBox(postPos[i], new Vector3(0.18f, 0.95f, 0.12f), Quaternion.Euler(0, 0, postAngles[i]));
        }

        // Horizontal connecting back rails
        mb.AddOrientedBox(new Vector3(-3.1f, 0.65f, -1.55f), new Vector3(2.0f, 0.12f, 0.08f), Quaternion.Euler(0, -28f, 0));
        mb.AddOrientedBox(new Vector3(-3.1f, 0.25f, -1.55f), new Vector3(2.0f, 0.12f, 0.08f), Quaternion.Euler(0, -28f, 0));

        return mb.Build("LeftFenceMesh");
    }

    private static Mesh GenerateRightFenceMesh()
    {
        MeshBuilder mb = new MeshBuilder();

        // Right wooden fence in foreground
        // Front main post where lantern is mounted
        Vector3 frontPost = new Vector3(1.85f, 0.48f, -2.3f);
        mb.AddOrientedBox(frontPost, new Vector3(0.24f, 1.0f, 0.24f), Quaternion.identity);

        // Rear post
        Vector3 rearPost = new Vector3(3.0f, 0.52f, -1.2f);
        mb.AddOrientedBox(rearPost, new Vector3(0.22f, 1.05f, 0.22f), Quaternion.identity);

        // 3 horizontal fence rails
        Vector3 railDir = rearPost - frontPost;
        Vector3 railCenter = (frontPost + rearPost) * 0.5f;
        float railLen = railDir.magnitude;
        Quaternion railRot = Quaternion.LookRotation(railDir);

        mb.AddOrientedBox(railCenter + new Vector3(0, 0.35f, 0), new Vector3(0.12f, 0.14f, railLen + 0.2f), railRot);
        mb.AddOrientedBox(railCenter + new Vector3(0, 0.05f, 0), new Vector3(0.12f, 0.14f, railLen + 0.2f), railRot);
        mb.AddOrientedBox(railCenter + new Vector3(0, -0.25f, 0), new Vector3(0.12f, 0.14f, railLen + 0.2f), railRot);

        // Diagonal brace plank
        mb.AddOrientedBox(railCenter + new Vector3(0, 0.05f, 0), new Vector3(0.10f, 0.12f, railLen * 0.95f), railRot * Quaternion.Euler(30f, 0, 0));

        return mb.Build("RightFenceMesh");
    }

    private static Mesh GenerateLanternMesh()
    {
        MeshBuilder mb = new MeshBuilder();

        // Rustic wooden lantern matching screenshot:
        // Sits atop front right fence post
        // Wooden base
        mb.AddOrientedBox(new Vector3(0f, 0.04f, 0f), new Vector3(0.32f, 0.08f, 0.32f), Quaternion.identity);

        // 4 corner struts
        float strutOffset = 0.12f;
        mb.AddOrientedBox(new Vector3(-strutOffset, 0.22f, -strutOffset), new Vector3(0.04f, 0.28f, 0.04f), Quaternion.identity);
        mb.AddOrientedBox(new Vector3( strutOffset, 0.22f, -strutOffset), new Vector3(0.04f, 0.28f, 0.04f), Quaternion.identity);
        mb.AddOrientedBox(new Vector3( strutOffset, 0.22f,  strutOffset), new Vector3(0.04f, 0.28f, 0.04f), Quaternion.identity);
        mb.AddOrientedBox(new Vector3(-strutOffset, 0.22f,  strutOffset), new Vector3(0.04f, 0.28f, 0.04f), Quaternion.identity);

        // Pagoda style roof cap
        mb.AddOrientedBox(new Vector3(0f, 0.40f, 0f), new Vector3(0.40f, 0.08f, 0.40f), Quaternion.identity);
        mb.AddOrientedBox(new Vector3(0f, 0.47f, 0f), new Vector3(0.24f, 0.07f, 0.24f), Quaternion.identity);
        mb.AddOrientedBox(new Vector3(0f, 0.53f, 0f), new Vector3(0.12f, 0.06f, 0.12f), Quaternion.identity);

        return mb.Build("LanternMesh");
    }

    private static Mesh GeneratePickaxeMesh()
    {
        MeshBuilder mb = new MeshBuilder();

        // Leaning pickaxe on right fence:
        // Wooden handle
        mb.AddOrientedBox(new Vector3(0f, 0.55f, 0f), new Vector3(0.06f, 1.15f, 0.06f), Quaternion.identity);

        // Curved steel pickaxe head at the top
        // Center collar around handle
        mb.AddOrientedBox(new Vector3(0f, 1.02f, 0f), new Vector3(0.12f, 0.14f, 0.12f), Quaternion.identity);

        // Left curved blade pointing down
        mb.AddOrientedBox(new Vector3(-0.16f, 1.0f, 0f), new Vector3(0.24f, 0.10f, 0.08f), Quaternion.Euler(0, 0, -18f));
        mb.AddOrientedBox(new Vector3(-0.32f, 0.91f, 0f), new Vector3(0.18f, 0.08f, 0.06f), Quaternion.Euler(0, 0, -35f));

        // Right curved pick point
        mb.AddOrientedBox(new Vector3(0.16f, 1.0f, 0f), new Vector3(0.24f, 0.10f, 0.08f), Quaternion.Euler(0, 0, 18f));
        mb.AddOrientedBox(new Vector3(0.32f, 0.91f, 0f), new Vector3(0.18f, 0.08f, 0.06f), Quaternion.Euler(0, 0, 35f));

        return mb.Build("PickaxeMesh");
    }

    private static Mesh GenerateGrassClumpMesh()
    {
        MeshBuilder mb = new MeshBuilder();

        // Stylized 5-blade low poly grass clump
        Random.InitState(345);
        int blades = 6;
        for (int i = 0; i < blades; i++)
        {
            float angle = (i / (float)blades) * Mathf.PI * 2f + Random.Range(-0.2f, 0.2f);
            float height = Random.Range(0.26f, 0.42f);
            float width = Random.Range(0.04f, 0.07f);
            float tilt = Random.Range(10f, 25f);

            Vector3 basePos = new Vector3(Mathf.Cos(angle) * 0.08f, 0f, Mathf.Sin(angle) * 0.08f);
            Vector3 tipPos = basePos + new Vector3(Mathf.Cos(angle) * height * 0.4f, height, Mathf.Sin(angle) * height * 0.4f);

            Vector3 sideA = basePos + new Vector3(-Mathf.Sin(angle) * width, 0f, Mathf.Cos(angle) * width);
            Vector3 sideB = basePos + new Vector3( Mathf.Sin(angle) * width, 0f, -Mathf.Cos(angle) * width);

            // Double sided triangle
            mb.AddTriangle(sideA, sideB, tipPos);
            mb.AddTriangle(sideB, sideA, tipPos);
        }

        return mb.Build("GrassClumpMesh");
    }

    private static Mesh GenerateSunShaftMesh()
    {
        MeshBuilder mb = new MeshBuilder();

        // Volumetric light shaft cone from skylight roof opening down onto stepped rocks
        int segments = 24;
        Vector3 topOrigin = new Vector3(0.1f, 6.2f, 5.0f);
        Vector3 bottomTarget = new Vector3(0f, 0.4f, 2.8f);

        float topRadius = 0.65f;
        float bottomRadius = 2.4f;

        List<Vector3> topVerts = new List<Vector3>();
        List<Vector3> bottomVerts = new List<Vector3>();

        for (int i = 0; i < segments; i++)
        {
            float a = (i / (float)segments) * Mathf.PI * 2f;
            topVerts.Add(topOrigin + new Vector3(Mathf.Cos(a) * topRadius, 0f, Mathf.Sin(a) * topRadius * 0.8f));
            bottomVerts.Add(bottomTarget + new Vector3(Mathf.Cos(a) * bottomRadius, 0f, Mathf.Sin(a) * bottomRadius * 0.8f));
        }

        for (int i = 0; i < segments; i++)
        {
            int next = (i + 1) % segments;
            // Double sided quad for visibility from all camera angles
            mb.AddQuad(topVerts[i], topVerts[next], bottomVerts[next], bottomVerts[i]);
            mb.AddQuad(topVerts[next], topVerts[i], bottomVerts[i], bottomVerts[next]);
        }

        return mb.Build("SunShaftMesh");
    }

    private static Mesh GenerateGroundPathMesh()
    {
        MeshBuilder mb = new MeshBuilder();

        // Smooth dirt path from foreground through the mine gate
        int zSegments = 10;
        float[] zCoords = { -6.0f, -4.5f, -3.0f, -1.5f, 0.0f, 1.5f, 2.5f, 3.5f, 5.0f, 7.0f };
        float[] widths  = {  4.2f,  3.8f,  3.5f,  3.2f, 3.0f, 3.2f, 3.5f, 3.8f, 4.2f, 4.5f };

        for (int i = 0; i < zSegments - 1; i++)
        {
            float z0 = zCoords[i];
            float z1 = zCoords[i + 1];
            float w0 = widths[i] * 0.5f;
            float w1 = widths[i + 1] * 0.5f;

            Vector3 v00 = new Vector3(-w0, 0f, z0);
            Vector3 v10 = new Vector3( w0, 0f, z0);
            Vector3 v11 = new Vector3( w1, 0f, z1);
            Vector3 v01 = new Vector3(-w1, 0f, z1);

            mb.AddQuad(v00, v10, v11, v01);
        }

        return mb.Build("GroundPathMesh");
    }

    // ==========================================
    // 3. CAMERA & BACKGROUND
    // ==========================================
    private static void SetupCamera()
    {
        Camera cam = Camera.main;
        if (cam == null)
        {
            cam = Object.FindObjectOfType<Camera>();
        }
        if (cam == null)
        {
            GameObject camGO = new GameObject("Main Camera");
            cam = camGO.AddComponent<Camera>();
            camGO.tag = "MainCamera";
        }

        // Camera position & angle framing the mine gate, lantern, and sunbeam
        cam.transform.position = new Vector3(0f, 0.95f, -5.2f);
        cam.transform.rotation = Quaternion.Euler(-5.5f, 0f, 0f);
        cam.fieldOfView = 58f;
        cam.nearClipPlane = 0.1f;
        cam.farClipPlane = 100f;

        // Background color: Deep midnight cave tone
        cam.clearFlags = CameraClearFlags.SolidColor;
        cam.backgroundColor = new Color(0.04f, 0.07f, 0.12f, 1.0f);

        // Ambient lighting settings
        RenderSettings.ambientMode = AmbientMode.Flat;
        RenderSettings.ambientLight = new Color(0.12f, 0.16f, 0.24f, 1.0f); // Deep cool slate ambient
    }

    // ==========================================
    // 4. CLEANUP OLD ENVIRONMENT
    // ==========================================
    private static void CleanOldEnvironment()
    {
        GameObject existing = GameObject.Find("Cave_3D_Environment");
        if (existing != null)
        {
            Object.DestroyImmediate(existing);
        }

        // Also clean up loose WalkUp02 or empty Terrain if present
        GameObject walkUp = GameObject.Find("WalkUp02");
        if (walkUp != null)
        {
            Object.DestroyImmediate(walkUp);
        }
        GameObject terrain = GameObject.Find("Terrain");
        if (terrain != null)
        {
            Object.DestroyImmediate(terrain);
        }

        // Clean up duplicate volumes if multiple exist
        var oldVolumes = Object.FindObjectsOfType<Volume>();
        for (int i = 0; i < oldVolumes.Length; i++)
        {
            if (oldVolumes[i] != null && oldVolumes[i].gameObject != null)
            {
                Object.DestroyImmediate(oldVolumes[i].gameObject);
            }
        }
    }

    // ==========================================
    // 5. SCENE CONSTRUCTION METHODS
    // ==========================================

    private static void BuildCaveCavern(Transform parent, EnvMeshes meshes, EnvMaterials mats)
    {
        GameObject cavernGO = new GameObject("Cave_Cavern");
        cavernGO.transform.SetParent(parent, false);
        cavernGO.transform.position = Vector3.zero;

        var mf = cavernGO.AddComponent<MeshFilter>();
        mf.sharedMesh = meshes.caveCavern;
        var mr = cavernGO.AddComponent<MeshRenderer>();
        mr.sharedMaterial = mats.caveStone;
        mr.shadowCastingMode = ShadowCastingMode.Off; // Prevent cave roof from casting blackout shadow inside
        mr.receiveShadows = true;
    }

    private static void BuildGround(Transform parent, EnvMeshes meshes, EnvMaterials mats)
    {
        GameObject groundGO = new GameObject("Ground_Pathway");
        groundGO.transform.SetParent(parent, false);
        groundGO.transform.position = Vector3.zero;

        var mf = groundGO.AddComponent<MeshFilter>();
        mf.sharedMesh = meshes.groundPath;
        var mr = groundGO.AddComponent<MeshRenderer>();
        mr.sharedMaterial = mats.ground;
    }

    private static void BuildMineGate(Transform parent, EnvMeshes meshes, EnvMaterials mats)
    {
        GameObject gateGO = new GameObject("Mine_Gate");
        gateGO.transform.SetParent(parent, false);
        gateGO.transform.position = new Vector3(-0.05f, 0f, 0f);

        var mf = gateGO.AddComponent<MeshFilter>();
        mf.sharedMesh = meshes.mineGate;
        var mr = gateGO.AddComponent<MeshRenderer>();
        mr.sharedMaterial = mats.timber;
    }

    private static void BuildSteppedRocks(Transform parent, EnvMeshes meshes, EnvMaterials mats)
    {
        GameObject rocksGO = new GameObject("Stepped_Cave_Rocks");
        rocksGO.transform.SetParent(parent, false);
        rocksGO.transform.position = Vector3.zero;

        // Rock base
        var mf = rocksGO.AddComponent<MeshFilter>();
        mf.sharedMesh = meshes.steppedRocks;
        var mr = rocksGO.AddComponent<MeshRenderer>();
        mr.sharedMaterial = mats.steppedRock;

        // Moss layer
        GameObject mossGO = new GameObject("Moss_Layer");
        mossGO.transform.SetParent(rocksGO.transform, false);
        var mfMoss = mossGO.AddComponent<MeshFilter>();
        mfMoss.sharedMesh = meshes.steppedMoss;
        var mrMoss = mossGO.AddComponent<MeshRenderer>();
        mrMoss.sharedMaterial = mats.moss;
    }

    private static void BuildFences(Transform parent, EnvMeshes meshes, EnvMaterials mats)
    {
        // Left Fence
        GameObject leftFenceGO = new GameObject("Left_Fence");
        leftFenceGO.transform.SetParent(parent, false);
        var mfL = leftFenceGO.AddComponent<MeshFilter>();
        mfL.sharedMesh = meshes.leftFence;
        var mrL = leftFenceGO.AddComponent<MeshRenderer>();
        mrL.sharedMaterial = mats.timber;

        // Right Fence
        GameObject rightFenceGO = new GameObject("Right_Fence");
        rightFenceGO.transform.SetParent(parent, false);
        var mfR = rightFenceGO.AddComponent<MeshFilter>();
        mfR.sharedMesh = meshes.rightFence;
        var mrR = rightFenceGO.AddComponent<MeshRenderer>();
        mrR.sharedMaterial = mats.timber;
    }

    private static void BuildLantern(Transform parent, EnvMeshes meshes, EnvMaterials mats)
    {
        GameObject lanternGO = new GameObject("Lantern");
        lanternGO.transform.SetParent(parent, false);
        // Mount on top of the front right fence post at (1.85, 0.98, -2.3)
        lanternGO.transform.position = new Vector3(1.85f, 0.98f, -2.3f);

        // Frame
        var mf = lanternGO.AddComponent<MeshFilter>();
        mf.sharedMesh = meshes.lantern;
        var mr = lanternGO.AddComponent<MeshRenderer>();
        mr.sharedMaterial = mats.lanternFrame;

        // Glowing Inner Core
        GameObject glowGO = GameObject.CreatePrimitive(PrimitiveType.Cube);
        glowGO.name = "Lantern_Glow_Core";
        glowGO.transform.SetParent(lanternGO.transform, false);
        glowGO.transform.localPosition = new Vector3(0f, 0.22f, 0f);
        glowGO.transform.localScale = new Vector3(0.18f, 0.24f, 0.18f);
        Object.DestroyImmediate(glowGO.GetComponent<Collider>());
        glowGO.GetComponent<MeshRenderer>().sharedMaterial = mats.lanternGlow;

        // Warm Point Light
        GameObject lightGO = new GameObject("Lantern_Point_Light");
        lightGO.transform.SetParent(lanternGO.transform, false);
        lightGO.transform.localPosition = new Vector3(0f, 0.22f, 0f);

        Light pLight = lightGO.AddComponent<Light>();
        pLight.type = LightType.Point;
        pLight.color = new Color(1.0f, 0.72f, 0.35f); // Warm golden amber
        pLight.intensity = 3.6f;
        pLight.range = 9.0f;
        pLight.shadows = LightShadows.None; // Set to None: prevents consuming 6 shadow maps and eliminates shadow atlas reduction warning

        // Add subtle cozy flicker
        LightFlicker flicker = lightGO.AddComponent<LightFlicker>();
        flicker.minIntensity = 2.8f;
        flicker.maxIntensity = 3.5f;
        flicker.flickerSpeed = 0.07f;
    }

    private static void BuildPickaxe(Transform parent, EnvMeshes meshes, EnvMaterials mats)
    {
        GameObject pickaxeGO = new GameObject("Pickaxe");
        pickaxeGO.transform.SetParent(parent, false);
        // Positioned leaning on the right fence, exactly as in screenshot
        pickaxeGO.transform.position = new Vector3(2.2f, 0.15f, -2.05f);
        pickaxeGO.transform.rotation = Quaternion.Euler(12f, -25f, -28f);

        var mf = pickaxeGO.AddComponent<MeshFilter>();
        mf.sharedMesh = meshes.pickaxe;
        var mr = pickaxeGO.AddComponent<MeshRenderer>();
        mr.sharedMaterial = mats.steel;
    }

    private static void BuildGrassTufts(Transform parent, EnvMeshes meshes, EnvMaterials mats)
    {
        GameObject grassContainer = new GameObject("Grass_Tufts");
        grassContainer.transform.SetParent(parent, false);

        // Scatter grass clumps along left and right pathways
        Vector3[] clumpPositions = new Vector3[]
        {
            // Left pathway edge
            new Vector3(-1.4f, 0f, -4.2f),
            new Vector3(-1.7f, 0f, -3.5f),
            new Vector3(-2.1f, 0f, -2.8f),
            new Vector3(-1.8f, 0f, -2.0f),
            new Vector3(-2.4f, 0f, -1.2f),
            new Vector3(-1.6f, 0f, -0.6f),
            new Vector3(-1.9f, 0f,  0.2f),
            new Vector3(-1.5f, 0f,  0.9f),
            new Vector3(-1.8f, 0f,  1.7f),

            // Right pathway edge (around fence & lantern)
            new Vector3( 1.4f, 0f, -4.2f),
            new Vector3( 1.7f, 0f, -3.6f),
            new Vector3( 1.6f, 0f, -2.7f),
            new Vector3( 2.1f, 0f, -2.1f),
            new Vector3( 2.4f, 0f, -1.5f),
            new Vector3( 1.8f, 0f, -0.8f),
            new Vector3( 1.6f, 0f,  0.1f),
            new Vector3( 1.8f, 0f,  1.0f),
            new Vector3( 2.0f, 0f,  1.8f),

            // Foreground center edges
            new Vector3(-0.9f, 0f, -4.8f),
            new Vector3( 0.9f, 0f, -4.8f)
        };

        Random.InitState(888);
        for (int i = 0; i < clumpPositions.Length; i++)
        {
            GameObject clump = new GameObject($"Grass_{i + 1}");
            clump.transform.SetParent(grassContainer.transform, false);
            clump.transform.position = clumpPositions[i];
            clump.transform.rotation = Quaternion.Euler(0, Random.Range(0f, 360f), 0);
            float scale = Random.Range(0.85f, 1.35f);
            clump.transform.localScale = Vector3.one * scale;

            var mf = clump.AddComponent<MeshFilter>();
            mf.sharedMesh = meshes.grassClump;
            var mr = clump.AddComponent<MeshRenderer>();
            mr.sharedMaterial = mats.grass;
        }
    }

    private static void BuildSunShaftAndLighting(Transform parent, EnvMeshes meshes, EnvMaterials mats)
    {
        // Volumetric Sun Shaft Beam Mesh
        GameObject sunShaftGO = new GameObject("Sun_Shaft_GodRay");
        sunShaftGO.transform.SetParent(parent, false);
        sunShaftGO.transform.position = Vector3.zero;

        var mf = sunShaftGO.AddComponent<MeshFilter>();
        mf.sharedMesh = meshes.sunShaft;
        var mr = sunShaftGO.AddComponent<MeshRenderer>();
        mr.sharedMaterial = mats.sunShaft;
        mr.shadowCastingMode = ShadowCastingMode.Off;
        mr.receiveShadows = false;

        // Spot Light simulating sunlight beam pouring down through skylight hole
        GameObject sunLightGO = new GameObject("Sun_Shaft_SpotLight");
        sunLightGO.transform.SetParent(parent, false);
        sunLightGO.transform.position = new Vector3(0.1f, 6.2f, 5.0f);
        sunLightGO.transform.rotation = Quaternion.Euler(58f, -22f, 0f);

        Light spot = sunLightGO.AddComponent<Light>();
        spot.type = LightType.Spot;
        spot.color = new Color(1.0f, 0.92f, 0.70f); // Warm sunny gold
        spot.intensity = 5.8f;
        spot.range = 16f;
        spot.spotAngle = 48f;
        spot.innerSpotAngle = 28f;
        spot.shadows = LightShadows.Soft;
        spot.shadowNormalBias = 0.1f;
        spot.shadowBias = 0.05f;

        // Directional Light for ambient cave fill
        Light dirLight = null;
        foreach (var l in Object.FindObjectsOfType<Light>())
        {
            if (l.type == LightType.Directional)
            {
                dirLight = l;
                break;
            }
        }
        if (dirLight == null)
        {
            GameObject dGO = new GameObject("Directional Light");
            dirLight = dGO.AddComponent<Light>();
            dirLight.type = LightType.Directional;
        }

        dirLight.transform.rotation = Quaternion.Euler(50f, -30f, 0f);
        dirLight.color = new Color(0.65f, 0.75f, 0.88f); // Soft cool blue fill
        dirLight.intensity = 0.35f; // Soft ambient cave fill so shadows remain deep
        dirLight.shadows = LightShadows.None; // Ambient fill light does not need harsh shadows
    }

    private static void BuildParticleSystems(Transform parent, EnvMaterials mats)
    {
        GameObject particlesContainer = new GameObject("Particle_Systems");
        particlesContainer.transform.SetParent(parent, false);

        // 1. Sunbeam Glowing Dust Motes
        GameObject dustGO = new GameObject("Sunbeam_DustMotes");
        dustGO.transform.SetParent(particlesContainer.transform, false);
        dustGO.transform.position = new Vector3(0.1f, 3.2f, 4.0f);

        ParticleSystem psDust = dustGO.AddComponent<ParticleSystem>();
        var main = psDust.main;
        main.loop = true;
        main.playOnAwake = true;
        main.startLifetime = new ParticleSystem.MinMaxCurve(4f, 7f);
        main.startSpeed = new ParticleSystem.MinMaxCurve(0.08f, 0.22f);
        main.startSize = new ParticleSystem.MinMaxCurve(0.06f, 0.14f);
        main.startColor = new Color(1.0f, 0.94f, 0.72f, 0.85f); // Golden glowing dust
        main.maxParticles = 60;
        main.simulationSpace = ParticleSystemSimulationSpace.World;

        var emission = psDust.emission;
        emission.rateOverTime = 12f;

        var shape = psDust.shape;
        shape.shapeType = ParticleSystemShapeType.Box;
        shape.scale = new Vector3(2.2f, 4.0f, 2.2f);

        var vel = psDust.velocityOverLifetime;
        vel.enabled = true;
        vel.x = new ParticleSystem.MinMaxCurve(-0.06f, 0.06f);
        vel.y = new ParticleSystem.MinMaxCurve(-0.08f, 0.02f);
        vel.z = new ParticleSystem.MinMaxCurve(-0.06f, 0.06f);

        var noise = psDust.noise;
        noise.enabled = true;
        noise.strength = 0.15f;
        noise.frequency = 0.4f;

        var rend = dustGO.GetComponent<ParticleSystemRenderer>();
        rend.sharedMaterial = mats.particles;

        // 2. Lantern Warm Floating Sparks / Fireflies
        GameObject lanternDustGO = new GameObject("Lantern_Sparks");
        lanternDustGO.transform.SetParent(particlesContainer.transform, false);
        lanternDustGO.transform.position = new Vector3(1.85f, 1.25f, -2.3f);

        ParticleSystem psLantern = lanternDustGO.AddComponent<ParticleSystem>();
        var lMain = psLantern.main;
        lMain.loop = true;
        lMain.playOnAwake = true;
        lMain.startLifetime = new ParticleSystem.MinMaxCurve(3f, 5f);
        lMain.startSpeed = new ParticleSystem.MinMaxCurve(0.05f, 0.15f);
        lMain.startSize = new ParticleSystem.MinMaxCurve(0.08f, 0.18f);
        lMain.startColor = new Color(1.0f, 0.80f, 0.40f, 0.95f);
        lMain.maxParticles = 15;

        var lEmission = psLantern.emission;
        lEmission.rateOverTime = 3f;

        var lShape = psLantern.shape;
        lShape.shapeType = ParticleSystemShapeType.Sphere;
        lShape.radius = 0.8f;

        var lNoise = psLantern.noise;
        lNoise.enabled = true;
        lNoise.strength = 0.25f;
        lNoise.frequency = 0.5f;

        var lRend = lanternDustGO.GetComponent<ParticleSystemRenderer>();
        lRend.sharedMaterial = mats.particles;
    }

    // ==========================================
    // 6. POST-PROCESSING (URP)
    // ==========================================
    private static void SetupPostProcessing()
    {
        // Clean existing volumes to guarantee a single, pristine Global Volume
        foreach (var v in Object.FindObjectsOfType<Volume>())
        {
            if (v != null && v.gameObject != null)
            {
                Object.DestroyImmediate(v.gameObject);
            }
        }

        GameObject volGO = new GameObject("Global Post Processing Volume");
        Volume globalVolume = volGO.AddComponent<Volume>();
        globalVolume.isGlobal = true;
        globalVolume.weight = 1f;
        globalVolume.priority = 10f; // Higher priority ensures it takes precedence

        string profilePath = $"{SETTINGS_DIR}/MainScreenVolumeProfile.asset";
        
        // Recreate profile cleanly to wipe any previous empty states
        if (AssetDatabase.LoadAssetAtPath<VolumeProfile>(profilePath) != null)
        {
            AssetDatabase.DeleteAsset(profilePath);
        }

        VolumeProfile profile = ScriptableObject.CreateInstance<VolumeProfile>();
        AssetDatabase.CreateAsset(profile, profilePath);

        // A. Bloom: Makes lantern and sunbeam glow warmly
        Bloom bloom = profile.Add<Bloom>();
        bloom.active = true;
        bloom.intensity.Override(1.55f);
        bloom.threshold.Override(0.78f);
        bloom.scatter.Override(0.72f);
        AssetDatabase.AddObjectToAsset(bloom, profile);

        // B. Vignette: Soft dark border framing
        Vignette vignette = profile.Add<Vignette>();
        vignette.active = true;
        vignette.intensity.Override(0.35f);
        vignette.smoothness.Override(0.48f);
        vignette.color.Override(new Color(0.02f, 0.03f, 0.06f));
        AssetDatabase.AddObjectToAsset(vignette, profile);

        // C. Color Adjustments: Teal & Orange contrast
        ColorAdjustments colorAdj = profile.Add<ColorAdjustments>();
        colorAdj.active = true;
        colorAdj.postExposure.Override(0.22f);
        colorAdj.contrast.Override(16f);
        colorAdj.saturation.Override(8f);
        AssetDatabase.AddObjectToAsset(colorAdj, profile);

        // D. Shadows Midtones Highlights
        ShadowsMidtonesHighlights smh = profile.Add<ShadowsMidtonesHighlights>();
        smh.active = true;
        smh.shadows.Override(new Vector4(0.85f, 0.90f, 1.08f, 1.0f)); // Cool slate shadows
        smh.midtones.Override(new Vector4(1.0f, 0.98f, 0.96f, 1.0f));
        smh.highlights.Override(new Vector4(1.15f, 1.08f, 0.92f, 1.0f)); // Warm sunny highlights
        AssetDatabase.AddObjectToAsset(smh, profile);

        EditorUtility.SetDirty(profile);
        AssetDatabase.SaveAssets();

        globalVolume.sharedProfile = profile;
        EditorUtility.SetDirty(globalVolume);
    }

    private static void EnsureCanvasCompatibility()
    {
        // Find MainMenu_Canvas if present, or instantiate from prefab
        var canvas = GameObject.Find("MainMenu_Canvas");
        if (canvas == null)
        {
            string canvasPrefabPath = "Assets/Prefabs/MainMenu_Canvas.prefab";
            GameObject canvasPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(canvasPrefabPath);
            if (canvasPrefab != null)
            {
                canvas = (GameObject)PrefabUtility.InstantiatePrefab(canvasPrefab);
                canvas.name = "MainMenu_Canvas";
                Debug.Log("[SetupMainScreenEnvironment] Instantiated MainMenu_Canvas in scene!");
            }
        }

        if (canvas != null)
        {
            Canvas c = canvas.GetComponent<Canvas>();
            if (c != null)
            {
                // Ensure UI renders clearly over the 3D scene
                c.renderMode = RenderMode.ScreenSpaceOverlay;
                c.sortingOrder = 10;
            }
        }

        // Ensure EventSystem exists for UI interaction
        if (Object.FindObjectOfType<UnityEngine.EventSystems.EventSystem>() == null)
        {
            GameObject es = new GameObject("EventSystem");
            es.AddComponent<UnityEngine.EventSystems.EventSystem>();
            es.AddComponent<UnityEngine.EventSystems.StandaloneInputModule>();
        }
    }

    // ==========================================
    // HELPER: MESH BUILDER
    // ==========================================
    private class MeshBuilder
    {
        private List<Vector3> verts = new List<Vector3>();
        private List<int> tris = new List<int>();
        private List<Vector3> norms = new List<Vector3>();
        private List<Vector2> uvs = new List<Vector2>();

        public void AddTriangle(Vector3 a, Vector3 b, Vector3 c)
        {
            int idx = verts.Count;
            verts.Add(a);
            verts.Add(b);
            verts.Add(c);

            tris.Add(idx);
            tris.Add(idx + 1);
            tris.Add(idx + 2);

            Vector3 norm = Vector3.Cross(b - a, c - a).normalized;
            norms.Add(norm);
            norms.Add(norm);
            norms.Add(norm);

            uvs.Add(new Vector2(0, 0));
            uvs.Add(new Vector2(1, 0));
            uvs.Add(new Vector2(0.5f, 1));
        }

        public void AddQuad(Vector3 a, Vector3 b, Vector3 c, Vector3 d)
        {
            int idx = verts.Count;
            verts.Add(a);
            verts.Add(b);
            verts.Add(c);
            verts.Add(d);

            tris.Add(idx);
            tris.Add(idx + 1);
            tris.Add(idx + 2);

            tris.Add(idx);
            tris.Add(idx + 2);
            tris.Add(idx + 3);

            Vector3 norm = Vector3.Cross(b - a, c - a).normalized;
            norms.Add(norm);
            norms.Add(norm);
            norms.Add(norm);
            norms.Add(norm);

            uvs.Add(new Vector2(0, 0));
            uvs.Add(new Vector2(1, 0));
            uvs.Add(new Vector2(1, 1));
            uvs.Add(new Vector2(0, 1));
        }

        public void AddOrientedBox(Vector3 center, Vector3 size, Quaternion rotation)
        {
            Vector3 h = size * 0.5f;

            // 8 corners in local space
            Vector3[] c = new Vector3[]
            {
                new Vector3(-h.x, -h.y, -h.z),
                new Vector3( h.x, -h.y, -h.z),
                new Vector3( h.x, -h.y,  h.z),
                new Vector3(-h.x, -h.y,  h.z),
                new Vector3(-h.x,  h.y, -h.z),
                new Vector3( h.x,  h.y, -h.z),
                new Vector3( h.x,  h.y,  h.z),
                new Vector3(-h.x,  h.y,  h.z)
            };

            for (int i = 0; i < 8; i++)
            {
                c[i] = center + rotation * c[i];
            }

            // 6 faces
            AddQuad(c[0], c[1], c[5], c[4]); // Front
            AddQuad(c[1], c[2], c[6], c[5]); // Right
            AddQuad(c[2], c[3], c[7], c[6]); // Back
            AddQuad(c[3], c[0], c[4], c[7]); // Left
            AddQuad(c[4], c[5], c[6], c[7]); // Top
            AddQuad(c[3], c[2], c[1], c[0]); // Bottom
        }

        public Mesh Build(string name)
        {
            Mesh mesh = new Mesh();
            mesh.name = name;
            mesh.vertices = verts.ToArray();
            mesh.triangles = tris.ToArray();
            mesh.normals = norms.ToArray();
            mesh.uv = uvs.ToArray();
            mesh.RecalculateBounds();
            mesh.RecalculateTangents();
            return mesh;
        }
    }
}
#endif
