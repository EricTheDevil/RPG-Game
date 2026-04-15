using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine.EventSystems;

/// <summary>
/// Adds a properly configured EventSystem to every RPG scene.
/// With the New Input System package installed, uses InputSystemUIInputModule
/// (falls back to StandaloneInputModule if the package type isn't found).
/// Run via  RPG > Add EventSystem To All Scenes
/// </summary>
public class RPGAddEventSystem
{
    [MenuItem("RPG/Add EventSystem To All Scenes")]
    public static void Execute()
    {
        string[] scenes =
        {
            "Assets/_Project/Scenes/CombatStage.unity",
            "Assets/_Project/Scenes/MainMenu.unity",
        };

        foreach (var path in scenes)
            FixScene(path);

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log("[RPGAddEventSystem] ✅ EventSystem added to all scenes.");
    }

    static void FixScene(string scenePath)
    {
        var scene = EditorSceneManager.OpenScene(scenePath, OpenSceneMode.Single);

        // Don't duplicate
        var existing = Object.FindFirstObjectByType<EventSystem>();
        if (existing != null)
        {
            Debug.Log($"[RPGAddEventSystem] EventSystem already present in {scenePath}, skipping.");
            EditorSceneManager.SaveScene(scene);
            return;
        }

        // ── EventSystem root ──────────────────────────────────────────
        var esGo = new GameObject("EventSystem");
        esGo.AddComponent<EventSystem>();

        // Try to use InputSystemUIInputModule (new Input System).
        // Fall back to StandaloneInputModule if type not available.
        bool usedNewInputModule = TryAddInputSystemModule(esGo);
        if (!usedNewInputModule)
        {
            esGo.AddComponent<StandaloneInputModule>();
            Debug.Log($"[RPGAddEventSystem] Using StandaloneInputModule in {scenePath}");
        }

        EditorSceneManager.SaveScene(scene);
        Debug.Log($"[RPGAddEventSystem] EventSystem added to {scenePath} " +
                  $"(new input module: {usedNewInputModule})");
    }

    /// <summary>
    /// Attempts to add InputSystemUIInputModule via reflection so this file
    /// compiles even if the type name changes between package versions.
    /// </summary>
    static bool TryAddInputSystemModule(GameObject go)
    {
        // Fully-qualified type name from com.unity.inputsystem
        var moduleType = System.Type.GetType(
            "UnityEngine.InputSystem.UI.InputSystemUIInputModule, Unity.InputSystem");

        if (moduleType == null)
        {
            // Try alternate assembly name
            moduleType = System.Type.GetType(
                "UnityEngine.InputSystem.UI.InputSystemUIInputModule, Unity.InputSystem.ForUI");
        }

        if (moduleType == null)
        {
            // Search all loaded assemblies
            foreach (var asm in System.AppDomain.CurrentDomain.GetAssemblies())
            {
                moduleType = asm.GetType("UnityEngine.InputSystem.UI.InputSystemUIInputModule");
                if (moduleType != null) break;
            }
        }

        if (moduleType == null) return false;

        go.AddComponent(moduleType);
        return true;
    }
}
