using UnityEngine;
using UnityEditor;
using UnityEngine.UI;
using TMPro;
using RPG.UI;

/// <summary>
/// Fixes two runtime issues found during first play test:
///   1. Rebuilds InitiativeEntry prefab with the now-standalone InitiativeEntry component
///   2. Ensures BuffSelectionUI and ResultScreen are active (hidden via CanvasGroup, not SetActive)
///
/// Run via  RPG > Fix → Runtime Issues
/// </summary>
public class RPGFixRuntime
{
    [MenuItem("RPG/Fix → Runtime Issues")]
    public static void Execute()
    {
        RebuildInitiativeEntryPrefab();
        FixInactiveUIElements();

        var scene = UnityEditor.SceneManagement.EditorSceneManager.GetActiveScene();
        UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(scene);
        UnityEditor.SceneManagement.EditorSceneManager.SaveScene(scene);
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        Debug.Log("[RPGFixRuntime] ✅ Runtime issues fixed!");
    }

    // ── Rebuild InitiativeEntry Prefab ────────────────────────────────────────
    // The old prefab had an inner-class component reference that Unity can't serialize.
    // Now that InitiativeEntry is a standalone script, rebuild the prefab cleanly.
    static void RebuildInitiativeEntryPrefab()
    {
        const string prefabPath = "Assets/_Project/Prefabs/UI/InitiativeEntry.prefab";

        // Build fresh root
        var entry = new GameObject("InitiativeEntry");

        var entryRect = entry.AddComponent<RectTransform>();
        entryRect.sizeDelta = new Vector2(68f, 68f);

        // Background
        var bg = entry.AddComponent<Image>();
        bg.color = new Color(0.1f, 0.1f, 0.2f, 0.95f);

        // Portrait
        var portraitGo = new GameObject("Portrait");
        portraitGo.transform.SetParent(entry.transform, false);
        var portRect = portraitGo.AddComponent<RectTransform>();
        portRect.anchorMin = new Vector2(0.1f, 0.3f);
        portRect.anchorMax = new Vector2(0.9f, 0.95f);
        portRect.offsetMin = Vector2.zero;
        portRect.offsetMax = Vector2.zero;
        var portImg = portraitGo.AddComponent<Image>();
        portImg.preserveAspect = true;

        // CT fill bar (bottom strip)
        var ctBarGo = new GameObject("CTBar");
        ctBarGo.transform.SetParent(entry.transform, false);
        var ctRect = ctBarGo.AddComponent<RectTransform>();
        ctRect.anchorMin = new Vector2(0f, 0f);
        ctRect.anchorMax = new Vector2(1f, 0.28f);
        ctRect.offsetMin = new Vector2(2f, 2f);
        ctRect.offsetMax = new Vector2(-2f, -1f);
        var ctImg = ctBarGo.AddComponent<Image>();
        ctImg.color      = new Color(0.25f, 0.70f, 1f);
        ctImg.type       = Image.Type.Filled;
        ctImg.fillMethod = Image.FillMethod.Horizontal;
        ctImg.fillOrigin = (int)Image.OriginHorizontal.Left;
        ctImg.fillAmount = 0f;

        // Name label
        var nameGo = new GameObject("NameText");
        nameGo.transform.SetParent(entry.transform, false);
        var nameRect = nameGo.AddComponent<RectTransform>();
        nameRect.anchorMin = new Vector2(0f, 0.25f);
        nameRect.anchorMax = new Vector2(1f, 0.52f);
        nameRect.offsetMin = new Vector2(2f, 0f);
        nameRect.offsetMax = new Vector2(-2f, 0f);
        var nameTMP = nameGo.AddComponent<TextMeshProUGUI>();
        nameTMP.text         = "Unit";
        nameTMP.fontSize     = 9f;
        nameTMP.color        = Color.white;
        nameTMP.alignment    = TextAlignmentOptions.Center;
        nameTMP.overflowMode = TextOverflowModes.Truncate;

        // Wire InitiativeEntry component (now a standalone MonoBehaviour)
        var entryComp       = entry.AddComponent<InitiativeEntry>();
        entryComp.Portrait  = portImg;
        entryComp.Background = bg;
        entryComp.CTBar     = ctImg;
        entryComp.NameLabel = nameTMP;

        // Save / overwrite prefab
        var saved = PrefabUtility.SaveAsPrefabAsset(entry, prefabPath);
        Object.DestroyImmediate(entry);

        // Re-wire into scene's InitiativeBarUI
        var barGo = GameObject.Find("UICanvas/InitiativeBar");
        if (barGo != null)
        {
            var bar = barGo.GetComponent<InitiativeBarUI>();
            if (bar != null && saved != null)
            {
                bar.EntryPrefab = saved;
                EditorUtility.SetDirty(bar);
            }
        }

        Debug.Log($"[RPGFixRuntime] InitiativeEntry prefab rebuilt at {prefabPath}");
    }

    // ── Ensure UI panels are active (hidden via CanvasGroup, not SetActive) ───
    static void FixInactiveUIElements()
    {
        // Elements that must stay active to allow StartCoroutine to work
        string[] mustBeActive = {
            "UICanvas/BuffSelectionUI",
            "UICanvas/ResultScreen",
            "UICanvas/PhaseBanner",
        };

        foreach (var path in mustBeActive)
        {
            var go = GameObject.Find(path);
            if (go == null)
            {
                // Try with inactive search
                go = FindInactive(path);
            }
            if (go == null) continue;

            if (!go.activeSelf)
            {
                go.SetActive(true);
                EditorUtility.SetDirty(go);
                Debug.Log($"[RPGFixRuntime] Set active: {path}");
            }

            // Make sure CanvasGroup blocks interaction while hidden
            var cg = go.GetComponent<CanvasGroup>();
            if (cg != null && cg.alpha < 0.01f)
            {
                cg.interactable   = false;
                cg.blocksRaycasts = false;
                EditorUtility.SetDirty(cg);
            }
        }
    }

    static GameObject FindInactive(string path)
    {
        // Split and walk hierarchy allowing inactive objects
        var parts = path.Split('/');
        Transform current = null;

        // Find root (may be inactive)
        foreach (var root in UnityEngine.SceneManagement.SceneManager.GetActiveScene().GetRootGameObjects())
        {
            if (root.name == parts[0]) { current = root.transform; break; }
        }
        if (current == null) return null;

        for (int i = 1; i < parts.Length; i++)
        {
            bool found = false;
            for (int c = 0; c < current.childCount; c++)
            {
                if (current.GetChild(c).name == parts[i])
                {
                    current = current.GetChild(c);
                    found = true;
                    break;
                }
            }
            if (!found) return null;
        }
        return current.gameObject;
    }
}
