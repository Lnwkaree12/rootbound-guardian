#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

public class SetupIntroScene
{
    [MenuItem("Tools/SproutScout/Setup Intro Scene with Dustmotes")]
    public static void RunSetup()
    {
        SetupScene("Assets/Prefabs/Intro_Cinematic_Canvas.prefab");
    }

    [MenuItem("Tools/SproutScout/Setup Original Intro Scene with Dustmotes")]
    public static void RunOriginalSetup()
    {
        SetupScene("Assets/Prefabs/Original_Intro_Cinematic_Canvas.prefab");
    }

    private static void SetupScene(string canvasPrefabPath)
    {
        string scenePath = "Assets/Scenes/IntroScene.unity";
        string dustmotesPrefabPath = "Assets/Image/Dustmotes.prefab";

        // 1. Open the IntroScene
        var scene = EditorSceneManager.OpenScene(scenePath, OpenSceneMode.Single);
        if (!scene.IsValid())
        {
            Debug.LogError($"[SetupIntroScene] Could not open scene at {scenePath}");
            return;
        }

        Debug.Log($"[SetupIntroScene] Setting up IntroScene using prefab: {canvasPrefabPath}...");

        // 2. Find or setup the Main Camera
        Camera mainCamera = Camera.main;
        if (mainCamera == null)
        {
            mainCamera = Object.FindObjectOfType<Camera>();
        }
        if (mainCamera == null)
        {
            GameObject camGO = new GameObject("Main Camera");
            mainCamera = camGO.AddComponent<Camera>();
            camGO.tag = "MainCamera";
        }

        // Clear existing instances to prevent duplicates
        foreach (var canvas in Object.FindObjectsOfType<Canvas>())
        {
            if (canvas.gameObject.name.StartsWith("Intro Cinematic Canvas") || canvas.gameObject.name.StartsWith("Original Intro Cinematic Canvas"))
            {
                Object.DestroyImmediate(canvas.gameObject);
            }
        }
        foreach (var ps in Object.FindObjectsOfType<ParticleSystem>())
        {
            if (ps.gameObject.name.StartsWith("Dustmotes"))
            {
                Object.DestroyImmediate(ps.gameObject);
            }
        }

        // 3. Load and Instantiate Canvas Prefab
        GameObject canvasPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(canvasPrefabPath);
        if (canvasPrefab == null)
        {
            Debug.LogError($"[SetupIntroScene] Canvas Prefab not found at {canvasPrefabPath}. Please generate it first!");
            return;
        }
        GameObject canvasInstance = (GameObject)PrefabUtility.InstantiatePrefab(canvasPrefab);
        canvasInstance.name = canvasPrefab.name; // Keep prefab name to match sorting checks

        // Configure Canvas to Screen Space - Camera for proper 3D sorting
        Canvas canvasComp = canvasInstance.GetComponent<Canvas>();
        canvasComp.renderMode = RenderMode.ScreenSpaceCamera;
        canvasComp.worldCamera = mainCamera;
        canvasComp.planeDistance = 10f; // Place Canvas 10 units away

        // 4. Load and Instantiate Dustmotes Prefab
        GameObject dustmotesPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(dustmotesPrefabPath);
        if (dustmotesPrefab == null)
        {
            Debug.LogError($"[SetupIntroScene] Dustmotes Prefab not found at {dustmotesPrefabPath}!");
            return;
        }
        GameObject dustmotesInstance = (GameObject)PrefabUtility.InstantiatePrefab(dustmotesPrefab);
        dustmotesInstance.name = "Dustmotes";

        // Parent Dustmotes to the Camera so they cover the screen dynamically
        dustmotesInstance.transform.SetParent(mainCamera.transform, false);
        dustmotesInstance.transform.localPosition = new Vector3(0f, 0f, 4f); // 4 units away (closer than the 10-unit Canvas, making them float in front)
        dustmotesInstance.transform.localRotation = Quaternion.identity;
        dustmotesInstance.transform.localScale = Vector3.one;

        // Set sorting order higher than UI Canvas to guarantee visibility
        ParticleSystemRenderer psr = dustmotesInstance.GetComponent<ParticleSystemRenderer>();
        if (psr != null)
        {
            psr.sortingOrder = 5; // Standard UI Canvas has order 0
        }

        // 5. Connect UI references in the manager if needed
        IntroCinematicManager manager = canvasInstance.GetComponent<IntroCinematicManager>();
        if (manager != null)
        {
            // Connect script UI properties (in case they were detached)
            if (manager.slideImage == null)
            {
                Transform slideTransform = canvasInstance.transform.Find("SlideImage");
                if (slideTransform != null) manager.slideImage = slideTransform.GetComponent<Image>();
            }
            if (manager.dialogueText == null)
            {
                Transform textTransform = canvasInstance.transform.Find("DialogueBox/NarrativeText");
                if (textTransform != null) manager.dialogueText = textTransform.GetComponent<Text>();
            }
            if (manager.fadeGroup == null)
            {
                manager.fadeGroup = canvasInstance.GetComponent<CanvasGroup>();
            }
            if (manager.pressSpacePrompt == null)
            {
                Transform promptTransform = canvasInstance.transform.Find("DialogueBox/NextPrompt");
                if (promptTransform != null) manager.pressSpacePrompt = promptTransform.gameObject;
            }
        }

        // 6. Save the Scene
        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);

        Debug.Log("[SetupIntroScene] IntroScene setup successfully saved!");
    }
}
#endif
