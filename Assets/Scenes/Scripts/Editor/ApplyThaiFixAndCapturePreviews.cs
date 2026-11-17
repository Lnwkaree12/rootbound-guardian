#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.TextCore;
using TMPro;

public static class ApplyThaiFixAndCapturePreviews
{
    private const string FONT_ASSET_PATH = "Assets/Fonts/Itim_Thai_SDF.asset";
    private const string FONT_ASSET_ALT = "Assets/Fonts/Itim_SDF_Valid.asset";
    private const string FONT_ASSET_DYN = "Assets/Fonts/Itim_TMP_Dynamic.asset";

    private const string ARTIFACT_DIR = "C:/Users/Artemis/.gemini/antigravity/brain/d24cfade-99b4-4091-84a3-280da0706f05";

    public static void RunFullPipeline()
    {
        Debug.Log("======================================================================");
        Debug.Log("[ApplyThaiFix] STARTING COMPREHENSIVE THAI FONT & TEXT FIX PIPELINE");
        Debug.Log("======================================================================");

        // 1. Configure the Font Asset
        TMP_FontAsset thaiFont = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(FONT_ASSET_PATH);
        if (thaiFont == null)
        {
            Debug.LogError($"[ApplyThaiFix] Missing font asset at {FONT_ASSET_PATH}");
            return;
        }

        ConfigureFontAsset(thaiFont);

        TMP_FontAsset altFont = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(FONT_ASSET_ALT);
        if (altFont != null) ConfigureFontAsset(altFont);

        TMP_FontAsset dynFont = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(FONT_ASSET_DYN);
        if (dynFont != null) ConfigureFontAsset(dynFont);

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        // 2. Update Scene Texts & Capture Previews
        ProcessMainGameplay(thaiFont);
        ProcessLoadingScreen(thaiFont);
        ProcessMainScreen(thaiFont);
        ProcessOtherScenes(thaiFont);

        AssetDatabase.SaveAssets();
        Debug.Log("======================================================================");
        Debug.Log("[ApplyThaiFix] ALL TASKS COMPLETED SUCCESSFULLY!");
        Debug.Log("======================================================================");
    }

