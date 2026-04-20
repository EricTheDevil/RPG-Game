using UnityEngine;
using UnityEngine.SceneManagement;
using RPG.Data;
using RPG.Map;

namespace RPG.Core
{
    public enum GameState { MainMenu, ClassSelect, WorldMap, AreaMap, Combat, GameOver }

    /// <summary>
    /// Central scene-flow controller. Persists across all scenes.
    ///
    /// Full flow:
    ///   MainMenu → ClassSelect → WorldMap → AreaMap → Combat → AreaMap → WorldMap → … → Demon Lord
    ///
    /// ClassSelect is shown once per run before the WorldMap.
    /// AreaMap is the local sector tile grid (movement costs Rations).
    /// All class XP and task progress feed into CommanderProfile on run end.
    /// </summary>
    public class GameManager : MonoBehaviour
    {
        public static GameManager Instance { get; private set; }

        // ── Scene Names ───────────────────────────────────────────────────────
        [Header("Scene Names")]
        public string MainMenuScene   = "MainMenu";
        public string ClassSelectScene = "ClassSelect";
        public string WorldMapScene   = "WorldMap";
        public string AreaMapScene    = "AreaMap";
        public string CombatScene     = "CombatStage";

        [Header("Map Config")]
        public WorldMapConfigSO WorldMapConfig;

        [Header("Classes — Beginner Tier")]
        [Tooltip("The three selectable starting classes. Unlocked by default.")]
        public ClassDefinitionSO[] BeginnerClasses = new ClassDefinitionSO[3];

        [Header("Data")]
        public BuffRegistry BuffRegistry;

        // ── Pending combat state ──────────────────────────────────────────────
        public GameState        CurrentState        { get; private set; }
        public SectorEventSO    PendingCombatEvent  { get; private set; }

        // ── Lifecycle ─────────────────────────────────────────────────────────

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void AutoBootstrap()
        {
            if (Instance != null) return;
            var prefab = Resources.Load<GameManager>("GameManager");
            if (prefab != null) Instantiate(prefab);
            else Debug.LogError("[GameManager] GameManager prefab not found in Resources/.");
        }

