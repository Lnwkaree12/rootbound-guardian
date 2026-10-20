#if UNITY_EDITOR
using System.IO;
using UnityEditor;
using UnityEngine;

public class GenerateStatusBarArt
{
    private const string ART_FOLDER = "Assets/Image/UI/";

    [MenuItem("Tools/Rootbound Guardian/Generate Fairytale Status Bar Art")]
    public static void GenerateAllArt()
    {
        if (!Directory.Exists(ART_FOLDER))
        {
            Directory.CreateDirectory(ART_FOLDER);
        }

        GenerateHPMedallion();
        GenerateOxygenMedallion();
        GenerateBarFrame();
        GenerateBarTrough();
        GenerateHPFill();
        GenerateOxygenFill();
        GenerateGlassGloss();

        AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);

        ConfigureSpriteImporter(ART_FOLDER + "Fairytale_HP_Medallion.png", new Vector4(0, 0, 0, 0));
        ConfigureSpriteImporter(ART_FOLDER + "Fairytale_Oxygen_Medallion.png", new Vector4(0, 0, 0, 0));
        ConfigureSpriteImporter(ART_FOLDER + "Fairytale_Bar_Frame.png", new Vector4(24, 16, 24, 16));
        ConfigureSpriteImporter(ART_FOLDER + "Fairytale_Bar_Trough.png", new Vector4(20, 14, 20, 14));
        ConfigureSpriteImporter(ART_FOLDER + "Fairytale_HP_Fill.png", new Vector4(16, 12, 16, 12));
        ConfigureSpriteImporter(ART_FOLDER + "Fairytale_Oxygen_Fill.png", new Vector4(16, 12, 16, 12));
        ConfigureSpriteImporter(ART_FOLDER + "Fairytale_Bar_GlassGloss.png", new Vector4(16, 12, 16, 12));

