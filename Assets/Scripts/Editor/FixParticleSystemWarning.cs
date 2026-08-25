#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;
using System.IO;

[InitializeOnLoad]
public class FixParticleSystemWarning
{
    static FixParticleSystemWarning()
    {
        EditorApplication.delayCall += AutoFix;
    }

    private static void AutoFix()
    {
        FixAll(false);
    }

    [MenuItem("Tools/SproutScout/Fix Particle Systems Warning")]
    public static void ForceFix()
    {
        FixAll(true);
    }

    public static void FixAll(bool logAll)
    {
        string[] guids = AssetDatabase.FindAssets("t:Prefab");
        bool anyFixed = false;

        foreach (string guid in guids)
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            if (prefab == null) continue;

            ParticleSystem[] psList = prefab.GetComponentsInChildren<ParticleSystem>(true);
            bool isPrefabModified = false;
            foreach (ParticleSystem ps in psList)
            {
                if (FixVelocityAndForceModules(ps))
                {
                    isPrefabModified = true;
                    anyFixed = true;
                }
            }

            if (isPrefabModified)
            {
                EditorUtility.SetDirty(prefab);
                AssetDatabase.SaveAssetIfDirty(prefab);
                Debug.Log("[FixParticleSystems] Fixed Particle System in Prefab: " + path);
            }
        }

        // Fix in active scene as well
        ParticleSystem[] scenePsList = Object.FindObjectsOfType<ParticleSystem>(true);
        foreach (ParticleSystem ps in scenePsList)
        {
            if (FixVelocityAndForceModules(ps))
            {
                UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(ps.gameObject.scene);
                anyFixed = true;
                Debug.Log("[FixParticleSystems] Fixed Particle System in Scene: " + ps.gameObject.name);
            }
        }

        if (anyFixed || logAll)
        {
            AssetDatabase.SaveAssets();
            Debug.Log("[FixParticleSystems] Particle curve modes successfully aligned. Warning resolved!");
        }
    }

    private static bool FixVelocityAndForceModules(ParticleSystem ps)
    {
        bool modified = false;

        // 1. Fix Velocity over Lifetime
        var velocity = ps.velocityOverLifetime;
        if (velocity.enabled)
        {
            ParticleSystem.MinMaxCurve x = velocity.x;
            ParticleSystem.MinMaxCurve y = velocity.y;
            ParticleSystem.MinMaxCurve z = velocity.z;

            if (x.mode != y.mode || x.mode != z.mode)
            {
                ParticleSystemCurveMode targetMode = x.mode;
                y.mode = targetMode;
                z.mode = targetMode;

                velocity.x = x;
                velocity.y = y;
                velocity.z = z;
                modified = true;
            }
        }

        // 2. Fix Force over Lifetime
        var force = ps.forceOverLifetime;
        if (force.enabled)
        {
            ParticleSystem.MinMaxCurve x = force.x;
            ParticleSystem.MinMaxCurve y = force.y;
            ParticleSystem.MinMaxCurve z = force.z;

            if (x.mode != y.mode || x.mode != z.mode)
            {
                ParticleSystemCurveMode targetMode = x.mode;
                y.mode = targetMode;
                z.mode = targetMode;

                force.x = x;
                force.y = y;
                force.z = z;
                modified = true;
            }
        }

        return modified;
    }
}
#endif
