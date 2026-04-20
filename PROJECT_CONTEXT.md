# RPG — TFT-Style Autobattle Tactical RPG
**Architecture context document for future Claude sessions**
Last updated: 2026-04-15

---

## Folder Workflow

- `Assets/_Project/` — **the only source of truth. All edits go here.**
- Each developer works on their own git branch and merges to `main` to share work.
- There is no `_Local/` folder — that pattern is deprecated.

---

## Vision

Final Fantasy Tactics art direction + Teamfight Tactics autobattle loop.
- Fully automated combat — no player input during battle.
- CT (Charge Time) speed system — units act at CT ≥ 100, faster units act more often.
- Roguelike progression — win combat → pick a buff from 3 options → next level.
- Designer-friendly — new units, abilities, buffs, and levels require zero code changes.

---

## Scene Flow

```
MainMenu → MapSelector → CombatStage → [BuffSelectionUI overlay] → RewardScene → MapSelector
                                      → [ResultScreenUI overlay on defeat]
```

- Direct entry: Open `CombatStage` scene in editor and press Play for a standalone combat test.
- `GameManager` (DontDestroyOnLoad) owns all `SceneManager.LoadScene` transitions.
- When no GameManager exists (direct play from CombatStage), `ResultScreen` shows as fallback after buff pick.

---

## Architecture Overview

### Layered responsibility model

```
GameManager          — scene flow, single source of truth for navigation
GameSession          — run state (buffs, XP, gold, completed levels); persists across scenes
CombatManager        — autobattle loop, unit spawning, event bus for this combat
CombatTimeline       — CT tick math; knows nothing about UI or VFX
Unit / UnitAI        — per-unit state + autonomous decision making
CombatHUD            — reacts to CombatManager events; never polls
UnitStatusPanel      — reacts to Unit events; never polls
CombatVFXManager     — fire-and-forget effects; no game logic
```

**Key principle:** Data flows downward via events. Nothing except `CombatManager` holds references to `Unit` instances at the HUD layer. UI subscribes to events, not state.

---

## Combat System

### `CombatManager.cs` — Combat/
The single entry point for a fight. Owns the autobattle loop.

**Events fired (HUD subscribes to these):**
| Event | Payload | When |
|---|---|---|
| `OnSpawned` | `(Unit hero, Unit enemy)` | Once, after both units are placed on grid |
| `OnPhaseChanged` | `CombatPhase` | Setup → Intro → Autobattle → Victory/Defeat |
| `OnUnitActed` | `Unit` | Every time a unit completes its turn |
| `OnCombatLog` | `string` | Rich-text combat log lines |
| `OnVictory` | — | All enemies dead |
| `OnDefeat` | — | All players dead |

**Inspector fields:**
- `Grid`, `Timeline`, `VFXManager`, `TraitSystem`
- `HeroPrefab`, `EnemyPrefab`, `HeroStats`, `EnemyStats`
- `AttackAbility`, `DefendAbility`, `SpecialAbility`, `EnemyAttackAbility`, `EnemySpecialAbility`
- `PlayerSpawns[]`, `EnemySpawns[]` — add entries to go N-vs-N
- `TickInterval` (0.05s), `PostActionPause` (0.35s)

### `CombatTimeline.cs` — Combat/
Pure math, no Unity state.
- `Tick()` — advances all unit CTs by `Speed × 0.5`; returns the first unit at CT ≥ 100 or null.
- `GetSortedUnits()` — descending CT order for initiative bar.
- `RegisterUnits()` / `RemoveUnit()` — mutate the internal unit list.

### `TraitSystem.cs` — Combat/
TFT synergy bonuses. Fully Inspector-driven — add traits in the `Bonuses` list, no code changes.
- Reads `UnitStatsSO.Traits` (List<string>).
- Groups units by trait, applies flat stat bonuses when count ≥ `Threshold`.
- Called once by `CombatManager.SetupCombat()` before the first tick.

