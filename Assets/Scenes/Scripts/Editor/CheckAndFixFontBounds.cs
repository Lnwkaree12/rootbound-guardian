#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;
using System.IO;

public static class CheckAndFixFontBounds
{
    private struct AuditResult
    {
        public string sceneOrPrefab;
        public string objectPath;
        public string textContent;
        public Vector2 rectSize;
        public Vector2 preferredSizeTH;
        public Vector2 preferredSizeEN;
        public Vector2 preferredSizeZH;
        public bool didExceed;
        public string actionTaken;
    }

    [MenuItem("Tools/Localization/Audit and Fix Font Bounds")]
    public static void RunAuditAndFix()
    {
        Debug.Log("======================================================================");
        Debug.Log("[CheckAndFixFontBounds] Starting Deep Audit and Fix for Font Boundaries...");
        Debug.Log("======================================================================");

        List<AuditResult> auditLog = new List<AuditResult>();

        // 1. Audit Prefabs
        AuditPrefab("Assets/Image/UI/HUD_Canvas_Quest Variant.prefab", auditLog);
        AuditPrefab("Assets/Prefabs/HUD_Canvas.prefab", auditLog);
        AuditPrefab("Assets/Prefabs 1/HUD_Canvas.prefab", auditLog);

        // 2. Audit Scenes
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

        foreach (string scenePath in scenes)
        {
            if (File.Exists(scenePath))
            {
                AuditScene(scenePath, auditLog);
            }
        }

        // Print Summary
        int fixedCount = 0;
        int exceedCount = 0;
        foreach (var res in auditLog)
        {
            if (res.didExceed) exceedCount++;
            if (!string.IsNullOrEmpty(res.actionTaken)) fixedCount++;
        }

        Debug.Log("======================================================================");
        Debug.Log($"[CheckAndFixFontBounds] Audit Finished: Scanned {auditLog.Count} text components.");
        Debug.Log($"[CheckAndFixFontBounds] Found {exceedCount} components exceeding bounds. Applied fixes to {fixedCount} components.");
        Debug.Log("======================================================================");

        AssetDatabase.SaveAssets();
    }

    private static void AuditPrefab(string prefabPath, List<AuditResult> log)
    {
        GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);
        if (prefab == null) return;

        Debug.Log($"--- Auditing Prefab: {prefabPath} ---");
        GameObject instance = PrefabUtility.InstantiatePrefab(prefab) as GameObject;
        bool modified = false;

        AuditHierarchy(instance.transform, prefabPath, ref modified, log);

        if (modified)
        {
            PrefabUtility.SaveAsPrefabAsset(instance, prefabPath);
            Debug.Log($"[CheckAndFixFontBounds] Saved fixes to prefab: {prefabPath}");
        }

