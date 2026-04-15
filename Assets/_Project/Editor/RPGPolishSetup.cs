using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine.UI;
using TMPro;
using RPG.VFX;
using RPG.UI;

/// <summary>
/// Applies the "polish" layer to existing scenes:
///   • Adds CameraShake to MainCamera in CombatStage
///   • Adds BuffStackUI panel to MapSelector
///
/// Run from  RPG > Apply Polish
/// </summary>
public class RPGPolishSetup
{
    [MenuItem("RPG/Apply Polish")]
    public static void Execute()
    {
        PatchCombatStage();
        PatchMapSelector();
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log("[RPGPolishSetup] ✅ Polish applied.");
    }

    // ── CombatStage: add CameraShake to MainCamera ────────────────────────────

    static void PatchCombatStage()
    {
        var scene = EditorSceneManager.OpenScene(
            "Assets/_Project/Scenes/CombatStage.unity",
            OpenSceneMode.Additive);

        var cam = GameObject.Find("MainCamera");
        if (cam == null)
        {
            // Try fallback tag
            var camObj = Camera.main;
            cam = camObj != null ? camObj.gameObject : null;
        }

        if (cam != null && cam.GetComponent<CameraShake>() == null)
        {
            cam.AddComponent<CameraShake>();
            Debug.Log("[RPGPolishSetup] CameraShake added to MainCamera.");
        }

        EditorSceneManager.SaveScene(scene);
        EditorSceneManager.CloseScene(scene, true);
    }

    // ── MapSelector: add BuffStackUI panel ────────────────────────────────────

    static void PatchMapSelector()
    {
        var scene = EditorSceneManager.OpenScene(
            "Assets/_Project/Scenes/MapSelector.unity",
            OpenSceneMode.Additive);

        // Skip if already present
        if (Object.FindObjectOfType<BuffStackUI>() != null)
        {
            EditorSceneManager.CloseScene(scene, true);
            return;
        }

        // Find the main UICanvas
        var canvas = GameObject.Find("UICanvas");
        if (canvas == null)
        {
            // Try any canvas
            var c = Object.FindObjectOfType<Canvas>();
            canvas = c != null ? c.gameObject : null;
        }

        Transform parent = canvas != null ? canvas.transform : null;

        // ── Panel root ──────────────────────────────────────────────────────────
        var panel = new GameObject("BuffStackPanel");
        panel.transform.SetParent(parent, false);

        var rect = panel.AddComponent<RectTransform>();
        rect.anchorMin = new Vector2(0f, 0f);
        rect.anchorMax = new Vector2(0f, 1f);
        rect.pivot     = new Vector2(0f, 0.5f);
        rect.offsetMin = new Vector2(10f, 10f);
        rect.offsetMax = new Vector2(260f, -10f);

        var bg = panel.AddComponent<Image>();
        bg.color = new Color(0f, 0f, 0f, 0.55f);

        var vlg = panel.AddComponent<VerticalLayoutGroup>();
        vlg.childAlignment = TextAnchor.UpperLeft;
        vlg.spacing = 4f;
        vlg.padding = new RectOffset(8, 8, 8, 8);
        vlg.childControlHeight = false;
        vlg.childControlWidth  = true;
        vlg.childForceExpandHeight = false;

        // ── Header ──────────────────────────────────────────────────────────────
        var headerGo = new GameObject("Header");
        headerGo.transform.SetParent(panel.transform, false);
        var headerRect = headerGo.AddComponent<RectTransform>();
        headerRect.sizeDelta = new Vector2(0f, 26f);
        var headerTmp = headerGo.AddComponent<TextMeshProUGUI>();
        headerTmp.text      = "ACTIVE BLESSINGS";
        headerTmp.fontSize  = 13f;
        headerTmp.fontStyle = FontStyles.Bold;
        headerTmp.color     = new Color(1f, 0.85f, 0.3f);

        // ── Empty label ─────────────────────────────────────────────────────────
        var emptyGo = new GameObject("EmptyLabel");
        emptyGo.transform.SetParent(panel.transform, false);
        var emptyRect = emptyGo.AddComponent<RectTransform>();
        emptyRect.sizeDelta = new Vector2(0f, 22f);
        var emptyTmp = emptyGo.AddComponent<TextMeshProUGUI>();
        emptyTmp.text     = "No blessings yet.";
        emptyTmp.fontSize = 11f;
        emptyTmp.color    = new Color(0.7f, 0.7f, 0.7f);

        // ── Row container ────────────────────────────────────────────────────────
        var containerGo = new GameObject("RowContainer");
        containerGo.transform.SetParent(panel.transform, false);
        var containerRect = containerGo.AddComponent<RectTransform>();
        containerRect.sizeDelta = new Vector2(0f, 0f);
        var cvlg = containerGo.AddComponent<VerticalLayoutGroup>();
        cvlg.spacing = 3f;
        cvlg.childControlWidth  = true;
        cvlg.childControlHeight = false;
        cvlg.childForceExpandHeight = false;
        containerGo.AddComponent<ContentSizeFitter>().verticalFit = ContentSizeFitter.FitMode.PreferredSize;

        // ── Row prefab (created as a regular GO, assigned as ref) ────────────────
        var rowPrefab = BuildRowPrefab();

        // ── BuffStackUI component ────────────────────────────────────────────────
        var ui = panel.AddComponent<BuffStackUI>();
        ui.RowContainer = containerGo.transform;
        ui.RowPrefab    = rowPrefab;
        ui.HeaderText   = headerTmp;
        ui.EmptyLabel   = emptyGo;

        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
        EditorSceneManager.CloseScene(scene, true);

        Debug.Log("[RPGPolishSetup] BuffStackUI added to MapSelector.");
    }

