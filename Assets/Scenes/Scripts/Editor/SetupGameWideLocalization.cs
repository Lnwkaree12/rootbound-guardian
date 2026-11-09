#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public static class SetupGameWideLocalization
{
    [MenuItem("Tools/Localization/Apply Game-Wide Localization Setup")]
    public static void RunSetup()
    {
        Debug.Log("==================================================");
        Debug.Log("[SetupGameWideLocalization] Starting Full Game Localization Setup...");
        Debug.Log("==================================================");

        SetupPrefabs();
        SetupMainGameplayScene();
        SetupCutsceneScene();
        SetupLoadingScreenScene();
        SetupCutsceneEndScene();
        SetupEndCreditScene();

        // Run validation suite across all 3 languages
        ValidateAllTranslations();

        AssetDatabase.SaveAssets();
        Debug.Log("==================================================");
        Debug.Log("[SetupGameWideLocalization] Completed Successfully!");
        Debug.Log("==================================================");
    }

    private static void SetupPrefabs()
    {
        string[] prefabPaths = new string[]
        {
            "Assets/Prefabs/HUD_Canvas.prefab",
            "Assets/Prefabs 1/HUD_Canvas.prefab",
            "Assets/Image/UI/HUD_Canvas_Quest Variant.prefab"
        };

        foreach (string path in prefabPaths)
        {
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            if (prefab == null) continue;

            bool modified = false;

            // 1. Backpack Title
            Transform backpackTitle = prefab.transform.Find("HUD Container/BackpackPanel/TitleText");
            if (backpackTitle == null) backpackTitle = prefab.transform.Find("BackpackPanel/TitleText");
            if (backpackTitle != null)
            {
                AttachLocalizedText(backpackTitle.gameObject, "hud_backpack_name");
                modified = true;
            }

            // 2. Backpack Instructions
            Transform backpackInstructions = prefab.transform.Find("HUD Container/BackpackPanel/InstructionsText");
            if (backpackInstructions == null) backpackInstructions = prefab.transform.Find("BackpackPanel/InstructionsText");
            if (backpackInstructions != null)
            {
                AttachLocalizedText(backpackInstructions.gameObject, "hud_backpack_hint");
                modified = true;
            }

            // 3. Potion Quickslot Hint
            Transform potionHint = prefab.transform.Find("HUD Container/HUD_PotionQuickSlot/HintText");
            if (potionHint == null) potionHint = prefab.transform.Find("HUD_PotionQuickSlot/HintText");
            if (potionHint != null)
            {
                AttachLocalizedText(potionHint.gameObject, "hud_potion_hint");
                modified = true;
            }

            // 4. Survival Tips
            Transform tipText = prefab.transform.Find("HUD Container/TipBox/TipText");
            if (tipText == null) tipText = prefab.transform.Find("TipBox/TipText");
            if (tipText != null)
            {
                AttachLocalizedText(tipText.gameObject, "gameover_survival_tips");
                modified = true;
            }

            // 5. Quest Category
            Transform questCat = prefab.transform.Find("HUD Container/QuestPanel/CategoryText");
            if (questCat == null) questCat = prefab.transform.Find("QuestPanel/CategoryText");
            if (questCat != null)
            {
                AttachLocalizedText(questCat.gameObject, "quest_category");
                modified = true;
            }

            // 6. Quest Title
            Transform questTitle = prefab.transform.Find("HUD Container/QuestPanel/TitleText");
            if (questTitle == null) questTitle = prefab.transform.Find("QuestPanel/TitleText");
            if (questTitle != null)
            {
                AttachLocalizedText(questTitle.gameObject, "quest_title");
                modified = true;
            }

            if (modified)
            {
                EditorUtility.SetDirty(prefab);
                PrefabUtility.SavePrefabAsset(prefab);
                Debug.Log($"[SetupGameWideLocalization] Updated prefab: {path}");
            }
        }
    }

    private static void SetupMainGameplayScene()
    {
        string scenePath = "Assets/Scenes/MainGameplay.unity";
        var scene = EditorSceneManager.OpenScene(scenePath, OpenSceneMode.Single);

        // Door Prompt Text
        GameObject doorCanvas = GameObject.Find("DoorPromptCanvas");
        if (doorCanvas != null)
        {
            Transform t = doorCanvas.transform.Find("TextTMP");
            if (t != null)
            {
                AttachLocalizedText(t.gameObject, "door_prompt_open");
            }
        }

        // Root Prompt Text
        GameObject rootCanvas = GameObject.Find("Root_Prompt_Canvas");
        if (rootCanvas != null)
        {
            Transform t = rootCanvas.transform.Find("PromptText");
            if (t != null)
            {
                AttachLocalizedText(t.gameObject, "root_prompt_water");
            }
        }

        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
        Debug.Log($"[SetupGameWideLocalization] Saved {scenePath}");
    }

    private static void SetupCutsceneScene()
    {
        string scenePath = "Assets/Scenes/Cutscene.unity";
        var scene = EditorSceneManager.OpenScene(scenePath, OpenSceneMode.Single);

        GameObject skipBtn = GameObject.Find("Btn_Skip");
        if (skipBtn == null) skipBtn = GameObject.Find("SkipButton");
        if (skipBtn != null)
        {
            Transform t = skipBtn.transform.Find("Text");
            if (t != null) AttachLocalizedText(t.gameObject, "hint_skip");
            else AttachLocalizedText(skipBtn, "hint_skip");
        }

        GameObject continuePrompt = GameObject.Find("ContinuePrompt");
        if (continuePrompt != null)
        {
            Transform t = continuePrompt.transform.Find("Text");
            if (t != null) AttachLocalizedText(t.gameObject, "hint_continue");
            else AttachLocalizedText(continuePrompt, "hint_continue");
        }

        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
        Debug.Log($"[SetupGameWideLocalization] Saved {scenePath}");
    }

    private static void SetupLoadingScreenScene()
    {
        string scenePath = "Assets/Scenes/LoadingScreenNew.unity";
        var scene = EditorSceneManager.OpenScene(scenePath, OpenSceneMode.Single);

        GameObject readyPrompt = GameObject.Find("Ready_Prompt");
        if (readyPrompt != null)
        {
            Transform t = readyPrompt.transform.Find("Ready_Prompt_Text");
            if (t != null) AttachLocalizedText(t.gameObject, "load_ready_prompt");
            else AttachLocalizedText(readyPrompt, "load_ready_prompt");
        }

        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
        Debug.Log($"[SetupGameWideLocalization] Saved {scenePath}");
    }

    private static void SetupCutsceneEndScene()
    {
        string scenePath = "Assets/Scenes/cutscene End.unity";
        var scene = EditorSceneManager.OpenScene(scenePath, OpenSceneMode.Single);

        GameObject skipBtn = GameObject.Find("Btn_Skip");
        if (skipBtn == null) skipBtn = GameObject.Find("SkipButton");
        if (skipBtn != null)
        {
            Transform t = skipBtn.transform.Find("Text");
            if (t != null) AttachLocalizedText(t.gameObject, "hint_skip");
            else AttachLocalizedText(skipBtn, "hint_skip");
        }

        GameObject continuePrompt = GameObject.Find("ContinuePrompt");
        if (continuePrompt != null)
        {
            Transform t = continuePrompt.transform.Find("Text");
            if (t != null) AttachLocalizedText(t.gameObject, "hint_continue");
            else AttachLocalizedText(continuePrompt, "hint_continue");
        }

        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
        Debug.Log($"[SetupGameWideLocalization] Saved {scenePath}");
    }

    private static void SetupEndCreditScene()
    {
        string scenePath = "Assets/Scenes/EndCredit.unity";
        var scene = EditorSceneManager.OpenScene(scenePath, OpenSceneMode.Single);

        AttachByName("AdvisorRole", "credits_advisor_role");
        AttachByName("TeamHeader", "credits_team_role");
        AttachByName("ThanksHeader", "credits_thanks_role");
        AttachByName("ThaiThanksMsg", "credits_finale_title");
        AttachByName("EnglishSubMsg", "credits_finale_sub");

        GameObject ff = GameObject.Find("FastForwardPrompt");
        if (ff != null)
        {
            Transform t = ff.transform.Find("Text");
            if (t != null) AttachLocalizedText(t.gameObject, "credits_hint_ff");
        }

        GameObject skip = GameObject.Find("Btn_Skip");
        if (skip != null)
        {
            Transform t = skip.transform.Find("Text");
            if (t != null) AttachLocalizedText(t.gameObject, "credits_hint_skip");
        }

        GameObject menu = GameObject.Find("Btn_MainMenu");
        if (menu != null)
        {
            Transform t = menu.transform.Find("Text");
            if (t != null) AttachLocalizedText(t.gameObject, "credits_btn_menu");
        }

        GameObject replay = GameObject.Find("Btn_Replay");
        if (replay != null)
        {
            Transform t = replay.transform.Find("Text");
            if (t != null) AttachLocalizedText(t.gameObject, "credits_btn_replay");
        }

        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
        Debug.Log($"[SetupGameWideLocalization] Saved {scenePath}");
    }

    private static void AttachByName(string goName, string locKey)
    {
        GameObject go = GameObject.Find(goName);
        if (go != null)
        {
            AttachLocalizedText(go, locKey);
        }
    }

    private static void AttachLocalizedText(GameObject go, string key)
    {
        if (go == null) return;
        LocalizedText loc = go.GetComponent<LocalizedText>();
        if (loc == null)
        {
            loc = go.AddComponent<LocalizedText>();
        }
        loc.localizationKey = key;
        loc.staticOverride = false;
        loc.Refresh();
    }

    private static void ValidateAllTranslations()
    {
        Debug.Log("--- Validating Translation Keys across TH, EN, ZH ---");
        LocalizationManager mgr = LocalizationManager.Instance;
        if (mgr == null)
        {
            GameObject go = new GameObject("[LocalizationManager_Temp]");
            mgr = go.AddComponent<LocalizationManager>();
        }
        mgr.EnsureInitialized();

        string[] testKeys = new string[]
        {
            "quest_category", "quest_title", "quest_find_key", "quest_collect_key", "quest_door_unlocked",
            "door_prompt_open", "door_prompt_locked", "door_prompt_need_key",
            "root_prompt_water", "root_prompt_restoring",
            "lumi_trap_warning", "lumi_banter_0", "lumi_banter_9",
            "pause_title", "pause_resume", "pause_restart", "pause_main_menu",
            "gameover_title", "gameover_oxygen_depleted", "gameover_hp_depleted",
            "win_badge", "win_congrats", "win_btn_replay", "win_btn_menu",
            "load_guide_spirit_name", "load_header_controls", "load_row_move", "load_row_climb",
            "cutscene_opening_1", "cutscene_opening_5", "endscene_lumi_speech", "cutscene_end_narrative",
            "credits_advisor_role", "credits_team_role", "credits_finale_title"
        };

        foreach (LocalizationManager.Language lang in (LocalizationManager.Language[])System.Enum.GetValues(typeof(LocalizationManager.Language)))
        {
            mgr.SetLanguage(lang);
            int validCount = 0;
            foreach (string key in testKeys)
            {
                string text = mgr.Get(key);
                if (!string.IsNullOrEmpty(text) && text != key)
                {
                    validCount++;
                }
                else
                {
                    Debug.LogWarning($"[Validation Warning] Key '{key}' returned empty or untranslated for {lang}!");
                }
            }
            Debug.Log($"[Validation Success] Language {lang}: {validCount}/{testKeys.Length} keys verified!");
        }

        // Return to TH default
        mgr.SetLanguage(LocalizationManager.Language.TH);
    }

    [MenuItem("Tools/Build/Build Standalone Windows")]
    public static void BuildStandaloneGame()
    {
        string[] scenes = new string[]
        {
            "Assets/Scenes/MainScreen.unity",
            "Assets/Scenes/Cutscene.unity",
            "Assets/Scenes/LoadingScreenNew.unity",
            "Assets/Scenes/MainGameplay.unity",
            "Assets/Scenes/EndScene.unity",
            "Assets/Scenes/cutscene End.unity",
            "Assets/Scenes/EndCredit.unity"
        };

        string buildPath = "Build_Test/RootboundGuardian.exe";
        BuildPlayerOptions buildPlayerOptions = new BuildPlayerOptions();
        buildPlayerOptions.scenes = scenes;
        buildPlayerOptions.locationPathName = buildPath;
        buildPlayerOptions.target = BuildTarget.StandaloneWindows64;
        buildPlayerOptions.options = BuildOptions.None;

        var report = BuildPipeline.BuildPlayer(buildPlayerOptions);
        Debug.Log($"[Build Result] {report.summary.result}, Total Errors: {report.summary.totalErrors}");
        if (report.summary.result != UnityEditor.Build.Reporting.BuildResult.Succeeded)
        {
            throw new System.Exception($"Build failed with result: {report.summary.result}");
        }
    }
}
#endif
