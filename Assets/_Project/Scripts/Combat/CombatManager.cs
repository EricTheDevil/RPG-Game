using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using RPG.Grid;
using RPG.Units;
using RPG.Data;
using RPG.VFX;

namespace RPG.Combat
{
    // ─────────────────────────────────────────────────────────────────────────
    //  TFT-Style Autobattle phases
    // ─────────────────────────────────────────────────────────────────────────
    public enum CombatPhase
    {
        Setup,
        Intro,
        Autobattle,
        Victory,
        Defeat
    }

    /// <summary>
    /// TFT-style auto-battle manager.
    ///
    /// Design pillars:
    ///   • Fully data-driven: assign a UnitSpawnConfig to define the encounter.
    ///   • Falls back to legacy inspector fields if no SpawnConfig is set.
    ///   • Supports N player units vs N enemy units.
    ///   • OnSpawned fires with full player + enemy lists for HUD wiring.
    ///   • All combat events fire to CombatHUD for display.
    /// </summary>
    public class CombatManager : MonoBehaviour
    {
        public static CombatManager Instance { get; private set; }

        // ── Scene References ──────────────────────────────────────────────────
        [Header("Scene References")]
        public BattleGrid       Grid;
        public CombatTimeline   Timeline;
        public CombatVFXManager VFXManager;
        public TraitSystem      TraitSystem;

        // ── Data-Driven Config (preferred) ────────────────────────────────────
        [Header("Encounter Config  (assign to override legacy fields)")]
        [Tooltip("If set, all unit/ability/spawn data comes from this asset.")]
        public UnitSpawnConfig SpawnConfig;

        // ── Legacy Inspector Fields (used when SpawnConfig is null) ───────────
        [Header("Legacy — Unit Prefabs (used if SpawnConfig is null)")]
        public HeroUnit  HeroPrefab;
        public EnemyUnit EnemyPrefab;

        [Header("Legacy — Unit Stats")]
        public UnitStatsSO HeroStats;
        public UnitStatsSO EnemyStats;

        [Header("Legacy — Hero Abilities")]
        public AbilitySO AttackAbility;
        public AbilitySO DefendAbility;
        public AbilitySO SpecialAbility;

        [Header("Legacy — Enemy Abilities")]
        public AbilitySO EnemyAttackAbility;
        public AbilitySO EnemySpecialAbility;

        [Header("Legacy — Spawn Positions")]
        public Vector2Int[] PlayerSpawns = { new Vector2Int(1, 2), new Vector2Int(1, 4), new Vector2Int(1, 6) };
        public Vector2Int[] EnemySpawns  = { new Vector2Int(6, 2), new Vector2Int(6, 4), new Vector2Int(6, 6) };

        // ── Autobattle Pacing ─────────────────────────────────────────────────
        [Header("Autobattle Pacing")]
        [Tooltip("Seconds each CT tick takes — lower = faster battle")]
        public float TickInterval    = 0.05f;
        [Tooltip("Pause after a unit acts before resuming ticks")]
        public float PostActionPause = 0.35f;

        // ── Runtime State ─────────────────────────────────────────────────────
        public CombatPhase CurrentPhase { get; private set; } = CombatPhase.Setup;

        private readonly List<Unit> _playerUnits = new();
        private readonly List<Unit> _enemyUnits  = new();
        private readonly List<Unit> _allUnits    = new();

        /// <summary>Applied to all enemy MaxHP and BaseDamage when > 1. Set from PendingCombatCard.DifficultyScale.</summary>
        private float _difficultyScale = 1f;

        // ── Events ────────────────────────────────────────────────────────────
        public event Action<CombatPhase>           OnPhaseChanged;
        public event Action<Unit>                  OnUnitActed;
        public event Action<string>                OnCombatLog;
        public event Action                        OnVictory;
        public event Action                        OnDefeat;

        /// <summary>
        /// Fired once all units are spawned.
        /// Replaces the old (hero, firstEnemy) signature with full team lists.
        /// </summary>
        public event Action<List<Unit>, List<Unit>> OnSpawned;

