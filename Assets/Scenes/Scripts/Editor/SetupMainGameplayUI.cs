#if UNITY_EDITOR
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// Editor utility to integrate Win_Canvas, GameOver_Canvas, and PauseMenu_Canvas
/// into MainGameplay.unity, and heal/fix any missing fonts across all UI elements.
/// </summary>
public static class SetupMainGameplayUI
{
    private const string SCENE_PATH = "Assets/Scenes/MainGameplay.unity";

    private const string WIN_PREFAB_PATH = "Assets/Prefabs/Win_Canvas.prefab";
    private const string GAMEOVER_PREFAB_PATH = "Assets/Prefabs/GameOver_Canvas.prefab";
    private const string PAUSE_PREFAB_PATH = "Assets/Prefabs/PauseMenu_Canvas.prefab";

    // Known good font assets
    private const string FONT_THAI_SDF = "Assets/Fonts/Itim_Thai_SDF.asset";
    private const string FONT_ONELITTLE_SDF = "Assets/Image/OneLittleFont-Full SDF.asset";
    private const string FONT_LEGACY_TTF = "Assets/Itim-Regular.ttf";

    [MenuItem("Tools/Rootbound Guardian/Integrate Gameplay UI into MainGameplay")]
    public static void IntegrateAllUI()
    {
        Debug.Log("[SetupMainGameplayUI] Opening scene: " + SCENE_PATH);
        var scene = EditorSceneManager.OpenScene(SCENE_PATH, OpenSceneMode.Single);

        // 1. Load Valid Fonts
        TMP_FontAsset thaiFont = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(FONT_THAI_SDF);
        TMP_FontAsset titleFont = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(FONT_ONELITTLE_SDF);
        Font legacyFont = AssetDatabase.LoadAssetAtPath<Font>(FONT_LEGACY_TTF);

        if (thaiFont == null)
        {
            Debug.LogWarning("[SetupMainGameplayUI] Could not load " + FONT_THAI_SDF + ", trying fallback...");
            thaiFont = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>("Assets/Fonts/Itim_TMP_Dynamic.asset");
        }

        // 2. Instantiate or find UI Prefabs in Scene
        GameObject winCanvasGO = EnsurePrefabInScene(WIN_PREFAB_PATH, "Win_Canvas");
        GameObject gameOverCanvasGO = EnsurePrefabInScene(GAMEOVER_PREFAB_PATH, "GameOver_Canvas");
        GameObject pauseCanvasGO = EnsurePrefabInScene(PAUSE_PREFAB_PATH, "PauseMenu_Canvas");

        // 3. Configure Sorting Orders to avoid overlap
        // Gameplay HUD: 10-20, Pause Menu: 100, Win/GameOver: 120
        if (winCanvasGO != null)
        {
            Canvas c = winCanvasGO.GetComponent<Canvas>();
            if (c != null) c.sortingOrder = 120;
        }

        if (gameOverCanvasGO != null)
        {
            Canvas c = gameOverCanvasGO.GetComponent<Canvas>();
            if (c != null) c.sortingOrder = 120;
        }

        if (pauseCanvasGO != null)
        {
            Canvas c = pauseCanvasGO.GetComponent<Canvas>();
            if (c != null) c.sortingOrder = 100;
        }

        // 4. Heal and Fix Missing Fonts in All UI Canvases in Scene
        int healedTMPCount = 0;
        int healedLegacyCount = 0;

        Canvas[] allCanvases = Object.FindObjectsByType<Canvas>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        foreach (Canvas c in allCanvases)
        {
            // Scan TextMeshProUGUI
            TextMeshProUGUI[] tmps = c.GetComponentsInChildren<TextMeshProUGUI>(true);
            foreach (var tmp in tmps)
            {
                if (FixTMPFont(tmp, thaiFont, titleFont))
                {
                    healedTMPCount++;
                }
            }

            // Scan Legacy UI.Text
            Text[] texts = c.GetComponentsInChildren<Text>(true);
            foreach (var t in texts)
            {
                if (FixLegacyTextFont(t, legacyFont))
                {
                    healedLegacyCount++;
                }
            }
        }

        Debug.Log($"[SetupMainGameplayUI] Font healing complete: {healedTMPCount} TMP texts and {healedLegacyCount} Legacy texts verified/repaired.");

        // 5. Connect PlayerHealth and PlayerOxygen to GameOverUIManager
        GameObject player = GameObject.Find("Player Variant");
        if (player == null) player = GameObject.FindGameObjectWithTag("Player");

        if (player != null)
        {
            var playerOxygen = player.GetComponent<PlayerOxygen>();
            if (playerOxygen != null)
            {
                Debug.Log("[SetupMainGameplayUI] Verified PlayerOxygen on Player.");
            }

            var playerHealth = player.GetComponent<PlayerHealth>();
            if (playerHealth != null)
            {
                Debug.Log("[SetupMainGameplayUI] Verified PlayerHealth on Player.");
            }
        }

        // 6. Connect DoorController to WinUIManager
        DoorController[] doors = Object.FindObjectsByType<DoorController>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        Debug.Log($"[SetupMainGameplayUI] Found {doors.Length} DoorController(s) in scene.");

        // 7. Ensure EventSystem exists
        var eventSystem = Object.FindFirstObjectByType<UnityEngine.EventSystems.EventSystem>();
        if (eventSystem == null)
        {
            GameObject esGO = new GameObject("EventSystem");
            esGO.AddComponent<UnityEngine.EventSystems.EventSystem>();
            esGO.AddComponent<UnityEngine.EventSystems.StandaloneInputModule>();
            Debug.Log("[SetupMainGameplayUI] Created missing EventSystem in scene.");
        }

        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
        Debug.Log("[SetupMainGameplayUI] Successfully integrated all UI into MainGameplay.unity!");
    }

    private static GameObject EnsurePrefabInScene(string prefabPath, string instanceName)
    {
        // Check if an instance already exists in scene
        GameObject existing = GameObject.Find(instanceName);
        if (existing != null)
        {
            Debug.Log($"[SetupMainGameplayUI] Found existing '{instanceName}' in scene.");
            return existing;
        }

        GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);
        if (prefab == null)
        {
            Debug.LogError($"[SetupMainGameplayUI] Prefab not found at {prefabPath}");
            return null;
        }

        GameObject instance = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
        instance.name = instanceName;
        Debug.Log($"[SetupMainGameplayUI] Instantiated '{instanceName}' from {prefabPath}");
        return instance;
    }

    private static bool FixTMPFont(TextMeshProUGUI tmp, TMP_FontAsset thaiFont, TMP_FontAsset titleFont)
    {
        bool changed = false;

        // If font asset is completely null or missing
        if (tmp.font == null)
        {
            tmp.font = thaiFont;
            changed = true;
        }
        else
        {
            // If the font asset is using Itim_SDF_Valid which has null atlas texture
            if (tmp.font.name == "Itim_SDF_Valid" && thaiFont != null)
            {
                tmp.font = thaiFont;
                changed = true;
            }
        }

        // Check material
        if (tmp.fontSharedMaterial == null && tmp.font != null)
        {
            tmp.fontSharedMaterial = tmp.font.material;
            changed = true;
        }

        return changed;
    }

    private static bool FixLegacyTextFont(Text text, Font legacyFont)
    {
        bool changed = false;
        if (text.font == null && legacyFont != null)
        {
            text.font = legacyFont;
            changed = true;
        }
        return changed;
    }
}
#endif