        AssetDatabase.Refresh();
        Debug.Log("[GenerateStatusBarArt] All fairytale status bar sprites generated and configured successfully!");
    }

    private static void ConfigureSpriteImporter(string path, Vector4 border)
    {
        TextureImporter importer = AssetImporter.GetAtPath(path) as TextureImporter;
        if (importer != null)
        {
            importer.textureType = TextureImporterType.Sprite;
            importer.spriteImportMode = SpriteImportMode.Single;
            importer.alphaIsTransparency = true;
            importer.mipmapEnabled = false;
            importer.filterMode = FilterMode.Bilinear;
            importer.spriteBorder = border;
            importer.SaveAndReimport();
        }
    }

    #region Medallion Generation

    private static void GenerateHPMedallion()
    {
        int size = 256;
        Texture2D tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
        Color32[] pixels = new Color32[size * size];

        Vector2 center = new Vector2(size / 2f, size / 2f);
        float outerR = 120f;
        float woodR = 98f;
        float goldR = 84f;

        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                float dx = x - center.x;
                float dy = y - center.y;
                float dist = Mathf.Sqrt(dx * dx + dy * dy);
                float angle = Mathf.Atan2(dy, dx);

                Color c = Color.clear;

                if (dist < outerR + 2f)
                {
                    // 1. Soft Outer Drop Shadow
                    if (dist > outerR - 4f)
                    {
                        float alpha = Mathf.Clamp01((outerR + 2f - dist) / 6f) * 0.45f;
                        c = new Color(0.04f, 0.02f, 0.01f, alpha);
                    }
                    // 2. Carved Oak Wood Ring
                    else if (dist >= woodR)
                    {
                        float t = (dist - woodR) / (outerR - woodR);
                        float grain = Mathf.Sin(angle * 12f) * 0.04f + Mathf.Sin(dist * 0.5f) * 0.03f;
                        float bevel = Mathf.Sin(t * Mathf.PI);

                        Color woodDark = new Color(0.18f, 0.11f, 0.06f);
                        Color woodLight = new Color(0.48f, 0.32f, 0.20f);
                        Color woodBase = Color.Lerp(woodDark, woodLight, t + grain);
                        c = Color.Lerp(woodBase, woodLight, bevel * 0.4f);

                        // Rivets / Notches at 4 corners
                        float notchDist = Mathf.Abs(Mathf.Sin(angle * 2f));
                        if (notchDist < 0.08f && dist > woodR + 4f && dist < outerR - 4f)
                        {
                            c = Color.Lerp(c, new Color(0.10f, 0.06f, 0.03f), 0.5f);
                        }
                    }
                    // 3. Golden Filigree Ring
                    else if (dist >= goldR)
                    {
                        float t = (dist - goldR) / (woodR - goldR);
                        float bevel = Mathf.Sin(t * Mathf.PI);
                        Color goldDark = new Color(0.55f, 0.38f, 0.12f);
                        Color goldMid = new Color(0.92f, 0.72f, 0.25f);
                        Color goldLight = new Color(1.0f, 0.94f, 0.60f);

                        Color goldBase = Color.Lerp(goldDark, goldMid, t);
                        c = Color.Lerp(goldBase, goldLight, bevel * 0.65f);
                    }
                    // 4. Inner Medallion Field (Deep Mahogany-Charcoal Well)
                    else
                    {
                        float innerT = dist / goldR;
                        Color wellDark = new Color(0.11f, 0.06f, 0.04f);
                        Color wellMid = new Color(0.19f, 0.11f, 0.08f);
                        c = Color.Lerp(wellMid, wellDark, innerT);

                        // 5. Flanking Emerald Laurel Leaves around bottom
                        Color leafColor = GetLeafColor(x - center.x, y - center.y);
                        if (leafColor.a > 0f)
                        {
                            c = Color.Lerp(c, leafColor, leafColor.a);
                        }

                        // 6. Draw Crisp Stylized Heart
                        Vector2 hPos = new Vector2(x - center.x, y - (center.y + 2f));
                        Color heartCol = GetClassicHeartColor(hPos.x, hPos.y, 48f);
                        if (heartCol.a > 0f)
                        {
                            c = Color.Lerp(c, heartCol, heartCol.a);
                        }

                        // 7. Golden Star Sparkle on top-right of heart
                        Vector2 sparkleCenter = new Vector2(center.x + 24f, center.y + 36f);
                        float sDist = Vector2.Distance(new Vector2(x, y), sparkleCenter);
                        if (sDist < 12f)
                        {
                            float sT = Mathf.Clamp01(1f - (sDist / 12f));
                            float cross = Mathf.Max(
                                1f - Mathf.Abs(x - sparkleCenter.x) / 3f,
                                1f - Mathf.Abs(y - sparkleCenter.y) / 3f
                            );
                            c = Color.Lerp(c, new Color(1.0f, 0.98f, 0.75f), Mathf.Clamp01(sT * cross * 1.8f));
                        }
                    }
                }

                pixels[y * size + x] = c;
            }
        }

        tex.SetPixels32(pixels);
        tex.Apply();
        File.WriteAllBytes(ART_FOLDER + "Fairytale_HP_Medallion.png", tex.EncodeToPNG());
        Object.DestroyImmediate(tex);
    }

    private static void GenerateOxygenMedallion()
    {
        int size = 256;
        Texture2D tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
        Color32[] pixels = new Color32[size * size];

        Vector2 center = new Vector2(size / 2f, size / 2f);
        float outerR = 120f;
        float woodR = 98f;
        float goldR = 84f;

        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                float dx = x - center.x;
                float dy = y - center.y;
                float dist = Mathf.Sqrt(dx * dx + dy * dy);
                float angle = Mathf.Atan2(dy, dx);

                Color c = Color.clear;

                if (dist < outerR + 2f)
                {
                    // 1. Soft Outer Drop Shadow
                    if (dist > outerR - 4f)
                    {
                        float alpha = Mathf.Clamp01((outerR + 2f - dist) / 6f) * 0.45f;
                        c = new Color(0.04f, 0.02f, 0.01f, alpha);
                    }
                    // 2. Carved Oak Wood Ring
                    else if (dist >= woodR)
                    {
                        float t = (dist - woodR) / (outerR - woodR);
                        float grain = Mathf.Sin(angle * 12f) * 0.04f + Mathf.Sin(dist * 0.5f) * 0.03f;
                        float bevel = Mathf.Sin(t * Mathf.PI);

                        Color woodDark = new Color(0.18f, 0.11f, 0.06f);
                        Color woodLight = new Color(0.48f, 0.32f, 0.20f);
                        Color woodBase = Color.Lerp(woodDark, woodLight, t + grain);
                        c = Color.Lerp(woodBase, woodLight, bevel * 0.4f);

                        // Rivets / Notches
                        float notchDist = Mathf.Abs(Mathf.Sin(angle * 2f));
                        if (notchDist < 0.08f && dist > woodR + 4f && dist < outerR - 4f)
                        {
                            c = Color.Lerp(c, new Color(0.10f, 0.06f, 0.03f), 0.5f);
                        }
                    }
                    // 3. Golden Filigree Ring with ethereal touch
                    else if (dist >= goldR)
                    {
                        float t = (dist - goldR) / (woodR - goldR);
                        float bevel = Mathf.Sin(t * Mathf.PI);
                        Color goldDark = new Color(0.45f, 0.36f, 0.18f);
                        Color goldMid = new Color(0.88f, 0.74f, 0.30f);
                        Color goldLight = new Color(0.98f, 0.94f, 0.65f);

                        Color goldBase = Color.Lerp(goldDark, goldMid, t);
                        c = Color.Lerp(goldBase, goldLight, bevel * 0.65f);
                    }
                    // 4. Inner Medallion Field (Deep Twilight Abyss Well)
                    else
                    {
                        float innerT = dist / goldR;
                        Color wellDark = new Color(0.04f, 0.08f, 0.12f);
                        Color wellMid = new Color(0.08f, 0.16f, 0.24f);
                        c = Color.Lerp(wellMid, wellDark, innerT);

                        // 5. Flanking Emerald Laurel Leaves around bottom
                        Color leafColor = GetLeafColor(x - center.x, y - center.y);
                        if (leafColor.a > 0f)
                        {
                            c = Color.Lerp(c, leafColor, leafColor.a);
                        }

                        // 6. Sacred Air / Oxygen Prana Bubble
                        Vector2 bPos = new Vector2(x - center.x, y - (center.y + 2f));
                        Color bubbleCol = GetPranaBubbleColor(bPos.x, bPos.y, 44f);
                        if (bubbleCol.a > 0f)
                        {
                            c = Color.Lerp(c, bubbleCol, bubbleCol.a);
                        }

                        // 7. Ambient Mini Sparkle / Floating Mote
                        Vector2 sparkleCenter = new Vector2(center.x + 26f, center.y + 36f);
                        float sDist = Vector2.Distance(new Vector2(x, y), sparkleCenter);
                        if (sDist < 12f)
                        {
                            float sT = Mathf.Clamp01(1f - (sDist / 12f));
                            float cross = Mathf.Max(
                                1f - Mathf.Abs(x - sparkleCenter.x) / 3f,
                                1f - Mathf.Abs(y - sparkleCenter.y) / 3f
                            );
                            c = Color.Lerp(c, new Color(0.85f, 0.98f, 1.0f), Mathf.Clamp01(sT * cross * 1.8f));
                        }
                    }
                }

                pixels[y * size + x] = c;
            }
        }

        tex.SetPixels32(pixels);
        tex.Apply();
        File.WriteAllBytes(ART_FOLDER + "Fairytale_Oxygen_Medallion.png", tex.EncodeToPNG());
        Object.DestroyImmediate(tex);
    }

    private static Color GetClassicHeartColor(float x, float y, float scale)
    {
        // Textbook Game Heart Geometry:
        // Left lobe center (-17, 10), Right lobe center (+17, 10), radius = 18
        // Bottom tip at (0, -36)
        // Scaled to 'scale' (nominal 48)
        float s = scale / 48f;
        float hx = x / s;
        float hy = (y - 2f) / s; // center vertically

        float lobeR = 18f;
        float lobeX = 17f;
        float lobeY = 8f;
        float tipY = -34f;

        // 1. Top Lobes
        float dLeft = Mathf.Sqrt((hx + lobeX) * (hx + lobeX) + (hy - lobeY) * (hy - lobeY));
        float dRight = Mathf.Sqrt((hx - lobeX) * (hx - lobeX) + (hy - lobeY) * (hy - lobeY));
        bool inLobes = (dLeft <= lobeR) || (dRight <= lobeR);

        // 2. Bottom V-Wedge
        bool inV = false;
        if (hy >= tipY && hy <= lobeY)
        {
            float maxHalfWidth = lobeX + Mathf.Sqrt(Mathf.Max(0f, lobeR * lobeR - (hy - lobeY) * (hy - lobeY)));
            float vWidth = ((hy - tipY) / (lobeY - tipY)) * (lobeX + lobeR * 0.96f);
            if (Mathf.Abs(hx) <= Mathf.Max(vWidth, maxHalfWidth * 0.95f))
            {
                inV = true;
            }
        }

        if (!inLobes && !inV)
        {
            // Subtle golden halo
            float glowDist = Mathf.Min(dLeft - lobeR, dRight - lobeR);
            if (glowDist < 5f && glowDist > 0f)
            {
                float a = (1f - glowDist / 5f) * 0.35f;
                return new Color(1.0f, 0.85f, 0.35f, a);
            }
            return Color.clear;
        }

        // Inside Heart!
        // 3D Ruby Shading:
        float vertT = Mathf.Clamp01((hy - tipY) / (lobeY + lobeR - tipY));
        Color rubyBright = new Color(1.0f, 0.20f, 0.35f);
        Color rubyMid = new Color(0.85f, 0.08f, 0.20f);
        Color rubyDark = new Color(0.42f, 0.02f, 0.08f);

        Color coreCol = Color.Lerp(rubyDark, rubyMid, vertT * 1.15f);
        if (vertT > 0.5f) coreCol = Color.Lerp(coreCol, rubyBright, (vertT - 0.5f) / 0.5f);

        // Crisp specular highlight on left lobe
        float specDist = Vector2.Distance(new Vector2(hx, hy), new Vector2(-lobeX + 3f, lobeY + 5f));
        if (specDist < 10f)
        {
            float spec = Mathf.Pow(1f - (specDist / 10f), 2f);
            coreCol = Color.Lerp(coreCol, new Color(1.0f, 0.92f, 0.95f), spec * 0.9f);
        }

        // Golden rim bevel around outer perimeter
        bool onEdge = false;
        if (dLeft > lobeR - 3.2f || dRight > lobeR - 3.2f) onEdge = true;
        if (hy <= lobeY && Mathf.Abs(hx) > (((hy - tipY) / (lobeY - tipY)) * (lobeX + lobeR * 0.96f)) - 3.2f) onEdge = true;

        if (onEdge)
        {
            coreCol = Color.Lerp(coreCol, new Color(1.0f, 0.90f, 0.45f), 0.85f);
        }

        return new Color(coreCol.r, coreCol.g, coreCol.b, 1f);
    }

    private static Color GetPranaBubbleColor(float x, float y, float radius)
    {
        float dist = Mathf.Sqrt(x * x + y * y);
        if (dist > radius)
        {
            // Outer cyan aura
            if (dist < radius + 8f)
            {
                float a = (1f - (dist - radius) / 8f) * 0.5f;
                return new Color(0.35f, 0.90f, 1.0f, a);
            }
            return Color.clear;
        }

        float t = dist / radius;

        // Spherical 3D shading
        float sphereZ = Mathf.Sqrt(Mathf.Max(0f, 1f - t * t));
        Color deepCyan = new Color(0.04f, 0.32f, 0.58f);
        Color midAqua = new Color(0.12f, 0.68f, 0.95f);
        Color brightIce = new Color(0.55f, 0.94f, 1.0f);

        Color bubbleCol = Color.Lerp(deepCyan, midAqua, sphereZ);

        // Luminous glowing rim
        if (t > 0.68f)
        {
            float rimT = (t - 0.68f) / 0.32f;
            bubbleCol = Color.Lerp(bubbleCol, brightIce, rimT * 0.9f);
        }

        // Crisp specular crescent gloss on top-left
        float specDist = Vector2.Distance(new Vector2(x, y), new Vector2(-radius * 0.38f, radius * 0.40f));
        if (specDist < radius * 0.42f)
        {
            float spec = Mathf.Pow(1f - (specDist / (radius * 0.42f)), 2.5f);
            bubbleCol = Color.Lerp(bubbleCol, Color.white, spec * 0.95f);
        }

        // Secondary small reflection on bottom-right
        float spec2Dist = Vector2.Distance(new Vector2(x, y), new Vector2(radius * 0.40f, -radius * 0.35f));
        if (spec2Dist < radius * 0.22f)
        {
            float spec2 = Mathf.Pow(1f - (spec2Dist / (radius * 0.22f)), 2f);
            bubbleCol = Color.Lerp(bubbleCol, new Color(0.85f, 0.98f, 1f), spec2 * 0.65f);
        }

        // Center sprout leaf silhouette glowing inside
        Vector2 sproutPos = new Vector2(x, y + 2f);
        float sDist = Mathf.Sqrt(sproutPos.x * sproutPos.x + sproutPos.y * sproutPos.y);
        if (sDist < radius * 0.38f)
        {
            float sT = 1f - (sDist / (radius * 0.38f));
            Color sproutGlow = Color.Lerp(new Color(0.2f, 0.95f, 0.55f), new Color(0.7f, 1.0f, 0.85f), sT);
            bubbleCol = Color.Lerp(bubbleCol, sproutGlow, sT * 0.75f);
        }

        // Golden rim line
        if (dist > radius - 3.5f)
        {
            bubbleCol = Color.Lerp(bubbleCol, new Color(1.0f, 0.90f, 0.55f), 0.7f);
        }

        return new Color(bubbleCol.r, bubbleCol.g, bubbleCol.b, 1f);
    }

    private static Color GetLeafColor(float x, float y)
    {
        // Stylized pointed fairytale leaves curling up on left & right sides
        // Left leaf (tilted up-left)
        Vector2 lPos = new Vector2(x + 46f, y + 26f);
        float lRotX = (lPos.x * 0.707f - lPos.y * 0.707f) / 24f; // length 24
        float lRotY = (lPos.x * 0.707f + lPos.y * 0.707f) / 10f; // half-width 10
        bool inLeft = (lRotX >= -1f && lRotX <= 1f && Mathf.Abs(lRotY) <= (1f - lRotX * lRotX));

        // Right leaf (tilted up-right)
        Vector2 rPos = new Vector2(x - 46f, y + 26f);
        float rRotX = (-rPos.x * 0.707f - rPos.y * 0.707f) / 24f;
        float rRotY = (rPos.x * 0.707f - rPos.y * 0.707f) / 10f;
        bool inRight = (rRotX >= -1f && rRotX <= 1f && Mathf.Abs(rRotY) <= (1f - rRotX * rRotX));

        if (inLeft || inRight)
        {
            float localY = inLeft ? Mathf.Abs(lRotY) : Mathf.Abs(rRotY);
            float maxW = inLeft ? (1f - lRotX * lRotX) : (1f - rRotX * rRotX);

            Color leafBase = new Color(0.16f, 0.76f, 0.36f);
            Color leafSpine = new Color(0.70f, 1.0f, 0.80f);
            Color leafGold = new Color(1.0f, 0.90f, 0.45f);

            // Gold edge rim
            if (localY > maxW - 0.28f)
            {
                return new Color(leafGold.r, leafGold.g, leafGold.b, 1f);
            }
            // Spine highlight
            if (localY < 0.22f)
            {
                return new Color(leafSpine.r, leafSpine.g, leafSpine.b, 1f);
            }
            return new Color(leafBase.r, leafBase.g, leafBase.b, 1f);
        }

        return Color.clear;
    }

    #endregion

    #region Bar Frame & Trough

    private static void GenerateBarFrame()
    {
        int w = 512;
        int h = 64;
        Texture2D tex = new Texture2D(w, h, TextureFormat.RGBA32, false);
        Color32[] pixels = new Color32[w * h];

        float cornerR = 20f;

        for (int y = 0; y < h; y++)
        {
            for (int x = 0; x < w; x++)
            {
                // Distance to capsule border
                float dist = GetRoundedRectDist(x, y, w, h, cornerR);
                Color c = Color.clear;

                // Frame outer bound (dist <= 0)
                if (dist <= 0f)
                {
                    float innerDist = -dist; // how deep inside we are
                    float frameThickness = 9f;

                    if (innerDist <= frameThickness)
                    {
                        float t = innerDist / frameThickness;

                        // Outer wood layer (0 to 0.5)
                        if (t < 0.55f)
                        {
                            float woodT = t / 0.55f;
                            Color woodEdge = new Color(0.14f, 0.09f, 0.05f);
                            Color woodMid = new Color(0.32f, 0.20f, 0.12f);
                            Color woodHighlight = new Color(0.48f, 0.32f, 0.20f);

                            float bevel = Mathf.Sin(woodT * Mathf.PI);
                            c = Color.Lerp(woodEdge, woodMid, woodT);
                            c = Color.Lerp(c, woodHighlight, bevel * 0.4f);
                        }
                        // Inner gold filigree line (0.55 to 1.0)
                        else
                        {
                            float goldT = (t - 0.55f) / 0.45f;
                            Color goldDark = new Color(0.55f, 0.40f, 0.15f);
                            Color goldLight = new Color(0.98f, 0.85f, 0.40f);
                            c = Color.Lerp(goldLight, goldDark, goldT);
                        }
                    }
                    else
                    {
                        // Inside is hollow so the background trough & fill are visible
                        c = Color.clear;
                    }
                }
                else if (dist < 3f)
                {
                    // Outer subtle shadow
                    float alpha = (1f - dist / 3f) * 0.35f;
                    c = new Color(0.04f, 0.02f, 0.01f, alpha);
                }

                pixels[y * w + x] = c;
            }
        }

        tex.SetPixels32(pixels);
        tex.Apply();
        File.WriteAllBytes(ART_FOLDER + "Fairytale_Bar_Frame.png", tex.EncodeToPNG());
        Object.DestroyImmediate(tex);
    }

    private static void GenerateBarTrough()
    {
        int w = 512;
        int h = 64;
        Texture2D tex = new Texture2D(w, h, TextureFormat.RGBA32, false);
        Color32[] pixels = new Color32[w * h];

        float cornerR = 18f;

        for (int y = 0; y < h; y++)
        {
            for (int x = 0; x < w; x++)
            {
                float dist = GetRoundedRectDist(x, y, w, h, cornerR);
                Color c = Color.clear;

                if (dist <= 0f)
                {
                    float vertT = (float)y / h;
                    // Deep mahogany/wood-bark well with top inner shadow
                    Color wellTop = new Color(0.06f, 0.04f, 0.03f, 0.95f);
                    Color wellMid = new Color(0.12f, 0.08f, 0.06f, 0.95f);
                    Color wellBottom = new Color(0.18f, 0.12f, 0.09f, 0.95f);

                    if (vertT > 0.6f)
                        c = Color.Lerp(wellMid, wellTop, (vertT - 0.6f) / 0.4f);
                    else
                        c = Color.Lerp(wellBottom, wellMid, vertT / 0.6f);
                }

                pixels[y * w + x] = c;
            }
        }

        tex.SetPixels32(pixels);
        tex.Apply();
        File.WriteAllBytes(ART_FOLDER + "Fairytale_Bar_Trough.png", tex.EncodeToPNG());
        Object.DestroyImmediate(tex);
    }

    #endregion

    #region Fills & Gloss

    private static void GenerateHPFill()
    {
        int w = 512;
        int h = 64;
        Texture2D tex = new Texture2D(w, h, TextureFormat.RGBA32, false);
        Color32[] pixels = new Color32[w * h];

        float cornerR = 16f;

        for (int y = 0; y < h; y++)
        {
            for (int x = 0; x < w; x++)
            {
                float dist = GetRoundedRectDist(x, y, w, h, cornerR);
                Color c = Color.clear;

                if (dist <= 0f)
                {
                    float vertT = (float)y / h;

                    // Rich Ruby Crimson Vitality gradient
                    Color topHighlight = new Color(1.0f, 0.65f, 0.75f); // Rose gloss
                    Color coreBright = new Color(1.0f, 0.18f, 0.32f);   // Bright ruby
                    Color coreMid = new Color(0.82f, 0.08f, 0.22f);      // Deep crimson
                    Color bottomShadow = new Color(0.48f, 0.02f, 0.10f); // Dark wine

                    if (vertT > 0.75f)
                    {
                        float t = (vertT - 0.75f) / 0.25f;
                        c = Color.Lerp(coreBright, topHighlight, t);
                    }
                    else if (vertT > 0.3f)
                    {
                        float t = (vertT - 0.3f) / 0.45f;
                        c = Color.Lerp(coreMid, coreBright, t);
                    }
                    else
                    {
                        float t = vertT / 0.3f;
                        c = Color.Lerp(bottomShadow, coreMid, t);
                    }
                }

                pixels[y * w + x] = c;
            }
        }

        tex.SetPixels32(pixels);
        tex.Apply();
        File.WriteAllBytes(ART_FOLDER + "Fairytale_HP_Fill.png", tex.EncodeToPNG());
        Object.DestroyImmediate(tex);
    }

    private static void GenerateOxygenFill()
    {
        int w = 512;
        int h = 64;
        Texture2D tex = new Texture2D(w, h, TextureFormat.RGBA32, false);
        Color32[] pixels = new Color32[w * h];

        float cornerR = 16f;

        for (int y = 0; y < h; y++)
        {
            for (int x = 0; x < w; x++)
            {
                float dist = GetRoundedRectDist(x, y, w, h, cornerR);
                Color c = Color.clear;

                if (dist <= 0f)
                {
                    float vertT = (float)y / h;

                    // Luminous Aqua-Cyan Prana Breeze gradient
                    Color topHighlight = new Color(0.85f, 0.98f, 1.0f);  // Ice-cyan gloss
                    Color coreBright = new Color(0.20f, 0.85f, 1.0f);   // Radiant cyan
                    Color coreMid = new Color(0.08f, 0.58f, 0.90f);     // Pure azure
                    Color bottomShadow = new Color(0.03f, 0.28f, 0.52f);// Deep sapphire

                    if (vertT > 0.75f)
                    {
                        float t = (vertT - 0.75f) / 0.25f;
                        c = Color.Lerp(coreBright, topHighlight, t);
                    }
                    else if (vertT > 0.3f)
                    {
                        float t = (vertT - 0.3f) / 0.45f;
                        c = Color.Lerp(coreMid, coreBright, t);
                    }
                    else
                    {
                        float t = vertT / 0.3f;
                        c = Color.Lerp(bottomShadow, coreMid, t);
                    }
                }

                pixels[y * w + x] = c;
            }
        }

        tex.SetPixels32(pixels);
        tex.Apply();
        File.WriteAllBytes(ART_FOLDER + "Fairytale_Oxygen_Fill.png", tex.EncodeToPNG());
        Object.DestroyImmediate(tex);
    }

    private static void GenerateGlassGloss()
    {
        int w = 512;
        int h = 64;
        Texture2D tex = new Texture2D(w, h, TextureFormat.RGBA32, false);
        Color32[] pixels = new Color32[w * h];

        float cornerR = 16f;

        for (int y = 0; y < h; y++)
        {
            for (int x = 0; x < w; x++)
            {
                float dist = GetRoundedRectDist(x, y, w, h, cornerR);
                Color c = Color.clear;

                if (dist <= 0f && y >= h * 0.45f)
                {
                    float vertT = (y - h * 0.45f) / (h * 0.55f);
                    float alpha = Mathf.Pow(vertT, 1.5f) * 0.28f;
                    c = new Color(1f, 1f, 1f, alpha);
                }

                pixels[y * w + x] = c;
            }
        }

        tex.SetPixels32(pixels);
        tex.Apply();
        File.WriteAllBytes(ART_FOLDER + "Fairytale_Bar_GlassGloss.png", tex.EncodeToPNG());
        Object.DestroyImmediate(tex);
    }

    #endregion

    private static float GetRoundedRectDist(float x, float y, float w, float h, float r)
    {
        float dx = Mathf.Max(r - x, 0f, x - (w - r));
        float dy = Mathf.Max(r - y, 0f, y - (h - r));
        if (dx > 0f && dy > 0f)
        {
            return Mathf.Sqrt(dx * dx + dy * dy) - r;
        }
        else
        {
            float insideX = Mathf.Min(x, w - x);
            float insideY = Mathf.Min(y, h - y);
            return -Mathf.Min(insideX, insideY);
        }
    }
}
#endif
