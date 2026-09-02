#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;
using System.Collections.Generic;

[InitializeOnLoad]
public class CreateMapPrefab
{
    static CreateMapPrefab()
    {
        EditorApplication.delayCall += EnsureMapPrefabExists;
    }

    [MenuItem("Tools/SproutScout/Create Stylized Map Prefab")]
    public static void ForceCreateMap()
    {
        CreateMap(true);
    }

    private static void EnsureMapPrefabExists()
    {
        if (EditorApplication.isPlaying || Application.isPlaying) return;
        CreateMap(false);
    }

    private static void CreateMap(bool forceOverwrite)
    {
        string prefabPath = "Assets/Prefabs/Prefap Map.prefab";
        
        if (!forceOverwrite && AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath) != null)
        {
            return;
        }

        Debug.Log("[CreateMapPrefab] Creating a beautiful, stylized 3D Dungeon Room map prefab...");

        // Ensure directories exist
        if (!AssetDatabase.IsValidFolder("Assets/Materials"))
            AssetDatabase.CreateFolder("Assets", "Materials");
        if (!AssetDatabase.IsValidFolder("Assets/Prefabs"))
            AssetDatabase.CreateFolder("Assets", "Prefabs");

        // 1. Create Materials (URP Lit)
        Shader standardShader = Shader.Find("Universal Render Pipeline/Lit");
        if (standardShader == null) standardShader = Shader.Find("Standard");

        // Cobblestone Floor Material (Dark Stone)
        string floorMatPath = "Assets/Materials/MapFloorMaterial.mat";
        Material floorMat = AssetDatabase.LoadAssetAtPath<Material>(floorMatPath);
        if (floorMat == null || forceOverwrite)
        {
            floorMat = new Material(standardShader);
            floorMat.color = new Color(0.2f, 0.22f, 0.24f); // Dark Charcoal Stone
            if (floorMat.HasProperty("_Roughness")) floorMat.SetFloat("_Roughness", 0.9f);
            AssetDatabase.CreateAsset(floorMat, floorMatPath);
        }

        // Stone Wall Material (Brick Gray)
        string wallMatPath = "Assets/Materials/MapWallMaterial.mat";
        Material wallMat = AssetDatabase.LoadAssetAtPath<Material>(wallMatPath);
        if (wallMat == null || forceOverwrite)
        {
            wallMat = new Material(standardShader);
            wallMat.color = new Color(0.35f, 0.36f, 0.38f); // Dungeon Stone Brick Gray
            if (wallMat.HasProperty("_Roughness")) wallMat.SetFloat("_Roughness", 0.85f);
            AssetDatabase.CreateAsset(wallMat, wallMatPath);
        }

        // Pillar Material (Chiseled Dark Stone)
        string pillarMatPath = "Assets/Materials/MapPillarMaterial.mat";
        Material pillarMat = AssetDatabase.LoadAssetAtPath<Material>(pillarMatPath);
        if (pillarMat == null || forceOverwrite)
        {
            pillarMat = new Material(standardShader);
            pillarMat.color = new Color(0.15f, 0.16f, 0.18f); // Very Dark Stone
            if (pillarMat.HasProperty("_Roughness")) pillarMat.SetFloat("_Roughness", 0.95f);
            AssetDatabase.CreateAsset(pillarMat, pillarMatPath);
        }

        // Wooden Props Material (Crate/Barrel Wood)
        string propMatPath = "Assets/Materials/MapPropMaterial.mat";
        Material propMat = AssetDatabase.LoadAssetAtPath<Material>(propMatPath);
        if (propMat == null || forceOverwrite)
        {
            propMat = new Material(standardShader);
            propMat.color = new Color(0.42f, 0.28f, 0.18f); // Wood Crate Brown
            if (propMat.HasProperty("_Roughness")) propMat.SetFloat("_Roughness", 0.8f);
            AssetDatabase.CreateAsset(propMat, propMatPath);
        }

        // 2. Build Map hierarchy
        GameObject mapRoot = new GameObject("Prefap Map");

        // A. Floors Group
        GameObject floorsGroup = new GameObject("Floors");
        floorsGroup.transform.SetParent(mapRoot.transform, false);
        
