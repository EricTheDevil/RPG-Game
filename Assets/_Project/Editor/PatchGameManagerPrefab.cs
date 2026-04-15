using UnityEngine;
using UnityEditor;
using RPG.Core;
using RPG.Data;

/// <summary>
/// Wires all GameManager prefab fields that require asset references:
///   - StarterCards (Skirmish, Ambush, Field Dressing, Campfire)
///   - FallbackCombatCard (Skirmish — always playable when hand is empty)
///   - DefaultDropTable
///   - CardRegistry
///
/// Run via  RPG > Patch → GameManager Prefab Fields
/// </summary>
public class PatchGameManagerPrefab
{
    const string PrefabPath    = "Assets/_Project/Prefabs/GameManager.prefab";
    const string CardsDir      = "Assets/_Project/ScriptableObjects/Cards";
    const string TablesDir     = "Assets/_Project/ScriptableObjects/DropTables";
    const string RegistryPath  = "Assets/_Project/ScriptableObjects/EventCardRegistry.asset";

    [MenuItem("RPG/Patch → GameManager Prefab Fields")]
    public static void Execute()
    {
        var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath);
        if (prefab == null) { Debug.LogError("[PatchGM] GameManager.prefab not found."); return; }

        var gm = prefab.GetComponent<GameManager>();
        if (gm == null) { Debug.LogError("[PatchGM] No GameManager component."); return; }

        // Starter hand
        var skirmish  = Load<EventCardSO>($"{CardsDir}/Card_Combat_Skirmish.asset");
        var ambush    = Load<EventCardSO>($"{CardsDir}/Card_Combat_Ambush.asset");
        var bandage   = Load<EventCardSO>($"{CardsDir}/Card_Heal_Bandage.asset");
        var campfire  = Load<EventCardSO>($"{CardsDir}/Card_Rest_Campfire.asset");

        if (skirmish && ambush && bandage && campfire)
            gm.StarterCards = new[] { skirmish, ambush, bandage, campfire };
        else
            Debug.LogWarning("[PatchGM] One or more starter card assets missing — check CardsDir.");

        // Fallback (Skirmish — always available)
        if (skirmish) gm.FallbackCombatCard = skirmish;

        // Drop table
        var tier1 = Load<DropTableSO>($"{TablesDir}/DropTable_Tier1.asset");
        if (tier1) gm.DefaultDropTable = tier1;
        else Debug.LogWarning("[PatchGM] DropTable_Tier1.asset not found.");

        // Card registry
        var reg = Load<EventCardRegistry>(RegistryPath);
        if (reg) gm.CardRegistry = reg;
        else Debug.LogWarning("[PatchGM] EventCardRegistry.asset not found — run 'RPG > Create → Event Cards + Deck Scene' first.");

        EditorUtility.SetDirty(prefab);
        PrefabUtility.SavePrefabAsset(prefab);
        Debug.Log("[PatchGM] ✅ GameManager prefab fields wired.");
    }

    static T Load<T>(string path) where T : Object
        => AssetDatabase.LoadAssetAtPath<T>(path);
}
