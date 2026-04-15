using System.Collections.Generic;
using UnityEngine;
using RPG.Data;

namespace RPG.Core
{
    /// <summary>
    /// Persistent cross-scene session data — survives all scene loads.
    ///
    /// Owns:
    ///   • PlayerResources  (Gold, Scrap, Rations, Morale)
    ///   • Card hand        (List of EventCardSO)
    ///   • Active run buffs (List of BuffSO)
    ///   • Level progression
    /// </summary>
    public class GameSession : MonoBehaviour
    {
        public static GameSession Instance { get; private set; }

        // ── Active level ──────────────────────────────────────────────────────
        public LevelDataSO CurrentLevel { get; private set; }

        // ── Resources ─────────────────────────────────────────────────────────
        public PlayerResources Resources { get; private set; } = new PlayerResources();

        // ── Card Hand ─────────────────────────────────────────────────────────
        /// <summary>Cards the player currently holds. Shown in the EventDeck scene.</summary>
        public IReadOnlyList<EventCardSO> Hand => _hand;
        private readonly List<EventCardSO> _hand = new();

        // ── Legacy progression (kept for XP/gold counters + save compat) ──────
        public int TotalGold { get; private set; }
        public int TotalXP   { get; private set; }

        // ── Roguelike Buffs ───────────────────────────────────────────────────
        public IReadOnlyList<BuffSO> ActiveBuffs => _activeBuffs;
        private readonly List<BuffSO> _activeBuffs = new();

        private readonly HashSet<int> _completedLevels = new();
        private readonly HashSet<int> _unlockedLevels  = new();

        public IEnumerable<int> CompletedLevelIndices => _completedLevels;
        public IEnumerable<int> UnlockedLevelIndices  => _unlockedLevels;

        // ── Pending card drops (queued by CombatManager, consumed by EventDeck) ─
        private readonly List<EventCardSO> _pendingDrops = new();
        public IReadOnlyList<EventCardSO> PendingDrops => _pendingDrops;

        // ── Lifecycle ─────────────────────────────────────────────────────────
        private void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(gameObject); return; }
            Instance = this;
            DontDestroyOnLoad(gameObject);
            _unlockedLevels.Add(0);
        }

        // ── Level API ─────────────────────────────────────────────────────────

        public void SetCurrentLevel(LevelDataSO level) => CurrentLevel = level;

        public void CompleteCurrentLevel()
        {
            if (CurrentLevel == null) return;
            _completedLevels.Add(CurrentLevel.LevelIndex);

            if (CurrentLevel.Reward != null)
            {
                TotalGold += CurrentLevel.Reward.GoldReward;
                TotalXP   += CurrentLevel.Reward.XPReward;
                Resources.Apply(new ResourceDelta { Gold = CurrentLevel.Reward.GoldReward });
            }

            foreach (int idx in CurrentLevel.UnlocksLevels)
                _unlockedLevels.Add(idx);
        }

        public bool IsLevelCompleted(int index) => _completedLevels.Contains(index);
        public bool IsLevelUnlocked(int index)  => _unlockedLevels.Contains(index);
        public void UnlockLevel(int index)       => _unlockedLevels.Add(index);

        // ── Resource API ──────────────────────────────────────────────────────

        public void ApplyResources(ResourceDelta delta) => Resources.Apply(delta);

        // ── Card Hand API ─────────────────────────────────────────────────────

        /// <summary>Add a card directly to the player's hand.</summary>
        public void AddCardToHand(EventCardSO card)
        {
            if (card == null) return;
            if (!card.CanDuplicate && _hand.Contains(card)) return;
            _hand.Add(card);
        }

        /// <summary>Remove a card from hand (called when played or discarded).</summary>
        public bool RemoveCardFromHand(EventCardSO card)
        {
            return _hand.Remove(card);
        }

        // ── Drop Queue API ────────────────────────────────────────────────────

        /// <summary>Called by CombatManager after victory to queue drops for the EventDeck screen.</summary>
        public void QueueCardDrops(List<EventCardSO> drops)
        {
            if (drops == null) return;
            _pendingDrops.AddRange(drops);
        }

        /// <summary>Consume the pending drops — moves them into the player's hand.</summary>
        public void AcceptPendingDrops()
        {
            foreach (var card in _pendingDrops)
                AddCardToHand(card);
            _pendingDrops.Clear();
        }

        /// <summary>Discard pending drops without adding them to hand.</summary>
        public void ClearPendingDrops() => _pendingDrops.Clear();

        // ── Buff API ──────────────────────────────────────────────────────────

        public void AddBuff(BuffSO buff)
        {
            if (buff != null) _activeBuffs.Add(buff);
        }

        // ── Reset ─────────────────────────────────────────────────────────────

        public void ResetSession()
        {
            _completedLevels.Clear();
            _unlockedLevels.Clear();
            _unlockedLevels.Add(0);
            TotalGold    = 0;
            TotalXP      = 0;
            CurrentLevel = null;
            _activeBuffs.Clear();
            _hand.Clear();
            _pendingDrops.Clear();
            Resources = new PlayerResources();
        }

        // ── Save / Load ───────────────────────────────────────────────────────

        public void LoadFromSave(int gold, int xp,
            List<int> completedIndices, List<int> unlockedIndices,
            List<string> buffNames, int currentLevelIndex,
            RPG.Data.BuffRegistry registry)
        {
            TotalGold = gold;
            TotalXP   = xp;
            Resources.Gold = gold;

            _completedLevels.Clear();
            foreach (int i in completedIndices) _completedLevels.Add(i);

            _unlockedLevels.Clear();
            _unlockedLevels.Add(0);
            foreach (int i in unlockedIndices) _unlockedLevels.Add(i);

            _activeBuffs.Clear();
            if (registry != null)
                foreach (string n in buffNames)
                {
                    var buff = registry.FindByName(n);
                    if (buff != null) _activeBuffs.Add(buff);
                }
        }
    }
}
