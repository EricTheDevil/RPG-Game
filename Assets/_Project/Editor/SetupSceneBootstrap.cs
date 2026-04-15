using UnityEngine;
using UnityEditor;
using RPG.Core;
using RPG.Data;

public class SetupSceneBootstrap
{
    public static void Execute()
    {
        // ── 1. Create or find GameManager prefab ──────────────────────────────
        const string prefabFolder = "Assets/_Project/Prefabs";
        const string prefabPath   = prefabFolder + "/GameManager.prefab";

        // Load BuffRegistry
        var buffRegistry = AssetDatabase.LoadAssetAtPath<BuffRegistry>(
            "Assets/_Project/ScriptableObjects/Buffs/BuffRegistry.asset");

        GameObject gmGO;
        bool prefabExists = System.IO.File.Exists(
            Application.dataPath + "/../" + prefabPath);

        if (!prefabExists)
        {
            gmGO = new GameObject("GameManager");
            var gm = gmGO.AddComponent<GameManager>();
            if (buffRegistry != null) gm.BuffRegistry = buffRegistry;

            PrefabUtility.SaveAsPrefabAsset(gmGO, prefabPath);
            Object.DestroyImmediate(gmGO);
            AssetDatabase.Refresh();
            Debug.Log("[SetupSceneBootstrap] Created GameManager prefab at " + prefabPath);
        }
        else
        {
            Debug.Log("[SetupSceneBootstrap] GameManager prefab already exists.");
        }

        var gmPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);

        // ── 2. Add SceneBootstrap to the scene if not already present ─────────
        var existing = Object.FindObjectOfType<SceneBootstrap>();
        if (existing != null)
        {
            Debug.Log("[SetupSceneBootstrap] SceneBootstrap already in scene.");
            // Make sure prefab is wired
            var so = new SerializedObject(existing);
            var prop = so.FindProperty("GameManagerPrefab");
            if (prop != null && gmPrefab != null)
            {
                prop.objectReferenceValue = gmPrefab.GetComponent<GameManager>();
                so.ApplyModifiedProperties();
            }
            return;
        }

        var bsGO = new GameObject("SceneBootstrap");
        var bootstrap = bsGO.AddComponent<SceneBootstrap>();

        // Wire the prefab via SerializedObject so the private-inspector field is set
        var serialized = new SerializedObject(bootstrap);
        var gmProp = serialized.FindProperty("GameManagerPrefab");
        if (gmProp != null && gmPrefab != null)
        {
            gmProp.objectReferenceValue = gmPrefab.GetComponent<GameManager>();
            serialized.ApplyModifiedProperties();
        }

        // Mark scene dirty
        UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(
            UnityEngine.SceneManagement.SceneManager.GetActiveScene());

        Debug.Log("[SetupSceneBootstrap] SceneBootstrap added to CombatStage and wired to GameManager prefab.");
    }
}
