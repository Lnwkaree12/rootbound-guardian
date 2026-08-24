#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;
using System.Collections.Generic;

[InitializeOnLoad]
public class CreateTorchModel
{
    static CreateTorchModel()
    {
        // Run once when Unity compiles or launches to ensure the prefab is ready
        EditorApplication.delayCall += EnsureTorchPrefabExists;
    }

    [MenuItem("Tools/SproutScout/Create Torch Model Prefab")]
    public static void ForceCreateTorch()
    {
        CreateTorch(true);
    }

    private static void EnsureTorchPrefabExists()
    {
        CreateTorch(false);
    }

    private static void CreateTorch(bool forceOverwrite)
    {
        string prefabPath = "Assets/Prefabs/Torch.prefab";
        string meshPath = "Assets/Models/TorchMesh.asset";
        string woodMatPath = "Assets/Materials/TorchWoodMaterial.mat";
        string metalMatPath = "Assets/Materials/TorchMetalMaterial.mat";

        // Skip if already exists and we are not forcing overwrite
        if (!forceOverwrite && AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath) != null)
        {
            return;
        }

        Debug.Log("[CreateTorchModel] Generating low-poly 3D Torch assets...");

        // Ensure directories exist
        if (!AssetDatabase.IsValidFolder("Assets/Models"))
            AssetDatabase.CreateFolder("Assets", "Models");
        if (!AssetDatabase.IsValidFolder("Assets/Materials"))
            AssetDatabase.CreateFolder("Assets", "Materials");
        if (!AssetDatabase.IsValidFolder("Assets/Prefabs"))
            AssetDatabase.CreateFolder("Assets", "Prefabs");

        // 1. Create Materials using URP Lit shader
        Shader standardShader = Shader.Find("Universal Render Pipeline/Lit");
        if (standardShader == null) standardShader = Shader.Find("Standard");

        Material woodMat = AssetDatabase.LoadAssetAtPath<Material>(woodMatPath);
        if (woodMat == null || forceOverwrite)
        {
            woodMat = new Material(standardShader);
            woodMat.color = new Color(0.38f, 0.24f, 0.15f); // Natural Wood Brown
            if (woodMat.HasProperty("_Smoothness")) woodMat.SetFloat("_Smoothness", 0.05f);
            if (woodMat.HasProperty("_Roughness")) woodMat.SetFloat("_Roughness", 0.95f);
            AssetDatabase.CreateAsset(woodMat, woodMatPath);
        }

        Material metalMat = AssetDatabase.LoadAssetAtPath<Material>(metalMatPath);
        if (metalMat == null || forceOverwrite)
        {
            metalMat = new Material(standardShader);
            metalMat.color = new Color(0.18f, 0.18f, 0.20f); // Dark Charcoal/Cast Iron Metal
            if (metalMat.HasProperty("_Metallic")) metalMat.SetFloat("_Metallic", 0.85f);
            if (metalMat.HasProperty("_Smoothness")) metalMat.SetFloat("_Smoothness", 0.55f);
            AssetDatabase.CreateAsset(metalMat, metalMatPath);
        }

        // 2. Generate Low-Poly Torch Mesh
        Mesh mesh = AssetDatabase.LoadAssetAtPath<Mesh>(meshPath);
        if (mesh == null || forceOverwrite)
        {
            mesh = GenerateLowPolyTorchMesh();
            AssetDatabase.CreateAsset(mesh, meshPath);
        }

        // 3. Create the GameObject
        GameObject torchGO = new GameObject("Torch");
        
        // Add MeshFilter & MeshRenderer
        MeshFilter mf = torchGO.AddComponent<MeshFilter>();
        mf.sharedMesh = mesh;
        MeshRenderer mr = torchGO.AddComponent<MeshRenderer>();
        mr.sharedMaterials = new Material[] { woodMat, metalMat };

        // Add Collider (suitable for a standing/holding torch)
        BoxCollider bc = torchGO.AddComponent<BoxCollider>();
        bc.center = new Vector3(0f, 0.38f, 0f);
        bc.size = new Vector3(0.16f, 0.85f, 0.16f);

        // 4. Instantiate and attach the TorchFire particle system if it exists
        GameObject firePrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/TorchFire.prefab");
        if (firePrefab != null)
        {
            GameObject fireInstance = GameObject.Instantiate(firePrefab, torchGO.transform);
            fireInstance.name = firePrefab.name; // Keep name clean
            fireInstance.transform.localPosition = new Vector3(0f, 0.74f, 0f); // Put it exactly at the top of the torch cap
            fireInstance.transform.localRotation = Quaternion.identity;
        }

