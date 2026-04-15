using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using RPG.UI;

/// <summary>
/// Patches scene objects that need to be fixed after code changes.
/// Run via  RPG > Patch Scenes
/// </summary>
public class RPGScenePatch
{
    [MenuItem("RPG/Patch Scenes")]
    public static void Execute()
    {
        PatchCombatStage();
        AssetDatabase.SaveAssets();
        Debug.Log("[RPGScenePatch] ✅ Scene patches applied.");
    }

    static void PatchCombatStage()
    {
        string path = "Assets/_Project/Scenes/CombatStage.unity";
        var scene = EditorSceneManager.OpenScene(path, OpenSceneMode.Single);

        // ResultScreen: must NOT be SetActive(false). It hides via CanvasGroup now.
        var result = Object.FindFirstObjectByType<ResultScreenUI>();
        if (result != null)
        {
            if (!result.gameObject.activeSelf)
            {
                result.gameObject.SetActive(true);
                EditorUtility.SetDirty(result.gameObject);
            }

            // Ensure it has a CanvasGroup and starts invisible
            var cg = result.GetComponent<CanvasGroup>();
            if (cg == null) cg = result.gameObject.AddComponent<CanvasGroup>();
            result.CanvasGroup         = cg;
            cg.alpha                   = 0f;
            cg.interactable            = false;
            cg.blocksRaycasts          = false;
            EditorUtility.SetDirty(result);
            Debug.Log("[RPGScenePatch] ResultScreen patched to use CanvasGroup visibility.");
        }
        else
        {
            Debug.LogWarning("[RPGScenePatch] ResultScreenUI not found in CombatStage.");
        }

        EditorSceneManager.SaveScene(scene, path);
    }
}
