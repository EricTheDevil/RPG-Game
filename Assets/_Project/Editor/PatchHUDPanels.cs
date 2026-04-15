using UnityEngine;
using UnityEditor;
using UnityEngine.UI;
using TMPro;
using RPG.UI;
using RPG.Data;

/// <summary>
/// Wires the live CombatStage scene:
///   1. Updates CombatHUD to use PlayerPanels[] / EnemyPanels[] arrays.
///   2. Creates a second EnemyStatus panel for the 2nd enemy.
///   3. Converts BannerRoot → BannerRect.
///   4. Creates a DarkKnight2Stats asset and a UnitSpawnConfig for 1 hero + 2 enemies.
///
/// Run via  RPG > Patch → HUD Panels + SpawnConfig
/// </summary>
public class PatchHUDPanels
{
    [MenuItem("RPG/Patch → HUD Panels + SpawnConfig")]
    public static void Execute()
    {
        PatchHUD();
        CreateSpawnConfig();

        var scene = UnityEditor.SceneManagement.EditorSceneManager.GetActiveScene();
        UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(scene);
        UnityEditor.SceneManagement.EditorSceneManager.SaveScene(scene);
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log("[PatchHUDPanels] ✅ Done — HUD panels wired, SpawnConfig created.");
    }

    // ── 1. HUD Panel Migration ────────────────────────────────────────────────
    static void PatchHUD()
    {
        var canvasGo = GameObject.Find("UICanvas");
        if (canvasGo == null) { Debug.LogError("[PatchHUDPanels] UICanvas not found."); return; }

        var hud = canvasGo.GetComponent<CombatHUD>();
        if (hud == null) { Debug.LogError("[PatchHUDPanels] CombatHUD not found."); return; }

        // ── Player panels ─────────────────────────────────────────────────────
        var heroStatus = GameObject.Find("UICanvas/HeroStatus");
        if (heroStatus != null)
        {
            var sp = heroStatus.GetComponent<UnitStatusPanel>();
            if (sp != null)
                hud.PlayerPanels = new[] { sp };
        }

        // ── Enemy panels — find existing + create 2nd if needed ───────────────
        var enemy1 = GameObject.Find("UICanvas/EnemyStatus");
        var enemy2 = GameObject.Find("UICanvas/EnemyStatus2");

        if (enemy2 == null && enemy1 != null)
        {
            // Duplicate the first panel
            enemy2 = Object.Instantiate(enemy1, canvasGo.transform);
            enemy2.name = "EnemyStatus2";

            // Shift it slightly to the left of the first enemy panel
            var e1Rect = enemy1.GetComponent<RectTransform>();
            var e2Rect = enemy2.GetComponent<RectTransform>();
            if (e1Rect != null && e2Rect != null)
            {
                e2Rect.anchorMin        = e1Rect.anchorMin;
                e2Rect.anchorMax        = e1Rect.anchorMax;
                e2Rect.pivot            = e1Rect.pivot;
                // Offset left by panel width + gap
                float panelW = e1Rect.sizeDelta.x;
                e2Rect.anchoredPosition = e1Rect.anchoredPosition +
                    new Vector2(-(panelW + 12f), 0f);
                e2Rect.sizeDelta        = e1Rect.sizeDelta;
            }

            Debug.Log("[PatchHUDPanels] Created EnemyStatus2 panel.");
        }

        // Build enemy array from whatever exists
        var enemyPanels = new System.Collections.Generic.List<UnitStatusPanel>();
        if (enemy1 != null) { var sp = enemy1.GetComponent<UnitStatusPanel>(); if (sp) enemyPanels.Add(sp); }
        if (enemy2 != null) { var sp = enemy2.GetComponent<UnitStatusPanel>(); if (sp) enemyPanels.Add(sp); }
        hud.EnemyPanels = enemyPanels.ToArray();

        // ── Banner: ensure BannerRect is set (not BannerRoot) ─────────────────
        var bannerGo = GameObject.Find("UICanvas/PhaseBanner");
        if (bannerGo != null)
        {
            hud.BannerRect = bannerGo.GetComponent<RectTransform>();
            if (hud.BannerText == null)
                hud.BannerText  = bannerGo.GetComponentInChildren<TextMeshProUGUI>();
            if (hud.BannerGroup == null)
                hud.BannerGroup = bannerGo.GetComponent<CanvasGroup>();
        }

        EditorUtility.SetDirty(hud);
        Debug.Log("[PatchHUDPanels] CombatHUD panels wired.");
    }

