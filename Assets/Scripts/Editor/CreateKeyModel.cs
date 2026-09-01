#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;
using System.Collections.Generic;

[InitializeOnLoad]
public class CreateKeyModel
{
    static CreateKeyModel()
    {
        EditorApplication.delayCall += EnsureKeyPrefabExists;
    }

    [MenuItem("Tools/SproutScout/Create Key Model Prefab")]
    public static void ForceCreateKey()
    {
        CreateKey(true);
    }

    private static void EnsureKeyPrefabExists()
    {
        if (EditorApplication.isPlaying || Application.isPlaying) return;
        CreateKey(false);
    }

    private static void CreateKey(bool forceOverwrite)
    {
        string prefabPath = "Assets/Prefabs/Key.prefab";
        string meshPath = "Assets/Models/KeyMesh.asset";
        string goldMatPath = "Assets/Materials/KeyGoldMaterial.mat";

        if (!forceOverwrite && AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath) != null)
        {
            return;
        }

        Debug.Log("[CreateKeyModel] Generating low-poly 3D Key assets...");

        // Ensure folders exist
        if (!AssetDatabase.IsValidFolder("Assets/Models"))
            AssetDatabase.CreateFolder("Assets", "Models");
        if (!AssetDatabase.IsValidFolder("Assets/Materials"))
            AssetDatabase.CreateFolder("Assets", "Materials");
        if (!AssetDatabase.IsValidFolder("Assets/Prefabs"))
            AssetDatabase.CreateFolder("Assets", "Prefabs");

        // 1. Create Gold Material (URP Lit)
        Shader standardShader = Shader.Find("Universal Render Pipeline/Lit");
        if (standardShader == null) standardShader = Shader.Find("Standard");

        Material goldMat = AssetDatabase.LoadAssetAtPath<Material>(goldMatPath);
        if (goldMat == null || forceOverwrite)
        {
            goldMat = new Material(standardShader);
            goldMat.color = new Color(1.0f, 0.82f, 0.23f); // Metallic Gold Yellow
            if (goldMat.HasProperty("_Metallic")) goldMat.SetFloat("_Metallic", 0.9f);
            if (goldMat.HasProperty("_Smoothness")) goldMat.SetFloat("_Smoothness", 0.75f);
            AssetDatabase.CreateAsset(goldMat, goldMatPath);
        }

        // 2. Generate Key Mesh
        Mesh mesh = AssetDatabase.LoadAssetAtPath<Mesh>(meshPath);
        if (mesh == null || forceOverwrite)
        {
            mesh = GenerateProceduralKeyMesh();
            AssetDatabase.CreateAsset(mesh, meshPath);
        }

        // 3. Create Key GameObject
        GameObject keyGO = new GameObject("Key");
        
        MeshFilter mf = keyGO.AddComponent<MeshFilter>();
        mf.sharedMesh = mesh;
        MeshRenderer mr = keyGO.AddComponent<MeshRenderer>();
        mr.sharedMaterial = goldMat;

        // Add trigger collider for picking up
        SphereCollider sc = keyGO.AddComponent<SphereCollider>();
        sc.isTrigger = true;
        sc.center = new Vector3(0.2f, 0f, 0f); // Center of the key
        sc.radius = 0.35f;

        // Attach KeyPickup script
        keyGO.AddComponent<KeyPickup>();

        // 4. Save as Prefab
        string finalPath = prefabPath;
        PrefabUtility.SaveAsPrefabAssetAndConnect(keyGO, finalPath, InteractionMode.AutomatedAction);
        Object.DestroyImmediate(keyGO);