    private static void ConfigureFontAsset(TMP_FontAsset font)
    {
        Undo.RecordObject(font, "Configure Thai Font Metrics & PUA");

        FaceInfo faceInfo = font.faceInfo;
        faceInfo.ascentLine = 66.0f;
        faceInfo.lineHeight = 82.0f;
        faceInfo.capLine = 41.0f;
        faceInfo.meanLine = 28.0f;
        faceInfo.descentLine = -18.0f;
        font.faceInfo = faceInfo;

        Dictionary<uint, TMP_Character> charMap = new Dictionary<uint, TMP_Character>();
        foreach (var ch in font.characterTable) charMap[ch.unicode] = ch;

        Dictionary<uint, Glyph> glyphMap = new Dictionary<uint, Glyph>();
        uint maxGlyphIndex = 0;
        foreach (var g in font.glyphTable)
        {
            glyphMap[g.index] = g;
            if (g.index > maxGlyphIndex) maxGlyphIndex = g.index;
        }

        // Calibrate Base Tier 2 Tone Marks (Sit lower when directly on normal consonants, fixing "สระลอย")
        CalibrateBaseGlyph(charMap, glyphMap, ThaiFontAdjuster.MAI_EK, -11.39f, 41.5f);
        CalibrateBaseGlyph(charMap, glyphMap, ThaiFontAdjuster.MAI_THO, -26.23f, 42.0f);
        CalibrateBaseGlyph(charMap, glyphMap, ThaiFontAdjuster.MAI_TRI, -20.0f, 42.0f);
        CalibrateBaseGlyph(charMap, glyphMap, ThaiFontAdjuster.MAI_CHATTAWA, -20.0f, 42.0f);
        CalibrateBaseGlyph(charMap, glyphMap, ThaiFontAdjuster.THANTHAKHAT, -20.61f, 43.0f);

        uint nextGlyphIndex = Math.Max(maxGlyphIndex + 1, 3000);

        // Tier 3 Upper Tone Marks (Elevated above upper vowels: BearingY = 59.0..61.5)
        AddOrUpdatePUA(font, charMap, glyphMap, ref nextGlyphIndex,
            ThaiFontAdjuster.PUA_MAI_EK_UPPER, ThaiFontAdjuster.MAI_EK, -12.0f, 59.0f);
        AddOrUpdatePUA(font, charMap, glyphMap, ref nextGlyphIndex,
            ThaiFontAdjuster.PUA_MAI_THO_UPPER, ThaiFontAdjuster.MAI_THO, -24.0f, 59.5f);
        AddOrUpdatePUA(font, charMap, glyphMap, ref nextGlyphIndex,
            ThaiFontAdjuster.PUA_MAI_TRI_UPPER, ThaiFontAdjuster.MAI_TRI, -20.0f, 59.5f);
        AddOrUpdatePUA(font, charMap, glyphMap, ref nextGlyphIndex,
            ThaiFontAdjuster.PUA_MAI_CHATTAWA_UPPER, ThaiFontAdjuster.MAI_CHATTAWA, -20.0f, 59.5f);
        AddOrUpdatePUA(font, charMap, glyphMap, ref nextGlyphIndex,
            ThaiFontAdjuster.PUA_THANTHAKHAT_UPPER, ThaiFontAdjuster.THANTHAKHAT, -20.6f, 61.5f);

        // Left-shifted Upper Vowels (for ป ฝ ฟ ฬ: BearingX shifted left -10)
        AddOrUpdatePUA(font, charMap, glyphMap, ref nextGlyphIndex,
            ThaiFontAdjuster.PUA_MAI_HAN_AKAT_LEFT, ThaiFontAdjuster.MAI_HAN_AKAT, -31.2f, 45.1f);
        AddOrUpdatePUA(font, charMap, glyphMap, ref nextGlyphIndex,
            ThaiFontAdjuster.PUA_SARA_I_LEFT, ThaiFontAdjuster.SARA_I, -42.0f, 45.1f);
        AddOrUpdatePUA(font, charMap, glyphMap, ref nextGlyphIndex,
            ThaiFontAdjuster.PUA_SARA_II_LEFT, ThaiFontAdjuster.SARA_II, -42.1f, 46.2f);
        AddOrUpdatePUA(font, charMap, glyphMap, ref nextGlyphIndex,
            ThaiFontAdjuster.PUA_SARA_UE_LEFT, ThaiFontAdjuster.SARA_UE, -42.0f, 46.5f);
        AddOrUpdatePUA(font, charMap, glyphMap, ref nextGlyphIndex,
            ThaiFontAdjuster.PUA_SARA_UEE_LEFT, ThaiFontAdjuster.SARA_UEE, -42.2f, 46.5f);
        AddOrUpdatePUA(font, charMap, glyphMap, ref nextGlyphIndex,
            ThaiFontAdjuster.PUA_MAITAIKHU_LEFT, ThaiFontAdjuster.MAITAIKHU, -30.0f, 45.0f);
        AddOrUpdatePUA(font, charMap, glyphMap, ref nextGlyphIndex,
            ThaiFontAdjuster.PUA_NIKHAHIT_LEFT, ThaiFontAdjuster.NIKHAHIT, -25.4f, 44.7f);

        // Left-shifted Tone Marks (on ป ฝ ฟ ฬ without upper vowel)
        AddOrUpdatePUA(font, charMap, glyphMap, ref nextGlyphIndex,
            ThaiFontAdjuster.PUA_MAI_EK_LEFT, ThaiFontAdjuster.MAI_EK, -21.4f, 46.5f);
        AddOrUpdatePUA(font, charMap, glyphMap, ref nextGlyphIndex,
            ThaiFontAdjuster.PUA_MAI_THO_LEFT, ThaiFontAdjuster.MAI_THO, -36.2f, 46.5f);
        AddOrUpdatePUA(font, charMap, glyphMap, ref nextGlyphIndex,
            ThaiFontAdjuster.PUA_MAI_TRI_LEFT, ThaiFontAdjuster.MAI_TRI, -30.0f, 46.5f);
        AddOrUpdatePUA(font, charMap, glyphMap, ref nextGlyphIndex,
            ThaiFontAdjuster.PUA_MAI_CHATTAWA_LEFT, ThaiFontAdjuster.MAI_CHATTAWA, -30.0f, 46.5f);
        AddOrUpdatePUA(font, charMap, glyphMap, ref nextGlyphIndex,
            ThaiFontAdjuster.PUA_THANTHAKHAT_LEFT, ThaiFontAdjuster.THANTHAKHAT, -30.6f, 48.0f);

        // Tier 3 Left-shifted Tone Marks (on ป ฝ ฟ ฬ with upper vowel)
        AddOrUpdatePUA(font, charMap, glyphMap, ref nextGlyphIndex,
            ThaiFontAdjuster.PUA_MAI_EK_UPPER_LEFT, ThaiFontAdjuster.MAI_EK, -22.0f, 59.0f);
        AddOrUpdatePUA(font, charMap, glyphMap, ref nextGlyphIndex,
            ThaiFontAdjuster.PUA_MAI_THO_UPPER_LEFT, ThaiFontAdjuster.MAI_THO, -34.0f, 59.5f);
        AddOrUpdatePUA(font, charMap, glyphMap, ref nextGlyphIndex,
            ThaiFontAdjuster.PUA_MAI_TRI_UPPER_LEFT, ThaiFontAdjuster.MAI_TRI, -30.0f, 59.5f);
        AddOrUpdatePUA(font, charMap, glyphMap, ref nextGlyphIndex,
            ThaiFontAdjuster.PUA_MAI_CHATTAWA_UPPER_LEFT, ThaiFontAdjuster.MAI_CHATTAWA, -30.0f, 59.5f);
        AddOrUpdatePUA(font, charMap, glyphMap, ref nextGlyphIndex,
            ThaiFontAdjuster.PUA_THANTHAKHAT_UPPER_LEFT, ThaiFontAdjuster.THANTHAKHAT, -30.6f, 61.5f);

        // Lower Vowels Shifted Down (under ฎ ฏ)
        AddOrUpdatePUA(font, charMap, glyphMap, ref nextGlyphIndex,
            ThaiFontAdjuster.PUA_SARA_U_LOWER, ThaiFontAdjuster.SARA_U, -17.3f, -10.4f);
        AddOrUpdatePUA(font, charMap, glyphMap, ref nextGlyphIndex,
            ThaiFontAdjuster.PUA_SARA_UU_LOWER, ThaiFontAdjuster.SARA_UU, -24.5f, -10.4f);
        AddOrUpdatePUA(font, charMap, glyphMap, ref nextGlyphIndex,
            ThaiFontAdjuster.PUA_PHINTHU_LOWER, ThaiFontAdjuster.PHINTHU, -15.0f, -10.4f);

        // Tailless Consonants (ญ ฐ)
        AddOrUpdatePUA(font, charMap, glyphMap, ref nextGlyphIndex,
            ThaiFontAdjuster.PUA_YO_YING_TAILLESS, ThaiFontAdjuster.YO_YING, 0.8f, 29.3f, false);
        AddOrUpdatePUA(font, charMap, glyphMap, ref nextGlyphIndex,
            ThaiFontAdjuster.PUA_THO_THAN_TAILLESS, ThaiFontAdjuster.THO_THAN, 1.7f, 29.3f, false);

        font.ReadFontAssetDefinition();
        EditorUtility.SetDirty(font);
        Debug.Log($"[ApplyThaiFix] Configured {font.name}: {font.characterTable.Count} chars, {font.glyphTable.Count} glyphs.");
    }

