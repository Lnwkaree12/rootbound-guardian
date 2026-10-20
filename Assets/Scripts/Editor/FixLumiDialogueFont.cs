#if UNITY_EDITOR
using System.IO;
using System.Text;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.TextCore.LowLevel;

public static class FixLumiDialogueFont
{
    private const string TTF_PATH = "Assets/Itim-Regular.ttf";
    private const string FONT_ASSET_PATH = "Assets/Fonts/Itim_Thai_SDF.asset";
    private const string DYNAMIC_FONT_PATH = "Assets/Fonts/Itim_TMP_Dynamic.asset";
    private const string PREFAB_PATH = "Assets/Prefabs/Lumi_Companion.prefab";

    [MenuItem("Tools/SproutScout/Fix Lumi Dialogue Font and Speech Bubble")]
    public static void ExecuteFix()
    {
        Debug.Log("[FixLumiDialogueFont] Starting comprehensive fix for Lumi Dialogue Font & Speech Bubble...");

        // 1. Build & Save the high-resolution Thai SDF Font Asset
        TMP_FontAsset thaiFontAsset = CreateOrUpdateThaiFontAsset();
        if (thaiFontAsset == null)
        {
            Debug.LogError("[FixLumiDialogueFont] Failed to create Thai Font Asset!");
            return;
        }

        // 2. Also repair Itim_TMP_Dynamic.asset so no legacy reference crashes
        RepairDynamicFontAsset(thaiFontAsset);

        // 3. Update the Lumi_Companion Prefab
        UpdateLumiPrefab(thaiFontAsset);

        // 4. Update any Lumi_Companion instances in the currently active scene
        UpdateLumiInActiveScene(thaiFontAsset);

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        Debug.Log("[FixLumiDialogueFont] All fixes successfully applied! Lumi dialogue font is now crystal clear.");
    }

    private static TMP_FontAsset CreateOrUpdateThaiFontAsset()
    {
        Font sourceFont = AssetDatabase.LoadAssetAtPath<Font>(TTF_PATH);
        if (sourceFont == null)
        {
            Debug.LogError($"[FixLumiDialogueFont] Source font not found at {TTF_PATH}");
            return null;
        }

        // Check if font asset already exists
        TMP_FontAsset fontAsset = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(FONT_ASSET_PATH);
        bool isNew = (fontAsset == null);

        if (isNew)
        {
            // 64pt sampling, 9 padding, 2048x2048 atlas with SDFAA render mode
            fontAsset = TMP_FontAsset.CreateFontAsset(sourceFont, 64, 9, GlyphRenderMode.SDFAA, 2048, 2048);
            fontAsset.name = "Itim_Thai_SDF";
        }

        // Gather all characters needed
        StringBuilder sb = new StringBuilder();
        // ASCII printable: 32 - 126
        for (int c = 32; c <= 126; c++) sb.Append((char)c);
        // Thai characters: 0x0E01 - 0x0E5B (consonants, vowels, tone marks, numerals, symbols)
        for (int c = 0x0E01; c <= 0x0E5B; c++) sb.Append((char)c);
        // Special typography: ellipsis, quotes, dashes
        sb.Append("…‘’“”–—•");

        // Extra characters from banter lines
        string sampleText = "เดินระวังหน่อยสิเจ้ามนุษย์! เดี๋ยวก็สะดุดหินหรอก ฮิๆ~ ว้าว ลาวาตรงนั้นสีสวยจัง... แต่อย่ากระโดดลงไปเล่นนะ! ปีกข้าสวยมั้ยล่ะ? บินทั้งวันยังไม่เหนื่อยเลยนะจะบอกให้~ นี่ๆ เดินเร็วๆ หน่อย ข้าบินนำไปไกลแล้วนะ! แถวนี้มืดจัง ดีนะที่มีข้าคอยส่องไฟให้อ่ะ! ฮั่นแน่! แอบมองข้าอยู่ล่ะสิ มีข้าไปด้วยอุ่นใจใช่ม้า~ สู้เขานะเจ้าสองขา ข้าเอาใจช่วยอยู่ข้างๆ นี่แหละ! เหนื่อยรึยัง? ข้าลอยตัวสบายจัง ไม่เมื่อยขาเลยสักนิด อิอิ ระวังหัวด้วยนะข้างหน้า ข้าตัวเล็กข้าหลบง่าย แต่เจ้าตัวโตนะ! ข้าเป็นวิญญาณแห่งแสงที่น่ารักที่สุดในป่าแล้ว ยอมรับมาซะดีๆ!";
        sb.Append(sampleText);

        fontAsset.TryAddCharacters(sb.ToString(), out string missingChars);
        Debug.Log($"[FixLumiDialogueFont] Generated {fontAsset.characterTable.Count} characters into font atlas.");

        // Configure Material for high clarity & contrast
        Material mat = fontAsset.material;
        if (mat != null)
        {
            mat.name = "Itim_Thai_SDF Material";
            // Ensure shader is TextMeshPro/Distance Field
            Shader dfShader = Shader.Find("TextMeshPro/Distance Field");
            if (dfShader != null) mat.shader = dfShader;

            // Subtle outline to make text pop against cream marshmallow background
            mat.EnableKeyword("OUTLINE_ON");
            mat.SetFloat(ShaderUtilities.ID_OutlineWidth, 0.08f);
            mat.SetColor(ShaderUtilities.ID_OutlineColor, new Color(0.12f, 0.08f, 0.04f, 0.85f));
            mat.SetFloat(ShaderUtilities.ID_OutlineSoftness, 0.05f);
        }

        Texture2D atlasTex = (fontAsset.atlasTextures != null && fontAsset.atlasTextures.Length > 0) ? fontAsset.atlasTextures[0] : null;
        if (atlasTex != null)
        {
            atlasTex.name = "Itim_Thai_SDF Atlas";
        }

        if (isNew)
        {
            AssetDatabase.CreateAsset(fontAsset, FONT_ASSET_PATH);
            if (mat != null) AssetDatabase.AddObjectToAsset(mat, fontAsset);
            if (atlasTex != null) AssetDatabase.AddObjectToAsset(atlasTex, fontAsset);
        }

        EditorUtility.SetDirty(fontAsset);
        return fontAsset;
    }