        Object.DestroyImmediate(instance);
    }

    private static void AuditScene(string scenePath, List<AuditResult> log)
    {
        Debug.Log($"--- Auditing Scene: {scenePath} ---");
        var scene = EditorSceneManager.OpenScene(scenePath, OpenSceneMode.Single);
        bool modified = false;

        GameObject[] rootObjects = scene.GetRootGameObjects();
        foreach (GameObject root in rootObjects)
        {
            AuditHierarchy(root.transform, scenePath, ref modified, log);
        }

        // Scene specific checks (e.g. Cutscene dialogue box, Loading screen rows, Credits)
        ApplySceneSpecificFixes(scene.name, ref modified);

        if (modified)
        {
            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            Debug.Log($"[CheckAndFixFontBounds] Saved fixes to scene: {scenePath}");
        }
    }

    private static void AuditHierarchy(Transform root, string location, ref bool modified, List<AuditResult> log)
    {
        // 1. TextMeshProUGUI
        TextMeshProUGUI[] tmps = root.GetComponentsInChildren<TextMeshProUGUI>(true);
        foreach (var tmp in tmps)
        {
            AuditTMP(tmp, location, ref modified, log);
        }

        // 2. Standard UI Text
        Text[] texts = root.GetComponentsInChildren<Text>(true);
        foreach (var txt in texts)
        {
            AuditUIText(txt, location, ref modified, log);
        }
    }

    private static void AuditTMP(TextMeshProUGUI tmp, string location, ref bool modified, List<AuditResult> log)
    {
        RectTransform rt = tmp.rectTransform;
        Vector2 size = rt.rect.size;
        if (size.x <= 1f || size.y <= 1f) return;

        string currentText = tmp.text;
        string path = GetHierarchyPath(tmp.transform);

        // Clean any missing glyphs or emojis from text
        if (!string.IsNullOrEmpty(tmp.text))
        {
            string cleaned = CleanEmojiAndMissingGlyphs(tmp.text);
            if (cleaned != tmp.text)
            {
                tmp.text = cleaned;
                currentText = cleaned;
                modified = true;
            }
        }

        // Get sample localized strings if LocalizedText component is attached
        string locKey = "";
        LocalizedText loc = tmp.GetComponent<LocalizedText>();
        if (loc != null) locKey = loc.localizationKey;

        string sampleTH = currentText;
        string sampleEN = currentText;
        string sampleZH = currentText;

        if (!string.IsNullOrEmpty(locKey) && LocalizationManager.Instance != null)
        {
            sampleTH = LocalizationManager.Instance.GetFromLanguage(locKey, "TH");
            sampleEN = LocalizationManager.Instance.GetFromLanguage(locKey, "EN");
            sampleZH = LocalizationManager.Instance.GetFromLanguage(locKey, "ZH");
        }

        Vector2 prefTH = tmp.GetPreferredValues(sampleTH);
        Vector2 prefEN = tmp.GetPreferredValues(sampleEN);
        Vector2 prefZH = tmp.GetPreferredValues(sampleZH);

        float maxPrefW = Mathf.Max(prefTH.x, Mathf.Max(prefEN.x, prefZH.x));
        float maxPrefH = Mathf.Max(prefTH.y, Mathf.Max(prefEN.y, prefZH.y));

        bool exceedsWidth = maxPrefW > size.x + 2f && tmp.textWrappingMode == TextWrappingModes.NoWrap;
        bool exceedsHeight = maxPrefH > size.y + 2f;
        bool needsFix = exceedsWidth || exceedsHeight;

        string action = "";

        // Standard safety enforcement on all UI TextMeshPro components:
        // 1. Ensure extraPadding is true so Thai tone marks / upper vowels never clip
        if (!tmp.extraPadding)
        {
            tmp.extraPadding = true;
            modified = true;
            action += "[Enabled extraPadding] ";
        }

        // 2. If it exceeds or has risk of varying language lengths, enable AutoSizing
        if (needsFix || (!tmp.enableAutoSizing && (tmp.text.Length > 8 || !string.IsNullOrEmpty(locKey))))
        {
            if (!tmp.enableAutoSizing)
            {
                tmp.enableAutoSizing = true;
                float currentSize = tmp.fontSize;
                tmp.fontSizeMax = Mathf.Max(currentSize, 16f);
                tmp.fontSizeMin = Mathf.Max(10f, currentSize * 0.6f);
                modified = true;
                action += $"[Enabled AutoSize {tmp.fontSizeMin}-{tmp.fontSizeMax}] ";
            }
        }

        // 3. For multi-line text or headers, ensure proper overflow mode
        if (tmp.overflowMode == TextOverflowModes.Overflow && size.y > 40f)
        {
            tmp.overflowMode = TextOverflowModes.Ellipsis;
            tmp.textWrappingMode = TextWrappingModes.Normal;
            modified = true;
            action += "[Set Ellipsis & WordWrapping] ";
        }

        // 4. Ensure line spacing is healthy for Thai fonts (margin & line spacing)
        if (tmp.lineSpacing < 0f)
        {
            tmp.lineSpacing = 0f;
            modified = true;
            action += "[Reset LineSpacing] ";
        }

        if (needsFix || !string.IsNullOrEmpty(action))
        {
            log.Add(new AuditResult
            {
                sceneOrPrefab = location,
                objectPath = path,
                textContent = currentText.Length > 25 ? currentText.Substring(0, 25) + "..." : currentText,
                rectSize = size,
                preferredSizeTH = prefTH,
                preferredSizeEN = prefEN,
                preferredSizeZH = prefZH,
                didExceed = needsFix,
                actionTaken = action
            });
        }
    }

    private static void AuditUIText(Text txt, string location, ref bool modified, List<AuditResult> log)
    {
        RectTransform rt = txt.rectTransform;
        Vector2 size = rt.rect.size;
        if (size.x <= 1f || size.y <= 1f) return;

        string path = GetHierarchyPath(txt.transform);
        string currentText = txt.text;

        if (!string.IsNullOrEmpty(currentText))
        {
            string cleaned = CleanEmojiAndMissingGlyphs(currentText);
            if (cleaned != currentText)
            {
                txt.text = cleaned;
                currentText = cleaned;
                modified = true;
            }
        }

        string action = "";

        // Check Best Fit for UI.Text
        if (!txt.resizeTextForBestFit && (currentText.Length > 6 || path.Contains("Quest") || path.Contains("HUD")))
        {
            txt.resizeTextForBestFit = true;
            txt.resizeTextMaxSize = Mathf.Max(txt.fontSize, 18);
            txt.resizeTextMinSize = Mathf.Max(10, (int)(txt.fontSize * 0.6f));
            txt.horizontalOverflow = HorizontalWrapMode.Wrap;
            txt.verticalOverflow = VerticalWrapMode.Truncate;
            modified = true;
            action += $"[Enabled BestFit {txt.resizeTextMinSize}-{txt.resizeTextMaxSize}] ";
        }

        // Ensure vertical overflow doesn't spill over card boundaries
        if (txt.verticalOverflow == VerticalWrapMode.Overflow && size.y < 50f)
        {
            txt.verticalOverflow = VerticalWrapMode.Truncate;
            modified = true;
            action += "[Clamped Vertical Overflow] ";
        }

        if (!string.IsNullOrEmpty(action))
        {
            log.Add(new AuditResult
            {
                sceneOrPrefab = location,
                objectPath = path,
                textContent = currentText.Length > 25 ? currentText.Substring(0, 25) + "..." : currentText,
                rectSize = size,
                preferredSizeTH = new Vector2(txt.preferredWidth, txt.preferredHeight),
                preferredSizeEN = Vector2.zero,
                preferredSizeZH = Vector2.zero,
                didExceed = txt.preferredWidth > size.x || txt.preferredHeight > size.y,
                actionTaken = action
            });
        }
    }

    private static void ApplySceneSpecificFixes(string sceneName, ref bool modified)
    {
        if (sceneName == "cutscene End" || sceneName == "Cutscene")
        {
            // Narrative / Subtitle container check
            GameObject subObj = GameObject.Find("SubtitleText");
            if (subObj == null) subObj = GameObject.Find("NarrativeText");
            if (subObj == null) subObj = GameObject.Find("DialogueText");
            if (subObj != null)
            {
                var tmp = subObj.GetComponent<TextMeshProUGUI>();
                if (tmp != null)
                {
                    tmp.enableAutoSizing = true;
                    tmp.fontSizeMin = 18f;
                    tmp.fontSizeMax = 32f;
                    tmp.textWrappingMode = TextWrappingModes.Normal;
                    tmp.overflowMode = TextOverflowModes.Ellipsis;
                    tmp.extraPadding = true;
                    // Add safe margins to prevent hugging frame edges
                    tmp.margin = new Vector4(30f, 15f, 30f, 15f);
                    modified = true;
                    Debug.Log($"[{sceneName}] Optimized narrative text bounds & margins.");
                }
            }
        }
        else if (sceneName == "LoadingScreenNew")
        {
            // Check tutorial rows
            for (int i = 0; i <= 6; i++)
            {
                GameObject rowObj = GameObject.Find($"Row_{i}");
                if (rowObj == null)
                {
                    string[] rowNames = new string[] { "Row_0_Move", "Row_1_Jump", "Row_2_Sprint", "Row_3_Dash", "Row_4_Potion", "Row_5_Water", "Row_6_Climb" };
                    if (i < rowNames.Length) rowObj = GameObject.Find(rowNames[i]);
                }
                if (rowObj != null)
                {
                    Transform textContainer = rowObj.transform.Find("Text_Container");
                    if (textContainer != null)
                    {
                        var tmp = textContainer.GetComponent<TextMeshProUGUI>();
                        if (tmp != null)
                        {
                            tmp.enableAutoSizing = true;
                            tmp.fontSizeMin = 12f;
                            tmp.fontSizeMax = 20f;
                            tmp.textWrappingMode = TextWrappingModes.Normal;
                            tmp.overflowMode = TextOverflowModes.Ellipsis;
                            tmp.extraPadding = true;
                            modified = true;
                        }
                    }
                }
            }

            // Ready Prompt text
            GameObject readyObj = GameObject.Find("ReadyPrompt_Text");
            if (readyObj != null)
            {
                var tmp = readyObj.GetComponent<TextMeshProUGUI>();
                if (tmp != null)
                {
                    tmp.enableAutoSizing = true;
                    tmp.fontSizeMin = 14f;
                    tmp.fontSizeMax = 22f;
                    tmp.extraPadding = true;
                    modified = true;
                }
            }

            // Lumi Dialogue text
            GameObject lumiDialog = GameObject.Find("LumiDialogue_Text");
            if (lumiDialog != null)
            {
                var tmp = lumiDialog.GetComponent<TextMeshProUGUI>();
                if (tmp != null)
                {
                    tmp.enableAutoSizing = true;
                    tmp.fontSizeMin = 13f;
                    tmp.fontSizeMax = 19f;
                    tmp.enableWordWrapping = true;
                    tmp.extraPadding = true;
                    tmp.margin = new Vector4(20f, 10f, 20f, 10f);
                    modified = true;
                }
            }
        }
        else if (sceneName == "EndCredit")
        {
            // Ensure Special Thanks body and finale texts don't clip
            string[] creditObjects = new string[] { "ThanksBody", "ThaiThanksMsg", "EnglishSubMsg", "AdvisorName", "TeamGrid" };
            foreach (string name in creditObjects)
            {
                GameObject obj = GameObject.Find(name);
                if (obj != null)
                {
                    var tmp = obj.GetComponent<TextMeshProUGUI>();
                    if (tmp != null)
                    {
                        tmp.enableAutoSizing = true;
                        tmp.fontSizeMin = 14f;
                        tmp.fontSizeMax = tmp.fontSize > 0 ? tmp.fontSize : 24f;
                        tmp.extraPadding = true;
                        modified = true;
                    }
                }
            }
        }
        else if (sceneName == "MainGameplay")
        {
            // Quest panel text
            GameObject questPanel = GameObject.Find("QuestPanel");
            if (questPanel != null)
            {
                Text[] qTexts = questPanel.GetComponentsInChildren<Text>(true);
                foreach (var txt in qTexts)
                {
                    txt.resizeTextForBestFit = true;
                    txt.resizeTextMinSize = 10;
                    txt.resizeTextMaxSize = Mathf.Max(txt.fontSize, 18);
                    txt.verticalOverflow = VerticalWrapMode.Truncate;
                    txt.horizontalOverflow = HorizontalWrapMode.Wrap;
                    modified = true;
                }
            }
        }
    }

    private static string CleanEmojiAndMissingGlyphs(string text)
    {
        if (string.IsNullOrEmpty(text)) return text;
        System.Text.StringBuilder sb = new System.Text.StringBuilder();
        for (int i = 0; i < text.Length; i++)
        {
            char c = text[i];
            // Skip surrogate pairs (emojis like 🎮, 🇹🇭, etc.)
            if (char.IsSurrogate(c))
            {
                if (char.IsHighSurrogate(c) && i + 1 < text.Length && char.IsLowSurrogate(text[i + 1]))
                {
                    i++; // skip low surrogate
                }
                continue;
            }
            // Skip variation selectors (e.g. \uFE0F) and zero-width characters
            if (c >= '\uFE00' && c <= '\uFE0F') continue;
            if (c == '\u200B' || c == '\u200C' || c == '\u200D') continue;

            // Skip specific symbols that don't exist in Itim font
            if (c == '\u23F1' || c == '\u2728' || c == '\u2764' || c == '\u2605' || c == '\u2726' || c == '\u25A1')
            {
                continue;
            }
            sb.Append(c);
        }
        return sb.ToString().Trim();
    }

    private static string GetHierarchyPath(Transform t)
    {
        string path = t.name;
        while (t.parent != null)
        {
            t = t.parent;
            path = t.name + "/" + path;
        }
        return path;
    }
}
#endif