        // Generate a 10x10 grid of large floor stone slabs
        // Spans from X = -10 to 10, Z = -10 to 10
        float tileSize = 2.0f;
        for (int x = 0; x < 10; x++)
        {
            for (int z = 0; z < 10; z++)
            {
                float posX = -9f + (x * tileSize);
                float posZ = -9f + (z * tileSize);
                
                GameObject tile = GameObject.CreatePrimitive(PrimitiveType.Cube);
                tile.name = $"FloorTile_{x}_{z}";
                tile.transform.SetParent(floorsGroup.transform, false);
                tile.transform.localPosition = new Vector3(posX, -0.05f, posZ);
                tile.transform.localScale = new Vector3(1.95f, 0.1f, 1.95f); // Tiny gap for low-poly look
                tile.GetComponent<Renderer>().sharedMaterial = floorMat;
            }
        }

        // B. Walls Group
        GameObject wallsGroup = new GameObject("Walls");
        wallsGroup.transform.SetParent(mapRoot.transform, false);

        // Left Wall (blocks X = -10)
        CreateWallBlock(wallsGroup.transform, new Vector3(-10f, 1.5f, 0f), new Vector3(0.4f, 3f, 20f), wallMat);
        
        // Right Wall (blocks X = 10)
        CreateWallBlock(wallsGroup.transform, new Vector3(10f, 1.5f, 0f), new Vector3(0.4f, 3f, 20f), wallMat);

        // Back Wall (blocks Z = 10, leaving a 4-unit gap for doorway from X = -2 to 2)
        CreateWallBlock(wallsGroup.transform, new Vector3(-6f, 1.5f, 10f), new Vector3(8f, 3f, 0.4f), wallMat); // Left section
        CreateWallBlock(wallsGroup.transform, new Vector3(6f, 1.5f, 10f), new Vector3(8f, 3f, 0.4f), wallMat);  // Right section
        CreateWallBlock(wallsGroup.transform, new Vector3(0f, 2.7f, 10f), new Vector3(4f, 0.6f, 0.4f), wallMat); // Doorway arch lintel

        // Front Wall (blocks Z = -10, leaving a 4-unit gap for entrance doorway from X = -2 to 2)
        CreateWallBlock(wallsGroup.transform, new Vector3(-6f, 1.5f, -10f), new Vector3(8f, 3f, 0.4f), wallMat); // Left section
        CreateWallBlock(wallsGroup.transform, new Vector3(6f, 1.5f, -10f), new Vector3(8f, 3f, 0.4f), wallMat);  // Right section
        CreateWallBlock(wallsGroup.transform, new Vector3(0f, 2.7f, -10f), new Vector3(4f, 0.6f, 0.4f), wallMat); // Doorway arch lintel

        // C. Pillars Group
        GameObject pillarsGroup = new GameObject("Pillars");
        pillarsGroup.transform.SetParent(mapRoot.transform, false);

        // Corner Pillars (4 corners)
        CreatePillar(pillarsGroup.transform, new Vector3(-9.7f, 1.6f, 9.7f), pillarMat);
        CreatePillar(pillarsGroup.transform, new Vector3(9.7f, 1.6f, 9.7f), pillarMat);
        CreatePillar(pillarsGroup.transform, new Vector3(-9.7f, 1.6f, -9.7f), pillarMat);
        CreatePillar(pillarsGroup.transform, new Vector3(9.7f, 1.6f, -9.7f), pillarMat);

        // Side wall Pillars
        CreatePillar(pillarsGroup.transform, new Vector3(-9.7f, 1.6f, 0f), pillarMat);
        CreatePillar(pillarsGroup.transform, new Vector3(9.7f, 1.6f, 0f), pillarMat);
        CreatePillar(pillarsGroup.transform, new Vector3(-2.1f, 1.6f, 9.7f), pillarMat); // Lintel support back
        CreatePillar(pillarsGroup.transform, new Vector3(2.1f, 1.6f, 9.7f), pillarMat);
        CreatePillar(pillarsGroup.transform, new Vector3(-2.1f, 1.6f, -9.7f), pillarMat); // Lintel support front
        CreatePillar(pillarsGroup.transform, new Vector3(2.1f, 1.6f, -9.7f), pillarMat);

        // D. Torches Group (Add light and glow using our custom Torch.prefab)
        GameObject torchesGroup = new GameObject("Torches");
        torchesGroup.transform.SetParent(mapRoot.transform, false);