---

## Unit System

### `Unit.cs` — Units/ (abstract)
| Field | Notes |
|---|---|
| `RuntimeStats RT` | Mutable layer; TraitSystem and BuffSystem write here |
| `UnitAI AI` | Auto-added as component in `Initialize()` |
| `UnitGlow` | Optional Point Light child; breathing pulse in `Update()` |
| `CurrentHP/MP` | Protected set; only `TakeDamage` and `Heal` mutate them |
| `OnDamaged(int, bool)` | Fired with (amount, isCrit) |
| `OnHealed(int)` | Fired with amount |
| `OnDeath` | Fired before sink animation |

Death sequence: set `State=Dead` → play animator Die trigger → `PlayDeathEffect()` → extinguish glow → `OnDeath` event → sink into ground → `SetActive(false)`.

### `UnitAI.cs` — Units/
Used by **all** units — hero and enemy. Priority order per `SelectAction()`:
1. Heal a wounded ally (HP < `HealTriggerHP`)
2. Use special ability (HP < `SpecialTriggerHP` OR random roll < `SpecialAbilityChance`)
3. Primary attack
4. Defend fallback

### `HeroUnit.cs` / `EnemyUnit.cs` — Units/
Thin MonoBehaviour subclasses. Abilities are injected by CombatManager, not hardcoded.

---

## UI System

### `CombatHUD.cs`
Subscribes to CombatManager events in `Start()`. Never polls. Key wiring:
- `OnSpawned` → calls `HeroPanel.SetUnit(hero)` + `EnemyPanel.SetUnit(enemy)` immediately.
- `OnVictory` → shows banner → 1.5s delay → `BuffSelectionUI.Show()` or `ResultScreen.Show(true)`.
- `OnDefeat` → shows banner → 1.5s delay → `ResultScreen.Show(false)`.
- Speed toggle cycles `Time.timeScale` through `{1×, 1.5×, 2×}`. `OnDestroy` resets to 1.

### `UnitStatusPanel.cs`
- Wired once per unit via `SetUnit(unit)` at spawn.
- Subscribes to `OnDamaged` / `OnHealed` events; never needs polling.
- Bars use `Image.fillAmount` (not Slider) + `MoveTowards` in `Update()` for smooth animation.
- HP fill color transitions to orange/red below 30%.

### `WorldHealthBar.cs`
World-space bar floating above each unit.
- **Uses `Image.fillAmount`, NOT `Slider`** — Slider's RectTransform fill area distorts under arbitrary world rotation.
- Billboard: `LookRotation(camForward with Y=0)` in `LateUpdate()` — keeps bars flat and readable.
- Wired in `Unit.Initialize()` via `GetComponentInChildren<WorldHealthBar>()`.

### `InitiativeBarUI.cs` + `InitiativeEntry.cs`
TFT-style CT strip at the top of screen.
- `InitiativeEntry` is a **separate file** — Unity cannot serialize inner MonoBehaviour classes on prefabs.
- `Refresh(List<Unit>)` rebuilds the entry pool.
- `Update()` animates `CTBar.fillAmount` each frame.

### `BuffSelectionUI.cs`
Post-victory roguelike buff picker.
- Hidden via `CanvasGroup.alpha=0`, **never** `SetActive(false)` — must stay active for `StartCoroutine`.
- `HasRegistry` property — CombatHUD checks this before calling `Show()`.
- `OnSelectionComplete` event → triggers `CombatHUD.OnBuffDone()`.

### `ResultScreenUI.cs`
Victory/defeat overlay. Same CanvasGroup pattern as BuffSelectionUI.
- Victory: shows Continue button → `GameManager.OnCombatVictory()`.
- Defeat: shows Retry + Retreat buttons.

---

## VFX System

### `CombatVFXManager.cs` — VFX/
Singleton. Particle pool: `Dictionary<ParticleSystem, Queue<ParticleSystem>>`, pool size 4.