    private static void CalibrateBaseGlyph(Dictionary<uint, TMP_Character> charMap,
        Dictionary<uint, Glyph> glyphMap, char baseChar, float targetBX, float targetBY)
    {
        if (charMap.TryGetValue((uint)baseChar, out TMP_Character character))
        {
            if (glyphMap.TryGetValue(character.glyphIndex, out Glyph glyph))
            {
                GlyphMetrics metrics = glyph.metrics;
                glyph.metrics = new GlyphMetrics(metrics.width, metrics.height, targetBX, targetBY, 0);
            }
        }
    }

    private static void AddOrUpdatePUA(TMP_FontAsset font, Dictionary<uint, TMP_Character> charMap,
        Dictionary<uint, Glyph> glyphMap, ref uint nextGlyphIndex, char puaChar, char baseChar,
        float targetBX, float targetBY, bool zeroAdvance = true)
    {
        uint puaUnicode = (uint)puaChar;
        uint baseUnicode = (uint)baseChar;

        if (!charMap.TryGetValue(baseUnicode, out TMP_Character baseCharacter)) return;
        if (!glyphMap.TryGetValue(baseCharacter.glyphIndex, out Glyph baseGlyph)) return;

        float advance = zeroAdvance ? 0 : baseGlyph.metrics.horizontalAdvance;
        GlyphMetrics newMetrics = new GlyphMetrics(baseGlyph.metrics.width, baseGlyph.metrics.height, targetBX, targetBY, advance);

        Glyph targetGlyph = null;
        if (charMap.TryGetValue(puaUnicode, out TMP_Character existingChar))
        {
            if (glyphMap.TryGetValue(existingChar.glyphIndex, out Glyph existingGlyph))
            {
                targetGlyph = existingGlyph;
                targetGlyph.metrics = newMetrics;
                targetGlyph.glyphRect = baseGlyph.glyphRect;
            }
        }

        if (targetGlyph == null)
        {
            uint newGlyphId = nextGlyphIndex++;
            targetGlyph = new Glyph(newGlyphId, newMetrics, baseGlyph.glyphRect, baseGlyph.scale, baseGlyph.atlasIndex);
            font.glyphTable.Add(targetGlyph);
            glyphMap[newGlyphId] = targetGlyph;

            TMP_Character newChar = new TMP_Character(puaUnicode, targetGlyph);
            font.characterTable.Add(newChar);
            charMap[puaUnicode] = newChar;
        }
    }

