using UnityEngine;
using UnityEditor;

public class InspectClips
{
    public static void Execute()
    {
        string[] fbxPaths = {
            "Assets/_Project/Models/Idle.fbx",
            "Assets/_Project/Models/Walking.fbx",
            "Assets/_Project/Models/Run.fbx",
            "Assets/_Project/Models/Standing 1H Magic Attack 02.fbx",
            "Assets/_Project/Models/Standing Death Left 01.fbx",
            "Assets/_Project/Models/Head hit.fbx",
            "Assets/_Project/Models/Meshy_AI_Cheshire_0415064128_texture_fbx/Meshy_AI_Cheshire_0415064128_texture.fbx",
        };

        foreach (var path in fbxPaths)
        {
            var assets = AssetDatabase.LoadAllAssetsAtPath(path);
            Debug.Log($"=== {path} ===");
            foreach (var a in assets)
            {
                if (a == null) continue;
                Debug.Log($"  [{a.GetType().Name}]  name='{a.name}'  path='{AssetDatabase.GetAssetPath(a)}'");
            }
        }

        // Also check the controller
        var ctrl = AssetDatabase.LoadAssetAtPath<RuntimeAnimatorController>(
            "Assets/_Project/Animations/UnitAnimator.controller");
        if (ctrl != null)
        {
            Debug.Log($"=== UnitAnimator.controller clips ===");
            foreach (var c in ctrl.animationClips)
                Debug.Log($"  clip: '{c.name}'  path='{AssetDatabase.GetAssetPath(c)}'");
        }

        // Check if Cheshire model has a humanoid avatar
        var importer = AssetImporter.GetAtPath(
            "Assets/_Project/Models/Meshy_AI_Cheshire_0415064128_texture_fbx/Meshy_AI_Cheshire_0415064128_texture.fbx")
            as ModelImporter;
        if (importer != null)
            Debug.Log($"Cheshire: animationType={importer.animationType}  avatar={importer.sourceAvatar}");
    }
}