    // ── 2. Create SpawnConfig ─────────────────────────────────────────────────
    static void CreateSpawnConfig()
    {
        const string configPath = "Assets/_Project/ScriptableObjects/Encounters/Level1_SpawnConfig.asset";
        const string statsDir   = "Assets/_Project/ScriptableObjects/Stats";
        const string abilitiesDir = "Assets/_Project/ScriptableObjects/Abilities";

        // Ensure folder exists
        if (!AssetDatabase.IsValidFolder("Assets/_Project/ScriptableObjects/Encounters"))
            AssetDatabase.CreateFolder("Assets/_Project/ScriptableObjects", "Encounters");

        // Load (or skip if already exists)
        var existing = AssetDatabase.LoadAssetAtPath<UnitSpawnConfig>(configPath);
        if (existing != null)
        {
            Debug.Log("[PatchHUDPanels] SpawnConfig already exists — skipping creation.");
            WireConfigToManager(existing);
            return;
        }

        // Load stats
        var heroStats    = AssetDatabase.LoadAssetAtPath<UnitStatsSO>($"{statsDir}/HeroStats.asset");
        var enemyStats   = AssetDatabase.LoadAssetAtPath<UnitStatsSO>($"{statsDir}/DarkKnightStats.asset");

        // Create a "Shadowed Knight" variant for the 2nd enemy (clone of DarkKnight, weaker)
        string enemy2Path = statsDir + "/ShadowedKnightStats.asset";
        var enemy2Stats = AssetDatabase.LoadAssetAtPath<UnitStatsSO>(enemy2Path);
        if (enemy2Stats == null && enemyStats != null)
        {
            enemy2Stats = Object.Instantiate(enemyStats);
            enemy2Stats.UnitName  = "Shadowed Knight";
            enemy2Stats.MaxHP     = 120;
            enemy2Stats.Attack    = 14;
            enemy2Stats.Defense   = 8;
            enemy2Stats.Speed     = 8;
            AssetDatabase.CreateAsset(enemy2Stats, enemy2Path);
            EditorUtility.SetDirty(enemy2Stats);
            Debug.Log("[PatchHUDPanels] Created ShadowedKnightStats asset.");
        }

        // Load prefabs
        var heroPrefab  = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/_Project/Prefabs/HeroPrefab.prefab");
        var enemyPrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/_Project/Prefabs/EnemyPrefab.prefab");

        // Load abilities
        var atkAb  = AssetDatabase.LoadAssetAtPath<AbilitySO>($"{abilitiesDir}/Attack.asset");
        var defAb  = AssetDatabase.LoadAssetAtPath<AbilitySO>($"{abilitiesDir}/Defend.asset");
        var spcAb  = AssetDatabase.LoadAssetAtPath<AbilitySO>($"{abilitiesDir}/Special.asset");
        var eAtkAb = AssetDatabase.LoadAssetAtPath<AbilitySO>($"{abilitiesDir}/EnemyAttack.asset");
        var eSpcAb = AssetDatabase.LoadAssetAtPath<AbilitySO>($"{abilitiesDir}/EnemySpecial.asset");

        var heroUnit  = heroPrefab?.GetComponent<RPG.Units.Unit>();
        var enemyUnit = enemyPrefab?.GetComponent<RPG.Units.Unit>();

        var config = ScriptableObject.CreateInstance<UnitSpawnConfig>();

        config.PlayerUnits = new[]
        {
            new UnitSpawnEntry
            {
                Prefab    = heroUnit,
                Stats     = heroStats,
                Abilities = new[] { atkAb, defAb, spcAb },
                GridCell  = new Vector2Int(1, 4),
            }
        };

        config.EnemyUnits = new[]
        {
            new UnitSpawnEntry
            {
                Prefab    = enemyUnit,
                Stats     = enemyStats,
                Abilities = new[] { eAtkAb, eSpcAb },
                GridCell  = new Vector2Int(6, 3),
            },
            new UnitSpawnEntry
            {
                Prefab    = enemyUnit,
                Stats     = enemy2Stats,
                Abilities = new[] { eAtkAb, eSpcAb },
                GridCell  = new Vector2Int(6, 5),
            }
        };

        AssetDatabase.CreateAsset(config, configPath);
        EditorUtility.SetDirty(config);

        WireConfigToManager(config);
        Debug.Log("[PatchHUDPanels] UnitSpawnConfig created with 1 hero + 2 enemies.");
    }

    static void WireConfigToManager(UnitSpawnConfig config)
    {
        var cmGo = GameObject.Find("CombatManager");
        if (cmGo == null) return;
        var cm = cmGo.GetComponent<RPG.Combat.CombatManager>();
        if (cm == null) return;
        cm.SpawnConfig = config;
        EditorUtility.SetDirty(cm);
        Debug.Log("[PatchHUDPanels] SpawnConfig wired to CombatManager.");
    }
}
