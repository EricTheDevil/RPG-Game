#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;
using RPG.Data;
using RPG.Core;

namespace RPG.Editor
{
    /// <summary>
    /// Creates the three Beginner ClassDefinitionSO assets (Warrior, Mage, Ranger)
    /// and wires them into GameManager.BeginnerClasses[].
    ///
    /// Menu: RPG > Classes > Create Beginner Classes
    /// </summary>
    public static class CreateBeginnerClasses
    {
        private const string ClassDir = "Assets/_Project/ScriptableObjects/Classes";

        [MenuItem("RPG/Classes/Create Beginner Classes")]
        public static void Create()
        {
            EnsureFolder();

            var heroStats = AssetDatabase.LoadAssetAtPath<UnitStatsSO>(
                "Assets/_Project/ScriptableObjects/Stats/HeroStats.asset");

            var warrior = GetOrCreate("Class_Warrior", cls =>
            {
                cls.ClassName   = "Warrior";
                cls.Description = "A frontline fighter who excels at absorbing damage and dealing physical blows.";
                cls.ClassColor  = new Color(0.85f, 0.35f, 0.25f);
                cls.Tier        = ClassTier.Beginner;
                cls.MaxLevel    = 10;
                cls.BaseStatsSO = heroStats;
                cls.StatGrowthPerLevel = new ClassStatGrowth
                    { MaxHP = 15, MaxMP = 2, Attack = 3, Defense = 2, MagicAttack = 0, Speed = 0, CritChance = 0.005f };
            });

            var mage = GetOrCreate("Class_Mage", cls =>
            {
                cls.ClassName   = "Mage";
                cls.Description = "A fragile spellcaster who devastates enemies with arcane power.";
                cls.ClassColor  = new Color(0.35f, 0.55f, 0.95f);
                cls.Tier        = ClassTier.Beginner;
                cls.MaxLevel    = 10;
                cls.BaseStatsSO = heroStats;
                cls.StatGrowthPerLevel = new ClassStatGrowth
                    { MaxHP = 6, MaxMP = 8, Attack = 1, Defense = 0, MagicAttack = 4, Speed = 0, CritChance = 0.01f };
            });

            var ranger = GetOrCreate("Class_Ranger", cls =>
            {
                cls.ClassName   = "Ranger";
                cls.Description = "A nimble skirmisher who strikes fast and keeps enemies off-balance.";
                cls.ClassColor  = new Color(0.35f, 0.75f, 0.45f);
                cls.Tier        = ClassTier.Beginner;
                cls.MaxLevel    = 10;
                cls.BaseStatsSO = heroStats;
                cls.StatGrowthPerLevel = new ClassStatGrowth
                    { MaxHP = 8, MaxMP = 3, Attack = 2, Defense = 1, MagicAttack = 1, Speed = 1, CritChance = 0.015f };
            });

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            WireIntoGameManager(warrior, mage, ranger);

            Debug.Log("[CreateBeginnerClasses] Warrior / Mage / Ranger created and wired into GameManager.");
        }

        // ── Helpers ───────────────────────────────────────────────────────────

        private static ClassDefinitionSO GetOrCreate(string assetName, System.Action<ClassDefinitionSO> configure)
        {
            string path = $"{ClassDir}/{assetName}.asset";
            var existing = AssetDatabase.LoadAssetAtPath<ClassDefinitionSO>(path);
            if (existing != null)
            {
                // Re-apply defaults only if field is missing; don't overwrite hand-tuned data.
                EditorUtility.SetDirty(existing);
                return existing;
            }

            var cls = ScriptableObject.CreateInstance<ClassDefinitionSO>();
            configure(cls);
            AssetDatabase.CreateAsset(cls, path);
            return cls;
        }

        private static void WireIntoGameManager(
            ClassDefinitionSO warrior, ClassDefinitionSO mage, ClassDefinitionSO ranger)
        {
            var gmPrefab = Resources.Load<GameManager>("GameManager");
            if (gmPrefab == null)
            {
                Debug.LogError("[CreateBeginnerClasses] GameManager prefab not found in a Resources/ folder. " +
                               "Drag Warrior/Mage/Ranger into GameManager.BeginnerClasses[] manually.");
                return;
            }

            // Work on the actual prefab asset via PrefabUtility so changes persist
            string prefabPath = AssetDatabase.GetAssetPath(gmPrefab);
            var root = PrefabUtility.LoadPrefabContents(prefabPath);
            var gm   = root.GetComponent<GameManager>();
            if (gm == null)
            {
                PrefabUtility.UnloadPrefabContents(root);
                Debug.LogError("[CreateBeginnerClasses] GameManager component not found on prefab root.");
                return;
            }

            gm.BeginnerClasses = new ClassDefinitionSO[] { warrior, mage, ranger };

            PrefabUtility.SaveAsPrefabAsset(root, prefabPath);
            PrefabUtility.UnloadPrefabContents(root);

            Debug.Log($"[CreateBeginnerClasses] BeginnerClasses wired: {warrior.ClassName}, {mage.ClassName}, {ranger.ClassName}");
        }

        private static void EnsureFolder()
        {
            if (!AssetDatabase.IsValidFolder("Assets/_Project/ScriptableObjects"))
                AssetDatabase.CreateFolder("Assets/_Project", "ScriptableObjects");
            if (!AssetDatabase.IsValidFolder(ClassDir))
                AssetDatabase.CreateFolder("Assets/_Project/ScriptableObjects", "Classes");
        }
    }
}
#endif
