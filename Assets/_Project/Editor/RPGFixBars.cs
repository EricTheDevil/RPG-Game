using UnityEngine;
using UnityEditor;
using UnityEngine.UI;
using RPG.UI;

/// <summary>
/// Rewires UnitStatusPanel and WorldHealthBar to use Image.fillAmount bars
/// instead of Slider components.
///
/// Run via  RPG/Fix → Health Bars
/// </summary>
public class RPGFixBars
{
    [MenuItem("RPG/Fix → Health Bars")]
    public static void Execute()
    {
        FixStatusPanels();
        FixWorldHealthBars();

        var scene = UnityEditor.SceneManagement.EditorSceneManager.GetActiveScene();
        UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(scene);
        UnityEditor.SceneManagement.EditorSceneManager.SaveScene(scene);
        AssetDatabase.SaveAssets();

        Debug.Log("[RPGFixBars] ✅ Health bars fixed!");
    }

    // ── UnitStatusPanel (HUD panels) ──────────────────────────────────────────
    static void FixStatusPanels()
    {
        string[] panelPaths = { "UICanvas/HeroStatus", "UICanvas/EnemyStatus" };

        foreach (var path in panelPaths)
        {
            var go = GameObject.Find(path);
            if (go == null) { Debug.LogWarning($"[RPGFixBars] {path} not found."); continue; }

            var sp = go.GetComponent<UnitStatusPanel>();
            if (sp == null) continue;

            // Wire HPBarFill — look for the Fill image inside HPBar/FillArea/Fill
            sp.HPBarFill = FindFillImage(go.transform, "HPBar");
            sp.MPBarFill = FindFillImage(go.transform, "MPBar");

            // Ensure both fill images are Type.Filled Horizontal
            EnsureFillImage(sp.HPBarFill);
            EnsureFillImage(sp.MPBarFill);

            EditorUtility.SetDirty(sp);
            Debug.Log($"[RPGFixBars] Wired status panel fills: {path}");
        }
    }

    // ── WorldHealthBar (world-space bars on prefab clones) ────────────────────
    static void FixWorldHealthBars()
    {
        // Fix the source prefabs so future instances are correct
        string[] prefabPaths = {
            "Assets/_Project/Prefabs/HeroPrefab.prefab",
            "Assets/_Project/Prefabs/EnemyPrefab.prefab"
        };

        foreach (var prefabPath in prefabPaths)
        {
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);
            if (prefab == null) continue;

            using var scope = new PrefabUtility.EditPrefabContentsScope(prefabPath);
            var root = scope.prefabContentsRoot;

            // Find HealthBarCanvas
            var canvas = FindChildRecursive(root.transform, "HealthBarCanvas");
            if (canvas == null) { Debug.LogWarning($"[RPGFixBars] No HealthBarCanvas in {prefabPath}"); continue; }

            var whb = canvas.GetComponent<WorldHealthBar>();
            if (whb == null) whb = canvas.gameObject.AddComponent<WorldHealthBar>();

            whb.HPFill = FindFillImage(canvas, "HPBar");
            whb.MPFill = FindFillImage(canvas, "MPBar");

            EnsureFillImage(whb.HPFill);
            EnsureFillImage(whb.MPFill);

            // Remove any Slider components (they break under world-space rotation)
            foreach (var slider in canvas.GetComponentsInChildren<Slider>())
            {
                slider.enabled = false;   // disable rather than destroy in case prefab references exist
            }

            EditorUtility.SetDirty(root);
            Debug.Log($"[RPGFixBars] Fixed WorldHealthBar in {prefabPath}");
        }
    }

    // ── Helpers ───────────────────────────────────────────────────────────────
    static Image FindFillImage(Transform parent, string barName)
    {
        // Look for  barName/FillArea/Fill  or  barName/Fill
        var bar = FindChildRecursive(parent, barName);
        if (bar == null) return null;

        var fillArea = FindChildRecursive(bar, "FillArea");
        if (fillArea != null)
        {
            var fill = FindChildRecursive(fillArea, "Fill");
            if (fill != null) return fill.GetComponent<Image>();
        }
        // Fallback: direct Fill child
        var directFill = FindChildRecursive(bar, "Fill");
        return directFill?.GetComponent<Image>();
    }

    static void EnsureFillImage(Image img)
    {
        if (img == null) return;
        img.type       = Image.Type.Filled;
        img.fillMethod = Image.FillMethod.Horizontal;
        img.fillOrigin = (int)Image.OriginHorizontal.Left;
        img.fillAmount = 1f;
    }

    static Transform FindChildRecursive(Transform parent, string name)
    {
        for (int i = 0; i < parent.childCount; i++)
        {
            var child = parent.GetChild(i);
            if (child.name == name) return child;
            var found = FindChildRecursive(child, name);
            if (found != null) return found;
        }
        return null;
    }
}
