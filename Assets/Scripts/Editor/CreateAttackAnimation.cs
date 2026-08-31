#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;
using UnityEditor.Animations;
using System.IO;

[InitializeOnLoad]
public class CreateAttackAnimation
{
    static CreateAttackAnimation()
    {
        EditorApplication.delayCall += AutoCreateAllAnimations;
    }

    private static void AutoCreateAllAnimations()
    {
        if (EditorApplication.isPlaying || Application.isPlaying)
        {
            return;
        }
        BuildAllAnimations(false);
    }

    [MenuItem("Tools/SproutScout/Build All Player Animations")]
    public static void ForceBuildAllAnimations()
    {
        BuildAllAnimations(true);
    }

    public static void BuildAllAnimations(bool force)
    {
        // 1. Configure all Sprites with 1024 PPU and Point Filter
        string mcDir = "Assets/Character/FinalMC";
        ConfigureSpritesInDirectory(mcDir);
        AssetDatabase.Refresh();

        // 2. Build individual animation clips
        BuildClip("AttackDown", new string[] { "AttackDown01", "AttackDown02", "AttackDown03" }, 10, force);
        BuildClip("Jump", new string[] { "Jump01", "Jump02", "Jump03" }, 10, force);
        BuildClip("Dash", new string[] { "Dash01", "Dash02", "Dash03" }, 12, force);
        BuildClip("Pickup", new string[] { "Pickup01", "Pickup02", "Pickup03" }, 10, force);
    }

    private static void BuildClip(string animName, string[] spriteNames, int fps, bool force)
    {
        string clipPath = $"Assets/Character/FinalMC/{animName}.anim";
        AnimationClip existingClip = AssetDatabase.LoadAssetAtPath<AnimationClip>(clipPath);
        if (existingClip != null && !force)
        {
            return;
        }

        // Load Sprites
        Sprite[] sprites = new Sprite[spriteNames.Length];
        for (int i = 0; i < spriteNames.Length; i++)
        {
            sprites[i] = AssetDatabase.LoadAssetAtPath<Sprite>($"Assets/Character/FinalMC/{spriteNames[i]}.png");
            if (sprites[i] == null)
            {
                Debug.LogWarning($"[CreateAttackAnimation] Sprite {spriteNames[i]} not found. Skipping build for {animName}.");
                return;
            }
        }

        // Create Clip
        AnimationClip clip = new AnimationClip();
        clip.frameRate = fps;
        
        var settings = AnimationUtility.GetAnimationClipSettings(clip);
        settings.loopTime = false;
        AnimationUtility.SetAnimationClipSettings(clip, settings);

        // Define keyframes (Hold last frame for a moment)
        ObjectReferenceKeyframe[] keyframes = new ObjectReferenceKeyframe[sprites.Length + 1];
        float frameTime = 1.0f / fps;
        for (int i = 0; i < sprites.Length; i++)
        {
            keyframes[i] = new ObjectReferenceKeyframe { time = i * frameTime, value = sprites[i] };
        }
        keyframes[sprites.Length] = new ObjectReferenceKeyframe { time = sprites.Length * frameTime, value = sprites[sprites.Length - 1] };

        EditorCurveBinding binding = new EditorCurveBinding
        {
            type = typeof(SpriteRenderer),
            path = "",
            propertyName = "m_Sprite"
        };

        AnimationUtility.SetObjectReferenceCurve(clip, binding, keyframes);
        AssetDatabase.CreateAsset(clip, clipPath);
        AssetDatabase.SaveAssets();
        Debug.Log($"[CreateAttackAnimation] Created/updated {clipPath}");

        // Link to controllers
        string[] controllerPaths = new string[]
        {
            "Assets/Character/FinalMC/Image.controller",
            "Assets/Anim/Player/Player.controller"
        };

        foreach (string controllerPath in controllerPaths)
        {
            var controller = AssetDatabase.LoadAssetAtPath<AnimatorController>(controllerPath);
            if (controller != null)
            {
                AnimatorStateMachine stateMachine = controller.layers[0].stateMachine;
                AnimatorState animState = null;
                bool stateExists = false;

                foreach (var state in stateMachine.states)
                {
                    if (state.state.name == animName)
                    {
                        state.state.motion = clip;
                        animState = state.state;
                        stateExists = true;
                        break;
                    }
                }

                if (!stateExists)
                {
                    animState = stateMachine.AddState(animName);
                    animState.motion = clip;
                    Debug.Log($"[CreateAttackAnimation] Added {animName} state to {controllerPath}");
                }

                // Add exit transition to default state
                if (animState != null && stateMachine.defaultState != null)
                {
                    bool transitionExists = false;
                    foreach (var transition in animState.transitions)
                    {
                        if (transition.destinationState == stateMachine.defaultState)
                        {
                            transitionExists = true;
                            break;
                        }
                    }

                    if (!transitionExists)
                    {
                        var transition = animState.AddTransition(stateMachine.defaultState);
                        transition.hasExitTime = true;
                        transition.exitTime = 1.0f;
                        transition.duration = 0.05f;
                        Debug.Log($"[CreateAttackAnimation] Added exit transition for {animName} in {controllerPath}");
                    }
                }

                EditorUtility.SetDirty(controller);
                AssetDatabase.SaveAssets();
            }
        }
    }

    private static void ConfigureSpritesInDirectory(string dirPath)
    {
        string[] files = Directory.GetFiles(dirPath, "*.png");
        foreach (string file in files)
        {
            string assetPath = file.Replace('\\', '/');
            // We configure the newly generated ones
            if (assetPath.Contains("AttackDown") || assetPath.Contains("Jump") || assetPath.Contains("Dash") || assetPath.Contains("Pickup"))
            {
                TextureImporter importer = AssetImporter.GetAtPath(assetPath) as TextureImporter;
                if (importer != null && (importer.spritePixelsPerUnit != 1024 || importer.filterMode != FilterMode.Point))
                {
                    importer.textureType = TextureImporterType.Sprite;
                    importer.spriteImportMode = SpriteImportMode.Single;
                    importer.mipmapEnabled = false;
                    importer.filterMode = FilterMode.Point;
                    importer.alphaIsTransparency = true;
                    importer.spritePixelsPerUnit = 1024;
                    importer.SaveAndReimport();
                    Debug.Log($"[CreateAttackAnimation] Configured sprite settings for: {assetPath}");
                }
            }
        }
    }
}
#endif