| Method | Effect |
|---|---|
| `PlayEffect(key, pos, color)` | Keyed lookup: "attack", "magic", "special", "heal", "defend", "death" |
| `ShowDamageNumber(dmg, pos, isCrit, isHeal)` | Floating text + optional CritBurst + HitRing |
| `FlashScreen(color)` | Full-screen quad flash via `Time.unscaledDeltaTime` (survives hit-pause) |
| `PlayDeathEffect(pos, color)` | Large sphere burst, not pooled (rare + dramatic) |
| `PlaySpawnEffect(pos)` | Upward float particles on unit spawn |

### `CameraShake.cs`
Singleton on MainCamera. `Shake(isCrit)` — crit uses larger magnitude + longer duration.

### `TilePulse.cs`
HDRP emissive sine pulse on highlighted grid tiles via `MaterialPropertyBlock` (`_EmissiveColor`).

---

## Data Layer (ScriptableObjects)

| Type | Location | Purpose |
|---|---|---|
| `UnitStatsSO` | Stats/ | HP/MP/ATK/DEF/SPD/MOV + `List<string> Traits` |
| `AbilitySO` | Abilities/ | Name, Type, Range, AOE, damage/heal multipliers, VFXKey, MPCost |
| `BuffSO` | Buffs/ | Roguelike stat delta, Rarity, Icon |
| `BuffRegistry` | Buffs/ | Master list; `PickRandom(n)` weighted by rarity |
| `LevelDataSO` | Levels/ | Scene name, map position, difficulty, unlock edges |
| `LevelRegistry` | Levels/ | All levels; read by MapSelectorUI |
| `LevelRewardSO` | Rewards/ | XP, Gold, item drop text |

**Existing assets (confirmed in project):**
- `HeroStats.asset` — traits: ["Hero", "Warrior"]
- `DarkKnightStats.asset` — traits: ["Knight", "Warrior"]
- Abilities: Attack, Defend, Special, EnemyAttack, EnemySpecial
- 12 buff assets with rarity weighting
- 3 level assets (Level0–2) + registries

---

## Grid System

### `BattleGrid.cs`
8×8 grid. `GenerateGrid()` creates `GridTile` objects with Perlin-height variation.
- `GetTile(Vector2Int)`, `GetMovableTiles(pos, range)` (BFS), `GetTilesInManhattanRange()`.
- `ClearAllHighlights()` resets all tile states.

### `GridTile.cs`
Highlight states: None / Movable / Attackable / Selected.
- Mouse events (OnMouseEnter/Down) are stubs — player interaction removed for autobattle.
- `OccupyingUnit` reference keeps the grid aware of unit positions.

---

## Core / Scene Infrastructure

| Script | Role |
|---|---|
| `GameManager.cs` | DontDestroyOnLoad singleton; owns `SceneManager.LoadScene` |
| `GameSession.cs` | Run state: `ActiveBuffs`, `CurrentLevel`, `CompletedLevels`, XP, Gold |
| `SaveSystem.cs` | JSON serialize/deserialize to `Application.persistentDataPath/save.json` |
| `SceneBootstrap.cs` | Ensures GameManager prefab exists in every scene on load |
| `CameraController.cs` | Isometric orbit camera; scroll zoom, middle-mouse pan |

---

## Editor Tools

All tools are in `Assets/_Project/Editor/`. Run from the **RPG** menu in the Unity Editor.

