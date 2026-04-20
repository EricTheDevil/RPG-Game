using System.Collections.Generic;
using UnityEngine;
using RPG.Data;
using RPG.Map;

namespace RPG.Core
{
    /// <summary>
    /// Persistent cross-scene session data — survives all scene loads.
    /// Resets on run start / death.
    ///
    /// Owns:
    ///   • PlayerResources  (Gold, Scrap, Rations, Morale)
    ///   • World map state  (WorldMapData, current layer/node)
    ///   • Area map state   (AreaMapData tile grid, starvation)
    ///   • Hero class state (which class is active, in-run XP pending delivery)
    ///   • Task progress    (RunTaskProgress counters for ClassTask evaluation)
    ///   • Earned run buffs (List of BuffSO)
    ///   • Hero runtime state (RuntimeStats snapshot + current HP)
    /// </summary>
    public class GameSession : MonoBehaviour
    {
        public static GameSession Instance { get; private set; }

        // ── Resources ─────────────────────────────────────────────────────────
        public PlayerResources Resources { get; private set; } = new PlayerResources();

        // ── World Map State ───────────────────────────────────────────────────
        public WorldMapData WorldMap          { get; set; }
        public int          CurrentLayerIndex { get; set; } = 0;
        public string       CurrentWorldNodeId { get; set; }

        // ── Area Map State ────────────────────────────────────────────────────
        public AreaMapData CurrentArea { get; set; }

        // ── Starvation ────────────────────────────────────────────────────────
        /// <summary>True when the player moved with Rations = 0 this area entry.</summary>
        public bool IsStarving { get; private set; } = false;

        /// <summary>Cumulative MaxHP penalty from starvation (cap at 30%).</summary>
        public float StarvationHPPenalty { get; private set; } = 0f;

        private const float StarvationHPLossPerMove = 0.05f;
        private const float StarvationHPCap         = 0.30f;
        private const int   StarvationMoraleLoss    = 1;

        // ── Hero Class State ──────────────────────────────────────────────────
        /// <summary>The class chosen at run start. Used to apply stat growth and rewards.</summary>
        public ClassDefinitionSO ActiveClass { get; set; }

        /// <summary>Pending class XP earned this run — delivered to CommanderProfile on victory.</summary>
        public int PendingClassXP { get; private set; } = 0;

        // ── Task Progress ─────────────────────────────────────────────────────
        public RunTaskProgress TaskProgress { get; set; } = new RunTaskProgress();

        // ── Earned Buffs ──────────────────────────────────────────────────────
        public IReadOnlyList<BuffSO> EarnedBuffs => _earnedBuffs;
        private readonly List<BuffSO> _earnedBuffs = new();

        // ── Hero Runtime State ────────────────────────────────────────────────
        public RPG.Units.RuntimeStats HeroRT      { get; set; } = null;
        public int                    HeroCurrentHP { get; set; } = -1;
        public int                    FightsWon     { get; set; } = 0;

        // ── Lifecycle ─────────────────────────────────────────────────────────

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void AutoCreate()
        {
            if (Instance != null) return;
            var go = new GameObject("GameSession");
            go.AddComponent<GameSession>();
        }

