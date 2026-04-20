using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using RPG.Grid;
using RPG.Combat;
using RPG.Data;
using RPG.Units;

public class WireCombatStage
{
    public static void Execute()
    {
        AssetDatabase.Refresh();

        var tilePrefab  = AssetDatabase.LoadAssetAtPath<GridTile>("Assets/_Project/Prefabs/GridTile.prefab");
        var defaultMat  = AssetDatabase.LoadAssetAtPath<Material>("Assets/_Project/Materials/TileDefault.mat");
        var moveMat     = AssetDatabase.LoadAssetAtPath<Material>("Assets/_Project/Materials/TileMove.mat");
        var attackMat   = AssetDatabase.LoadAssetAtPath<Material>("Assets/_Project/Materials/TileAttack.mat");
        var selectedMat = AssetDatabase.LoadAssetAtPath<Material>("Assets/_Project/Materials/TileSelected.mat");
        var heroPrefab  = AssetDatabase.LoadAssetAtPath<HeroUnit>("Assets/_Project/Prefabs/HeroPrefab.prefab");
        var enemyPrefab = AssetDatabase.LoadAssetAtPath<EnemyUnit>("Assets/_Project/Prefabs/EnemyPrefab.prefab");
        var heroStats   = AssetDatabase.LoadAssetAtPath<UnitStatsSO>("Assets/_Project/ScriptableObjects/Stats/HeroStats.asset");
        var enemyStats  = AssetDatabase.LoadAssetAtPath<UnitStatsSO>("Assets/_Project/ScriptableObjects/Stats/ShadowedKnightStats.asset");
        var atkAbility  = AssetDatabase.LoadAssetAtPath<AbilitySO>("Assets/_Project/ScriptableObjects/Abilities/Attack.asset");
        var defAbility  = AssetDatabase.LoadAssetAtPath<AbilitySO>("Assets/_Project/ScriptableObjects/Abilities/Defend.asset");
        var spcAbility  = AssetDatabase.LoadAssetAtPath<AbilitySO>("Assets/_Project/ScriptableObjects/Abilities/Special.asset");
        var eAtk        = AssetDatabase.LoadAssetAtPath<AbilitySO>("Assets/_Project/ScriptableObjects/Abilities/EnemyAttack.asset");
        var eSpc        = AssetDatabase.LoadAssetAtPath<AbilitySO>("Assets/_Project/ScriptableObjects/Abilities/EnemySpecial.asset");
        var spawnCfg    = AssetDatabase.LoadAssetAtPath<UnitSpawnConfig>("Assets/_Project/ScriptableObjects/Encounters/Level1_SpawnConfig.asset");

        Debug.Log($"tilePrefab={tilePrefab}, hero={heroPrefab}, enemy={enemyPrefab}");
        Debug.Log($"heroStats={heroStats}, enemyStats={enemyStats}, spawnCfg={spawnCfg}");
        Debug.Log($"atk={atkAbility}, def={defAbility}, spc={spcAbility}, eAtk={eAtk}, eSpc={eSpc}");

        // Open scene
        var scene = EditorSceneManager.OpenScene("Assets/_Project/Scenes/CombatStage.unity", OpenSceneMode.Single);

        // Wire BattleGrid
        var grid = Object.FindFirstObjectByType<BattleGrid>();
        if (grid == null) { Debug.LogError("BattleGrid not found"); return; }
        grid.TilePrefab    = tilePrefab;
        grid.DefaultTileMat = defaultMat;
        grid.MoveTileMat   = moveMat;
        grid.AttackTileMat = attackMat;
        grid.SelectedTileMat = selectedMat;
        EditorUtility.SetDirty(grid);
        Debug.Log($"BattleGrid wired: TilePrefab={grid.TilePrefab}");

        // Wire CombatManager
        var cm = Object.FindFirstObjectByType<CombatManager>();
        if (cm == null) { Debug.LogError("CombatManager not found"); return; }
        cm.HeroPrefab         = heroPrefab;
        cm.EnemyPrefab        = enemyPrefab;
        cm.HeroStats          = heroStats;
        cm.EnemyStats         = enemyStats;
        cm.AttackAbility      = atkAbility;
        cm.DefendAbility      = defAbility;
        cm.SpecialAbility     = spcAbility;
        cm.EnemyAttackAbility = eAtk;
        cm.EnemySpecialAbility = eSpc;
        cm.SpawnConfig        = spawnCfg;
        EditorUtility.SetDirty(cm);
        Debug.Log($"CombatManager wired: Hero={cm.HeroPrefab}, Enemy={cm.EnemyPrefab}");

        EditorSceneManager.SaveScene(scene);
        Debug.Log("[WireCombatStage] Done.");
    }
}
