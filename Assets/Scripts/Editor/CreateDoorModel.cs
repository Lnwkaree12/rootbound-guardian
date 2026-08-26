#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;
using System.Collections.Generic;

[InitializeOnLoad]
public class CreateDoorModel
{
    static CreateDoorModel()
    {
        EditorApplication.delayCall += EnsureDoorPrefabExists;
    }

    [MenuItem("Tools/SproutScout/Create Door Model Prefab")]
    public static void ForceCreateDoor()
    {
        CreateDoor(true);
    }

    private static void EnsureDoorPrefabExists()
    {
        if (EditorApplication.isPlaying || Application.isPlaying) return;
        CreateDoor(false);
    }

    private static void CreateDoor(bool forceOverwrite)
    {
        string prefabPath = "Assets/Prefabs/Door.prefab";
        string frameMeshPath = "Assets/Models/DoorFrameMesh.asset";
        string panelMeshPath = "Assets/Models/DoorPanelMesh.asset";
        string frameMatPath = "Assets/Materials/DoorFrameMaterial.mat";
        string panelMatPath = "Assets/Materials/DoorPanelMaterial.mat";

        if (!forceOverwrite && AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath) != null)
        {
            return;
        }

        Debug.Log("[CreateDoorModel] Generating low-poly 3D Door assets...");

        // Ensure folders exist
        if (!AssetDatabase.IsValidFolder("Assets/Models"))
            AssetDatabase.CreateFolder("Assets", "Models");
        if (!AssetDatabase.IsValidFolder("Assets/Materials"))
            AssetDatabase.CreateFolder("Assets", "Materials");
        if (!AssetDatabase.IsValidFolder("Assets/Prefabs"))
            AssetDatabase.CreateFolder("Assets", "Prefabs");

        // 1. Create Materials (URP Lit)
        Shader standardShader = Shader.Find("Universal Render Pipeline/Lit");
        if (standardShader == null) standardShader = Shader.Find("Standard");

        // Frame Material: Stone Gray
        Material frameMat = AssetDatabase.LoadAssetAtPath<Material>(frameMatPath);
        if (frameMat == null || forceOverwrite)
        {
            frameMat = new Material(standardShader);
            frameMat.color = new Color(0.38f, 0.40f, 0.42f); // Stone Slate Gray
            if (frameMat.HasProperty("_Roughness")) frameMat.SetFloat("_Roughness", 0.9f);
            AssetDatabase.CreateAsset(frameMat, frameMatPath);
        }

        // Panel Material: Oak Wood Brown
        Material panelMat = AssetDatabase.LoadAssetAtPath<Material>(panelMatPath);
        if (panelMat == null || forceOverwrite)
        {
            panelMat = new Material(standardShader);
            panelMat.color = new Color(0.48f, 0.31f, 0.20f); // Warm Wood Brown
            if (panelMat.HasProperty("_Roughness")) panelMat.SetFloat("_Roughness", 0.8f);
            AssetDatabase.CreateAsset(panelMat, panelMatPath);
        }

        // 2. Generate Frame Mesh
        Mesh frameMesh = AssetDatabase.LoadAssetAtPath<Mesh>(frameMeshPath);
        if (frameMesh == null || forceOverwrite)
        {
            frameMesh = GenerateFrameMesh();
            AssetDatabase.CreateAsset(frameMesh, frameMeshPath);
        }

        // 3. Generate Panel Mesh
        Mesh panelMesh = AssetDatabase.LoadAssetAtPath<Mesh>(panelMeshPath);
        if (panelMesh == null || forceOverwrite)
        {
            panelMesh = GeneratePanelMesh();
            AssetDatabase.CreateAsset(panelMesh, panelMeshPath);
        }

        // 4. Build Hierarchy
        GameObject doorRoot = new GameObject("Door");
        MeshFilter rf = doorRoot.AddComponent<MeshFilter>();
        rf.sharedMesh = frameMesh;
        MeshRenderer rr = doorRoot.AddComponent<MeshRenderer>();
        rr.sharedMaterial = frameMat;

        // Add trigger collider on root for player interaction zone
        BoxCollider triggerCol = doorRoot.AddComponent<BoxCollider>();
        triggerCol.isTrigger = true;
        triggerCol.center = new Vector3(0f, 1.0f, 0f);
        triggerCol.size = new Vector3(2.0f, 2.0f, 2.5f);

        // Add DoorController script
        DoorController controller = doorRoot.AddComponent<DoorController>();

        // Create Panel Child
        GameObject panelChild = new GameObject("DoorPanel");
        panelChild.transform.SetParent(doorRoot.transform, false);
        panelChild.transform.localPosition = new Vector3(-0.5f, 0f, 0f); // Position at left hinge

        MeshFilter pf = panelChild.AddComponent<MeshFilter>();
        pf.sharedMesh = panelMesh;
        MeshRenderer pr = panelChild.AddComponent<MeshRenderer>();
        pr.sharedMaterial = panelMat;

        // Add solid collision BoxCollider on Panel Child (keeps player out when closed)
        BoxCollider solidCol = panelChild.AddComponent<BoxCollider>();
        solidCol.isTrigger = false;
        solidCol.center = new Vector3(0.5f, 1.0f, 0f); // Center relative to panel pivot (-0.5f local offset)
        solidCol.size = new Vector3(1.0f, 2.0f, 0.05f);

