#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

[InitializeOnLoad]
public class SpawnQuestKeyInScene
{
    static SpawnQuestKeyInScene()
    {
        EditorApplication.delayCall += EnsureKeySpawnedInScene;
    }

    [MenuItem("Tools/SproutScout/Spawn Key in Scene")]
    public static void ForceSpawnKey()
    {
        SpawnKey(true);
    }

    private static void EnsureKeySpawnedInScene()
    {
        if (Application.isPlaying) return;
        SpawnKey(false);
    }

    private static void SpawnKey(bool force)
    {
        string scenePath = "Assets/Scenes/map.unity";
        string prefabPath = "Assets/Prefabs/Key.prefab";

        var activeScene = EditorSceneManager.GetActiveScene();
        if (!activeScene.IsValid()) return;

        if (activeScene.path != scenePath && !force) return;

        GameObject keyPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);
        if (keyPrefab == null)
        {
            Debug.LogWarning("[SpawnKey] Key prefab not found. Please create the prefab first.");
            return;
        }

        // Check if a Key already exists in the active scene
        GameObject existingKey = GameObject.Find("Key");
        if (existingKey != null && !force) return;

        if (existingKey != null && force)
        {
            Object.DestroyImmediate(existingKey);
        }

        // Spawn key near Cube 1 coordinates (18.14f, 1.2f, 10.74f)
        GameObject keyInstance = (GameObject)PrefabUtility.InstantiatePrefab(keyPrefab);
        keyInstance.name = "Key";
        keyInstance.transform.position = new Vector3(18.14f, 1.2f, 10.74f);

        EditorSceneManager.MarkSceneDirty(activeScene);
        EditorSceneManager.SaveScene(activeScene);
        Debug.Log($"[SpawnKey] Spawned Quest Key in scene {activeScene.name} at: (18.14f, 1.2f, 10.74f)");
    }
}
#endif
