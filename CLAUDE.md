# CLAUDE.md — RPG Project Instructions

## Folder Workflow

- `Assets/_Project/` — **the only source of truth. All edits go here.**
- Work directly on a personal git branch. Merge to `main` to share with teammates.
- Never create an `_Local/` folder — it's a deprecated pattern.

## Project

TFT-style autobattle tactical RPG in Unity HDRP. See PROJECT_CONTEXT.md for full architecture.

## Key rules

- Never bypass CanvasGroup hide pattern — overlays stay SetActive(true), hidden via alpha=0.
- UI subscribes to events, never polls CombatManager directly.
- Health bars use Image.fillAmount, never Unity Slider.
- OnSpawned signature: Action<List<Unit>, List<Unit>> — player list first, enemy list second.
- Non-combat cards resolve in EventDeckUI; combat/elite go through GameManager.PlayCombatCard().

---

## Current State (as of 2026-04-20)

### What works
- Real-time TFT autobattle — per-unit coroutines, staggered starts, approach + attack loop
- Hero vs N enemies, fully data-driven via `UnitSpawnConfig` SO
- HUD: `PlayerPanels[]` / `EnemyPanels[]` arrays support any team size
- Health bars (HUD + world-space): `Image.fillAmount`, smooth `MoveTowards`, billboard
- Victory → `BuffSelectionUI` (3 cards) → `GameManager.OnCombatVictory()` → EventDeck
- Defeat → `ResultScreen` (Retry / Abandon Run)
- Banner scale-punch animation, HDRP crit post-process, speed toggle (x1 / x1.5 / x2)
- Hero HP + full `RuntimeStats` persist across fights (deep copy on victory, reset on defeat)
- Morale economy: rations consumed per fight; morale drops when starved
- Dynamic difficulty: +5% enemy scaling per fight won this run
- Crit system: `CritChance` + `CritMultiplier` on `RuntimeStats`, buff-upgradeable
- EventDeck: scrollable FTL-style card hand, resource bar, detail panel, drop notification
- Shop overlay + LevelUp overlay wired into EventDeck
- Cheshire model + PBR material on hero; Mixamo animator controller (Idle/Move/Attack/Hit/Die)
- World map + area map navigation with threat/starvation systems
- Commander profile + class XP + class tasks (persists across runs via SaveSystem)

### Known bugs (fix these before extending)
- **Double `FightsWon` increment** — `PersistHeroHP()` and `RecordCombatWin()` both do `FightsWon++`. One fight counts as 2. Difficulty scaling is double what it should be.
- **Double victory resource grant** — `PlaySectorCombat()` calls `ApplyResources(ev.VictoryGrant)` before combat, then `OnCombatVictory()` calls it again. Player gets 2× gold/scrap every win.
- **No run-win screen** — `OnBossDefeated()` loads `MainMenuScene` with a TODO comment. Winning the game feels identical to losing.

---

## Next Steps — Prioritised

### TIER 1 — Fix before anything else
1. **Fix double `FightsWon`** — remove `FightsWon++` from `PersistHeroHP()`, keep only `RecordCombatWin()`.
2. **Fix double resource grant** — remove the speculative `ApplyResources` from `PlaySectorCombat()`; only grant on confirmed victory in `OnCombatVictory()`.
3. **Run-win screen** — replace the `MainMenuScene` fallback in `OnBossDefeated()` with a proper victory overlay (reuse `ResultScreenUI` pattern with a "Run Complete" banner).

### TIER 2 — Core loop feel
4. **Pre-combat planning phase** — 5-second window before `CombatPhase.Intro` where the player drags units to any tile on their half of the grid. Add `CombatPhase.Planning`. This is the one missing strategic moment that makes the game active instead of passive.
5. **Team Campfire screen** (see feature brief below).
6. **Enemy variety** — add 2 new enemy archetypes with distinct ability SOs (Archer: range 3, Mage: AOE).

### TIER 3 — Meta-game expansion
7. Card offer screen — "choose 1 of 3" on drop instead of auto-adding (Slay the Spire style).
8. Scrap sinks beyond shop — forge (buff upgrade), salvage (discard card for Scrap).
9. More unit classes — Archer, Mage, Healer (UnitAI already prioritises heals).
10. Difficulty scaling tuning once FightsWon bug is fixed.

---

## Feature Brief: Team Campfire Screen

### Concept
A pre-combat / between-stage popup that shows the player's team in a living, atmospheric scene — characters walking around a campfire, idle chatter animations. Accessible from the **world map / area map navigation screens** via a corner button (bottom-right, like the "View Party" button in Darkest Dungeon or the roster button in XCOM before a mission). Opens as a full-screen overlay.

### What it contains
- **3D campfire viewport** — characters navigate around a campfire using NavMesh or waypoint patrol. HDRP emissive fire light pulses on them. Can reuse existing unit prefabs + Idle/Walk animator states.
- **Character cards** (left panel) — click a unit card to select that unit. Shows name, class, level, HP, key stats.
- **Selected unit detail** (right panel) — full stat block, equipped abilities, active buffs (from `EarnedBuffs`), class task progress bar.
- **Team composition controls** — reorder units (drag or up/down arrows), swap bench ↔ active roster. Active slots = whatever `UnitSpawnConfig.PlayerUnits[]` supports.
- **Dismiss button** — closes overlay, returns to map navigation.

### Entry points
- Corner button on WorldMapUI / AreaMapUI (bottom-right, always visible during navigation).
- Automatically shown after class selection at run start (first time only).

### Architecture notes
- Campfire scene rendered in a `RenderTexture` → `RawImage` in the overlay UI, OR a separate additive scene loaded on top. RenderTexture approach is simpler and keeps scene management clean.
- `CampfireOverlayUI.cs` — CanvasGroup hide pattern. Reads `GameSession.ActiveUnits[]` (add this list to GameSession) and `GameSession.EarnedBuffs`.
- Unit 3D viewport uses a dedicated camera pointing at the campfire diorama. Units are spawned from `GameSession.ActiveUnits[]` prefabs when the overlay opens, destroyed when closed.
- Team reorder writes back to `GameSession.ActiveUnits[]` order — `CombatManager.SpawnFromConfig()` already respects spawn order, so no further plumbing needed.
- Keep the campfire diorama small (3×3 tiles max) — it's atmosphere, not gameplay.

---

## Architectural Patterns (never break these)

| Pattern | Rule |
|---|---|
| CanvasGroup hide | Overlays stay `SetActive(true)`; hidden via `alpha=0, interactable=false, blocksRaycasts=false` |
| Event bus | `CombatHUD` subscribes to `CombatManager` events — never queries state directly |
| Health bars | Always `Image.fillAmount` — never Unity `Slider` for world-space bars |
| Death coroutine host | `DieRoutine` runs on `CombatManager.Instance` (unit may deactivate mid-coroutine) |
| RuntimeStats | Always `unit.RT?.MaxHP ?? unit.Stats.MaxHP` — RT is live, Stats is base SO |
| Deep copy on persist | Always `new RuntimeStats(hero.RT)` when saving to `GameSession` |
| OnSpawned signature | `Action<List<Unit>, List<Unit>>` — player list first, enemy list second |
| Card resolution | Non-combat cards resolve in `EventDeckUI`; combat/elite call `GameManager.PlayCombatCard()` |
