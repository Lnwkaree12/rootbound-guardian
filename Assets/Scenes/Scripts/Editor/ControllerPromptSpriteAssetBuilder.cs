#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using TMPro;
using UnityEditor;
using UnityEditor.U2D.Sprites;
using UnityEngine;

[InitializeOnLoad]
internal static class ControllerPromptSpriteAssetBuilder
{
    private const string AssetRoot = "Assets/Scenes/UI New/Botton";
    private const string AtlasPath = "Assets/Resources/Sprite Assets/ControllerPromptIcons.png";
    private const string SpriteAssetPath = "Assets/Resources/Sprite Assets/ControllerPromptIcons.asset";
    private const string SpriteAssetMenuPath = "Assets/Create/TextMeshPro/Sprite Asset";
    private const string GeneratorMenuPath = "Tools/SproutScout/Generate Controller Prompt Sprite Asset";

    private static readonly string[] IconNames =
    {
        "analogL",
        "analogL4",
        "analogLUp",
        "bottonArrow5",
        "bottonArrowDown",
        "bottonArrowLeft",
        "bottonArrowRight",
        "bottonArrowUp",
        "buttonJoyA",
        "buttonJoyB",
        "buttonJoyRB",
        "buttonJoyY"
    };

    static ControllerPromptSpriteAssetBuilder()
    {
        EditorApplication.delayCall += EnsureSpriteAsset;
    }

    [MenuItem(GeneratorMenuPath)]
    private static void GenerateSpriteAsset()
    {
        EnsureSpriteAsset();
    }

    private static void EnsureSpriteAsset()
    {
        if (AssetDatabase.LoadAssetAtPath<TMP_SpriteAsset>(SpriteAssetPath) != null)
        {
            return;
        }

        Directory.CreateDirectory(Path.GetDirectoryName(AtlasPath));
        if (!File.Exists(AtlasPath))
        {
            CreateAtlasTexture();
        }

        ConfigureAtlasSprites(ReadPackedRects());

        Texture2D atlas = AssetDatabase.LoadAssetAtPath<Texture2D>(AtlasPath);
        if (atlas == null)
        {
            throw new InvalidOperationException($"Unable to load controller prompt atlas at {AtlasPath}.");
        }

        Selection.objects = new UnityEngine.Object[] { atlas };
        if (!EditorApplication.ExecuteMenuItem(SpriteAssetMenuPath))
        {
            throw new InvalidOperationException("TextMeshPro could not create the controller prompt sprite asset.");
        }

        AssetDatabase.SaveAssets();
        if (AssetDatabase.LoadAssetAtPath<TMP_SpriteAsset>(SpriteAssetPath) == null)
        {
            throw new InvalidOperationException($"TextMeshPro did not create the expected sprite asset at {SpriteAssetPath}.");
        }

        Debug.Log("[ControllerPromptSpriteAssetBuilder] Created controller prompt icons.");
    }

    private static void CreateAtlasTexture()
    {
        List<Texture2D> sourceTextures = LoadSourceTextures();
        Texture2D atlas = new Texture2D(2, 2, TextureFormat.RGBA32, false);

        try
        {
            Rect[] packedRects = atlas.PackTextures(sourceTextures.ToArray(), 4, 4096, false);
            if (packedRects.Length != IconNames.Length)
            {
                throw new InvalidOperationException("The controller prompt atlas did not pack every icon.");
            }

            string absoluteAtlasPath = Path.Combine(Directory.GetCurrentDirectory(), AtlasPath);
            File.WriteAllBytes(absoluteAtlasPath, atlas.EncodeToPNG());
            AssetDatabase.ImportAsset(AtlasPath, ImportAssetOptions.ForceSynchronousImport);
        }
        finally
        {
            DestroyTextures(sourceTextures);
            UnityEngine.Object.DestroyImmediate(atlas);
        }
    }

