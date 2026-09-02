#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;
using TMPro;

[InitializeOnLoad]
public class CreateSlimePrefab
{
    static CreateSlimePrefab()
    {
        EditorApplication.delayCall += EnsureSlimePrefabsExist;
    }

    [MenuItem("Tools/SproutScout/Create Slime Prefabs")]
    public static void ForceCreateSlimes()
    {
        CreateSlimes(true);
    }

    private static void EnsureSlimePrefabsExist()
    {
        if (EditorApplication.isPlaying || Application.isPlaying) return;
        CreateSlimes(false);
    }

    private static void CreateSlimes(bool forceOverwrite)
    {
        string[] slimeNames = new string[] { "SlimeV2LV1", "SlimeV2LV2", "SlimeV2LV3" };
        string folderPath = "Assets/Character/Monsters";

        // Ensure folder exists
        if (!AssetDatabase.IsValidFolder(folderPath))
        {
            Debug.LogWarning($"[CreateSlimePrefab] Directory {folderPath} not found! Cannot create prefabs.");
            return;
        }

        foreach (string slimeName in slimeNames)
        {
            string pngPath = $"{folderPath}/{slimeName}.png";
            string prefabPath = $"{folderPath}/{slimeName}.prefab";

            if (!forceOverwrite && AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath) != null)
            {
                continue;
            }

            Sprite sprite = AssetDatabase.LoadAssetAtPath<Sprite>(pngPath);
            if (sprite == null)
            {
                Debug.LogWarning($"[CreateSlimePrefab] Sprite not found at: {pngPath}. Skipping {slimeName}.");
                continue;
            }

            Debug.Log($"[CreateSlimePrefab] Creating NPC Prefab for {slimeName}...");

            // 1. Create temporary GameObject
            GameObject slimeGO = new GameObject(slimeName);
            
            // Add SpriteRenderer
            SpriteRenderer sr = slimeGO.AddComponent<SpriteRenderer>();
            sr.sprite = sprite;
            ConfigureSpritesLighting.ConfigureSpriteLighting(sr);

            // Add Rigidbody (3D physics)
            Rigidbody rb = slimeGO.AddComponent<Rigidbody>();
            rb.useGravity = true;
            rb.constraints = RigidbodyConstraints.FreezeRotation;

            // Calculate BoxCollider size and center dynamically based on Sprite properties
            BoxCollider bc = slimeGO.AddComponent<BoxCollider>();
            float width = sprite.rect.width / sprite.pixelsPerUnit;
            float height = sprite.rect.height / sprite.pixelsPerUnit;
            float pivotYNormalized = sprite.pivot.y / sprite.rect.height;
            float pivotXNormalized = sprite.pivot.x / sprite.rect.width;

            float centerY = (0.5f - pivotYNormalized) * height;
            float centerX = (0.5f - pivotXNormalized) * width;

            bc.size = new Vector3(width, height, 0.2f);
            bc.center = new Vector3(centerX, centerY, 0f);

            // Add SlimeV2NPC behavior script
            SlimeV2NPC npc = slimeGO.AddComponent<SlimeV2NPC>();

            // 2. Create Dialogue Canvas
            GameObject canvasGO = new GameObject("DialogueCanvas");
            canvasGO.transform.SetParent(slimeGO.transform);
            
            // Position dialogue canvas above the top of the slime sprite
            float slimeTopY = (1f - pivotYNormalized) * height;
            canvasGO.transform.localPosition = new Vector3(centerX, slimeTopY + 0.25f, 0f);
            canvasGO.transform.localScale = new Vector3(0.003f, 0.003f, 0.003f);

            Canvas canvas = canvasGO.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.WorldSpace;

            RectTransform canvasRT = canvasGO.GetComponent<RectTransform>();
            canvasRT.sizeDelta = new Vector2(250f, 75f);

            // Add background bubble
            GameObject bgGO = new GameObject("BubbleBackground");
            bgGO.transform.SetParent(canvasGO.transform, false);
            UnityEngine.UI.Image bgImg = bgGO.AddComponent<UnityEngine.UI.Image>();
            
            // Check if there is a default sprite for panel/background, otherwise use standard color
            Sprite panelSprite = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/Background.psd");
            if (panelSprite != null)
            {
                bgImg.sprite = panelSprite;
                bgImg.type = UnityEngine.UI.Image.Type.Sliced;
                bgImg.color = new Color(0f, 0f, 0f, 0.75f); // Semi-transparent black bubble
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
            tmpText.text = "Boing!";
            
            // Apply default font
            TMP_FontAsset fontAsset = GetDefaultTMPFont();
            if (fontAsset != null)
            {
                tmpText.font = fontAsset;
            }

            RectTransform textRT = textGO.GetComponent<RectTransform>();
            textRT.anchorMin = Vector2.zero;
            textRT.anchorMax = Vector2.one;
            textRT.sizeDelta = new Vector2(-15f, -10f); // Padding

            // 3. Connect references via SerializedObject
            SerializedObject so = new SerializedObject(npc);
            so.FindProperty("spriteRenderer").objectReferenceValue = sr;
            so.FindProperty("dialogueCanvas").objectReferenceValue = canvas;
            so.FindProperty("dialogueText").objectReferenceValue = tmpText;
            so.FindProperty("spriteTransform").objectReferenceValue = sr.transform;
            so.ApplyModifiedProperties();

            // 4. Save as Prefab
            PrefabUtility.SaveAsPrefabAssetAndConnect(slimeGO, prefabPath, InteractionMode.AutomatedAction);
            
            // Destroy temporary instance
            Object.DestroyImmediate(slimeGO);
            Debug.Log($"[CreateSlimePrefab] Successfully created and saved prefab at: {prefabPath}");
        }

        AssetDatabase.Refresh();
    }

    private static TMP_FontAsset GetDefaultTMPFont()
    {
        // Search in Assets
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
