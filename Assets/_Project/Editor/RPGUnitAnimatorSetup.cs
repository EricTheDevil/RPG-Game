using UnityEngine;
using UnityEditor;
using UnityEditor.Animations;
using RPG.Units;

/// <summary>
/// Creates a shared AnimatorController with the triggers/bools that Unit.cs
/// expects (IsMoving, Attack, Hit, Die) and assigns it to HeroPrefab and
/// EnemyPrefab.  Run from  RPG > Setup Unit Animators.
/// </summary>
public class RPGUnitAnimatorSetup
{
    private const string ControllerPath = "Assets/_Project/Animations/UnitAnimator.controller";
    private const string HeroPrefabPath  = "Assets/_Project/Prefabs/HeroPrefab.prefab";
    private const string EnemyPrefabPath = "Assets/_Project/Prefabs/EnemyPrefab.prefab";

    [MenuItem("RPG/Setup Unit Animators")]
    public static void Execute()
    {
        EnsureFolder("Assets/_Project/Animations");

        var controller = BuildController();
        WirePrefab(HeroPrefabPath,  controller);
        WirePrefab(EnemyPrefabPath, controller);

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log("[RPGUnitAnimatorSetup] ✅ Unit animators created and assigned.");
    }

    static AnimatorController BuildController()
    {
        // Reuse existing controller if present
        var existing = AssetDatabase.LoadAssetAtPath<AnimatorController>(ControllerPath);
        if (existing != null) return existing;

        var ctrl = AnimatorController.CreateAnimatorControllerAtPath(ControllerPath);

        // ── Parameters ─────────────────────────────────────────────────────────
        ctrl.AddParameter("IsMoving", AnimatorControllerParameterType.Bool);
        ctrl.AddParameter("Attack",   AnimatorControllerParameterType.Trigger);
        ctrl.AddParameter("Hit",      AnimatorControllerParameterType.Trigger);
        ctrl.AddParameter("Die",      AnimatorControllerParameterType.Trigger);

        var root = ctrl.layers[0].stateMachine;

        // ── States ──────────────────────────────────────────────────────────────
        var idle   = root.AddState("Idle");
        var move   = root.AddState("Move");
        var attack = root.AddState("Attack");
        var hit    = root.AddState("Hit");
        var die    = root.AddState("Die");

        root.defaultState = idle;

        // ── Transitions ─────────────────────────────────────────────────────────
        // Idle ↔ Move
        AddBoolTransition(idle,   move,  "IsMoving", true,  0.05f);
        AddBoolTransition(move,   idle,  "IsMoving", false, 0.05f);

        // Any State → Attack / Hit / Die  (can interrupt idle or move)
        AddTriggerFromAny(root, attack, "Attack", 0.1f);
        AddTriggerFromAny(root, hit,    "Hit",    0.05f);
        AddTriggerFromAny(root, die,    "Die",    0.1f);

        // Return to Idle after Attack / Hit
        AddExitTransition(attack, idle, 0.25f);
        AddExitTransition(hit,    idle, 0.2f);
        // Die has no return transition — unit is destroyed after death anim

        return ctrl;
    }

    static void WirePrefab(string prefabPath, AnimatorController ctrl)
    {
        var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);
        if (prefab == null) { Debug.LogWarning($"[RPGUnitAnimatorSetup] Prefab not found: {prefabPath}"); return; }

        using (var scope = new PrefabUtility.EditPrefabContentsScope(prefabPath))
        {
            var root = scope.prefabContentsRoot;

            // Add Animator component if missing
            var animator = root.GetComponent<Animator>();
            if (animator == null) animator = root.AddComponent<Animator>();
            animator.runtimeAnimatorController = ctrl;
            animator.applyRootMotion = false;

            // Assign to Unit.UnitAnimator field
            var unit = root.GetComponent<Unit>();
            if (unit != null)
            {
                // Use SerializedObject to set the field properly
                var so  = new SerializedObject(unit);
                var prop = so.FindProperty("UnitAnimator");
                if (prop != null)
                {
                    prop.objectReferenceValue = animator;
                    so.ApplyModifiedProperties();
                }
            }
        }
    }

    // ── Helpers ──────────────────────────────────────────────────────────────────

    static void AddBoolTransition(AnimatorState from, AnimatorState to,
        string param, bool value, float duration)
    {
        var t = from.AddTransition(to);
        t.hasExitTime          = false;
        t.duration             = duration;
        t.AddCondition(value ? AnimatorConditionMode.If : AnimatorConditionMode.IfNot, 0, param);
    }

    static void AddTriggerFromAny(AnimatorStateMachine sm, AnimatorState to,
        string trigger, float duration)
    {
        var t = sm.AddAnyStateTransition(to);
        t.hasExitTime = false;
        t.duration    = duration;
        t.canTransitionToSelf = false;
        t.AddCondition(AnimatorConditionMode.If, 0, trigger);
    }

    static void AddExitTransition(AnimatorState from, AnimatorState to, float exitTime)
    {
        var t = from.AddTransition(to);
        t.hasExitTime = true;
        t.exitTime    = exitTime;
        t.duration    = 0.1f;
    }

    static void EnsureFolder(string path)
    {
        if (!AssetDatabase.IsValidFolder(path))
        {
            var parts  = path.Split('/');
            string parent = string.Join("/", parts[..^1]);
            AssetDatabase.CreateFolder(parent, parts[^1]);
        }
    }
}
