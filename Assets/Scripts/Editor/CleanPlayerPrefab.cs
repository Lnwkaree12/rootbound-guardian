#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

[InitializeOnLoad]
public class CleanPlayerPrefab
{
    static CleanPlayerPrefab()
    {
        EditorApplication.delayCall += CleanPlayer;
    }

    public static void CleanPlayer()
    {
        if (Application.isPlaying) return;

        string playerPrefabPath = "Assets/Prefabs/Player.prefab";
        GameObject playerPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(playerPrefabPath);
        if (playerPrefab == null)
        {
            return;
        }

        // Load prefab contents
        GameObject prefabRoot = PrefabUtility.LoadPrefabContents(playerPrefabPath);
        bool modified = false;

        // Remove components
        var stats = prefabRoot.GetComponent<PlayerStats>();
        if (stats != null)
        {
            Object.DestroyImmediate(stats, true);
            modified = true;
            Debug.Log("[CleanPlayer] Removed duplicate PlayerStats component from Player prefab");
        }

        var inv = prefabRoot.GetComponent<InventoryManager>();
        if (inv != null)
        {
            Object.DestroyImmediate(inv, true);
            modified = true;
            Debug.Log("[CleanPlayer] Removed duplicate InventoryManager component from Player prefab");
        }

        var qm = prefabRoot.GetComponent<QuestManager>();
        if (qm != null)
        {
            Object.DestroyImmediate(qm, true);
            modified = true;
            Debug.Log("[CleanPlayer] Removed duplicate QuestManager component from Player prefab");
        }

        if (modified)
        {
            PrefabUtility.SaveAsPrefabAsset(prefabRoot, playerPrefabPath);
            Debug.Log("[CleanPlayer] Successfully cleaned and saved Player prefab.");
        }
        
        PrefabUtility.UnloadPrefabContents(prefabRoot);
    }
}
#endif
