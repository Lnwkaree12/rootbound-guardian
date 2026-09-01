#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;
using System.Collections.Generic;
using System.IO;

[InitializeOnLoad]
public class CreateRockPrefab
{
    static CreateRockPrefab()
    {
        EditorApplication.delayCall += EnsureRockPrefabExists;
    }

    [MenuItem("Tools/SproutScout/Create Rock Prefab")]
    public static void ForceCreateRock()
    {
        CreateRock(true);
    }

    private static void EnsureRockPrefabExists()
    {
        if (EditorApplication.isPlaying || Application.isPlaying) return;
        CreateRock(false);
    }

    private static void CreateRock(bool forceOverwrite)
    {
        string prefabPath = "Assets/Prefabs/Rock.prefab";
        string meshPath = "Assets/Models/RockMesh.asset";
        string matPath = "Assets/Materials/RockMaterial.mat";
        string texPath = "Assets/Models/Textures/RockTexture.png";

        // Skip if already exists and we are not forcing overwrite
        if (!forceOverwrite && AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath) != null)
        {
            return;
        }

        Debug.Log("[CreateRockPrefab] Generating stylized 3D Rock assets...");

        // Ensure directories exist
        EnsureDirectoriesExist();

        // 1. Generate procedural rock texture if it doesn't exist
        if (!File.Exists(texPath) || forceOverwrite)
        {
            GenerateRockTexture(texPath);
        }

        // Load the texture
        Texture2D rockTex = AssetDatabase.LoadAssetAtPath<Texture2D>(texPath);
        if (rockTex == null)
        {
            // Trigger an import if it was just written
            AssetDatabase.ImportAsset(texPath);
            rockTex = AssetDatabase.LoadAssetAtPath<Texture2D>(texPath);
        }

        // 2. Create Material using URP Lit shader
        Shader urpShader = Shader.Find("Universal Render Pipeline/Lit");
        if (urpShader == null) urpShader = Shader.Find("Standard");

        Material rockMat = AssetDatabase.LoadAssetAtPath<Material>(matPath);
        if (rockMat == null || forceOverwrite)
        {
            rockMat = new Material(urpShader);
            rockMat.color = Color.white; // Base color comes from texture
            if (rockTex != null)
            {
                rockMat.SetTexture("_BaseMap", rockTex);
                rockMat.SetTexture("_MainTex", rockTex); // Backup for standard
            }
            if (rockMat.HasProperty("_Smoothness")) rockMat.SetFloat("_Smoothness", 0.1f);
            if (rockMat.HasProperty("_Roughness")) rockMat.SetFloat("_Roughness", 0.9f);
            if (rockMat.HasProperty("_Metallic")) rockMat.SetFloat("_Metallic", 0.0f);
            
            AssetDatabase.CreateAsset(rockMat, matPath);
        }

        // 3. Generate Low-Poly Rock Mesh
        Mesh mesh = AssetDatabase.LoadAssetAtPath<Mesh>(meshPath);
        if (mesh == null || forceOverwrite)
        {
            mesh = GenerateLowPolyRockMesh();
            AssetDatabase.CreateAsset(mesh, meshPath);
        }

        // 4. Create GameObject
        GameObject rockGO = new GameObject("Rock");
        
        // Add MeshFilter & MeshRenderer
        MeshFilter mf = rockGO.AddComponent<MeshFilter>();
        mf.sharedMesh = mesh;
        MeshRenderer mr = rockGO.AddComponent<MeshRenderer>();
        mr.sharedMaterial = rockMat;

        // Add MeshCollider for precise, convex collisions
        MeshCollider mc = rockGO.AddComponent<MeshCollider>();
        mc.sharedMesh = mesh;
        mc.convex = true;

        // 5. Save as Prefab
        PrefabUtility.SaveAsPrefabAssetAndConnect(rockGO, prefabPath, InteractionMode.AutomatedAction);
        Object.DestroyImmediate(rockGO);