        // 5. Save as Prefab
        string localPath = AssetDatabase.GenerateUniqueAssetPath(prefabPath);
        if (!forceOverwrite && AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath) == null)
        {
            localPath = prefabPath;
        }
        else if (forceOverwrite)
        {
            localPath = prefabPath;
        }

        PrefabUtility.SaveAsPrefabAssetAndConnect(torchGO, localPath, InteractionMode.AutomatedAction);
        Object.DestroyImmediate(torchGO);

        AssetDatabase.Refresh();
        Debug.Log($"[CreateTorchModel] Successfully generated Torch Prefab at: {localPath}");
    }

    private static Mesh GenerateLowPolyTorchMesh()
    {
        Mesh mesh = new Mesh();
        mesh.name = "TorchMesh";

        List<Vector3> vertices = new List<Vector3>();
        List<int> woodTriangles = new List<int>();
        List<int> metalTriangles = new List<int>();

        int numSides = 6; // 6 sides for flat-shaded low-poly style
        
        // --- 1. Wooden Handle Geometry ---
        // Cylinder from y = 0 to y = 0.60
        float hHeight = 0.60f;
        float rBottom = 0.022f;
        float rTop = 0.038f;

        // Side quads (flat-shaded, unique vertices)
        for (int i = 0; i < numSides; i++)
        {
            float angle1 = (i * 2 * Mathf.PI) / numSides;
            float angle2 = ((i + 1) * 2 * Mathf.PI) / numSides;

            Vector3 b1 = new Vector3(Mathf.Cos(angle1) * rBottom, 0f, Mathf.Sin(angle1) * rBottom);
            Vector3 b2 = new Vector3(Mathf.Cos(angle2) * rBottom, 0f, Mathf.Sin(angle2) * rBottom);
            Vector3 t1 = new Vector3(Mathf.Cos(angle1) * rTop, hHeight, Mathf.Sin(angle1) * rTop);
            Vector3 t2 = new Vector3(Mathf.Cos(angle2) * rTop, hHeight, Mathf.Sin(angle2) * rTop);

            int startVert = vertices.Count;
            vertices.Add(b1);
            vertices.Add(b2);
            vertices.Add(t2);
            vertices.Add(t1);

            woodTriangles.Add(startVert);
            woodTriangles.Add(startVert + 2);
            woodTriangles.Add(startVert + 1);

            woodTriangles.Add(startVert);
            woodTriangles.Add(startVert + 3);
            woodTriangles.Add(startVert + 2);
        }

        // Bottom Cap (wood)
        for (int i = 0; i < numSides; i++)
        {
            float angle1 = (i * 2 * Mathf.PI) / numSides;
            float angle2 = ((i + 1) * 2 * Mathf.PI) / numSides;

            Vector3 b1 = new Vector3(Mathf.Cos(angle1) * rBottom, 0f, Mathf.Sin(angle1) * rBottom);
            Vector3 b2 = new Vector3(Mathf.Cos(angle2) * rBottom, 0f, Mathf.Sin(angle2) * rBottom);
            Vector3 center = Vector3.zero;

            int startVert = vertices.Count;
            vertices.Add(center);
            vertices.Add(b1);
            vertices.Add(b2);

            woodTriangles.Add(startVert);
            woodTriangles.Add(startVert + 2);
            woodTriangles.Add(startVert + 1);
        }

        // --- 2. Metal Bracket Geometry ---
        // Flared top cap from y = 0.60 to y = 0.75
        float bHeight = 0.15f;
        float rMetalBottom = 0.038f;
        float rMetalTop = 0.075f;
        float yOffset = 0.60f;

        // Side quads (metal)
        for (int i = 0; i < numSides; i++)
        {
            float angle1 = (i * 2 * Mathf.PI) / numSides;
            float angle2 = ((i + 1) * 2 * Mathf.PI) / numSides;

            Vector3 b1 = new Vector3(Mathf.Cos(angle1) * rMetalBottom, yOffset, Mathf.Sin(angle1) * rMetalBottom);
            Vector3 b2 = new Vector3(Mathf.Cos(angle2) * rMetalBottom, yOffset, Mathf.Sin(angle2) * rMetalBottom);
            Vector3 t1 = new Vector3(Mathf.Cos(angle1) * rMetalTop, yOffset + bHeight, Mathf.Sin(angle1) * rMetalTop);
            Vector3 t2 = new Vector3(Mathf.Cos(angle2) * rMetalTop, yOffset + bHeight, Mathf.Sin(angle2) * rMetalTop);

            int startVert = vertices.Count;
            vertices.Add(b1);
            vertices.Add(b2);
            vertices.Add(t2);
            vertices.Add(t1);

            metalTriangles.Add(startVert);
            metalTriangles.Add(startVert + 2);
            metalTriangles.Add(startVert + 1);

            metalTriangles.Add(startVert);
            metalTriangles.Add(startVert + 3);
            metalTriangles.Add(startVert + 2);
        }

        // Top Cap (metal, slightly recessed where the fire particles emit)
        for (int i = 0; i < numSides; i++)
        {
            float angle1 = (i * 2 * Mathf.PI) / numSides;
            float angle2 = ((i + 1) * 2 * Mathf.PI) / numSides;

            Vector3 t1 = new Vector3(Mathf.Cos(angle1) * rMetalTop, yOffset + bHeight, Mathf.Sin(angle1) * rMetalTop);
            Vector3 t2 = new Vector3(Mathf.Cos(angle2) * rMetalTop, yOffset + bHeight, Mathf.Sin(angle2) * rMetalTop);
            Vector3 center = new Vector3(0f, yOffset + bHeight - 0.015f, 0f); // Slightly indented for realism

            int startVert = vertices.Count;
            vertices.Add(center);
            vertices.Add(t1);
            vertices.Add(t2);

            metalTriangles.Add(startVert);
            metalTriangles.Add(startVert + 1);
            metalTriangles.Add(startVert + 2);
        }

        // Assign to mesh
        mesh.vertices = vertices.ToArray();
        mesh.subMeshCount = 2;
        mesh.SetTriangles(woodTriangles.ToArray(), 0);
        mesh.SetTriangles(metalTriangles.ToArray(), 1);

        // Recalculate variables for flat shading low-poly look
        mesh.RecalculateNormals();
        mesh.RecalculateBounds();
        mesh.RecalculateTangents();

        return mesh;
    }
}
#endif