    private static void RepairDynamicFontAsset(TMP_FontAsset validFontAsset)
    {
        TMP_FontAsset dynamicAsset = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(DYNAMIC_FONT_PATH);
        if (dynamicAsset == null) return;

        // If dynamicAsset has null atlas, copy valid atlas and characters so any remaining references don't crash
        if (dynamicAsset.atlasTextures == null || dynamicAsset.atlasTextures.Length == 0 || dynamicAsset.atlasTextures[0] == null)
        {
            Debug.Log("[FixLumiDialogueFont] Repairing legacy Itim_TMP_Dynamic.asset...");
            if (validFontAsset.atlasTextures != null && validFontAsset.atlasTextures.Length > 0)
            {
                // Assign valid font as fallback or copy
                dynamicAsset.fallbackFontAssetTable = new System.Collections.Generic.List<TMP_FontAsset> { validFontAsset };
            }
            EditorUtility.SetDirty(dynamicAsset);
        }
    }

    private static void UpdateLumiPrefab(TMP_FontAsset fontAsset)
    {
        GameObject prefabRoot = PrefabUtility.LoadPrefabContents(PREFAB_PATH);
        if (prefabRoot == null)
        {
            Debug.LogError($"[FixLumiDialogueFont] Could not load prefab at {PREFAB_PATH}");
            return;
        }

        ConfigureLumiComponents(prefabRoot, fontAsset);

        PrefabUtility.SaveAsPrefabAsset(prefabRoot, PREFAB_PATH);
        PrefabUtility.UnloadPrefabContents(prefabRoot);
        Debug.Log($"[FixLumiDialogueFont] Successfully updated {PREFAB_PATH}");
    }

    public static void UpdateLumiInActiveScene(TMP_FontAsset fontAsset)
    {
        LumiCompanion[] lumisInScene = Object.FindObjectsByType<LumiCompanion>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        Debug.Log($"[FixLumiDialogueFont] Found {lumisInScene.Length} LumiCompanion instance(s) in active scene.");

        GameObject player = GameObject.Find("Player Variant");
        if (player == null) player = GameObject.FindGameObjectWithTag("Player");

        foreach (var lumi in lumisInScene)
        {
            ConfigureLumiComponents(lumi.gameObject, fontAsset);

            // In EndScene, place Lumi gracefully beside player shoulder if it's currently far away
            if (player != null)
            {
                float dist = Vector3.Distance(lumi.transform.position, player.transform.position);
                if (dist > 3.0f)
                {
                    Vector3 shoulderPos = player.transform.position + new Vector3(-0.75f, 1.35f, -0.4f);
                    lumi.transform.position = shoulderPos;
                    lumi.transform.rotation = Quaternion.Euler(0f, 155.4f, 0f);
                    Debug.Log($"[FixLumiDialogueFont] Repositioned Lumi to player shoulder at {shoulderPos}");
                }
            }

            EditorUtility.SetDirty(lumi.gameObject);
        }

        var scene = EditorSceneManager.GetActiveScene();
        if (scene.IsValid())
        {
            EditorSceneManager.MarkSceneDirty(scene);
        }
    }

