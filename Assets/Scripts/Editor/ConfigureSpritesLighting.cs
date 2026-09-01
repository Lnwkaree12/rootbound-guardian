#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;
using System.IO;

[InitializeOnLoad]
public class ConfigureSpritesLighting
{
    static ConfigureSpritesLighting()
    {
        EditorApplication.delayCall += EnsureAllSpritesAreLit;
    }

    [MenuItem("Tools/SproutScout/Configure Sprites Lighting and Shadows")]
    public static void ForceConfigureSpritesLighting()
    {
        ConfigureAll(true);
    }

    private static void EnsureAllSpritesAreLit()
    {
        if (EditorApplication.isPlaying || Application.isPlaying) return;
        ConfigureAll(false);
    }

    private static void ConfigureAll(bool force)
    {
        string[] searchFolders = new string[] { "Assets/Character", "Assets/Prefabs" };
        string[] guids = AssetDatabase.FindAssets("t:Prefab", searchFolders);
        
        int count = 0;
        foreach (string guid in guids)
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            if (prefab == null) continue;

            SpriteRenderer[] srs = prefab.GetComponentsInChildren<SpriteRenderer>(true);
            if (srs.Length == 0) continue;

            bool modified = false;
            foreach (SpriteRenderer sr in srs)
            {
                if (sr.sprite == null) continue;

                // Configure lighting for this SpriteRenderer
                ConfigureSpriteLighting(sr);
                modified = true;
            }

            if (modified)
            {
                EditorUtility.SetDirty(prefab);
                count++;
            }
        }

        if (count > 0)
        {
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log($"[ConfigureSpritesLighting] Successfully configured lighting and shadows for {count} prefabs.");
        }
    }

    public static void ConfigureSpriteLighting(SpriteRenderer sr)
    {
        if (sr == null || sr.sprite == null) return;

        Texture2D tex = sr.sprite.texture;
        if (tex == null) return;

        string materialsFolder = "Assets/Character/Materials";
        if (!AssetDatabase.IsValidFolder(materialsFolder))
        {
            if (!AssetDatabase.IsValidFolder("Assets/Character"))
            {
                AssetDatabase.CreateFolder("Assets", "Character");
            }
            AssetDatabase.CreateFolder("Assets/Character", "Materials");
        }

        string materialPath = $"{materialsFolder}/SpriteLit_{tex.name}.mat";
        Material mat = AssetDatabase.LoadAssetAtPath<Material>(materialPath);
        if (mat == null)
        {
            Shader litShader = Shader.Find("Universal Render Pipeline/Lit");
            if (litShader == null) litShader = Shader.Find("Standard");
            
            mat = new Material(litShader);
            AssetDatabase.CreateAsset(mat, materialPath);
        }

        mat.shader = Shader.Find("Universal Render Pipeline/Lit");
        mat.SetTexture("_BaseMap", tex);
        mat.SetFloat("_AlphaClip", 1f);
        mat.SetFloat("_Cutoff", 0.1f);
        mat.SetFloat("_Cull", 0f); // Double-sided
        mat.SetFloat("_ReceiveShadows", 1f);
        mat.SetFloat("_Smoothness", 0f);
        mat.SetFloat("_Metallic", 0f);
        mat.SetOverrideTag("RenderType", "TransparentCutout");
        mat.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.One);
        mat.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.Zero);
        mat.SetInt("_ZWrite", 1);
        mat.EnableKeyword("_ALPHATEST_ON");
        mat.renderQueue = (int)UnityEngine.Rendering.RenderQueue.AlphaTest;
        
        EditorUtility.SetDirty(mat);

        sr.sharedMaterial = mat;
        sr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.TwoSided;
        sr.receiveShadows = true;
    }
}
#endif
