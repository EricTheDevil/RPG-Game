using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine.UI;
using TMPro;
using RPG.UI;
using RPG.Combat;

/// <summary>
/// Applies visual fixes to the CombatStage:
///   1. Reworks tile materials — dark matte base, vivid emissive highlights
///   2. Rebalances scene lighting — cinematic three-point setup
///   3. Injects the PhaseInstruction bar into the HUD
/// Run via  RPG > Fix Visuals
/// </summary>
public class RPGVisualFix
{
    [MenuItem("RPG/Fix Visuals")]
    public static void Execute()
    {
        FixMaterials();
        FixCombatStageScene();
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log("[RPGVisualFix] ✅ Visual fixes applied.");
    }

    // ── MATERIALS ────────────────────────────────────────────────────────────────

    static void FixMaterials()
    {
        // Default tile — dark stone, almost no reflections
        PatchMat("TileDefault",
            albedo:     new Color(0.18f, 0.15f, 0.12f),
            emissive:   Color.black,
            emissiveMult: 0f,
            smoothness: 0.08f, metallic: 0f);

        // Move tile — vivid electric blue
        PatchMat("TileMove",
            albedo:     new Color(0.05f, 0.12f, 0.30f),
            emissive:   new Color(0.10f, 0.55f, 1.00f),
            emissiveMult: 8f,
            smoothness: 0.05f, metallic: 0f);

        // Attack tile — vivid crimson
        PatchMat("TileAttack",
            albedo:     new Color(0.30f, 0.05f, 0.05f),
            emissive:   new Color(1.00f, 0.12f, 0.05f),
            emissiveMult: 8f,
            smoothness: 0.05f, metallic: 0f);

        // Selected tile — vivid gold
        PatchMat("TileSelected",
            albedo:     new Color(0.30f, 0.25f, 0.04f),
            emissive:   new Color(1.00f, 0.80f, 0.10f),
            emissiveMult: 7f,
            smoothness: 0.05f, metallic: 0f);

        // Unit materials
        PatchMat("HeroUnit",
            albedo:     new Color(0.10f, 0.28f, 0.70f),
            emissive:   new Color(0.20f, 0.50f, 1.00f),
            emissiveMult: 1.5f,
            smoothness: 0.25f, metallic: 0.1f);

        PatchMat("EnemyUnit",
            albedo:     new Color(0.55f, 0.05f, 0.05f),
            emissive:   new Color(1.00f, 0.10f, 0.05f),
            emissiveMult: 1.5f,
            smoothness: 0.25f, metallic: 0.1f);
    }

    static void PatchMat(string name, Color albedo, Color emissive, float emissiveMult,
                         float smoothness, float metallic)
    {
        string path = $"Assets/_Project/Materials/{name}.mat";
        var mat = AssetDatabase.LoadAssetAtPath<Material>(path);
        if (mat == null) { Debug.LogWarning($"[RPGVisualFix] Material not found: {path}"); return; }

        // Base colour
        if (mat.HasProperty("_BaseColor"))        mat.SetColor("_BaseColor",        albedo);
        else if (mat.HasProperty("_Color"))       mat.SetColor("_Color",            albedo);

        // Smoothness / metallic
        if (mat.HasProperty("_Smoothness"))       mat.SetFloat("_Smoothness",       smoothness);
        else if (mat.HasProperty("_Glossiness"))  mat.SetFloat("_Glossiness",       smoothness);
        if (mat.HasProperty("_Metallic"))         mat.SetFloat("_Metallic",         metallic);

        // Emissive — HDRP stores in linear HDR, multiply for perceived brightness
        Color hdrEmissive = emissiveMult > 0f ? emissive * emissiveMult : Color.black;
        if (mat.HasProperty("_EmissiveColor"))    mat.SetColor("_EmissiveColor",    hdrEmissive);
        else if (mat.HasProperty("_EmissionColor")) mat.SetColor("_EmissionColor",  hdrEmissive);

        mat.EnableKeyword("_EMISSION");
        mat.globalIlluminationFlags = MaterialGlobalIlluminationFlags.RealtimeEmissive;

        EditorUtility.SetDirty(mat);
    }

    // ── SCENE FIXES ──────────────────────────────────────────────────────────────

    static void FixCombatStageScene()
    {
        // Open the scene
        string scenePath = "Assets/_Project/Scenes/CombatStage.unity";
        var scene = EditorSceneManager.OpenScene(scenePath, OpenSceneMode.Single);

        FixLighting();
        AddInstructionBarToHUD();

        EditorSceneManager.SaveScene(scene, scenePath);
    }

    // ── LIGHTING ─────────────────────────────────────────────────────────────────

    static void FixLighting()
    {
        // Patch existing lights — find by name
        PatchLight("KeyLight",  0.65f, new Color(0.95f, 0.88f, 0.78f));
        PatchLight("FillLight", 0.18f, new Color(0.55f, 0.65f, 0.85f));
        PatchLight("RimLight",  0.10f, new Color(0.85f, 0.45f, 0.75f));

        // Much darker ambient — only the emissive tiles should "glow"
        RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Trilight;
        RenderSettings.ambientSkyColor     = new Color(0.06f, 0.07f, 0.12f);
        RenderSettings.ambientEquatorColor = new Color(0.04f, 0.04f, 0.07f);
        RenderSettings.ambientGroundColor  = new Color(0.02f, 0.02f, 0.04f);
    }

    static void PatchLight(string goName, float intensity, Color color)
    {
        var go = GameObject.Find(goName);
        if (go == null) return;
        var light = go.GetComponent<Light>();
        if (light == null) return;
        light.intensity = intensity;
        light.color     = color;
    }

    // ── INSTRUCTION BAR ──────────────────────────────────────────────────────────
    // Removed in autobattle redesign — the instruction bar was for player-action guidance.
    // Speed toggle and phase banner now live in CombatHUD.
    static void AddInstructionBarToHUD()
    {
        Debug.Log("[RPGVisualFix] Instruction bar is deprecated in autobattle mode. Skipping.");
    }
}
