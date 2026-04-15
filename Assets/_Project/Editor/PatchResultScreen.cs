using UnityEngine;
using UnityEditor;
using UnityEngine.UI;
using TMPro;
using RPG.UI;

/// <summary>
/// Patches the ResultScreen in CombatStage to match the new FTL game loop:
///   Victory:  "Continue →"  button  (was "Return" / nothing wired)
///   Defeat:   "Retry"  +  "Abandon Run"  buttons
///
/// Run via  RPG > Patch → Result Screen Buttons
/// </summary>
public class PatchResultScreen
{
    [MenuItem("RPG/Patch → Result Screen Buttons")]
    public static void Execute()
    {
        var resultGo = GameObject.Find("UICanvas/ResultScreen");
        if (resultGo == null) { Debug.LogError("[PatchResultScreen] UICanvas/ResultScreen not found. Open CombatStage first."); return; }

        var rs = resultGo.GetComponent<ResultScreenUI>();
        if (rs == null) { Debug.LogError("[PatchResultScreen] ResultScreenUI component missing."); return; }

        // ── Wire text refs ────────────────────────────────────────────────────
        rs.ResultText = FindTMP(resultGo, "ResultText");
        rs.FlavorText = FindTMP(resultGo, "FlavorText");

        // ── RetryBtn — already exists, just re-wire ───────────────────────────
        var retryGo = FindChild(resultGo, "RetryBtn");
        if (retryGo != null)
        {
            rs.RetryButton = retryGo.GetComponent<Button>();
            SetLabel(retryGo, "Retry");
        }

        // ── ReturnBtn → rename to AbandonBtn, re-label ────────────────────────
        var abandonGo = FindChild(resultGo, "ReturnBtn") ?? FindChild(resultGo, "AbandonBtn");
        if (abandonGo != null)
        {
            abandonGo.name = "AbandonBtn";
            rs.AbandonButton = abandonGo.GetComponent<Button>();
            rs.AbandonLabel  = GetOrAddLabel(abandonGo);
            SetLabel(abandonGo, "Abandon Run");
        }

        // ── ContinueBtn — create if missing ───────────────────────────────────
        var continueGo = FindChild(resultGo, "ContinueBtn");
        if (continueGo == null)
            continueGo = BuildButton(resultGo.transform, "ContinueBtn",
                new Color(0.45f, 0.30f, 0.02f), new Vector2(0f, -100f), new Vector2(260f, 58f));

        rs.ContinueButton = continueGo.GetComponent<Button>();
        rs.ContinueLabel  = GetOrAddLabel(continueGo);
        SetLabel(continueGo, "Continue  →");

        // ── Position buttons neatly ────────────────────────────────────────────
        // Victory: ContinueBtn centred
        SetAnchored(continueGo, new Vector2(0f, -100f));
        // Defeat: RetryBtn left, AbandonBtn right
        if (retryGo  != null) SetAnchored(retryGo,  new Vector2(-140f, -100f));
        if (abandonGo != null) SetAnchored(abandonGo, new Vector2( 140f, -100f));

        EditorUtility.SetDirty(rs);
        UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(
            UnityEngine.SceneManagement.SceneManager.GetActiveScene());
        UnityEditor.SceneManagement.EditorSceneManager.SaveScene(
            UnityEngine.SceneManagement.SceneManager.GetActiveScene());

        Debug.Log("[PatchResultScreen] ✅ ResultScreen buttons patched and saved.");
    }

    // ── Helpers ───────────────────────────────────────────────────────────────

    static TextMeshProUGUI FindTMP(GameObject root, string childName)
    {
        var t = root.transform.Find(childName);
        return t != null ? t.GetComponent<TextMeshProUGUI>() : null;
    }

    static GameObject FindChild(GameObject root, string name)
    {
        var t = root.transform.Find(name);
        return t != null ? t.gameObject : null;
    }

    static void SetLabel(GameObject btnGo, string text)
    {
        var label = GetOrAddLabel(btnGo);
        if (label != null) label.text = text;
    }

    static TextMeshProUGUI GetOrAddLabel(GameObject btnGo)
    {
        // Check for a "Label" child first
        var labelT = btnGo.transform.Find("Label");
        if (labelT != null) return labelT.GetComponent<TextMeshProUGUI>();
        // Fall back to any TMP on the button itself or its children
        return btnGo.GetComponentInChildren<TextMeshProUGUI>();
    }

    static GameObject BuildButton(Transform parent, string name, Color bg, Vector2 pos, Vector2 size)
    {
        var go  = new GameObject(name);
        go.transform.SetParent(parent, false);
        var img = go.AddComponent<Image>(); img.color = bg;
        var btn = go.AddComponent<Button>();
        var cols = btn.colors;
        cols.highlightedColor = bg * 1.3f; cols.pressedColor = bg * 0.7f;
        btn.colors = cols; btn.targetGraphic = img;
        var r = go.GetComponent<RectTransform>();
        r.anchorMin = new Vector2(0.5f, 0.5f); r.anchorMax = new Vector2(0.5f, 0.5f);
        r.pivot = new Vector2(0.5f, 0.5f);
        r.anchoredPosition = pos; r.sizeDelta = size;

        var labelGo = new GameObject("Label");
        labelGo.transform.SetParent(go.transform, false);
        var tmp = labelGo.AddComponent<TextMeshProUGUI>();
        tmp.text = "Button"; tmp.fontSize = 22f;
        tmp.color = Color.white; tmp.fontStyle = FontStyles.Bold;
        tmp.alignment = TextAlignmentOptions.Center;
        var lr = labelGo.GetComponent<RectTransform>();
        lr.anchorMin = Vector2.zero; lr.anchorMax = Vector2.one;
        lr.offsetMin = Vector2.zero; lr.offsetMax = Vector2.zero;

        return go;
    }

    static void SetAnchored(GameObject go, Vector2 pos)
    {
        var r = go.GetComponent<RectTransform>();
        if (r == null) return;
        r.anchorMin = new Vector2(0.5f, 0.5f);
        r.anchorMax = new Vector2(0.5f, 0.5f);
        r.pivot     = new Vector2(0.5f, 0.5f);
        r.anchoredPosition = pos;
    }
}