    private static void ProcessMainGameplay(TMP_FontAsset thaiFont)
    {
        string path = "Assets/Scenes/MainGameplay.unity";
        var scene = EditorSceneManager.OpenScene(path, OpenSceneMode.Single);
        Debug.Log("[ApplyThaiFix] Processing MainGameplay scene...");

        // Update all TMP texts in scene
        AdjustAllTMPInScene(thaiFont);

        GameObject gameOverGo = null;
        GameObject pauseGo = null;
        foreach (var root in scene.GetRootGameObjects())
        {
            if (root.name == "GameOver_Canvas") gameOverGo = root;
            else if (root.name == "PauseMenu_Canvas") pauseGo = root;
        }

        // Save active states of all roots so we can isolate UI cleanly
        var rootObjects = scene.GetRootGameObjects();
        Dictionary<GameObject, bool> rootActiveMap = new Dictionary<GameObject, bool>();
        foreach (var r in rootObjects) rootActiveMap[r] = r.activeSelf;

        void IsolateOnly(GameObject target)
        {
            foreach (var r in rootObjects)
            {
                if (r != null) r.SetActive(r == target);
            }
        }

        void RestoreRoots()
        {
            foreach (var kvp in rootActiveMap)
            {
                if (kvp.Key != null) kvp.Key.SetActive(kvp.Value);
            }
        }

        // Ensure language is TH
        LocalizationManager.Instance.SetLanguage(LocalizationManager.Language.TH);

        // ── 1. GameOver Canvas Preview ─────────────────────────────────────────
        if (gameOverGo != null)
        {
            Debug.Log("[ApplyThaiFix] Capturing GameOver_Canvas Preview...");
            IsolateOnly(gameOverGo);

            foreach (var cg in gameOverGo.GetComponentsInChildren<CanvasGroup>(true))
            {
                cg.alpha = 1f;
            }

            Transform panel = gameOverGo.transform.Find("GameOverPanel");
            if (panel != null)
            {
                panel.gameObject.SetActive(true);
                Transform innerCard = panel.Find("ModalContainer/InnerCard");
                if (innerCard != null)
                {
                    Transform tipBox = innerCard.Find("TipBox");
                    if (tipBox != null)
                    {
                        Transform body = tipBox.Find("TipText");
                        if (body != null)
                        {
                            var tmp = body.GetComponent<TextMeshProUGUI>();
                            if (tmp != null)
                            {
                                tmp.font = thaiFont;
                                string text = LocalizationManager.Instance.GetFromLanguage("gameover_tips_body", "TH");
                                tmp.text = ThaiFontAdjuster.Adjust(text);
                                tmp.fontSize = 17.5f;
                                tmp.lineSpacing = -5f;
                                tmp.overflowMode = TextOverflowModes.Overflow;
                            }
                        }
                        Transform header = tipBox.Find("TipHeader");
                        if (header != null)
                        {
                            var tmp = header.GetComponent<TextMeshProUGUI>();
                            if (tmp != null)
                            {
                                tmp.font = thaiFont;
                                string text = LocalizationManager.Instance.GetFromLanguage("gameover_survival_tips", "TH");
                                tmp.text = ThaiFontAdjuster.Adjust(text);
                            }
                        }
                    }

                    Transform encourage = innerCard.Find("EncouragementWhisper");
                    if (encourage != null)
                    {
                        var tmp = encourage.GetComponent<TextMeshProUGUI>();
                        if (tmp != null)
                        {
                            tmp.font = thaiFont;
                            string text = LocalizationManager.Instance.GetFromLanguage("gameover_encouragement", "TH");
                            tmp.text = ThaiFontAdjuster.Adjust(text);
                        }
                    }

                    Transform reason = innerCard.Find("ReasonText");
                    if (reason != null)
                    {
                        var tmp = reason.GetComponent<TextMeshProUGUI>();
                        if (tmp != null)
                        {
                            tmp.font = thaiFont;
                            string text = LocalizationManager.Instance.GetFromLanguage("gameover_no_checkpoint", "TH");
                            tmp.text = ThaiFontAdjuster.Adjust(text);
                        }
                    }

                    Transform badge = innerCard.Find("BadgeText");
                    if (badge != null)
                    {
                        var tmp = badge.GetComponent<TextMeshProUGUI>();
                        if (tmp != null)
                        {
                            tmp.font = thaiFont;
                            tmp.text = ThaiFontAdjuster.Adjust(LocalizationManager.Instance.GetFromLanguage("gameover_badge", "TH"));
                        }
                    }

                    Transform btnRow = innerCard.Find("ButtonRow");
                    if (btnRow != null)
                    {
                        SetButtonLabel(btnRow, "RestartButton", "เริ่มใหม่", thaiFont);
                        SetButtonLabel(btnRow, "MainMenuButton", "หน้าหลัก", thaiFont);
                    }
                }
            }

            CaptureCanvasAutoFrame(gameOverGo.GetComponent<Canvas>(), Path.Combine(ARTIFACT_DIR, "GameOver_Thai_Verified.png"), 1920, 1080);

            // Also capture a focused close-up on the TipBox
            Transform tipBoxObj = panel?.Find("ModalContainer/InnerCard/TipBox");
            if (tipBoxObj != null)
            {
                CaptureRectCloseup(tipBoxObj.GetComponent<RectTransform>(), Path.Combine(ARTIFACT_DIR, "GameOver_Tips_Closeup.png"), 1200, 500);
            }

            RestoreRoots();
        }

        // ── 2. PauseMenu Canvas Preview ────────────────────────────────────────
        if (pauseGo != null)
        {
            Debug.Log("[ApplyThaiFix] Capturing PauseMenu_Canvas Preview...");
            IsolateOnly(pauseGo);

            foreach (var cg in pauseGo.GetComponentsInChildren<CanvasGroup>(true))
            {
                cg.alpha = 1f;
            }

            Transform panel = pauseGo.transform.Find("PauseMenuPanel");
            if (panel != null)
            {
                panel.gameObject.SetActive(true);
                Transform innerCard = panel.Find("InnerCard");
                if (innerCard != null)
                {
                    Transform title = innerCard.Find("HeaderBar/Title_Text");
                    if (title != null)
                    {
                        var tmp = title.GetComponent<TextMeshProUGUI>();
                        if (tmp != null)
                        {
                            tmp.font = thaiFont;
                            tmp.text = ThaiFontAdjuster.Adjust("PAUSE อยู่ในขณะนี้");
                        }
                    }
                    Transform audioSec = innerCard.Find("AudioSettingsSection");
                    if (audioSec != null)
                    {
                        Transform h = audioSec.Find("Header");
                        if (h != null)
                        {
                            var tmp = h.GetComponent<TextMeshProUGUI>();
                            if (tmp != null)
                            {
                                tmp.font = thaiFont;
                                tmp.text = ThaiFontAdjuster.Adjust("การตั้งค่าเสียง (AUDIO SETTINGS)");
                            }
                        }
                    }
                    Transform btnRow = innerCard.Find("ButtonRow");
                    if (btnRow != null)
                    {
                        SetButtonLabel(btnRow, "ResumeButton", "เล่นต่อ", thaiFont);
                        SetButtonLabel(btnRow, "RestartButton", "เริ่มใหม่", thaiFont);
                        SetButtonLabel(btnRow, "MainMenuButton", "หน้าหลัก", thaiFont);
                    }
                }
            }

            CaptureCanvasAutoFrame(pauseGo.GetComponent<Canvas>(), Path.Combine(ARTIFACT_DIR, "PauseMenu_Thai_Verified.png"), 1920, 1080);

            // Also capture close-up on Header and Buttons
            Transform innerCardObj = panel?.Find("InnerCard");
            if (innerCardObj != null)
            {
                CaptureRectCloseup(innerCardObj.GetComponent<RectTransform>(), Path.Combine(ARTIFACT_DIR, "PauseMenu_Closeup.png"), 1400, 800);
            }

            RestoreRoots();
        }

        EditorSceneManager.SaveScene(scene);
    }