| Tool | Menu | Status | Purpose |
|---|---|---|---|
| `RPGAutobattlePatch.cs` | RPG/Patch → Autobattle Scene | ✅ Run | Wires Timeline, TraitSystem, InitiativeBar, Banner, SpeedToggle |
| `RPGFinalWire.cs` | RPG/Patch → Final Wire | ✅ Run | ScrollRect on CombatLog, spawn positions, VFX WorldCanvas |
| `RPGVFXAndUnitSetup.cs` | RPG/Setup → VFX & Units | ✅ Run | VFX prefabs, UnitGlow lights, Traits on stats SOs |
| `RPGFixRuntime.cs` | RPG/Fix → Runtime Issues | ✅ Run | InitiativeEntry prefab, overlay panels SetActive(true) |
| `RPGFixBars.cs` | RPG/Fix → Health Bars | ✅ Run | Rewires bars to Image.fillAmount; fixes WorldHealthBar prefabs |
| `RPGSetup.cs` | RPG/Setup Full Demo | ⚠️ Legacy | Original setup — references updated for new bar system |
| `RPGPolishSetup.cs` | RPG/Polish | ⚠️ Legacy | Uses deprecated `FindObjectOfType<T>()` |
| `RPGGameLoopSetup.cs` | RPG/Setup Game Loop | ⚠️ Legacy | Wires MainMenu + MapSelector |

---

## File Map

```
Assets/_Project/Scripts/
├── Combat/
│   ├── CombatManager.cs        ✅ Autobattle loop; fires OnSpawned, OnVictory, OnDefeat etc.
│   ├── CombatTimeline.cs       ✅ CT tick math; no Unity dependencies
│   ├── TraitSystem.cs          ✅ TFT synergy bonuses; Inspector-driven
│   ├── TurnManager.cs          🗑  Deprecated stub (keep for prefab compat)
│   └── EnemyAI.cs (moved to Units/)
├── Units/
│   ├── Unit.cs                 ✅ Abstract base; RuntimeStats; UnitAI; glow pulse; smooth death
│   ├── UnitAI.cs               ✅ Priority AI used by ALL units (hero + enemy)
│   ├── HeroUnit.cs             ✅ Thin subclass
│   ├── EnemyUnit.cs            ✅ Thin subclass
│   └── EnemyAI.cs              🗑  Deprecated stub
├── UI/
│   ├── CombatHUD.cs            ✅ Event-driven; wires panels at OnSpawned; delayed overlay show
│   ├── UnitStatusPanel.cs      ✅ Image.fillAmount bars; MoveTowards smooth animation
│   ├── WorldHealthBar.cs       ✅ Image.fillAmount billboard; Y-axis only rotation
│   ├── InitiativeBarUI.cs      ✅ CT strip; pool management
│   ├── InitiativeEntry.cs      ✅ Standalone file (required for Unity prefab serialization)
│   ├── BuffSelectionUI.cs      ✅ 3-card roguelike picker; CanvasGroup hide pattern
│   ├── BuffCardUI.cs           ✅ Single buff card with rarity tint
│   ├── BuffStackUI.cs          ✅ Map screen buff icon strip
│   ├── ResultScreenUI.cs       ✅ Victory/defeat overlay; CanvasGroup hide pattern
│   ├── RewardUI.cs             ✅ Reward scene XP/gold counters
│   ├── MainMenuUI.cs           ✅
│   ├── MapSelectorUI.cs        ✅ Dynamic level node map
│   └── ActionMenuUI.cs         🗑  Stub — disabled on Awake; reserved for future planning phase
├── VFX/
│   ├── CombatVFXManager.cs     ✅ Particle pool; keyed effects; screen flash; death/spawn VFX
│   ├── FloatingText.cs         ✅ Rising damage/heal numbers
│   └── CameraShake.cs          ✅ Singleton; normal + crit shake
├── Grid/
│   ├── BattleGrid.cs           ✅ 8×8 Perlin grid; BFS movement range
│   ├── GridTile.cs             ✅ Highlight state machine; mouse events stubbed out
│   └── TilePulse.cs            ✅ HDRP emissive sine pulse via MaterialPropertyBlock
├── Data/
│   ├── UnitStatsSO.cs          ✅ Stats + Traits list
│   ├── AbilitySO.cs            ✅
│   ├── BuffSO.cs               ✅
│   ├── BuffRegistry.cs         ✅ Weighted random pick
│   ├── LevelDataSO.cs          ✅
│   ├── LevelRegistry.cs        ✅
│   └── LevelRewardSO.cs        ✅
├── Map/
│   ├── MapNode.cs              ✅
│   └── MapSelectorUI.cs        ✅
└── Core/
    ├── GameManager.cs          ✅ Scene flow singleton
    ├── GameSession.cs          ✅ Cross-scene run state
    ├── SaveSystem.cs           ✅ JSON persistence
    ├── SceneBootstrap.cs       ✅ Auto-ensures GameManager
    └── CameraController.cs     ✅ Isometric orbit + scroll zoom
```

