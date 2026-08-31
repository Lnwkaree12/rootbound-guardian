#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

[InitializeOnLoad]
public class CreateQuestUI
{
    static CreateQuestUI()
    {
        EditorApplication.delayCall += EnsureQuestUIReady;
    }

    [MenuItem("Tools/SproutScout/Create Quest UI")]
    public static void ForceCreateQuestUI()
    {
        BuildQuestUI(true);
    }

    private static void EnsureQuestUIReady()
    {
        if (EditorApplication.isPlaying || Application.isPlaying) return;
        BuildQuestUI(false);
    }

    private static void BuildQuestUI(bool forceOverwrite)
    {
        string prefabPath = "Assets/Prefabs/HUD_Canvas.prefab";
        GameObject canvasPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);

        if (canvasPrefab == null)
        {
            Debug.LogWarning("[CreateQuestUI] HUD_Canvas prefab not found. Please create the Inventory HUD first!");
            return;
        }

        // Check if QuestPanel already exists in the prefab to avoid double building
        Transform existingPanel = canvasPrefab.transform.Find("QuestPanel");
        if (existingPanel != null && !forceOverwrite)
        {
            return;
        }

        Debug.Log("[CreateQuestUI] Loading prefab contents to add Quest Panel...");

        // 1. Load prefab contents cleanly using LoadPrefabContents
        GameObject prefabRoot = PrefabUtility.LoadPrefabContents(prefabPath);
        
        // Remove existing panel if force overwriting
        Transform oldPanel = prefabRoot.transform.Find("QuestPanel");
        if (oldPanel != null)
        {
            Object.DestroyImmediate(oldPanel.gameObject);
        }

        // Load custom font Itim-Regular.ttf with legacy fallbacks
        Font customFont = AssetDatabase.LoadAssetAtPath<Font>("Assets/Itim-Regular.ttf");
        if (customFont == null)
        {
            try { customFont = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf"); }
            catch { try { customFont = Resources.GetBuiltinResource<Font>("Arial.ttf"); } catch { } }
        }

        // 2. Create Quest Panel GameObject (แสดงผลภารกิจด้านซ้ายมือตรงกลาง)
        GameObject questGO = new GameObject("QuestPanel");
        questGO.transform.SetParent(prefabRoot.transform, false);
        Image questBg = questGO.AddComponent<Image>();
        questBg.color = new Color(0.12f, 0.08f, 0.06f, 0.85f); // Dark semi-transparent wood brown background
        RectTransform questRect = questGO.GetComponent<RectTransform>();
        questRect.anchorMin = new Vector2(0f, 0.5f); // Left-middle anchor
        questRect.anchorMax = new Vector2(0f, 0.5f);
        questRect.pivot = new Vector2(0f, 0.5f);
        questRect.anchoredPosition = new Vector2(25f, 0f);
        questRect.sizeDelta = new Vector2(280f, 80f);

        // Quest Panel Title
        GameObject questTitleGO = new GameObject("TitleText");
        questTitleGO.transform.SetParent(questGO.transform, false);
        Text questTitleText = questTitleGO.AddComponent<Text>();
        questTitleText.font = customFont;
        questTitleText.fontSize = 13;
        questTitleText.color = new Color(0.85f, 0.65f, 0.25f); // Gold
        questTitleText.text = "QUEST (ภารกิจหลัก)";
        questTitleText.alignment = TextAnchor.MiddleLeft;
        RectTransform questTitleRect = questTitleGO.GetComponent<RectTransform>();
        questTitleRect.anchorMin = new Vector2(0f, 1f);
        questTitleRect.anchorMax = new Vector2(1f, 1f);
        questTitleRect.pivot = new Vector2(0.5f, 1f);
        questTitleRect.anchoredPosition = new Vector2(15f, -15f);
        questTitleRect.sizeDelta = new Vector2(250f, 20f);

        // Quest Panel Description
        GameObject questDescGO = new GameObject("DescText");
        questDescGO.transform.SetParent(questGO.transform, false);
        Text questDescText = questDescGO.AddComponent<Text>();
        questDescText.font = customFont;
        questDescText.fontSize = 16;
        questDescText.color = Color.white;
        questDescText.text = "🔑 เก็บกุญแจสำคัญ (0/1)";
        questDescText.alignment = TextAnchor.MiddleLeft;
        RectTransform questDescRect = questDescGO.GetComponent<RectTransform>();
        questDescRect.anchorMin = new Vector2(0f, 1f);
        questDescRect.anchorMax = new Vector2(1f, 1f);
        questDescRect.pivot = new Vector2(0.5f, 1f);
        questDescRect.anchoredPosition = new Vector2(15f, -45f);
        questDescRect.sizeDelta = new Vector2(250f, 25f);

        // 3. Attach and configure QuestManager component on HUD Canvas root
        QuestManager questComp = prefabRoot.GetComponent<QuestManager>();
        if (questComp == null)
        {
            questComp = prefabRoot.AddComponent<QuestManager>();
        }
        questComp.questPanel = questGO;
        questComp.questTitleText = questTitleText;
        questComp.questDescText = questDescText;

        // 4. Save and Unload prefab contents
        PrefabUtility.SaveAsPrefabAsset(prefabRoot, prefabPath);
        PrefabUtility.UnloadPrefabContents(prefabRoot);

        AssetDatabase.Refresh();

        // Synchronize HUD Canvas prefab instances in the active scene to show the Quest Panel immediately
        Canvas[] sceneCanvases = Object.FindObjectsOfType<Canvas>(true);
        foreach (Canvas c in sceneCanvases)
        {
            if (c.gameObject.name.StartsWith("HUD Canvas") || c.gameObject.name.StartsWith("HUD_Canvas"))
            {
                if (PrefabUtility.IsPartOfPrefabInstance(c.gameObject))
                {
                    PrefabUtility.RevertPrefabInstance(c.gameObject, InteractionMode.AutomatedAction);
                    Debug.Log("[CreateQuestUI] Synchronized and updated prefab instance in active scene: " + c.gameObject.name);
                }
            }
        }

        Debug.Log($"[CreateQuestUI] Successfully integrated Quest UI Panel into HUD Canvas prefab at: {prefabPath}");
    }
}
#endif