        private void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(gameObject); return; }
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }

        // ── Resource API ──────────────────────────────────────────────────────

        public void ApplyResources(ResourceDelta delta)
        {
            Resources.Apply(delta);

            // Track peak Gold/Morale for class tasks
            if (Resources.Gold   > TaskProgress.PeakGold)   TaskProgress.PeakGold   = Resources.Gold;
            if (Resources.Morale > TaskProgress.PeakMorale) TaskProgress.PeakMorale = Resources.Morale;
        }

        // ── Tile Movement (costs Rations) ─────────────────────────────────────

        /// <summary>
        /// Move the player one tile on the area map.
        /// Deducts 1 Ration, or applies starvation if Rations = 0.
        /// </summary>
        public void MoveToTile(Vector2Int newPos)
        {
            if (CurrentArea == null) return;
            CurrentArea.PlayerPosition = newPos;

            if (Resources.Rations > 0)
            {
                Resources.Apply(new ResourceDelta { Rations = -1 });
                IsStarving = false;
            }
            else
            {
                // Starvation — lose HP fraction and morale instead
                IsStarving = true;
                TaskProgress.StarvedCombats++; // used broadly as "starved moves"

                if (HeroRT != null)
                {
                    StarvationHPPenalty = Mathf.Min(StarvationHPPenalty + StarvationHPLossPerMove, StarvationHPCap);
                    int penaltyHP = Mathf.RoundToInt(HeroRT.MaxHP * StarvationHPLossPerMove);
                    HeroCurrentHP = Mathf.Max(1,
                        (HeroCurrentHP < 0 ? HeroRT.MaxHP : HeroCurrentHP) - penaltyHP);
                }

                Resources.Apply(new ResourceDelta { Morale = -StarvationMoraleLoss });
                Debug.Log("[GameSession] Starvation! HP and Morale reduced.");
            }
        }

        // ── Area Map API ──────────────────────────────────────────────────────

        public void IncrementThreat()
        {
            if (CurrentArea == null) return;
            CurrentArea.ThreatCurrent++;

            if (CurrentArea.ThreatCurrent >= CurrentArea.ThreatMax)
                LockRemainingTiles();
        }

        private void LockRemainingTiles()
        {
            if (CurrentArea == null) return;
            foreach (var tile in CurrentArea.Tiles)
            {
                if (!tile.Visited && tile.TileType == TileType.Event)
                    tile.Locked = true;
            }
        }

        // ── World Map API ─────────────────────────────────────────────────────

        public WorldMapLayer GetCurrentLayer()
        {
            if (WorldMap == null || CurrentLayerIndex < 0 || CurrentLayerIndex >= WorldMap.Layers.Count)
                return null;
            return WorldMap.Layers[CurrentLayerIndex];
        }

        public WorldMapNode FindWorldNode(string nodeId)
        {
            if (WorldMap == null || string.IsNullOrEmpty(nodeId)) return null;
            foreach (var layer in WorldMap.Layers)
                foreach (var node in layer.Nodes)
                    if (node.Id == nodeId) return node;
            return null;
        }

        public void CompleteCurrentWorldNode()
        {
            var node = FindWorldNode(CurrentWorldNodeId);
            if (node != null) node.Visited = true;

            CurrentWorldNodeId  = null;
            CurrentArea         = null;
            IsStarving          = false;
            StarvationHPPenalty = 0f;
            CurrentLayerIndex++;
        }

        // ── Class XP API ──────────────────────────────────────────────────────

        public void AddClassXP(int xp)
        {
            if (xp <= 0) return;
            PendingClassXP += xp;
            Debug.Log($"[GameSession] +{xp} class XP (pending). Total pending: {PendingClassXP}");
        }

        /// <summary>Flush pending XP to CommanderProfile at run end.</summary>
        public void FlushClassXPToProfile()
        {
            if (ActiveClass == null || PendingClassXP <= 0) return;
            CommanderProfile.Instance?.AddClassXP(ActiveClass, PendingClassXP);
            PendingClassXP = 0;
        }

        // ── Task Progress API ─────────────────────────────────────────────────

        public void RecordCombatWin()
        {
            TaskProgress.CombatsWon++;
            FightsWon++;
        }

        public void RecordCitizenHelped()
        {
            TaskProgress.CitizensHelped++;
        }

        public void RecordBossReached()
        {
            TaskProgress.ReachedBoss = true;
        }

        public void RecordRunComplete()
        {
            TaskProgress.RunCompleted = true;
        }

        public void RecordBuffSkipped()
        {
            TaskProgress.ConsecutiveWinsNoBuff++;
        }

        public void ResetConsecutiveNoBuff()
        {
            TaskProgress.ConsecutiveWinsNoBuff = 0;
        }

        // ── Buff API ──────────────────────────────────────────────────────────

        public void AddBuff(BuffSO buff)
        {
            if (buff == null) return;
            _earnedBuffs.Add(buff);
            HeroRT?.ApplyBuff(buff);
            ResetConsecutiveNoBuff();
        }

        // ── Reset ─────────────────────────────────────────────────────────────

        public void ResetSession()
        {
            _earnedBuffs.Clear();
            Resources           = new PlayerResources();
            HeroCurrentHP       = -1;
            HeroRT              = null;
            FightsWon           = 0;
            WorldMap            = null;
            CurrentLayerIndex   = 0;
            CurrentWorldNodeId  = null;
            CurrentArea         = null;
            IsStarving          = false;
            StarvationHPPenalty = 0f;
            PendingClassXP      = 0;
            TaskProgress        = new RunTaskProgress();
            // Note: ActiveClass is set AFTER reset by GameManager from class selection.
        }

        // ── Save / Load ───────────────────────────────────────────────────────

        /// <summary>Called by SaveSystem to restore starvation state without re-triggering side-effects.</summary>
        public void LoadStarvationState(float hpPenalty, bool isStarving)
        {
            StarvationHPPenalty = hpPenalty;
            IsStarving          = isStarving;
        }

        /// <summary>Called by SaveSystem to restore in-run class XP.</summary>
        public void LoadPendingClassXP(int xp)
        {
            PendingClassXP = xp;
        }

        public void LoadFromSave(int gold, int scrap, int rations, int morale,
            List<string> buffNames, RPG.Data.BuffRegistry registry)
        {
            Resources = new PlayerResources
            {
                Gold    = gold,
                Scrap   = scrap,
                Rations = rations,
                Morale  = morale,
            };

            _earnedBuffs.Clear();
            if (registry != null)
                foreach (string n in buffNames)
                {
                    var buff = registry.FindByName(n);
                    if (buff != null) _earnedBuffs.Add(buff);
                }
        }
    }
}
