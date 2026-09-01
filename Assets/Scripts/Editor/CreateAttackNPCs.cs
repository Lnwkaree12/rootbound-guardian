#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;
using TMPro;

[InitializeOnLoad]
public class CreateAttackNPCs
{
    static CreateAttackNPCs()
    {
        EditorApplication.delayCall += EnsureAttackNPCsExist;
    }

    [MenuItem("Tools/SproutScout/Create Attack NPC Prefabs")]
    public static void ForceCreateAttackNPCs()
    {
        CreateNPCs(true);
    }

    private static void EnsureAttackNPCsExist()
    {
        if (EditorApplication.isPlaying || Application.isPlaying) return;
        CreateNPCs(false);
    }

    private static void CreateNPCs(bool forceOverwrite)
    {
        string[] npcNames = new string[] 
        { 
            "TomatoAttack1", 
            "TomatoAttack2", 
            "TomatoAttack3", 
            "CarrotAttack", 
            "BeanAttack0.1" 
        };
        string folderPath = "Assets/Character/Attack";

        if (!AssetDatabase.IsValidFolder(folderPath))
        {
            Debug.LogWarning($"[CreateAttackNPCs] Directory {folderPath} not found! Cannot create prefabs.");
            return;
        }

        foreach (string npcName in npcNames)
        {
            string pngPath = $"{folderPath}/{npcName}.png";
            string prefabPath = $"{folderPath}/{npcName}.prefab";

            if (!forceOverwrite && AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath) != null)
            {
                continue;
            }

            // Ensure the texture is imported correctly as a Sprite
            ConfigureSpriteSettings(pngPath);

            Sprite sprite = AssetDatabase.LoadAssetAtPath<Sprite>(pngPath);
            if (sprite == null)
            {
                Debug.LogWarning($"[CreateAttackNPCs] Sprite not found at: {pngPath}. Skipping {npcName}.");
                continue;
            }

            Debug.Log($"[CreateAttackNPCs] Creating NPC Prefab for {npcName}...");

            // 1. Create temporary GameObject
            GameObject npcGO = new GameObject(npcName);
            
            // Add SpriteRenderer
            SpriteRenderer sr = npcGO.AddComponent<SpriteRenderer>();
            sr.sprite = sprite;
            ConfigureSpritesLighting.ConfigureSpriteLighting(sr);

            // Add Rigidbody (3D physics)
            Rigidbody rb = npcGO.AddComponent<Rigidbody>();
            rb.useGravity = true;
            rb.constraints = RigidbodyConstraints.FreezeRotation;

            // Calculate BoxCollider size and center dynamically based on Sprite properties
            BoxCollider bc = npcGO.AddComponent<BoxCollider>();
            float width = sprite.rect.width / sprite.pixelsPerUnit;
            float height = sprite.rect.height / sprite.pixelsPerUnit;
            float pivotYNormalized = sprite.pivot.y / sprite.rect.height;
            float pivotXNormalized = sprite.pivot.x / sprite.rect.width;

            float centerY = (0.5f - pivotYNormalized) * height;
            float centerX = (0.5f - pivotXNormalized) * width;

            bc.size = new Vector3(width, height, 0.2f);
            bc.center = new Vector3(centerX, centerY, 0f);

            // Add SlimeV2NPC behavior script (reused for jumping/dialogue)
            SlimeV2NPC npcScript = npcGO.AddComponent<SlimeV2NPC>();

            // Customize dialogue lines based on the type of sprite
            string[] customDialogue = GetCustomDialogue(npcName);

            // 2. Create Dialogue Canvas
            GameObject canvasGO = new GameObject("DialogueCanvas");
            canvasGO.transform.SetParent(npcGO.transform);
            
            float npcTopY = (1f - pivotYNormalized) * height;
            canvasGO.transform.localPosition = new Vector3(centerX, npcTopY + 0.25f, 0f);
            canvasGO.transform.localScale = new Vector3(0.003f, 0.003f, 0.003f);

            Canvas canvas = canvasGO.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.WorldSpace;

            RectTransform canvasRT = canvasGO.GetComponent<RectTransform>();
            canvasRT.sizeDelta = new Vector2(250f, 75f);

            // Add background bubble panel
            GameObject bgGO = new GameObject("BubbleBackground");
            bgGO.transform.SetParent(canvasGO.transform, false);
            UnityEngine.UI.Image bgImg = bgGO.AddComponent<UnityEngine.UI.Image>();
            
            Sprite panelSprite = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/Background.psd");
            if (panelSprite != null)
            {
                bgImg.sprite = panelSprite;
                bgImg.type = UnityEngine.UI.Image.Type.Sliced;
                bgImg.color = new Color(0f, 0f, 0f, 0.75f);
            }
            else
            {
                bgImg.color = new Color(0.1f, 0.1f, 0.1f, 0.85f);
            }

            RectTransform bgRT = bgGO.GetComponent<RectTransform>();
            bgRT.anchorMin = Vector2.zero;
            bgRT.anchorMax = Vector2.one;
            bgRT.sizeDelta = Vector2.zero;

            // Add Text (TMP)
            GameObject textGO = new GameObject("Text (TMP)");
            textGO.transform.SetParent(bgGO.transform, false);
            TextMeshProUGUI tmpText = textGO.AddComponent<TextMeshProUGUI>();
            tmpText.alignment = TextAlignmentOptions.Center;
            tmpText.fontSize = 15f;
            tmpText.color = Color.white;
            tmpText.text = customDialogue[0]; // Set initial text
            
            TMP_FontAsset fontAsset = GetDefaultTMPFont();
            if (fontAsset != null)
            {
                tmpText.font = fontAsset;
            }

            RectTransform textRT = textGO.GetComponent<RectTransform>();
            textRT.anchorMin = Vector2.zero;
            textRT.anchorMax = Vector2.one;
            textRT.sizeDelta = new Vector2(-15f, -10f); // Padding

            // 3. Connect references & assign dialogue lines via SerializedObject
            SerializedObject so = new SerializedObject(npcScript);
            so.FindProperty("spriteRenderer").objectReferenceValue = sr;
            so.FindProperty("dialogueCanvas").objectReferenceValue = canvas;
            so.FindProperty("dialogueText").objectReferenceValue = tmpText;
            so.FindProperty("spriteTransform").objectReferenceValue = sr.transform;

            // Set dialogueLines field
            SerializedProperty dialogueProp = so.FindProperty("dialogueLines");
            dialogueProp.ClearArray();
            dialogueProp.arraySize = customDialogue.Length;
            for (int i = 0; i < customDialogue.Length; i++)
            {
                dialogueProp.GetArrayElementAtIndex(i).stringValue = customDialogue[i];
            }

            so.ApplyModifiedProperties();

            // 4. Save as Prefab
            PrefabUtility.SaveAsPrefabAssetAndConnect(npcGO, prefabPath, InteractionMode.AutomatedAction);
            
            // Destroy temporary instance
            Object.DestroyImmediate(npcGO);
            Debug.Log($"[CreateAttackNPCs] Successfully created and saved prefab at: {prefabPath}");
        }