        AssetDatabase.Refresh();
        Debug.Log($"[CreateRockPrefab] Successfully generated Rock Prefab at: {prefabPath}");
    }

    private static void EnsureDirectoriesExist()
    {
        string[] dirs = new string[] { "Assets/Models", "Assets/Models/Textures", "Assets/Materials", "Assets/Prefabs" };
        foreach (string dir in dirs)
        {
            if (!Directory.Exists(Path.Combine(Application.dataPath, "..", dir)))
            {
                Directory.CreateDirectory(Path.Combine(Application.dataPath, "..", dir));
            }
        }
    }

    private static void GenerateRockTexture(string path)
    {
        int size = 512;
        Texture2D rockTex = new Texture2D(size, size, TextureFormat.RGB24, false);
        
        // Seed random for reproducible rock pattern
        Random.InitState(12345);

        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                // Dark grey base color
                float rBase = 0.42f;
                
                // Add Perlin noise for stone texture details
                float n1 = Mathf.PerlinNoise(x * 0.03f, y * 0.03f) * 0.08f;
                float n2 = Mathf.PerlinNoise(x * 0.15f, y * 0.15f) * 0.04f;
                
                float colVal = rBase + n1 + n2;
                
                // Slight variance between channels for organic slate tone
                Color pixelColor = new Color(
                    colVal + Random.Range(-0.01f, 0.01f),
                    colVal + Random.Range(-0.01f, 0.01f) - 0.01f, // slightly less green
                    colVal + Random.Range(0f, 0.02f) + 0.01f     // slightly more blue for slate/grey feel
                );
                
                rockTex.SetPixel(x, y, pixelColor);
            }
        }

        // Draw rock cracks / veins
        int numVeins = 6;
        for (int vein = 0; vein < numVeins; vein++)
        {
            int startX = Random.Range(0, size);
            int startY = Random.Range(0, size);
            int len = Random.Range(60, 200);
            float angle = Random.Range(0f, Mathf.PI * 2f);
            
            float cx = startX;
            float cy = startY;
            for (int i = 0; i < len; i++)
            {
                cx += Mathf.Cos(angle) + Random.Range(-1.2f, 1.2f);
                cy += Mathf.Sin(angle) + Random.Range(-1.2f, 1.2f);
                
                int px = Mathf.Clamp((int)cx, 0, size - 1);
                int py = Mathf.Clamp((int)cy, 0, size - 1);
                
                // Draw dark cracks
                rockTex.SetPixel(px, py, new Color(0.22f, 0.22f, 0.25f));
                
                // Slight shading next to the crack for depth
                if (px + 1 < size) rockTex.SetPixel(px + 1, py, new Color(0.25f, 0.25f, 0.28f));
                if (py + 1 < size) rockTex.SetPixel(px, py + 1, new Color(0.25f, 0.25f, 0.28f));
            }
        }

        rockTex.Apply();

        byte[] pngBytes = rockTex.EncodeToPNG();
        File.WriteAllBytes(path, pngBytes);
        
        // Clean up memory
        Object.DestroyImmediate(rockTex);
    }

    private static Mesh GenerateLowPolyRockMesh()
    {
        List<Vector3> verts = new List<Vector3>();
        List<int> tris = new List<int>();

        // Golden ratio
        float t = (1.0f + Mathf.Sqrt(5.0f)) / 2.0f;

        // Base 12 vertices of an icosahedron
        verts.Add(new Vector3(-1f,  t,  0f).normalized);
        verts.Add(new Vector3( 1f,  t,  0f).normalized);
        verts.Add(new Vector3(-1f, -t,  0f).normalized);
        verts.Add(new Vector3( 1f, -t,  0f).normalized);

        verts.Add(new Vector3( 0f, -1f,  t).normalized);
        verts.Add(new Vector3( 0f,  1f,  t).normalized);
        verts.Add(new Vector3( 0f, -1f, -t).normalized);
        verts.Add(new Vector3( 0f,  1f, -t).normalized);

        verts.Add(new Vector3( t,  0f, -1f).normalized);
        verts.Add(new Vector3( t,  0f,  1f).normalized);
        verts.Add(new Vector3(-t,  0f, -1f).normalized);
        verts.Add(new Vector3(-t,  0f,  1f).normalized);

        // 20 faces of icosahedron
        int[] baseTris = new int[]
        {
            0, 11, 5,  0, 5, 1,  0, 1, 7,  0, 7, 10,  0, 10, 11,
            1, 5, 9,   5, 11, 4, 11, 10, 2, 10, 7, 6,  7, 1, 8,
            3, 9, 4,   3, 4, 2,  3, 2, 6,  3, 6, 8,   3, 8, 9,
            4, 9, 5,   2, 4, 11, 6, 2, 10, 8, 6, 7,   9, 8, 1
        };
        tris.AddRange(baseTris);

        // Subdivide twice
        Subdivide(verts, tris);
        Subdivide(verts, tris);

        // Perturb vertices to create a rocky look
        Random.InitState(98765); // Stable seed for same rock shape
        for (int i = 0; i < verts.Count; i++)
        {
            Vector3 v = verts[i];
            
            // Noise values based on coordinates
            float n1 = Mathf.PerlinNoise(v.x * 2.2f + 1.1f, v.y * 2.2f + 2.2f) * 0.18f;
            float n2 = Mathf.PerlinNoise(v.y * 4.4f + 3.3f, v.z * 4.4f + 4.4f) * 0.08f;
            
            // Apply noise along vertex normal
            v = v * (1.0f + n1 + n2);
            
            // Apply elongation / compression for rock silhouette
            v.x *= 1.35f; // slightly wider
            v.y *= 0.85f; // slightly flatter
            v.z *= 1.05f;
            
            verts[i] = v;
        }

        // Duplicate vertices to generate flat shading and compute UVs
        List<Vector3> flatVerts = new List<Vector3>();
        List<int> flatTris = new List<int>();
        List<Vector2> flatUVs = new List<Vector2>();

        for (int i = 0; i < tris.Count; i++)
        {
            int origIdx = tris[i];
            Vector3 v = verts[origIdx];
            flatVerts.Add(v);
            flatTris.Add(i);

            // Spherical UV projection
            Vector3 dir = v.normalized;
            float u = 0.5f + Mathf.Atan2(dir.z, dir.x) / (2.0f * Mathf.PI);
            float vCoord = 0.5f - Mathf.Asin(dir.y) / Mathf.PI;
            flatUVs.Add(new Vector2(u, vCoord));
        }

        Mesh mesh = new Mesh();
        mesh.name = "RockMesh";
        mesh.vertices = flatVerts.ToArray();
        mesh.triangles = flatTris.ToArray();
        mesh.uv = flatUVs.ToArray();
        
        mesh.RecalculateNormals();
        mesh.RecalculateBounds();
        mesh.RecalculateTangents();

        return mesh;
    }

    private static void Subdivide(List<Vector3> verts, List<int> tris)
    {
        Dictionary<long, int> midpointCache = new Dictionary<long, int>();
        List<int> newTris = new List<int>();

        for (int i = 0; i < tris.Count; i += 3)
        {
            int a = tris[i];
            int b = tris[i + 1];
            int c = tris[i + 2];

            int ab = GetMidpointIndex(midpointCache, verts, a, b);
            int bc = GetMidpointIndex(midpointCache, verts, b, c);
            int ca = GetMidpointIndex(midpointCache, verts, c, a);

            newTris.Add(a); newTris.Add(ab); newTris.Add(ca);
            newTris.Add(b); newTris.Add(bc); newTris.Add(ab);
            newTris.Add(c); newTris.Add(ca); newTris.Add(bc);
            newTris.Add(ab); newTris.Add(bc); newTris.Add(ca);
        }

        tris.Clear();
        tris.AddRange(newTris);
    }

    private static int GetMidpointIndex(Dictionary<long, int> cache, List<Vector3> verts, int p1, int p2)
    {
        long key = ((long)Mathf.Min(p1, p2) << 32) + Mathf.Max(p1, p2);
        if (cache.TryGetValue(key, out int index))
        {
            return index;
        }

        Vector3 v1 = verts[p1];
        Vector3 v2 = verts[p2];
        Vector3 middle = ((v1 + v2) / 2.0f).normalized;

        verts.Add(middle);
        int newIndex = verts.Count - 1;
        cache.Add(key, newIndex);
        return newIndex;
    }
}
#endif