    private static Rect[] ReadPackedRects()
    {
        List<Texture2D> sourceTextures = LoadSourceTextures();
        Texture2D packedAtlas = new Texture2D(2, 2, TextureFormat.RGBA32, false);

        try
        {
            return packedAtlas.PackTextures(sourceTextures.ToArray(), 4, 4096, false);
        }
        finally
        {
            DestroyTextures(sourceTextures);
            UnityEngine.Object.DestroyImmediate(packedAtlas);
        }
    }

    private static List<Texture2D> LoadSourceTextures()
    {
        List<Texture2D> sourceTextures = new List<Texture2D>(IconNames.Length);
        try
        {
            foreach (string iconName in IconNames)
            {
                string path = Path.Combine(Directory.GetCurrentDirectory(), AssetRoot, iconName + ".png");
                if (!File.Exists(path))
                {
                    throw new FileNotFoundException($"Controller prompt icon not found: {path}", path);
                }

                Texture2D sourceTexture = new Texture2D(2, 2, TextureFormat.RGBA32, false);
                if (!sourceTexture.LoadImage(File.ReadAllBytes(path)))
                {
                    UnityEngine.Object.DestroyImmediate(sourceTexture);
                    throw new InvalidDataException($"Unable to decode controller prompt icon: {path}");
                }

                sourceTextures.Add(sourceTexture);
            }

            return sourceTextures;
        }
        catch
        {
            DestroyTextures(sourceTextures);
            throw;
        }
    }

    private static void DestroyTextures(List<Texture2D> textures)
    {
        foreach (Texture2D texture in textures)
        {
            UnityEngine.Object.DestroyImmediate(texture);
        }
    }

    private static void ConfigureAtlasSprites(Rect[] packedRects)
    {
        TextureImporter importer = AssetImporter.GetAtPath(AtlasPath) as TextureImporter;
        if (importer == null)
        {
            throw new InvalidOperationException($"Unable to configure controller prompt atlas at {AtlasPath}.");
        }

        importer.textureType = TextureImporterType.Sprite;
        importer.spriteImportMode = SpriteImportMode.Multiple;
        importer.mipmapEnabled = false;
        importer.alphaIsTransparency = true;
        importer.SaveAndReimport();

        importer = AssetImporter.GetAtPath(AtlasPath) as TextureImporter;
        if (importer == null)
        {
            throw new InvalidOperationException($"Unable to reload controller prompt atlas importer at {AtlasPath}.");
        }

        SpriteDataProviderFactories factories = new SpriteDataProviderFactories();
        factories.Init();
        ISpriteEditorDataProvider dataProvider = factories.GetSpriteEditorDataProviderFromObject(importer);
        if (dataProvider == null)
        {
            throw new InvalidOperationException("Unable to configure controller prompt atlas sprite data.");
        }

        dataProvider.InitSpriteEditorDataProvider();
        Texture2D atlas = AssetDatabase.LoadAssetAtPath<Texture2D>(AtlasPath);
        if (atlas == null)
        {
            throw new InvalidOperationException($"Unable to load controller prompt atlas at {AtlasPath}.");
        }

        SpriteRect[] spriteRects = new SpriteRect[IconNames.Length];
        for (int i = 0; i < IconNames.Length; i++)
        {
            Rect normalizedRect = packedRects[i];
            spriteRects[i] = new SpriteRect
            {
                name = IconNames[i],
                rect = new Rect(
                    normalizedRect.x * atlas.width,
                    normalizedRect.y * atlas.height,
                    normalizedRect.width * atlas.width,
                    normalizedRect.height * atlas.height),
                alignment = SpriteAlignment.Center,
                pivot = new Vector2(0.5f, 0.5f),
                spriteID = GUID.Generate()
            };
        }

        dataProvider.SetSpriteRects(spriteRects);
        dataProvider.Apply();
        importer.SaveAndReimport();
    }
}
#endif