        AssetDatabase.Refresh();
    }

    private static void ConfigureSpriteSettings(string assetPath)
    {
        TextureImporter importer = AssetImporter.GetAtPath(assetPath) as TextureImporter;
        if (importer != null && (importer.textureType != TextureImporterType.Sprite || importer.spritePixelsPerUnit != 1024 || importer.filterMode != FilterMode.Point))
        {
            importer.textureType = TextureImporterType.Sprite;
            importer.spriteImportMode = SpriteImportMode.Single;
            importer.mipmapEnabled = false;
            importer.filterMode = FilterMode.Point;
            importer.alphaIsTransparency = true;
            importer.spritePixelsPerUnit = 1024;
            importer.SaveAndReimport();
            AssetDatabase.ImportAsset(assetPath, ImportAssetOptions.ForceUpdate);
            Debug.Log($"[CreateAttackNPCs] Configured sprite settings for: {assetPath}");
        }
    }

    private static string[] GetCustomDialogue(string name)
    {
        if (name.Contains("Tomato"))
        {
            return new string[]
            {
                "I'm a juicy tomato!",
                "Catch me if you can!",
                "Watch out for my roll!",
                "Rolling around...",
                "Boing!"
            };
        }
        else if (name.Contains("Carrot"))
        {
            return new string[]
            {
                "Crunchy and sweet!",
                "I have high vitamin A!",
                "Standing tall!",
                "Do you like carrots?",
                "Deep under the soil..."
            };
        }
        else if (name.Contains("Bean"))
        {
            return new string[]
            {
                "I am a tiny bean!",
                "Sprout power!",
                "Green and energetic!",
                "Don't step on me!",
                "Bouncing away!"
            };
        }
        
        return new string[] { "Hello!", "Boing!" };
    }

    private static TMP_FontAsset GetDefaultTMPFont()
    {
        string[] guids = AssetDatabase.FindAssets("t:TMP_FontAsset");
        foreach (string guid in guids)
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            if (path.Contains("LiberationSans") || path.Contains("Itim") || path.Contains("Mali") || path.Contains("Kanit"))
            {
                return AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(path);
            }
        }
        
        if (guids.Length > 0)
        {
            return AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(AssetDatabase.GUIDToAssetPath(guids[0]));
        }
        
        return null;
    }
}
#endif