---

## Current Status ✅

**Combat loop — fully working (confirmed 2026-04-11, third playtest)**
- Hero vs Dark Knight autobattle runs to completion with no runtime errors
- `OnSpawned` fires immediately → HUD panels show real HP/MP values from frame 1
- Health bars (HUD panels + world-space) react to damage in real time with smooth animation
- World health bars billboard correctly (flat, no distortion)
- Victory → 1.5s pause → BuffSelectionUI fades in with 3 buff cards
- Buff selection fades out → ResultScreen shows (Victory, with Continue button)
- Defeat flow → ResultScreen with Retry/Retreat buttons
- CT initiative bar updates on every turn
- Speed toggle cycles x1 / x1.5 / x2 correctly
- Death VFX, spawn VFX, UnitGlow pulse all working
- Traits wired: Hero = ["Hero","Warrior"], DarkKnight = ["Knight","Warrior"]

---

## Known Issues / Tech Debt

| Issue | Severity | Notes |
|---|---|---|
| Animator triggers don't fire actual animations | Low | `UnitAnimator.SetTrigger("Attack")` is called but the unit model has no clips — placeholder capsule objects |
| `RPGPolishSetup.cs` uses deprecated `FindObjectOfType<T>()` | Low | Compiler warning only; doesn't affect runtime |
| `TurnManager.cs` + `EnemyAI.cs` are dead stubs | Low | Keep to avoid missing-script warnings on old scene objects |
| No GameManager prefab bootstrapped in CombatStage | Low | Direct play works via ResultScreen fallback; full flow needs SceneBootstrap wired |
| BuffSelectionUI `Registry` must be assigned in Inspector | Low | Will silently skip buff pick if unassigned (logs a warning) |

---

## Next Steps — Prioritised

### TIER 1 — Core loop completeness
These are required before the game feels like a real product.

**1. Wire the full scene flow (est. 1 session)**
- Add `SceneBootstrap` to CombatStage so `GameManager` initialises when playing any scene.
- Verify: pick buff → `GameManager.OnCombatVictory()` → loads RewardScene → continue → MapSelector.
- Verify: Defeat Retry reloads CombatStage cleanly.

**2. Add a second enemy unit (est. half session)**
- Create `DarkKnightStats2.asset` (or reuse DarkKnightStats with a different name).
- Add `EnemySpawns[1] = (6, 2)` in CombatManager Inspector.
- Extend `SpawnUnits()` to loop over all spawn slots (not hardcoded hero + one enemy).
- This unlocks the core TFT "team vs team" feel.

**3. Unit animator clips (est. 1–2 sessions)**
- The `UnitAnimator.controller` exists. Add at minimum: Idle loop, Attack lunge, Hit flash, Die fall.
- Can use Unity primitive animations (scale/rotate keyframes) until real models arrive.
- Triggers already wired in code: `"Attack"`, `"Hit"`, `"Die"`, `"IsMoving"`.

### TIER 2 — Polish & feel
These make it feel good to watch.