        // ── Lifecycle ─────────────────────────────────────────────────────────
        private void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(gameObject); return; }
            Instance = this;
        }

        private void Start() => StartCoroutine(SetupCombat());

        // ── Setup ─────────────────────────────────────────────────────────────
        private IEnumerator SetupCombat()
        {
            SetPhase(CombatPhase.Setup);
            Grid.GenerateGrid();
            yield return null;

            // Pull encounter config from the card the player played, if any.
            // This bridges GameManager.PendingCombatCard → CombatManager.SpawnConfig
            // without tight coupling: CombatManager simply asks for what's available.
            var pendingCard = RPG.Core.GameManager.Instance?.PendingCombatCard;
            if (pendingCard != null)
            {
                if (pendingCard.SpawnConfig != null)
                    SpawnConfig = pendingCard.SpawnConfig;

                // Register the card's difficulty scale for future stat scaling
                _difficultyScale = pendingCard.DifficultyScale;

                // Mark this as the active level so CompleteCurrentLevel() works
                RPG.Core.GameSession.Instance?.SetCurrentLevel(null); // card-based runs don't use LevelDataSO
            }

            SpawnUnits();
            ApplyDifficultyScale();
            yield return null;

            TraitSystem?.ApplySynergies(_allUnits);
            Timeline.RegisterUnits(_allUnits);

            // Guard: abort if no units on either side
            if (_playerUnits.Count == 0 || _enemyUnits.Count == 0)
            {
                Debug.LogError("[CombatManager] Combat aborted — no units spawned on one or both sides.");
                yield break;
            }

            SetPhase(CombatPhase.Intro);
            Log("<color=#88CCFF><b>Auto-Battle  —  BEGIN!</b></color>");
            yield return new WaitForSeconds(1.2f);

            SetPhase(CombatPhase.Autobattle);
            StartCoroutine(AutobattleLoop());
        }

        // ── Spawn ─────────────────────────────────────────────────────────────
        private void SpawnUnits()
        {
            if (SpawnConfig != null)
                SpawnFromConfig();
            else
                SpawnLegacy();

            OnSpawned?.Invoke(
                new List<Unit>(_playerUnits),
                new List<Unit>(_enemyUnits));
        }

        private void SpawnFromConfig()
        {
            for (int i = 0; i < SpawnConfig.PlayerUnits.Length; i++)
            {
                var entry = SpawnConfig.PlayerUnits[i];
                if (entry?.Prefab == null || entry.Stats == null) continue;

                var cell = entry.GridCell != Vector2Int.zero
                    ? entry.GridCell
                    : (i < PlayerSpawns.Length ? PlayerSpawns[i] : new Vector2Int(1, i * 2 + 2));

                var unit = SpawnUnit(entry.Prefab, entry.Stats, Team.Player,
                                     cell, Quaternion.Euler(0, 90, 0), entry.Abilities);
                if (unit == null) continue;
                WireUnitVFX(unit);
                _playerUnits.Add(unit);
                _allUnits.Add(unit);
            }

            for (int i = 0; i < SpawnConfig.EnemyUnits.Length; i++)
            {
                var entry = SpawnConfig.EnemyUnits[i];
                if (entry?.Prefab == null || entry.Stats == null) continue;

                var cell = entry.GridCell != Vector2Int.zero
                    ? entry.GridCell
                    : (i < EnemySpawns.Length ? EnemySpawns[i] : new Vector2Int(6, i * 2 + 2));

                var unit = SpawnUnit(entry.Prefab, entry.Stats, Team.Enemy,
                                     cell, Quaternion.Euler(0, 270, 0), entry.Abilities);
                if (unit == null) continue;
                WireUnitVFX(unit);
                _enemyUnits.Add(unit);
                _allUnits.Add(unit);
            }
        }

        private void SpawnLegacy()
        {
            // ── Hero ──────────────────────────────────────────────────────────
            var heroCell = PlayerSpawns.Length > 0 ? PlayerSpawns[0] : new Vector2Int(1, 3);
            var hero = SpawnUnit(HeroPrefab, HeroStats, Team.Player,
                                 heroCell, Quaternion.Euler(0, 90, 0),
                                 new[] { AttackAbility, DefendAbility, SpecialAbility });
            if (hero != null) { WireUnitVFX(hero); _playerUnits.Add(hero); _allUnits.Add(hero); }

            // ── Enemy 1+ ──────────────────────────────────────────────────────
            for (int i = 0; i < EnemySpawns.Length; i++)
            {
                // Only first enemy has a prefab wired in legacy mode
                if (i > 0) break;
                var enemy = SpawnUnit(EnemyPrefab, EnemyStats, Team.Enemy,
                                      EnemySpawns[i], Quaternion.Euler(0, 270, 0),
                                      new[] { EnemyAttackAbility, EnemySpecialAbility });
                if (enemy != null) { WireUnitVFX(enemy); _enemyUnits.Add(enemy); _allUnits.Add(enemy); }
            }
        }

        private Unit SpawnUnit(Unit prefab, UnitStatsSO stats, Team team,
                               Vector2Int gridPos, Quaternion rotation,
                               AbilitySO[] abilities)
        {
            if (prefab == null || stats == null) return null;

            var tile = Grid.GetTile(gridPos);
            if (tile == null)
            {
                Debug.LogWarning($"[CombatManager] No tile at {gridPos} for unit {stats.UnitName}");
                return null;
            }

            var unit = Instantiate(prefab, tile.WorldPosition + Vector3.up * 0.65f, rotation);
            unit.Stats = stats;
            unit.Initialize(team);
            unit.GridPosition = gridPos;

            if (abilities != null)
                foreach (var ab in abilities)
                    if (ab != null) unit.Abilities.Add(ab);

            tile.OccupyingUnit = unit;
            return unit;
        }

        /// <summary>
        /// Scales enemy MaxHP and BaseDamage by _difficultyScale (set from PendingCombatCard).
        /// Called once after spawn so Elite cards feel meaningfully harder than Skirmish cards.
        /// </summary>
        private void ApplyDifficultyScale()
        {
            if (Mathf.Approximately(_difficultyScale, 1f)) return;
            foreach (var u in _enemyUnits)
            {
                if (u?.RT == null) continue;
                u.RT.MaxHP       = Mathf.RoundToInt(u.RT.MaxHP       * _difficultyScale);
                u.RT.Attack      = Mathf.RoundToInt(u.RT.Attack      * _difficultyScale);
                u.RT.MagicAttack = Mathf.RoundToInt(u.RT.MagicAttack * _difficultyScale);
                u.Heal(u.RT.MaxHP); // clamp CurrentHP up to new scaled max
            }
        }

        // ── Per-unit VFX subscriptions ────────────────────────────────────────
        private void WireUnitVFX(Unit unit)
        {
            bool isPlayer = unit.Team == Team.Player;

            unit.OnDamaged += (dmg, crit) =>
            {
                VFXManager?.ShowDamageNumber(dmg, unit.transform.position, crit, false);
                VFXManager?.FlashScreen(crit
                    ? new Color(1f, 0.3f, 0.1f, isPlayer ? 0.35f : 0.3f)
                    : new Color(1f, 1f, 1f, isPlayer ? 0.08f : 0.05f));
                CameraShake.Instance?.Shake(crit);
                if (crit)
                {
                    StartCoroutine(HitPause());
                    VFXManager?.PulseCritPostProcess();
                }
            };

            unit.OnHealed += heal =>
                VFXManager?.ShowDamageNumber(heal, unit.transform.position, false, true);

            unit.OnDeath += () =>
            {
                string msg = isPlayer
                    ? $"<color=#FF6666>{unit.Stats.UnitName} has fallen!</color>"
                    : $"<color=#FFAA44>{unit.Stats.UnitName} is defeated!</color>";
                HandleUnitDeath(unit, msg);
            };
        }

        private void HandleUnitDeath(Unit unit, string logMsg)
        {
            Timeline.RemoveUnit(unit);
            var tile = Grid.GetTile(unit.GridPosition);
            if (tile != null) tile.OccupyingUnit = null;
            Log(logMsg);
        }

        // ── Autobattle Loop ────────────────────────────────────────────────────
        private IEnumerator AutobattleLoop()
        {
            while (CurrentPhase == CombatPhase.Autobattle)
            {
                if (_enemyUnits.All(e => !e.IsAlive))
                {
                    yield return new WaitForSeconds(0.8f);
                    SetPhase(CombatPhase.Victory);
                    OnVictory?.Invoke();
                    yield break;
                }
                if (_playerUnits.All(p => !p.IsAlive))
                {
                    yield return new WaitForSeconds(0.8f);
                    SetPhase(CombatPhase.Defeat);
                    OnDefeat?.Invoke();
                    yield break;
                }

                Unit readyUnit = Timeline.Tick();

                if (readyUnit == null || !readyUnit.IsAlive)
                {
                    yield return new WaitForSeconds(TickInterval);
                    continue;
                }

                readyUnit.StartTurn();
                OnUnitActed?.Invoke(readyUnit);
                Log($"<b>{readyUnit.Stats.UnitName}</b> acts.");

                Grid.ClearAllHighlights();
                Grid.GetTile(readyUnit.GridPosition)?.SetHighlight(TileHighlight.Selected);

                yield return ExecuteUnitTurn(readyUnit);

                Grid.ClearAllHighlights();
                readyUnit.EndTurn();
                yield return new WaitForSeconds(PostActionPause);
            }
        }

        // ── Unit Turn Execution ────────────────────────────────────────────────
        private IEnumerator ExecuteUnitTurn(Unit actor)
        {
            var opponents      = actor.Team == Team.Player ? _enemyUnits  : _playerUnits;
            var allies         = actor.Team == Team.Player ? _playerUnits : _enemyUnits;
            var aliveOpponents = opponents.Where(u => u.IsAlive).ToList();
            var aliveAllies    = allies.Where(u => u.IsAlive).ToList();

            if (aliveOpponents.Count == 0) yield break;

            var (ability, target) = actor.AI.SelectAction(actor, aliveOpponents, aliveAllies, Grid);
            if (ability == null || target == null) yield break;

            int dist = ManhattanDist(actor.GridPosition, target.GridPosition);
            if (dist > ability.Range && !actor.HasMoved)
            {
                var movable = Grid.GetMovableTiles(actor.GridPosition, actor.RT.Movement);
                var bestTile = movable
                    .Where(t => !t.IsOccupied)
                    .OrderBy(t => ManhattanDist(t.GridPos, target.GridPosition))
                    .FirstOrDefault();

                if (bestTile != null)
                {
                    var fromTile = Grid.GetTile(actor.GridPosition);
                    if (fromTile != null) fromTile.OccupyingUnit = null;
                    yield return actor.MoveToTile(bestTile);
                    bestTile.OccupyingUnit = actor;
                }
            }

            dist = ManhattanDist(actor.GridPosition, target.GridPosition);
            if (dist > ability.Range) yield break;

            yield return ExecuteAbility(actor, target, ability);
        }

        // ── Ability Execution ──────────────────────────────────────────────────
        public IEnumerator ExecuteAbility(Unit caster, Unit target, AbilitySO ability)
        {
            Log($"{caster.Stats.UnitName} uses <color=#FFD700><b>{ability.AbilityName}</b></color>!");
            caster.ConsumeMP(ability.MPCost);

            if (target != caster)
            {
                Vector3 dir = (target.transform.position - caster.transform.position);
                dir.y = 0;
                if (dir.sqrMagnitude > 0.01f)
                    caster.transform.rotation = Quaternion.LookRotation(dir);
            }

            if (target != caster && ability.DamageMultiplier > 0)
                yield return caster.AttackAnimation(target.transform.position);
            else
                yield return new WaitForSeconds(0.2f);

            VFXManager?.PlayEffect(ability.VFXKey, target.transform.position, ability.EffectColor);
            yield return new WaitForSeconds(0.35f);

            if (ability.ApplyDefendBuff)
            {
                caster.IsDefending = true;
                Log($"{caster.Stats.UnitName} braces! (DEF ×2)");
            }

            if (ability.AOERadius > 0 && target != caster)
            {
                // AOE: hit every enemy unit in radius (primary target is included in range)
                var aoeTargets = Grid.GetTilesInManhattanRange(target.GridPosition, ability.AOERadius);
                foreach (var t in aoeTargets)
                {
                    var u = t.OccupyingUnit;
                    if (u != null && u.Team != caster.Team && u.IsAlive)
                    {
                        u.TakeDamage(caster.CalculateDamage(ability));
                        VFXManager?.PlayEffect(ability.VFXKey, u.transform.position, ability.EffectColor);
                    }
                }
            }
            else if (ability.DamageMultiplier > 0 || ability.FlatDamage > 0)
            {
                target.TakeDamage(caster.CalculateDamage(ability));
            }

            if (ability.FlatHeal > 0 || ability.HealMultiplier > 0)
            {
                int heal = ability.FlatHeal + Mathf.RoundToInt(caster.RT.MagicAttack * ability.HealMultiplier);
                target.Heal(heal);
            }

            caster.HasActed = true;
            yield return new WaitForSeconds(0.2f);
        }

        // ── Hit Pause ─────────────────────────────────────────────────────────
        private IEnumerator HitPause(int frames = 3)
        {
            Time.timeScale = 0f;
            for (int i = 0; i < frames; i++)
                yield return new WaitForSecondsRealtime(0.016f);
            Time.timeScale = 1f;
        }

        // ── Helpers ───────────────────────────────────────────────────────────
        private void SetPhase(CombatPhase phase)
        {
            CurrentPhase = phase;
            OnPhaseChanged?.Invoke(phase);
        }

        private void Log(string msg) => OnCombatLog?.Invoke(msg);

        private static int ManhattanDist(Vector2Int a, Vector2Int b)
            => Mathf.Abs(a.x - b.x) + Mathf.Abs(a.y - b.y);

        // ── Public Accessors ──────────────────────────────────────────────────
        public IReadOnlyList<Unit> PlayerUnits => _playerUnits;
        public IReadOnlyList<Unit> EnemyUnits  => _enemyUnits;
        public IReadOnlyList<Unit> AllUnits    => _allUnits;
    }
}