    private static void CaptureRectCloseup(RectTransform target, string outputPath, int width, int height)
    {
        if (target == null) return;
        Canvas canvas = target.GetComponentInParent<Canvas>();
        if (canvas == null) return;

        Canvas.ForceUpdateCanvases();

        GameObject camGo = new GameObject("CloseupCam");
        Camera cam = camGo.AddComponent<Camera>();
        cam.clearFlags = CameraClearFlags.SolidColor;
        cam.backgroundColor = new Color(0.12f, 0.08f, 0.05f, 1f);
        cam.orthographic = true;

        RenderMode prevMode = canvas.renderMode;
        Camera prevCam = canvas.worldCamera;
        float prevPlaneDist = canvas.planeDistance;

        canvas.renderMode = RenderMode.ScreenSpaceCamera;
        canvas.worldCamera = cam;
        canvas.planeDistance = 50f;

        Canvas.ForceUpdateCanvases();

        Rect rect = target.rect;
        Vector3 worldCenter = target.TransformPoint(rect.center);

        cam.orthographicSize = Mathf.Max(rect.height * 0.6f, 100f);
        camGo.transform.position = new Vector3(worldCenter.x, worldCenter.y, worldCenter.z - 50f);
        camGo.transform.rotation = target.rotation;
        cam.nearClipPlane = 0.1f;
        cam.farClipPlane = 1000f;

        RenderTexture rt = new RenderTexture(width, height, 24, RenderTextureFormat.ARGB32);
        rt.antiAliasing = 4;
        RenderTexture prevActive = RenderTexture.active;

        cam.targetTexture = rt;
        cam.Render();

        RenderTexture.active = rt;
        Texture2D tex = new Texture2D(width, height, TextureFormat.RGB24, false);
        tex.ReadPixels(new Rect(0, 0, width, height), 0, 0);
        tex.Apply();

        byte[] png = tex.EncodeToPNG();
        File.WriteAllBytes(outputPath, png);
        Debug.Log($"[ApplyThaiFix] Saved closeup screenshot: {outputPath} ({png.Length} bytes)");

        // Restore
        cam.targetTexture = null;
        RenderTexture.active = prevActive;
        rt.Release();
        UnityEngine.Object.DestroyImmediate(rt);
        UnityEngine.Object.DestroyImmediate(tex);
        UnityEngine.Object.DestroyImmediate(camGo);

        canvas.renderMode = prevMode;
        canvas.worldCamera = prevCam;
        canvas.planeDistance = prevPlaneDist;
    }