        private void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(gameObject); return; }
            Instance = this;
            DontDestroyOnLoad(gameObject);
            EnsureSession();
            TryLoadSave();
            EnsureBeginnerClassesUnlocked();
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
            SaveSystem.Load(GameSession.Instance, BuffRegistry);
        }

        private void EnsureBeginnerClassesUnlocked()
        {
            var profile = CommanderProfile.Instance;
            if (profile == null) return;
            foreach (var cls in BeginnerClasses)
                if (cls != null) profile.UnlockClass(cls);
        }

        // ── Run Start Flow ────────────────────────────────────────────────────

        /// <summary>Begin a new run — reset session and go to class selection.</summary>
        public void StartNewRun()
        {
            CommanderProfile.Instance?.OnRunStarted();
            GameSession.Instance?.ResetSession();
            SetState(GameState.ClassSelect);
            Load(ClassSelectScene);
        }

        /// <summary>Called by ClassSelectUI after the player picks a class.</summary>
        public void OnClassSelected(ClassDefinitionSO chosenClass)
        {
            var session = GameSession.Instance;
            if (session == null || chosenClass == null) return;

            session.ActiveClass = chosenClass;
            Debug.Log($"[GameManager] Hero class selected: {chosenClass.ClassName}");

            // Apply cumulative stat growth from this class's commander level
            ApplyClassGrowthToHero(chosenClass);

            // Generate world map
            if (WorldMapConfig != null)
                session.WorldMap = WorldMapGenerator.Generate(WorldMapConfig);

            GoToWorldMap();
        }

        /// <summary>
        /// Apply all stat growth and level rewards from the hero's commander-level record
        /// into the session's base stats. This is called once at run start.
        /// </summary>
        private void ApplyClassGrowthToHero(ClassDefinitionSO cls)
        {
            var session = GameSession.Instance;
            if (session == null) return;

            var profile = CommanderProfile.Instance;
            int classLevel = profile?.GetRecord(cls.name).Level ?? 0;
            if (classLevel <= 0) return;

            var growth = cls.CumulativeGrowthAt(classLevel);

            // We store the growth as a pending buff-equivalent on HeroRT at combat init.
            // GameSession stores the ClassDefinitionSO + class level so CombatManager
            // can apply the growth in Unit.Initialize().
            // Nothing to do here yet — CombatManager reads session.ActiveClass + profile.
            Debug.Log($"[GameManager] {cls.ClassName} level {classLevel} — growth will be applied at combat start.");
        }

        // ── Navigation ────────────────────────────────────────────────────────

        public void GoToWorldMap()
        {
            SetState(GameState.WorldMap);
            Load(WorldMapScene);
        }

        /// <summary>Enter a world node — generates an area map and loads the AreaMap scene.</summary>
        public void EnterAreaNode(string worldNodeId)
        {
            var session = GameSession.Instance;
            if (session == null) return;

            var node = session.FindWorldNode(worldNodeId);
            if (node == null)
            {
                Debug.LogError($"[GameManager] World node '{worldNodeId}' not found.");
                return;
            }

            session.CurrentWorldNodeId = worldNodeId;

            // Boss node — flag task progress and go straight to combat
            if (node.IsBossNode)
            {
                session.RecordBossReached();
                PendingCombatEvent = null;
                SetState(GameState.Combat);
                Load(CombatScene);
                return;
            }

            // Grant rations for entering a new sector, then deduct entry cost
            session.ApplyResources(new ResourceDelta { Rations = 4 });
            session.ApplyResources(new ResourceDelta { Rations = -1 });

            // Generate area tile grid
            var areaConfig = WorldMapConfig?.GetAreaConfig(node.Type);
            if (areaConfig != null)
                session.CurrentArea = AreaMapGenerator.Generate(areaConfig, node.Type);

            GoToAreaMap();
        }

        public void GoToAreaMap()
        {
            SetState(GameState.AreaMap);
            Load(AreaMapScene);
        }

        /// <summary>Player reached the exit tile — mark node visited, return to world map.</summary>
        public void LeaveArea()
        {
            GameSession.Instance?.CompleteCurrentWorldNode();
            SaveSystem.Save(GameSession.Instance);
            GoToWorldMap();
        }

        /// <summary>Called by AreaMapUI when the player resolves a combat event tile.</summary>
        public void PlaySectorCombat(SectorEventSO ev)
        {
            PendingCombatEvent = ev;
            if (ev != null)
                GameSession.Instance?.ApplyResources(ev.VictoryGrant); // pre-store, not applied yet

            // Track starvation before entering combat
            if (GameSession.Instance?.IsStarving == true)
                GameSession.Instance.TaskProgress.StarvedCombats++;

            SetState(GameState.Combat);
            Load(CombatScene);
        }

        public void GoToMainMenu()
        {
            SetState(GameState.MainMenu);
            Load(MainMenuScene);
        }

        // ── Combat Result ─────────────────────────────────────────────────────

        /// <summary>Called by ResultScreenUI on combat victory (after buff pick).</summary>
        public void OnCombatVictory()
        {
            var session = GameSession.Instance;
            if (session == null) { GoToAreaMap(); return; }

            session.RecordCombatWin();

            // Grant class XP from combat
            int xp = PendingCombatEvent?.CombatClassXPReward ?? 50;
            session.AddClassXP(xp);

            // Grant resource reward from the event
            if (PendingCombatEvent != null)
                session.ApplyResources(PendingCombatEvent.VictoryGrant);

            // Evaluate class tasks
            EvaluateClassTasks(session);

            // Boss fight won?
            var worldNode = session.FindWorldNode(session.CurrentWorldNodeId);
            if (worldNode != null && worldNode.IsBossNode)
            {
                OnBossDefeated(session);
                return;
            }

            PendingCombatEvent = null;
            SaveSystem.Save(session);
            GoToAreaMap();
        }

        private void OnBossDefeated(GameSession session)
        {
            session.RecordRunComplete();
            session.FlushClassXPToProfile();
            CommanderProfile.Instance?.OnRunCompleted(won: true, session.FightsWon);

            // Evaluate tasks one final time
            EvaluateClassTasks(session);

            SaveSystem.Save(session);
            PendingCombatEvent = null;

            // For now: main menu. TODO: victory scene.
            SetState(GameState.GameOver);
            Load(MainMenuScene);
        }

        /// <summary>Retry the same combat.</summary>
        public void RetryCombat()
        {
            SetState(GameState.Combat);
            Load(CombatScene);
        }

        /// <summary>Forfeit combat — lose this run.</summary>
        public void AbandonRun()
        {
            var session = GameSession.Instance;
            session?.FlushClassXPToProfile();
            CommanderProfile.Instance?.OnRunCompleted(won: false, session?.FightsWon ?? 0);
            EvaluateClassTasks(session);
            SaveSystem.Save(session);
            PendingCombatEvent = null;
            GoToMainMenu();
        }

        // ── Class Task Evaluation ─────────────────────────────────────────────

        private void EvaluateClassTasks(GameSession session)
        {
            if (session?.ActiveClass == null) return;
            var cls     = session.ActiveClass;
            var task    = cls.EvolutionTask;
            var profile = CommanderProfile.Instance;

            if (task == null || profile == null) return;
            if (profile.IsTaskCompleted(cls, task)) return;

            if (task.IsSatisfied(session.TaskProgress))
                profile.CompleteTask(cls, task);
        }

        // ── Combat Pending Event accessor (for CombatManager) ─────────────────
        // CombatManager reads GameManager.Instance.PendingCombatEvent for
        // SpawnConfig + DifficultyScale. Null = use CombatManager's own defaults.

        // ── Legacy compat ─────────────────────────────────────────────────────
        public void ReturnToMainMenu()     => GoToMainMenu();
        public void OnRewardAcknowledged() => GoToWorldMap();
        public void RetreatToMap()         => GoToAreaMap();

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