**4. HDRP Post-Process Volume**
- Add Bloom (threshold 1.0, intensity 0.4) + Chromatic Aberration (on crit) + subtle Vignette.
- Lens Distortion burst: drive `LensDistortion.intensity` from `CombatVFXManager.FlashScreen()` via a coroutine.

**5. Banner scale-punch animation**
- "Auto-Battle — Begin!", "VICTORY!", "DEFEAT" currently just fade in/out.
- Add a `Vector3.Lerp` scale punch (0.7 → 1.05 → 1.0 over 0.3s) for impact.
- Lives entirely in `CombatHUD.ShowBanner()`.

**6. InitiativeBar acting highlight**
- When a unit acts, scale its entry to 1.2× for 0.3s (pulse effect).
- Add a gold border Image on the active entry.

**7. Combat log polish**
- Color-code ability names by type (physical = orange, magic = blue, heal = green).
- Already uses rich text — just needs the `AbilitySO.Type` passed through to the log string.

### TIER 3 — Roguelike loop expansion
These flesh out the meta-game.

**8. Planning Phase (pre-combat positioning)**
- 5-second window before `CombatPhase.Intro` where the player can drag units to any tile on their half of the grid.
- `ActionMenuUI.cs` is stubbed and ready to be repurposed as the planning HUD.
- Add `CombatPhase.Planning` between Setup and Intro.

**9. Unit Shop / Bench**
- Core TFT mechanic: buy units between combats on the MapSelector screen.
- `GameSession.ActiveUnits[]` — list of `UnitStatsSO` the player owns.
- `CombatManager.PlayerSpawns` already supports N units.

**10. More unit classes**
Each needs: a `UnitStatsSO`, a prefab variant, and (optionally) custom AI weights.
- **Archer** — `AbilitySO` with `Range=3`, `DamageMultiplier=0.9`
- **Mage** — AOERadius=1, `AbilityType.Magical`, high MagicAttack stat
- **Healer** — `HealMultiplier>0` ability; UnitAI already prioritises healing allies

**11. Map selector visual polish**
- Replace plain UI buttons with glowing crystal node prefabs (using HDRP emissive materials).
- `MapNode.cs` already exists and handles unlock state.

**12. Difficulty scaling**
- `LevelDataSO` has a `DifficultyMultiplier` field.
- Apply it in `CombatManager.SpawnUnits()` when creating enemy `RuntimeStats`.

---

## Architectural Notes for Future Work

**Adding a new unit class:**
1. Create `UnitStatsSO` asset in `Stats/` — set traits, base stats.
2. Duplicate `HeroPrefab` or `EnemyPrefab` — adjust glow color, animator.
3. Add `AbilitySO` assets in `Abilities/` for its kit.
4. Add entry to `CombatManager.EnemySpawns[]` + assign stats/prefab in Inspector.
5. Zero code changes required.

**Adding a new buff:**
1. Create `BuffSO` in `Buffs/` — fill stat deltas and rarity.
2. Add to `BuffRegistry.Buffs[]` list in the Inspector.
3. Zero code changes required.

**Adding a new ability:**
1. Create `AbilitySO` in `Abilities/`.
2. Assign `VFXKey` to one of: `"attack"`, `"magic"`, `"special"`, `"heal"`, `"defend"`, `"death"`.
3. Assign to a unit prefab's ability list or inject via `CombatManager`.
4. Zero code changes required.

**CanvasGroup hide pattern (critical — don't break this):**
Overlay panels (BuffSelectionUI, ResultScreenUI, PhaseBanner) must stay `SetActive(true)`.
Hide them with `CanvasGroup.alpha=0, interactable=false, blocksRaycasts=false`.
`SetActive(false)` prevents `StartCoroutine()` from working, which breaks fade-in animations.

**Event bus pattern (don't bypass this):**
`CombatHUD` and `UnitStatusPanel` subscribe to events — they never query `CombatManager.Instance` for state directly. This keeps UI decoupled from combat logic and makes it trivial to add new UI panels without touching combat code.