    private static void CaptureCanvasAutoFrame(Canvas canvas, string outputPath, int width, int height)
    {
        if (canvas == null) return;

        Canvas.ForceUpdateCanvases();

        RenderMode prevMode = canvas.renderMode;
        Camera prevCam = canvas.worldCamera;
        float prevPlaneDist = canvas.planeDistance;

        GameObject camGo = new GameObject("AutoFrameCam");
        Camera cam = camGo.AddComponent<Camera>();
        cam.clearFlags = CameraClearFlags.SolidColor;
        cam.backgroundColor = new Color(0.12f, 0.08f, 0.05f, 1f);
        cam.orthographic = true;

        canvas.renderMode = RenderMode.ScreenSpaceCamera;
        canvas.worldCamera = cam;
        canvas.planeDistance = 50f;

        Canvas.ForceUpdateCanvases();

        RectTransform canvasRt = canvas.GetComponent<RectTransform>();
        Rect rect = canvasRt.rect;
        Vector3 worldCenter = canvasRt.TransformPoint(rect.center);

        cam.orthographicSize = rect.height * 0.5f;
        camGo.transform.position = new Vector3(worldCenter.x, worldCenter.y, worldCenter.z - 50f);
        camGo.transform.rotation = canvasRt.rotation;
        cam.nearClipPlane = 0.1f;
        cam.farClipPlane = 1000f;

        RenderTexture rt = new RenderTexture(width, height, 24, RenderTextureFormat.ARGB32);
        rt.antiAliasing = 4;
        RenderTexture prevActive = RenderTexture.active;

        cam.targetTexture = rt;
        cam.Render();

        RenderTexture.active = rt;
        Texture2D tex = new Texture2D(width, height, TextureFormat.RGB24, false);
        tex.ReadPixels(new Rect(0, 0, width, height), 0, 0);
        tex.Apply();

        byte[] png = tex.EncodeToPNG();
        File.WriteAllBytes(outputPath, png);
        Debug.Log($"[ApplyThaiFix] Saved auto-framed screenshot: {outputPath} ({png.Length} bytes)");

        // Restore
        cam.targetTexture = null;
        RenderTexture.active = prevActive;
        rt.Release();
        UnityEngine.Object.DestroyImmediate(rt);
        UnityEngine.Object.DestroyImmediate(tex);
        UnityEngine.Object.DestroyImmediate(camGo);

        canvas.renderMode = prevMode;
        canvas.worldCamera = prevCam;
        canvas.planeDistance = prevPlaneDist;
    }