    public static void ConfigureLumiComponents(GameObject lumiRoot, TMP_FontAsset fontAsset)
    {
        Canvas canvas = lumiRoot.GetComponentInChildren<Canvas>(true);
        if (canvas != null)
        {
            canvas.renderMode = RenderMode.WorldSpace;
            // Channel flags for TextMeshPro (TexCoord1 | Normal | Tangent = 25)
            canvas.additionalShaderChannels = AdditionalCanvasShaderChannels.TexCoord1 | AdditionalCanvasShaderChannels.Normal | AdditionalCanvasShaderChannels.Tangent;

            // Remove or disable CanvasScaler if present to prevent downsampling
            CanvasScaler scaler = canvas.GetComponent<CanvasScaler>();
            if (scaler != null)
            {
                scaler.dynamicPixelsPerUnit = 1f;
                scaler.referencePixelsPerUnit = 100f;
            }

            // High-resolution Canvas Sizing:
            // Sized for crisp display: 520 x 180 at scale 0.0055 = ~2.8m x 1.0m world size
            RectTransform canvasRect = canvas.GetComponent<RectTransform>();
            if (canvasRect != null)
            {
                canvasRect.sizeDelta = new Vector2(520f, 180f);
                canvasRect.localScale = new Vector3(0.0055f, 0.0055f, 0.0055f);
                canvasRect.localPosition = new Vector3(0f, 0.45f, 0f);
            }
        }

        // Configure BubbleRoot / Background Image
        Transform bubbleRoot = lumiRoot.transform.Find("SpeechBubbleCanvas/BubbleRoot");
        if (bubbleRoot == null)
        {
            // Try find anywhere under canvas
            foreach (var t in lumiRoot.GetComponentsInChildren<Transform>(true))
            {
                if (t.name == "BubbleRoot") { bubbleRoot = t; break; }
            }
        }

        if (bubbleRoot != null)
        {
            RectTransform bubbleRect = bubbleRoot.GetComponent<RectTransform>();
            if (bubbleRect != null)
            {
                bubbleRect.sizeDelta = new Vector2(510f, 170f);
            }

            Image bgImage = bubbleRoot.GetComponent<Image>();
            if (bgImage != null)
            {
                bgImage.color = new Color(1f, 1f, 1f, 0.98f);
            }
        }

        // Configure DialogueText TextMeshProUGUI
        TextMeshProUGUI tmp = lumiRoot.GetComponentInChildren<TextMeshProUGUI>(true);
        if (tmp != null)
        {
            tmp.font = fontAsset;
            if (fontAsset.material != null)
            {
                tmp.fontSharedMaterial = fontAsset.material;
            }

            RectTransform textRect = tmp.GetComponent<RectTransform>();
            if (textRect != null)
            {
                textRect.anchorMin = new Vector2(0f, 0f);
                textRect.anchorMax = new Vector2(1f, 1f);
                // Padding: 35px left/right, 25px bottom, 15px top
                textRect.offsetMin = new Vector2(35f, 25f);
                textRect.offsetMax = new Vector2(-35f, -15f);
            }

            // Typography settings for ultimate clarity:
            tmp.enableAutoSizing = true;
            tmp.fontSizeMin = 24f;
            tmp.fontSizeMax = 36f;
            tmp.fontSize = 32f;
            tmp.fontStyle = FontStyles.Bold;
            tmp.alignment = TextAlignmentOptions.Center;
            // Line spacing & paragraph spacing for Thai diacritics
            tmp.lineSpacing = 16f;
            tmp.paragraphSpacing = 8f;
            tmp.wordWrappingRatios = 0.4f;
            tmp.textWrappingMode = TextWrappingModes.Normal;
            tmp.overflowMode = TextOverflowModes.Overflow;

            // Rich warm brown text for high contrast against light marshmallow background
            tmp.color = new Color(0.18f, 0.11f, 0.05f, 1.0f);
            tmp.raycastTarget = false;

            EditorUtility.SetDirty(tmp);
        }
    }
}
#endif