        AssetDatabase.Refresh();
        Debug.Log($"[CreateKeyModel] Successfully generated Key Prefab at: {finalPath}");
    }

    private static Mesh GenerateProceduralKeyMesh()
    {
        Mesh mesh = new Mesh();
        mesh.name = "KeyMesh";

        List<Vector3> vertices = new List<Vector3>();
        List<int> triangles = new List<int>();

        // We will build:
        // A. Hexagonal hollow ring (head) centered at (0, 0, 0)
        // B. Shaft extending along X axis from X = 0.10 to 0.45
        // C. Teeth extending along -Y axis from X = 0.35 to 0.43

        // A. Ring (Head) - Hollow Hexagon
        int ringSides = 6;
        float outerRadius = 0.12f;
        float innerRadius = 0.06f;
        float thickness = 0.03f;

        // Front vertices (Z = -thickness/2)
        int frontOuterStart = vertices.Count;
        for (int i = 0; i < ringSides; i++)
        {
            float a = i * 2 * Mathf.PI / ringSides;
            vertices.Add(new Vector3(Mathf.Cos(a) * outerRadius, Mathf.Sin(a) * outerRadius, -thickness / 2f));
        }
        int frontInnerStart = vertices.Count;
        for (int i = 0; i < ringSides; i++)
        {
            float a = i * 2 * Mathf.PI / ringSides;
            vertices.Add(new Vector3(Mathf.Cos(a) * innerRadius, Mathf.Sin(a) * innerRadius, -thickness / 2f));
        }

        // Back vertices (Z = thickness/2)
        int backOuterStart = vertices.Count;
        for (int i = 0; i < ringSides; i++)
        {
            float a = i * 2 * Mathf.PI / ringSides;
            vertices.Add(new Vector3(Mathf.Cos(a) * outerRadius, Mathf.Sin(a) * outerRadius, thickness / 2f));
        }
        int backInnerStart = vertices.Count;
        for (int i = 0; i < ringSides; i++)
        {
            float a = i * 2 * Mathf.PI / ringSides;
            vertices.Add(new Vector3(Mathf.Cos(a) * innerRadius, Mathf.Sin(a) * innerRadius, thickness / 2f));
        }

        // Triangles for Ring Front Face
        for (int i = 0; i < ringSides; i++)
        {
            int next = (i + 1) % ringSides;
            // Triangle 1
            triangles.Add(frontOuterStart + i);
            triangles.Add(frontOuterStart + next);
            triangles.Add(frontInnerStart + next);
            // Triangle 2
            triangles.Add(frontOuterStart + i);
            triangles.Add(frontInnerStart + next);
            triangles.Add(frontInnerStart + i);
        }

        // Triangles for Ring Back Face (opposite winding order)
        for (int i = 0; i < ringSides; i++)
        {
            int next = (i + 1) % ringSides;
            // Triangle 1
            triangles.Add(backOuterStart + i);
            triangles.Add(backInnerStart + next);
            triangles.Add(backOuterStart + next);
            // Triangle 2
            triangles.Add(backOuterStart + i);
            triangles.Add(backInnerStart + i);
            triangles.Add(backInnerStart + next);
        }

        // Outer Edge Quads
        for (int i = 0; i < ringSides; i++)
        {
            int next = (i + 1) % ringSides;
            triangles.Add(frontOuterStart + i);
            triangles.Add(backOuterStart + next);
            triangles.Add(backOuterStart + i);

            triangles.Add(frontOuterStart + i);
            triangles.Add(frontOuterStart + next);
            triangles.Add(backOuterStart + next);
        }

        // Inner Edge Quads (pointing inside)
        for (int i = 0; i < ringSides; i++)
        {
            int next = (i + 1) % ringSides;
            triangles.Add(frontInnerStart + i);
            triangles.Add(backInnerStart + i);
            triangles.Add(backInnerStart + next);

            triangles.Add(frontInnerStart + i);
            triangles.Add(backInnerStart + next);
            triangles.Add(frontInnerStart + next);
        }

        // B. Shaft (Body) - Hexagonal Cylinder from X = 0.10f to X = 0.45f
        float shaftRadius = 0.02f;
        int shaftStartVert = vertices.Count;

        // Vertices at X = 0.10
        for (int i = 0; i < 6; i++)
        {
            float a = i * 2 * Mathf.PI / 6;
            vertices.Add(new Vector3(0.10f, Mathf.Cos(a) * shaftRadius, Mathf.Sin(a) * shaftRadius));
        }

        // Vertices at X = 0.45
        for (int i = 0; i < 6; i++)
        {
            float a = i * 2 * Mathf.PI / 6;
            vertices.Add(new Vector3(0.45f, Mathf.Cos(a) * shaftRadius, Mathf.Sin(a) * shaftRadius));
        }

        // Quads for Shaft Sides
        for (int i = 0; i < 6; i++)
        {
            int next = (i + 1) % 6;
            int b1 = shaftStartVert + i;
            int b2 = shaftStartVert + next;
            int t1 = shaftStartVert + 6 + i;
            int t2 = shaftStartVert + 6 + next;

            triangles.Add(b1);
            triangles.Add(t2);
            triangles.Add(t1);

            triangles.Add(b1);
            triangles.Add(b2);
            triangles.Add(t2);
        }

        // End Cap at X = 0.45 (pointing outwards)
        int endCenterVert = vertices.Count;
        vertices.Add(new Vector3(0.45f, 0f, 0f));
        for (int i = 0; i < 6; i++)
        {
            int next = (i + 1) % 6;
            triangles.Add(endCenterVert);
            triangles.Add(shaftStartVert + 6 + i);
            triangles.Add(shaftStartVert + 6 + next);
        }

        // C. Teeth (The Bit) - Flat Box from X = 0.35f to 0.43f, Y = -0.02f to -0.09f, Z = -0.01f to 0.01f
        // Box vertices
        int bitStartVert = vertices.Count;
        float xMin = 0.35f;
        float xMax = 0.43f;
        float yMin = -0.09f;
        float yMax = -0.02f;
        float zHalf = 0.01f;

        // Z = -zHalf face
        vertices.Add(new Vector3(xMin, yMin, -zHalf)); // 0
        vertices.Add(new Vector3(xMax, yMin, -zHalf)); // 1
        vertices.Add(new Vector3(xMax, yMax, -zHalf)); // 2
        vertices.Add(new Vector3(xMin, yMax, -zHalf)); // 3

        // Z = zHalf face
        vertices.Add(new Vector3(xMin, yMin, zHalf)); // 4
        vertices.Add(new Vector3(xMax, yMin, zHalf)); // 5
        vertices.Add(new Vector3(xMax, yMax, zHalf)); // 6
        vertices.Add(new Vector3(xMin, yMax, zHalf)); // 7

        // Triangles for Teeth Box
        // Front (Z = -zHalf)
        triangles.Add(bitStartVert);
        triangles.Add(bitStartVert + 2);
        triangles.Add(bitStartVert + 1);
        triangles.Add(bitStartVert);
        triangles.Add(bitStartVert + 3);
        triangles.Add(bitStartVert + 2);

        // Back (Z = zHalf)
        triangles.Add(bitStartVert + 4);
        triangles.Add(bitStartVert + 5);
        triangles.Add(bitStartVert + 6);
        triangles.Add(bitStartVert + 4);
        triangles.Add(bitStartVert + 6);
        triangles.Add(bitStartVert + 7);

        // Left (X = xMin)
        triangles.Add(bitStartVert);
        triangles.Add(bitStartVert + 4);
        triangles.Add(bitStartVert + 7);
        triangles.Add(bitStartVert);
        triangles.Add(bitStartVert + 7);
        triangles.Add(bitStartVert + 3);

        // Right (X = xMax)
        triangles.Add(bitStartVert + 1);
        triangles.Add(bitStartVert + 6);
        triangles.Add(bitStartVert + 5);
        triangles.Add(bitStartVert + 1);
        triangles.Add(bitStartVert + 2);
        triangles.Add(bitStartVert + 6);

        // Bottom (Y = yMin)
        triangles.Add(bitStartVert);
        triangles.Add(bitStartVert + 1);
        triangles.Add(bitStartVert + 5);
        triangles.Add(bitStartVert);
        triangles.Add(bitStartVert + 5);
        triangles.Add(bitStartVert + 4);

        // Top is embedded in the shaft, no need to draw top face

        // D. Second Tooth (Notch) - Flat Box from X = 0.35f to 0.38f, Y = -0.09f to -0.13f, Z = -0.01f to 0.01f
        int notchStartVert = vertices.Count;
        float nxMin = 0.35f;
        float nxMax = 0.38f;
        float nyMin = -0.13f;
        float nyMax = -0.09f;

        // Z = -zHalf face
        vertices.Add(new Vector3(nxMin, nyMin, -zHalf)); // 0
        vertices.Add(new Vector3(nxMax, nyMin, -zHalf)); // 1
        vertices.Add(new Vector3(nxMax, nyMax, -zHalf)); // 2
        vertices.Add(new Vector3(nxMin, nyMax, -zHalf)); // 3

        // Z = zHalf face
        vertices.Add(new Vector3(nxMin, nyMin, zHalf)); // 4
        vertices.Add(new Vector3(nxMax, nyMin, zHalf)); // 5
        vertices.Add(new Vector3(nxMax, nyMax, zHalf)); // 6
        vertices.Add(new Vector3(nxMin, nyMax, zHalf)); // 7

        // Triangles for Notch Box
        // Front (Z = -zHalf)
        triangles.Add(notchStartVert);
        triangles.Add(notchStartVert + 2);
        triangles.Add(notchStartVert + 1);
        triangles.Add(notchStartVert);
        triangles.Add(notchStartVert + 3);
        triangles.Add(notchStartVert + 2);

        // Back (Z = zHalf)
        triangles.Add(notchStartVert + 4);
        triangles.Add(notchStartVert + 5);
        triangles.Add(notchStartVert + 6);
        triangles.Add(notchStartVert + 4);
        triangles.Add(notchStartVert + 6);
        triangles.Add(notchStartVert + 7);

        // Left (X = nxMin)
        triangles.Add(notchStartVert);
        triangles.Add(notchStartVert + 4);
        triangles.Add(notchStartVert + 7);
        triangles.Add(notchStartVert);
        triangles.Add(notchStartVert + 7);
        triangles.Add(notchStartVert + 3);

        // Right (X = nxMax)
        triangles.Add(notchStartVert + 1);
        triangles.Add(notchStartVert + 6);
        triangles.Add(notchStartVert + 5);
        triangles.Add(notchStartVert + 1);
        triangles.Add(notchStartVert + 2);
        triangles.Add(notchStartVert + 6);

        // Bottom (Y = nyMin)
        triangles.Add(notchStartVert);
        triangles.Add(notchStartVert + 1);
        triangles.Add(notchStartVert + 5);
        triangles.Add(notchStartVert);
        triangles.Add(notchStartVert + 5);
        triangles.Add(notchStartVert + 4);

        // Assign to mesh
        mesh.vertices = vertices.ToArray();
        mesh.triangles = triangles.ToArray();

        // Flat shade mesh
        mesh.RecalculateNormals();
        mesh.RecalculateBounds();
        mesh.RecalculateTangents();

        return mesh;
    }
}
#endif