    private static void SetButtonLabel(Transform parent, string btnName, string rawLabel, TMP_FontAsset font)
    {
        Transform btn = parent.Find(btnName);
        if (btn == null) return;
        Transform label = btn.Find("Content/Label");
        if (label == null) label = btn.Find("Label");
        if (label == null) label = btn.Find("Text");
        if (label != null)
        {
            var tmp = label.GetComponent<TextMeshProUGUI>();
            if (tmp != null)
            {
                tmp.font = font;
                tmp.text = ThaiFontAdjuster.Adjust(rawLabel);
            }
        }
    }

    private static void ProcessLoadingScreen(TMP_FontAsset thaiFont)
    {
        string path = "Assets/Scenes/LoadingScreenNew.unity";
        if (!File.Exists(path)) return;
        var scene = EditorSceneManager.OpenScene(path, OpenSceneMode.Single);
        Debug.Log("[ApplyThaiFix] Processing LoadingScreenNew scene...");

        AdjustAllTMPInScene(thaiFont);

        // Find Canvas
        Canvas canvas = UnityEngine.Object.FindFirstObjectByType<Canvas>();
        if (canvas != null)
        {
            CaptureCanvasScreenshot(canvas, Path.Combine(ARTIFACT_DIR, "LoadingScreen_Thai_Verified.png"), 1920, 1080);
        }

        EditorSceneManager.SaveScene(scene);
    }

    private static void ProcessMainScreen(TMP_FontAsset thaiFont)
    {
        string path = "Assets/Scenes/MainScreen.unity";
        if (!File.Exists(path)) return;
        var scene = EditorSceneManager.OpenScene(path, OpenSceneMode.Single);
        Debug.Log("[ApplyThaiFix] Processing MainScreen scene...");

        AdjustAllTMPInScene(thaiFont);

        // Check for Controller Settings dialog if present
        Canvas canvas = UnityEngine.Object.FindFirstObjectByType<Canvas>();
        if (canvas != null)
        {
            CaptureCanvasScreenshot(canvas, Path.Combine(ARTIFACT_DIR, "MainScreen_Thai_Verified.png"), 1920, 1080);
        }

        EditorSceneManager.SaveScene(scene);
    }

