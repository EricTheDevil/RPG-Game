# CLAUDE.md — RPG Project Instructions

## Folder Workflow (CRITICAL)

- `Assets/_Project/` — canonical shared source. **Never write here directly.**
- `Assets/_Local/` — personal working copy. **All edits go here.**
- `Assets/_Local/` is git-ignored and will not be committed.
- To finalise: copy changed files from `_Local/` → `_Project/`, then commit.

When asked to edit any script, prefab, or asset — always target the `_Local/` path, not `_Project/`.

## Project

TFT-style autobattle tactical RPG in Unity HDRP. See PROJECT_CONTEXT.md for full architecture.

## Key rules

- Never bypass CanvasGroup hide pattern — overlays stay SetActive(true), hidden via alpha=0.
- UI subscribes to events, never polls CombatManager directly.
- Health bars use Image.fillAmount, never Unity Slider.
- OnSpawned signature: Action<List<Unit>, List<Unit>> — player list first, enemy list second.
- Non-combat cards resolve in EventDeckUI; combat/elite go through GameManager.PlayCombatCard().