        // Assign panel child reference to the door controller
        controller.doorPanel = panelChild.transform;

        // 5. Save as Prefab
        PrefabUtility.SaveAsPrefabAssetAndConnect(doorRoot, prefabPath, InteractionMode.AutomatedAction);
        Object.DestroyImmediate(doorRoot);

        AssetDatabase.Refresh();
        Debug.Log($"[CreateDoorModel] Successfully generated Door Prefab at: {prefabPath}");
    }

    private static Mesh GenerateFrameMesh()
    {
        Mesh mesh = new Mesh();
        mesh.name = "DoorFrameMesh";

        List<Vector3> vertices = new List<Vector3>();
        List<int> triangles = new List<int>();

        // Left post (wood/stone block)
        AddBoxToMesh(vertices, triangles, new Vector3(-0.55f, 1.0f, 0f), new Vector3(0.1f, 2.0f, 0.1f));
        // Right post (wood/stone block)
        AddBoxToMesh(vertices, triangles, new Vector3(0.55f, 1.0f, 0f), new Vector3(0.1f, 2.0f, 0.1f));
        // Top bar
        AddBoxToMesh(vertices, triangles, new Vector3(0f, 2.05f, 0f), new Vector3(1.2f, 0.1f, 0.1f));

        mesh.vertices = vertices.ToArray();
        mesh.triangles = triangles.ToArray();

        mesh.RecalculateNormals();
        mesh.RecalculateBounds();
        mesh.RecalculateTangents();

        return mesh;
    }

    private static Mesh GeneratePanelMesh()
    {
        Mesh mesh = new Mesh();
        mesh.name = "DoorPanelMesh";

        List<Vector3> vertices = new List<Vector3>();
        List<int> triangles = new List<int>();

        // Main wood panel (relative to left pivot X=0)
        AddBoxToMesh(vertices, triangles, new Vector3(0.5f, 1.0f, 0f), new Vector3(1.0f, 2.0f, 0.05f));

        // Horizontal decorative panel bands (raised wood details)
        AddBoxToMesh(vertices, triangles, new Vector3(0.5f, 1.7f, 0f), new Vector3(0.9f, 0.1f, 0.06f));
        AddBoxToMesh(vertices, triangles, new Vector3(0.5f, 1.0f, 0f), new Vector3(0.9f, 0.1f, 0.06f));
        AddBoxToMesh(vertices, triangles, new Vector3(0.5f, 0.3f, 0f), new Vector3(0.9f, 0.1f, 0.06f));

        // Vertical decorative details (sides)
        AddBoxToMesh(vertices, triangles, new Vector3(0.15f, 1.0f, 0f), new Vector3(0.1f, 1.8f, 0.06f));
        AddBoxToMesh(vertices, triangles, new Vector3(0.85f, 1.0f, 0f), new Vector3(0.1f, 1.8f, 0.06f));

        // Doorknob post (front and back)
        AddBoxToMesh(vertices, triangles, new Vector3(0.85f, 1.0f, -0.04f), new Vector3(0.03f, 0.03f, 0.04f));
        AddBoxToMesh(vertices, triangles, new Vector3(0.85f, 1.0f, 0.04f), new Vector3(0.03f, 0.03f, 0.04f));

        // Doorknob sphere (front and back)
        AddBoxToMesh(vertices, triangles, new Vector3(0.85f, 1.0f, -0.06f), new Vector3(0.06f, 0.06f, 0.06f));
        AddBoxToMesh(vertices, triangles, new Vector3(0.85f, 1.0f, 0.06f), new Vector3(0.06f, 0.06f, 0.06f));

        mesh.vertices = vertices.ToArray();
        mesh.triangles = triangles.ToArray();

        mesh.RecalculateNormals();
        mesh.RecalculateBounds();
        mesh.RecalculateTangents();

        return mesh;
    }

    private static void AddBoxToMesh(List<Vector3> verts, List<int> tris, Vector3 center, Vector3 size)
    {
        int start = verts.Count;
        Vector3 h = size * 0.5f;

        // 8 vertices of the box
        verts.Add(center + new Vector3(-h.x, -h.y, -h.z)); // 0
        verts.Add(center + new Vector3(h.x, -h.y, -h.z));  // 1
        verts.Add(center + new Vector3(h.x, h.y, -h.z));   // 2
        verts.Add(center + new Vector3(-h.x, h.y, -h.z));  // 3
        verts.Add(center + new Vector3(-h.x, -h.y, h.z));  // 4
        verts.Add(center + new Vector3(h.x, -h.y, h.z));   // 5
        verts.Add(center + new Vector3(h.x, h.y, h.z));    // 6
        verts.Add(center + new Vector3(-h.x, h.y, h.z));   // 7

        // Triangles winding order
        int[] boxTris = new int[]
        {
            // Front (Z-)
            0, 2, 1, 0, 3, 2,
            // Back (Z+)
            5, 6, 4, 6, 7, 4,
            // Left (X-)
            4, 3, 0, 4, 7, 3,
            // Right (X+)
            1, 6, 5, 1, 2, 6,
            // Top (Y+)
            3, 6, 2, 3, 7, 6,
            // Bottom (Y-)
            4, 1, 5, 4, 0, 1
        };

        foreach (int t in boxTris)
        {
            tris.Add(start + t);
        }
    }
}
#endif
