#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.TextCore;
using TMPro;

public static class ConfigureThaiFontAsset
{
    private const string FONT_ASSET_PATH = "Assets/Fonts/Itim_Thai_SDF.asset";
    private const string FONT_ASSET_ALT = "Assets/Fonts/Itim_SDF_Valid.asset";
    private const string FONT_ASSET_DYN = "Assets/Fonts/Itim_TMP_Dynamic.asset";

    [MenuItem("Tools/Localization/Configure Thai Font Asset & PUA")]
    public static void ExecuteConfigure()
    {
        Debug.Log("======================================================================");
        Debug.Log("[ConfigureThaiFontAsset] Starting Thai Font Calibration & PUA Setup...");
        Debug.Log("======================================================================");

        TMP_FontAsset thaiFont = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(FONT_ASSET_PATH);
        if (thaiFont == null)
        {
            Debug.LogError($"[ConfigureThaiFontAsset] Could not find {FONT_ASSET_PATH}!");
            return;
        }

        ConfigureFontAsset(thaiFont);

        // Also sync to alt and dynamic if they exist
        TMP_FontAsset altFont = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(FONT_ASSET_ALT);
        if (altFont != null) ConfigureFontAsset(altFont);

        TMP_FontAsset dynFont = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(FONT_ASSET_DYN);
        if (dynFont != null) ConfigureFontAsset(dynFont);

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        Debug.Log("[ConfigureThaiFontAsset] Thai Font Asset successfully configured with PUA support!");
    }

    private static void ConfigureFontAsset(TMP_FontAsset font)
    {
        Undo.RecordObject(font, "Configure Thai Font Metrics & PUA");

        // 1. Adjust FaceInfo for headroom
        FaceInfo faceInfo = font.faceInfo;
        faceInfo.ascentLine = 66.0f;
        faceInfo.lineHeight = 82.0f;
        faceInfo.capLine = 41.0f;
        faceInfo.meanLine = 28.0f;
        faceInfo.descentLine = -18.0f;
        font.faceInfo = faceInfo;

        // Build quick lookup dictionaries
        Dictionary<uint, TMP_Character> charMap = new Dictionary<uint, TMP_Character>();
        foreach (var ch in font.characterTable)
        {
            charMap[ch.unicode] = ch;
        }

        Dictionary<uint, Glyph> glyphMap = new Dictionary<uint, Glyph>();
        uint maxGlyphIndex = 0;
        foreach (var g in font.glyphTable)
        {
            glyphMap[g.index] = g;
            if (g.index > maxGlyphIndex) maxGlyphIndex = g.index;
        }

        // Helper to get glyph for a unicode character
        Glyph GetGlyphForChar(char c)
        {
            if (charMap.TryGetValue((uint)c, out TMP_Character character))
            {
                if (glyphMap.TryGetValue(character.glyphIndex, out Glyph glyph))
                {
                    return glyph;
                }
            }
            return null;
        }

        // 2. Calibrate base tone marks (0x0E48 - 0x0E4C) to sit lower when directly on consonants (Fix "สระลอยมาก")
        // Normal consonant top is at ~29.3. Height of tone mark is ~13.
        // When directly on consonant, BearingY should be ~41.5..42.5 so it sits right above consonant (no float!).
        CalibrateBaseGlyph(font, charMap, glyphMap, ThaiFontAdjuster.MAI_EK, -11.39f, 41.5f);
        CalibrateBaseGlyph(font, charMap, glyphMap, ThaiFontAdjuster.MAI_THO, -26.23f, 42.0f);
        CalibrateBaseGlyph(font, charMap, glyphMap, ThaiFontAdjuster.MAI_TRI, -20.0f, 42.0f);
        CalibrateBaseGlyph(font, charMap, glyphMap, ThaiFontAdjuster.MAI_CHATTAWA, -20.0f, 42.0f);
        CalibrateBaseGlyph(font, charMap, glyphMap, ThaiFontAdjuster.THANTHAKHAT, -20.61f, 43.0f);

        // 3. Add PUA Characters & Glyphs
        uint nextGlyphIndex = Math.Max(maxGlyphIndex + 1, 3000);

        // ── Tier 3 Upper Tone Marks (วางซ้อนเหนือสระบน: BearingY = 59.0..61.5) ──
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

        // ── Left-shifted Upper Vowels (หลบหาง ป ฝ ฟ ฬ: BearingX เลื่อนซ้าย -10) ──
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

        // ── Left-shifted Tone Marks (บน ป ฝ ฟ ฬ แบบไม่มีสระบน: BearingX -10, BearingY 46.5) ──
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

        // ── Tier 3 Left-shifted Tone Marks (บน ป ฝ ฟ ฬ ที่มีสระบน: BearingX -10, BearingY 59.0..61.5) ──
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

        // ── Lower Vowels Shifted Down (หลบหาง ฎ ฏ: BearingY -6.0) ──
        AddOrUpdatePUA(font, charMap, glyphMap, ref nextGlyphIndex,
            ThaiFontAdjuster.PUA_SARA_U_LOWER, ThaiFontAdjuster.SARA_U, -17.3f, -10.4f);
        AddOrUpdatePUA(font, charMap, glyphMap, ref nextGlyphIndex,
            ThaiFontAdjuster.PUA_SARA_UU_LOWER, ThaiFontAdjuster.SARA_UU, -24.5f, -10.4f);
        AddOrUpdatePUA(font, charMap, glyphMap, ref nextGlyphIndex,
            ThaiFontAdjuster.PUA_PHINTHU_LOWER, ThaiFontAdjuster.PHINTHU, -15.0f, -10.4f);

        // ── Tailless Consonants (ญ ฐ) ──
        AddOrUpdatePUA(font, charMap, glyphMap, ref nextGlyphIndex,
            ThaiFontAdjuster.PUA_YO_YING_TAILLESS, ThaiFontAdjuster.YO_YING, 0.8f, 29.3f, false);
        AddOrUpdatePUA(font, charMap, glyphMap, ref nextGlyphIndex,
            ThaiFontAdjuster.PUA_THO_THAN_TAILLESS, ThaiFontAdjuster.THO_THAN, 1.7f, 29.3f, false);

        // Rebuild internal tables
        font.ReadFontAssetDefinition();
        EditorUtility.SetDirty(font);
        Debug.Log($"[ConfigureThaiFontAsset] Updated {font.name}: {font.characterTable.Count} chars, {font.glyphTable.Count} glyphs.");
    }

    private static void CalibrateBaseGlyph(TMP_FontAsset font, Dictionary<uint, TMP_Character> charMap,
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

        if (!charMap.TryGetValue(baseUnicode, out TMP_Character baseCharacter))
        {
            Debug.LogWarning($"[ConfigureThaiFontAsset] Base char U+{(int)baseChar:X4} not found in font!");
            return;
        }

        if (!glyphMap.TryGetValue(baseCharacter.glyphIndex, out Glyph baseGlyph))
        {
            Debug.LogWarning($"[ConfigureThaiFontAsset] Base glyph {baseCharacter.glyphIndex} not found in font!");
            return;
        }

        float advance = zeroAdvance ? 0 : baseGlyph.metrics.horizontalAdvance;
        GlyphMetrics newMetrics = new GlyphMetrics(
            baseGlyph.metrics.width,
            baseGlyph.metrics.height,
            targetBX,
            targetBY,
            advance
        );

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
}
#endif

