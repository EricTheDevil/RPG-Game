using UnityEngine;
using UnityEditor;
using UnityEngine.UI;
using TMPro;
using RPG.Combat;
using RPG.UI;

/// <summary>
/// Wires remaining loose ends after the autobattle patch:
///   1. CombatLog → adds ScrollRect + wires LogScroll on CombatHUD
///   2. CombatManager spawn positions (Vector2Int arrays)
///   3. WorldCanvas on CombatVFXManager
///   4. Ensures ActionMenu, InstructionBar are fully hidden
/// </summary>
public class RPGFinalWire
{
    [MenuItem("RPG/Patch → Final Wire")]
    public static void Execute()
    {
        PatchLogScroll();
        PatchSpawnPositions();
        PatchVFXWorldCanvas();
        HideDeprecatedUI();
        MaxLogLines();

        var scene = UnityEditor.SceneManagement.EditorSceneManager.GetActiveScene();
        UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(scene);
        UnityEditor.SceneManagement.EditorSceneManager.SaveScene(scene);
        AssetDatabase.SaveAssets();

        Debug.Log("[RPGFinalWire] ✅ Final wiring complete!");
    }

    // ── Log ScrollRect ────────────────────────────────────────────────────────
    static void PatchLogScroll()
    {
        var logGo = GameObject.Find("UICanvas/CombatLog");
        if (logGo == null) { Debug.Log("[RPGFinalWire] CombatLog not found — skipping scroll."); return; }

        var sr = logGo.GetComponent<ScrollRect>();
        if (sr == null)
        {
            sr = logGo.AddComponent<ScrollRect>();
            sr.horizontal = false;
            sr.vertical   = true;
            sr.scrollSensitivity = 20f;
            sr.movementType = ScrollRect.MovementType.Clamped;

            // Content = LogText rect
            var logText = logGo.GetComponentInChildren<TextMeshProUGUI>();
            if (logText != null) sr.content = logText.rectTransform;

            Debug.Log("[RPGFinalWire] Added ScrollRect to CombatLog.");
        }

        // Wire to HUD
        var hud = GameObject.Find("UICanvas")?.GetComponent<CombatHUD>();
        if (hud != null && hud.LogScroll == null)
        {
            hud.LogScroll = sr;
            EditorUtility.SetDirty(hud);
            Debug.Log("[RPGFinalWire] Wired LogScroll to CombatHUD.");
        }
    }

    // ── Spawn Positions ───────────────────────────────────────────────────────
    static void PatchSpawnPositions()
    {
        var cmGo = GameObject.Find("CombatManager");
        if (cmGo == null) return;
        var cm = cmGo.GetComponent<CombatManager>();
        if (cm == null) return;

        // Set explicitly — one player spawn, one enemy spawn for demo
        cm.PlayerSpawns = new[] { new Vector2Int(1, 3) };
        cm.EnemySpawns  = new[] { new Vector2Int(6, 4) };

        EditorUtility.SetDirty(cm);
        Debug.Log("[RPGFinalWire] Spawn positions set: Hero(1,3) Enemy(6,4).");
    }

    // ── VFX WorldCanvas ───────────────────────────────────────────────────────
    static void PatchVFXWorldCanvas()
    {
        var vfxGo = GameObject.Find("CombatVFXManager");
        if (vfxGo == null) return;
        var vfx = vfxGo.GetComponent<RPG.VFX.CombatVFXManager>();
        if (vfx == null) return;

        if (vfx.WorldCanvas == null)
        {
            var canvas = GameObject.Find("UICanvas")?.GetComponent<Canvas>();
            if (canvas != null)
            {
                vfx.WorldCanvas = canvas;
                EditorUtility.SetDirty(vfx);
                Debug.Log("[RPGFinalWire] Wired WorldCanvas to CombatVFXManager.");
            }
        }
    }

    // ── Hide deprecated / turn-based UI ──────────────────────────────────────
    static void HideDeprecatedUI()
    {
        // ActionMenu was the old player-action panel — hide it
        var actionMenu = GameObject.Find("UICanvas/ActionMenu");
        if (actionMenu != null && actionMenu.activeSelf)
        {
            actionMenu.SetActive(false);
            EditorUtility.SetDirty(actionMenu);
            Debug.Log("[RPGFinalWire] Hid ActionMenu (deprecated in autobattle).");
        }

        // InstructionBar was for player turn guidance — hide it
        var instrBar = GameObject.Find("UICanvas/InstructionBar");
        if (instrBar != null && instrBar.activeSelf)
        {
            instrBar.SetActive(false);
            EditorUtility.SetDirty(instrBar);
            Debug.Log("[RPGFinalWire] Hid InstructionBar (deprecated in autobattle).");
        }
    }

    // ── Bump max log lines ────────────────────────────────────────────────────
    static void MaxLogLines()
    {
        var hud = GameObject.Find("UICanvas")?.GetComponent<CombatHUD>();
        if (hud != null)
        {
            hud.MaxLogLines = 10;
            EditorUtility.SetDirty(hud);
        }
    }
}
