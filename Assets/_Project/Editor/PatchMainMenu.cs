using UnityEngine;
using UnityEditor;
using UnityEngine.UI;
using TMPro;
using RPG.UI;
using RPG.Core;

/// <summary>
/// Patches the MainMenu scene to match the FTL-style game loop:
///   - Removes old LevelCard and StartButton
///   - Adds NewRunButton, ContinueButton, QuitButton
///   - Rewires MainMenuUI fields
///
/// Run via  RPG > Patch → Main Menu Scene
/// </summary>
public class PatchMainMenu
{
    [MenuItem("RPG/Patch → Main Menu Scene")]
    public static void Execute()
    {
        var canvasGo = GameObject.Find("UICanvas");
        if (canvasGo == null) { Debug.LogError("[PatchMainMenu] UICanvas not found. Open MainMenu scene first."); return; }

        // ── Remove old objects ────────────────────────────────────────────────
        DestroyChild(canvasGo, "LevelCard");
        DestroyChild(canvasGo, "StartButton");
        DestroyChild(canvasGo, "Tagline");       // remove if exists from prev patch

        // ── Update subtitle ───────────────────────────────────────────────────
        var subT = canvasGo.transform.Find("Subtitle");
        if (subT != null)
        {
            var tmp = subT.GetComponent<TextMeshProUGUI>();
            if (tmp) tmp.text = "Card-Driven Tactical RPG";
            var r = subT.GetComponent<RectTransform>();
            if (r) r.anchoredPosition = new Vector2(0f, 155f);
        }

        // ── Tagline ───────────────────────────────────────────────────────────
        var taglineGo = new GameObject("Tagline");
        taglineGo.transform.SetParent(canvasGo.transform, false);
        var taglineTMP = taglineGo.AddComponent<TextMeshProUGUI>();
        taglineTMP.text      = "Build your deck. Fight your way. Survive.";
        taglineTMP.fontSize  = 22f;
        taglineTMP.alignment = TextAlignmentOptions.Center;
        taglineTMP.color     = new Color(0.55f, 0.55f, 0.75f);
        var taglineRect = taglineGo.GetComponent<RectTransform>();
        taglineRect.anchorMin = new Vector2(0.5f, 0.5f);
        taglineRect.anchorMax = new Vector2(0.5f, 0.5f);
        taglineRect.anchoredPosition = new Vector2(0f, 100f);
        taglineRect.sizeDelta = new Vector2(700f, 40f);

        // ── Buttons ───────────────────────────────────────────────────────────
        var newRunBtn  = GetOrBuildButton(canvasGo.transform, "NewRunButton",  "NEW RUN",
            new Color(0.8f, 0.6f, 0.05f), new Color(0.05f, 0.04f, 0.02f),
            new Vector2(0f, 0f),    new Vector2(280f, 60f));

        var continueBtn = GetOrBuildButton(canvasGo.transform, "ContinueButton", "CONTINUE",
            new Color(0.18f, 0.36f, 0.55f), new Color(0.85f, 0.9f, 1f),
            new Vector2(0f, -80f),  new Vector2(280f, 60f));

        var quitBtn = GetOrBuildButton(canvasGo.transform, "QuitButton", "QUIT",
            new Color(0.22f, 0.10f, 0.10f), new Color(0.9f, 0.6f, 0.6f),
            new Vector2(0f, -160f), new Vector2(180f, 44f));

        // ── Wire MainMenuUI ───────────────────────────────────────────────────
        var mm = canvasGo.GetComponent<MainMenuUI>();
        if (mm == null) mm = canvasGo.AddComponent<MainMenuUI>();

        mm.TitleText    = canvasGo.transform.Find("Title")?.GetComponent<TextMeshProUGUI>();
        mm.SubtitleText = canvasGo.transform.Find("Subtitle")?.GetComponent<TextMeshProUGUI>();
        mm.NewRunButton   = newRunBtn;
        mm.ContinueButton = continueBtn;
        mm.QuitButton     = quitBtn;
        mm.ContinueRow    = null; // optional row — not needed in flat layout

        EditorUtility.SetDirty(canvasGo);
        UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(
            UnityEngine.SceneManagement.SceneManager.GetActiveScene());
        UnityEditor.SceneManagement.EditorSceneManager.SaveScene(
            UnityEngine.SceneManagement.SceneManager.GetActiveScene());

        Debug.Log("[PatchMainMenu] ✅ MainMenu scene patched and saved.");
    }

    // ── Helpers ───────────────────────────────────────────────────────────────

    static void DestroyChild(GameObject root, string childName)
    {
        var t = root.transform.Find(childName);
        if (t != null) Object.DestroyImmediate(t.gameObject);
    }

    static Button GetOrBuildButton(Transform parent, string name, string label,
        Color bgColor, Color textColor, Vector2 pos, Vector2 size)
    {
        var existing = parent.Find(name);
        if (existing != null) { SetButtonLabel(existing.gameObject, label); return existing.GetComponent<Button>(); }

        var go  = new GameObject(name);
        go.transform.SetParent(parent, false);
        var img = go.AddComponent<Image>();
        img.color = bgColor;
        var btn = go.AddComponent<Button>();
        var cols = btn.colors;
        cols.highlightedColor = bgColor * 1.3f;
        cols.pressedColor     = bgColor * 0.7f;
        btn.colors = cols;
        btn.targetGraphic = img;
        var r = go.GetComponent<RectTransform>();
        r.anchorMin = new Vector2(0.5f, 0.5f);
        r.anchorMax = new Vector2(0.5f, 0.5f);
        r.pivot     = new Vector2(0.5f, 0.5f);
        r.anchoredPosition = pos;
        r.sizeDelta = size;

        var labelGo  = new GameObject("Label");
        labelGo.transform.SetParent(go.transform, false);
        var tmp = labelGo.AddComponent<TextMeshProUGUI>();
        tmp.text      = label;
        tmp.fontSize  = 22f;
        tmp.color     = textColor;
        tmp.fontStyle = FontStyles.Bold;
        tmp.alignment = TextAlignmentOptions.Center;
        var lr = labelGo.GetComponent<RectTransform>();
        lr.anchorMin  = Vector2.zero;
        lr.anchorMax  = Vector2.one;
        lr.offsetMin  = Vector2.zero;
        lr.offsetMax  = Vector2.zero;

        return btn;
    }

    static void SetButtonLabel(GameObject btnGo, string text)
    {
        var tmp = btnGo.GetComponentInChildren<TextMeshProUGUI>();
        if (tmp) tmp.text = text;
    }
}
