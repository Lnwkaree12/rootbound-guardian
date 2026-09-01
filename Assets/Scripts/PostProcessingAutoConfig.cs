using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.SceneManagement;
#endif

#if UNITY_EDITOR
[InitializeOnLoad]
#endif
public static class PostProcessingAutoConfig
{
    static PostProcessingAutoConfig()
    {
        #if UNITY_EDITOR
        // Run configuration whenever a scene is opened in the Editor
        EditorSceneManager.sceneOpened += (scene, mode) => ConfigureActiveScene();
        // Also run once when Unity starts or recompiles
        EditorApplication.delayCall += ConfigureActiveScene;
        #endif
    }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    public static void ConfigureActiveScene()
    {
        ConfigureCamera();
        ConfigureVolume();
    }

    public static void ConfigureCamera()
    {
        // 1. Find the Main Camera in the active scene
        Camera mainCam = Camera.main;
        if (mainCam == null)
        {
            mainCam = GameObject.FindObjectOfType<Camera>();
        }

        if (mainCam != null)
        {
            // 2. Ensure URP Additional Camera Data exists
            var cameraData = mainCam.GetComponent<UniversalAdditionalCameraData>();
            if (cameraData == null)
            {
                cameraData = mainCam.gameObject.AddComponent<UniversalAdditionalCameraData>();
            }

            if (cameraData != null && !cameraData.renderPostProcessing)
            {
                cameraData.renderPostProcessing = true;
                Debug.Log($"[PostProcessingAutoConfig] Enabled Post Processing on Camera: '{mainCam.name}'");
                
                #if UNITY_EDITOR
                if (!Application.isPlaying)
                {
                    EditorUtility.SetDirty(cameraData);
                    EditorUtility.SetDirty(mainCam.gameObject);
                }
                #endif
            }
        }
    }

    public static void ConfigureVolume()
    {
        // 3. Check if there is already a Volume component in the scene
        Volume existingVolume = GameObject.FindObjectOfType<Volume>();
        if (existingVolume == null)
        {
            // Create a new Global Volume GameObject
            GameObject volumeGO = new GameObject("Global Post Processing Volume");
            Volume volume = volumeGO.AddComponent<Volume>();
            volume.isGlobal = true;
            volume.weight = 1f;

            // 4. Find and assign the default Volume Profile in the project
            #if UNITY_EDITOR
            string[] guids = AssetDatabase.FindAssets("t:VolumeProfile");
            if (guids.Length > 0)
            {
                // Prioritize 'DefaultVolumeProfile' or 'SampleSceneProfile' if they exist
                string selectedPath = "";
                foreach (string guid in guids)
                {
                    string path = AssetDatabase.GUIDToAssetPath(guid);
                    if (path.Contains("DefaultVolumeProfile") || path.Contains("SampleSceneProfile"))
                    {
                        selectedPath = path;
                        break;
                    }
                }
                
                // Fallback to the first profile found
                if (string.IsNullOrEmpty(selectedPath))
                {
                    selectedPath = AssetDatabase.GUIDToAssetPath(guids[0]);
                }

                volume.sharedProfile = AssetDatabase.LoadAssetAtPath<VolumeProfile>(selectedPath);
                Debug.Log($"[PostProcessingAutoConfig] Created 'Global Post Processing Volume' and assigned Profile: '{selectedPath}'");
            }
            else
            {
                Debug.LogWarning("[PostProcessingAutoConfig] Created 'Global Post Processing Volume', but no Volume Profile (.asset) was found in the project.");
            }
            
            if (!Application.isPlaying)
            {
                Undo.RegisterCreatedObjectUndo(volumeGO, "Create Global Volume");
                EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
            }
            #endif
        }
    }
}
