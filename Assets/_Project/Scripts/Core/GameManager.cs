using UnityEngine;
using UnityEngine.SceneManagement;
using RPG.Data;

namespace RPG.Core
{
    public enum GameState { MainMenu, EventDeck, Combat, GameOver }

    /// <summary>
    /// Central scene-flow controller. Persists across all scenes.
    ///
    /// Flow:
    ///   MainMenu → EventDeck → Combat → (card drops queued) → EventDeck → …
    ///
    /// EventDeck is the between-battle hub (FTL-style card hand).
    /// </summary>
    public class GameManager : MonoBehaviour
    {
        public static GameManager Instance { get; private set; }

        // ── Scene Names ───────────────────────────────────────────────────────
        [Header("Scene Names")]
        public string MainMenuScene  = "MainMenu";
        public string EventDeckScene = "EventDeck";
        public string CombatScene    = "CombatStage";

        [Header("Starting Hand")]
        [Tooltip("Cards dealt to the player at the start of every new run.")]
        public EventCardSO[] StarterCards = new EventCardSO[0];

        [Header("Drop Tables")]
        [Tooltip("Fallback table used when a combat card has no custom SpawnConfig.")]
        public DropTableSO DefaultDropTable;

        [Header("Empty Hand Fallback")]
        [Tooltip("If the player's hand is empty after drops are applied, this card is added automatically so the player is never soft-locked. Assign a basic Skirmish card.")]
        public EventCardSO FallbackCombatCard;

        [Header("Data")]
        public BuffRegistry      BuffRegistry;
        public EventCardRegistry CardRegistry;

        // ── Runtime State ─────────────────────────────────────────────────────
        public GameState   CurrentState      { get; private set; }
        public EventCardSO PendingCombatCard { get; private set; }

        // ── Lifecycle ─────────────────────────────────────────────────────────
        private void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(gameObject); return; }
            Instance = this;
            DontDestroyOnLoad(gameObject);
            EnsureSession();
            TryLoadSave();
        }

        private void EnsureSession()
        {
            if (GameSession.Instance == null)
            {
                var go = new GameObject("GameSession");
                go.AddComponent<GameSession>();
            }
        }

        private void TryLoadSave()
        {
            if (!SaveSystem.SaveExists() || GameSession.Instance == null) return;
            if (CardRegistry == null)
                Debug.LogWarning("[GameManager] CardRegistry not assigned — saved card hand will not be restored. Assign EventCardRegistry asset to GameManager prefab.");
            SaveSystem.Load(GameSession.Instance, BuffRegistry, CardRegistry);
        }

        // ── Navigation ────────────────────────────────────────────────────────

        /// <summary>Reset session and deal starter hand, then go to EventDeck.</summary>
        public void StartNewRun()
        {
            GameSession.Instance?.ResetSession();
            if (GameSession.Instance != null)
                foreach (var card in StarterCards)
                    GameSession.Instance.AddCardToHand(card);
            GoToEventDeck();
        }

        public void GoToEventDeck()
        {
            SetState(GameState.EventDeck);
            Load(EventDeckScene);
        }

        public void GoToMainMenu()
        {
            SetState(GameState.MainMenu);
            Load(MainMenuScene);
        }

        /// <summary>Called by EventDeckUI when the player plays a Combat/Elite card.</summary>
        public void PlayCombatCard(EventCardSO card)
        {
            PendingCombatCard = card;
            GameSession.Instance?.ApplyResources(card.ResourceCost);
            SetState(GameState.Combat);
            Load(CombatScene);
        }

        /// <summary>Called by CombatHUD on victory (after buff selection if any).</summary>
        public void OnCombatVictory()
        {
            var session = GameSession.Instance;

            // Use the card's own drop table if it has one; fall back to default.
            // This lets Elite cards guarantee better loot than Skirmish cards.
            var dropTable = (PendingCombatCard?.SpawnConfig != null && DefaultDropTable != null)
                ? DefaultDropTable   // could be card.DropTable once we add that field — for now always default
                : DefaultDropTable;

            if (dropTable != null && session != null)
            {
                var drops = dropTable.Roll(morale: session.Resources.Morale);
                session.QueueCardDrops(drops);
            }
            else if (session != null)
            {
                Debug.LogWarning("[GameManager] OnCombatVictory: DefaultDropTable not assigned — player gets no card drops.");
            }

            // Grant resource bonus from the played card (e.g. Bounty Hunt gives Gold)
            if (PendingCombatCard != null && session != null)
                session.ApplyResources(PendingCombatCard.ResourceGrant);

            SaveSystem.Save(session);
            PendingCombatCard = null;
            GoToEventDeck();
        }

        /// <summary>Retry the same combat (keeps pending card).</summary>
        public void RetryCombat()
        {
            SetState(GameState.Combat);
            Load(CombatScene);
        }

        /// <summary>Forfeit and return to deck without rewards.</summary>
        public void RetreatToMap()
        {
            PendingCombatCard = null;
            GoToEventDeck();
        }

        // ── Legacy compat ─────────────────────────────────────────────────────
        public void StartCombat()          => StartNewRun();
        public void ReturnToMainMenu()     => GoToMainMenu();
        public void OnRewardAcknowledged() => GoToEventDeck();

        // Kept for MapSelector / old wiring — redirect to EventDeck
        public void GoToMapSelector()      => GoToEventDeck();
        public void StartLevel(LevelDataSO _) => GoToEventDeck();

        // ── Helpers ───────────────────────────────────────────────────────────
        private void SetState(GameState s) => CurrentState = s;
        private void Load(string scene)
        {
            if (string.IsNullOrEmpty(scene))
            {
                Debug.LogError($"[GameManager] Empty scene name. State={CurrentState}");
                return;
            }
            SceneManager.LoadScene(scene);
        }
    }
}