        GameObject torchPrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Torch.prefab");
        if (torchPrefab != null)
        {
            // Spawn torches on left/right walls pointing into room
            SpawnWallTorch(torchesGroup.transform, torchPrefab, new Vector3(-9.7f, 1.7f, -4f), Quaternion.Euler(0, 90, 0));
            SpawnWallTorch(torchesGroup.transform, torchPrefab, new Vector3(-9.7f, 1.7f, 4f), Quaternion.Euler(0, 90, 0));
            SpawnWallTorch(torchesGroup.transform, torchPrefab, new Vector3(9.7f, 1.7f, -4f), Quaternion.Euler(0, -90, 0));
            SpawnWallTorch(torchesGroup.transform, torchPrefab, new Vector3(9.7f, 1.7f, 4f), Quaternion.Euler(0, -90, 0));
        }

        // E. Props & Decorations (Wood crates and barrels)
        GameObject propsGroup = new GameObject("Props");
        propsGroup.transform.SetParent(mapRoot.transform, false);

        // Corner 1: Crates Stack
        CreateCrate(propsGroup.transform, new Vector3(-8f, 0.5f, 8f), propMat);
        CreateCrate(propsGroup.transform, new Vector3(-7f, 0.5f, 8f), propMat);
        CreateCrate(propsGroup.transform, new Vector3(-7.5f, 1.4f, 8f), propMat); // top stacked

        // Corner 2: Barrel and crate
        CreateBarrel(propsGroup.transform, new Vector3(8f, 0.6f, -8f), propMat);
        CreateCrate(propsGroup.transform, new Vector3(7f, 0.5f, -8f), propMat);

        // 3. Save as Prefab
        PrefabUtility.SaveAsPrefabAssetAndConnect(mapRoot, prefabPath, InteractionMode.AutomatedAction);
        Object.DestroyImmediate(mapRoot);

        AssetDatabase.Refresh();
        Debug.Log($"[CreateMapPrefab] Successfully created Stylized Map Prefab at: {prefabPath}");
    }

    private static void CreateWallBlock(Transform parent, Vector3 pos, Vector3 size, Material mat)
    {
        GameObject wall = GameObject.CreatePrimitive(PrimitiveType.Cube);
        wall.transform.SetParent(parent, false);
        wall.transform.localPosition = pos;
        wall.transform.localScale = size;
        wall.GetComponent<Renderer>().sharedMaterial = mat;
    }

    private static void CreatePillar(Transform parent, Vector3 pos, Material mat)
    {
        GameObject pillar = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        pillar.name = "Pillar";
        pillar.transform.SetParent(parent, false);
        pillar.transform.localPosition = pos;
        pillar.transform.localScale = new Vector3(0.5f, 1.6f, 0.5f); // Height is 3.2m total (clyinder scale * 2)
        pillar.GetComponent<Renderer>().sharedMaterial = mat;

        // Clean cylinder collider to box/capsule for better low-poly navigation
        Object.DestroyImmediate(pillar.GetComponent<CapsuleCollider>());
        BoxCollider bc = pillar.AddComponent<BoxCollider>();
        bc.size = new Vector3(0.6f, 2f, 0.6f);
    }

    private static void SpawnWallTorch(Transform parent, GameObject prefab, Vector3 pos, Quaternion rot)
    {
        GameObject torch = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
        torch.transform.SetParent(parent, false);
        torch.transform.localPosition = pos;
        torch.transform.localRotation = rot;
        
        // Tilt the torch slightly out from the wall
        torch.transform.Rotate(new Vector3(15f, 0f, 0f), Space.Self);
    }

    private static void CreateCrate(Transform parent, Vector3 pos, Material mat)
    {
        GameObject crate = GameObject.CreatePrimitive(PrimitiveType.Cube);
        crate.name = "WoodCrate";
        crate.transform.SetParent(parent, false);
        crate.transform.localPosition = pos;
        crate.transform.localScale = new Vector3(0.9f, 0.9f, 0.9f);
        crate.GetComponent<Renderer>().sharedMaterial = mat;
    }

    private static void CreateBarrel(Transform parent, Vector3 pos, Material mat)
    {
        GameObject barrel = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        barrel.name = "WoodBarrel";
        barrel.transform.SetParent(parent, false);
        barrel.transform.localPosition = pos;
        barrel.transform.localScale = new Vector3(0.8f, 0.6f, 0.8f);
        barrel.GetComponent<Renderer>().sharedMaterial = mat;
    }
}
#endif
