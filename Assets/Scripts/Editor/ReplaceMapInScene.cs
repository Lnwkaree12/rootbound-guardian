#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

public class ReplaceMapInScene
{
    [MenuItem("Tools/SproutScout/Replace Map in Scene with Stylized Prefab")]
    public static void ReplaceMap()
    {
        string scenePath = "Assets/Scenes/map.unity";
        string prefabPath = "Assets/Prefabs/Prefap Map.prefab";

        var activeScene = EditorSceneManager.GetActiveScene();
        if (activeScene.path != scenePath)
        {
            Debug.LogWarning("[ReplaceMap] Please open the 'map.unity' scene first!");
            return;
        }

        GameObject mapPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);
        if (mapPrefab == null)
        {
            Debug.LogError("[ReplaceMap] New Map Prefab not found. Run 'Tools -> SproutScout -> Create Stylized Map Prefab' first.");
            return;
        }

        // Find the existing static "Prefap Map" in the scene
        GameObject oldMap = GameObject.Find("Prefap Map");
        Vector3 position = new Vector3(245.54639f, 0.025f, 100.34908f); // Default coordinate from scene
        Quaternion rotation = Quaternion.identity;

        if (oldMap != null)
        {
            position = oldMap.transform.position;
            rotation = oldMap.transform.rotation;
            
            // Register Undo for scene modification
            Undo.DestroyObjectImmediate(oldMap);
            Debug.Log("[ReplaceMap] Deleted old static 'Prefap Map' GameObject from scene.");
        }

        // Instantiate new prefab at the same position
        GameObject newMapInstance = (GameObject)PrefabUtility.InstantiatePrefab(mapPrefab);
        newMapInstance.name = "Prefap Map";
        newMapInstance.transform.position = position;
        newMapInstance.transform.rotation = rotation;

        Undo.RegisterCreatedObjectUndo(newMapInstance, "Replace Map");
        EditorSceneManager.MarkSceneDirty(activeScene);
        EditorSceneManager.SaveScene(activeScene);

        Debug.Log($"[ReplaceMap] Successfully instantiated new 'Prefap Map' prefab at {position} in scene!");
    }
}
#endif