    private static void ProcessOtherScenes(TMP_FontAsset thaiFont)
    {
        string[] scenes = new string[]
        {
            "Assets/Scenes/EndCredit.unity",
            "Assets/Scenes/Cutscene.unity",
            "Assets/Scenes/cutscene End.unity",
            "Assets/Scenes/EndScene.unity"
        };

        foreach (var p in scenes)
        {
            if (File.Exists(p))
            {
                var scene = EditorSceneManager.OpenScene(p, OpenSceneMode.Single);
                AdjustAllTMPInScene(thaiFont);
                EditorSceneManager.SaveScene(scene);
            }
        }
    }

    private static void AdjustAllTMPInScene(TMP_FontAsset thaiFont)
    {
        var tmps = UnityEngine.Object.FindObjectsByType<TMP_Text>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        int modified = 0;
        foreach (var t in tmps)
        {
            if (string.IsNullOrEmpty(t.text)) continue;

            bool hasThai = false;
            foreach (char c in t.text)
            {
                if ((c >= 0x0E01 && c <= 0x0E5B) || (c >= 0xF700 && c <= 0xF71A))
                {
                    hasThai = true;
                    break;
                }
            }

            if (hasThai)
            {
                Undo.RecordObject(t, "Adjust Thai Text");
                string original = t.text;
                string adjusted = ThaiFontAdjuster.Adjust(original);
                t.text = adjusted;
                t.font = thaiFont;
                EditorUtility.SetDirty(t);
                modified++;
            }
        }
        Debug.Log($"[ApplyThaiFix] Adjusted {modified} Thai TMP text components.");
    }

    private static void CaptureCanvasScreenshot(Canvas canvas, string outputPath, int width, int height)
    {
        if (canvas == null) return;

        Camera cam = canvas.worldCamera;
        bool createdTempCam = false;

        if (cam == null)
        {
            GameObject camGo = new GameObject("TempCaptureCam");
            cam = camGo.AddComponent<Camera>();
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = new Color(0.12f, 0.08f, 0.05f, 1f);
            cam.orthographic = true;
            cam.orthographicSize = 5f;
            cam.nearClipPlane = 0.1f;
            cam.farClipPlane = 1000f;
            camGo.transform.position = new Vector3(0, 0, -10f);
            createdTempCam = true;
        }

        RenderMode prevRenderMode = canvas.renderMode;
        Camera prevCam = canvas.worldCamera;
        float prevPlaneDistance = canvas.planeDistance;

        canvas.renderMode = RenderMode.ScreenSpaceCamera;
        canvas.worldCamera = cam;
        canvas.planeDistance = 10f;

        Canvas.ForceUpdateCanvases();

        RenderTexture rt = new RenderTexture(width, height, 24, RenderTextureFormat.ARGB32);
        rt.antiAliasing = 4;
        RenderTexture prevActive = RenderTexture.active;
        RenderTexture prevTarget = cam.targetTexture;

        cam.targetTexture = rt;
        cam.Render();

        RenderTexture.active = rt;
        Texture2D tex = new Texture2D(width, height, TextureFormat.RGB24, false);
        tex.ReadPixels(new Rect(0, 0, width, height), 0, 0);
        tex.Apply();

        byte[] png = tex.EncodeToPNG();
        File.WriteAllBytes(outputPath, png);
        Debug.Log($"[ApplyThaiFix] Saved preview screenshot to: {outputPath} ({png.Length} bytes)");

        // Restore
        cam.targetTexture = prevTarget;
        RenderTexture.active = prevActive;
        rt.Release();
        UnityEngine.Object.DestroyImmediate(rt);
        UnityEngine.Object.DestroyImmediate(tex);

        canvas.renderMode = prevRenderMode;
        canvas.worldCamera = prevCam;
        canvas.planeDistance = prevPlaneDistance;

        if (createdTempCam)
        {
            UnityEngine.Object.DestroyImmediate(cam.gameObject);
        }
    }
}
#endif