    static GameObject BuildRowPrefab()
    {
        const string path = "Assets/_Project/Prefabs/UI/BuffStackRow.prefab";
        var existing = AssetDatabase.LoadAssetAtPath<GameObject>(path);
        if (existing != null) return existing;

        var row = new GameObject("BuffStackRow");

        var rowRect = row.AddComponent<RectTransform>();
        rowRect.sizeDelta = new Vector2(0f, 40f);

        var hlg = row.AddComponent<HorizontalLayoutGroup>();
        hlg.spacing = 6f;
        hlg.childAlignment = TextAnchor.MiddleLeft;
        hlg.childControlHeight = true;
        hlg.childForceExpandWidth = false;
        hlg.padding = new RectOffset(4, 4, 2, 2);

        // Rarity accent bar
        var rarityGo = new GameObject("RarityTag");
        rarityGo.transform.SetParent(row.transform, false);
        var rarityRect = rarityGo.AddComponent<RectTransform>();
        rarityRect.sizeDelta = new Vector2(6f, 0f);
        rarityGo.AddComponent<Image>().color = Color.grey;
        var le = rarityGo.AddComponent<LayoutElement>();
        le.preferredWidth  = 6f;
        le.flexibleWidth   = 0f;

        // Text column
        var textCol = new GameObject("TextColumn");
        textCol.transform.SetParent(row.transform, false);
        var textColRect = textCol.AddComponent<RectTransform>();
        textColRect.sizeDelta = new Vector2(0f, 0f);
        var tcvlg = textCol.AddComponent<VerticalLayoutGroup>();
        tcvlg.childControlHeight = false;
        tcvlg.childForceExpandWidth = true;
        tcvlg.spacing = 1f;
        var tcle = textCol.AddComponent<LayoutElement>();
        tcle.flexibleWidth = 1f;

        // Name
        var nameGo = new GameObject("NameText");
        nameGo.transform.SetParent(textCol.transform, false);
        var nameRect = nameGo.AddComponent<RectTransform>();
        nameRect.sizeDelta = new Vector2(0f, 20f);
        var nameTmp = nameGo.AddComponent<TextMeshProUGUI>();
        nameTmp.text      = "Buff Name";
        nameTmp.fontSize  = 12f;
        nameTmp.fontStyle = FontStyles.Bold;
        nameTmp.color     = Color.white;

        // Stats
        var statsGo = new GameObject("StatsText");
        statsGo.transform.SetParent(textCol.transform, false);
        var statsRect = statsGo.AddComponent<RectTransform>();
        statsRect.sizeDelta = new Vector2(0f, 16f);
        var statsTmp = statsGo.AddComponent<TextMeshProUGUI>();
        statsTmp.text     = "+5 ATK";
        statsTmp.fontSize = 10f;
        statsTmp.color    = new Color(0.8f, 0.8f, 0.8f);

        var prefab = PrefabUtility.SaveAsPrefabAsset(row, path);
        Object.DestroyImmediate(row);
        return prefab;
    }
}
