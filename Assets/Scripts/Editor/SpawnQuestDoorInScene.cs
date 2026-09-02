#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

[InitializeOnLoad]
public class SpawnQuestDoorInScene
{
    static SpawnQuestDoorInScene()
    {
        EditorApplication.delayCall += EnsureDoorSpawnedInScene;
    }

    [MenuItem("Tools/SproutScout/Spawn Door in Scene")]
    public static void ForceSpawnDoor()
    {
        SpawnDoor(true);
    }

    private static void EnsureDoorSpawnedInScene()
    {
        if (Application.isPlaying) return;
        SpawnDoor(false);
    }

    private static void SpawnDoor(bool force)
    {
        string scenePath = "Assets/Scenes/map.unity";
        string prefabPath = "Assets/Prefabs/Door.prefab";

        var activeScene = EditorSceneManager.GetActiveScene();
        if (!activeScene.IsValid()) return;

        // If force requested and not on map.unity, we can still spawn in the current active scene or map
        if (activeScene.path != scenePath && !force) return;

        GameObject doorPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);
        if (doorPrefab == null)
        {
            Debug.LogWarning("[SpawnDoor] Door prefab not found. Please create the prefab first.");
            return;
        }

        // Check if a Door already exists in the active scene
        GameObject existingDoor = GameObject.Find("Door");
        if (existingDoor != null && !force) return;

        if (existingDoor != null && force)
        {
            Object.DestroyImmediate(existingDoor);
        }

        // Spawn Door at (8.0f, 0.0f, 0.0f) rotated 90 degrees on Y to block the corridor
        GameObject doorInstance = (GameObject)PrefabUtility.InstantiatePrefab(doorPrefab);
        doorInstance.name = "Door";
        doorInstance.transform.position = new Vector3(8.0f, 0.0f, 0.0f);
        doorInstance.transform.rotation = Quaternion.Euler(0f, 90f, 0f);

        EditorSceneManager.MarkSceneDirty(activeScene);
        EditorSceneManager.SaveScene(activeScene);
        Debug.Log($"[SpawnDoor] Spawned Quest Door in scene {activeScene.name} at: (8.0f, 0.0f, 0.0f)");
    }
}
#endif
