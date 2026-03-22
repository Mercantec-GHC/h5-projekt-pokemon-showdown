# Pokemon Bot Battle Backlog

This backlog is grouped by priority so you can ship a playable version first, then iterate toward Showdown-like depth.

## Must Have

1. Start battle endpoint creates player and bot teams.
2. Turn endpoint supports move and switch actions.
3. Battle state endpoint returns current state by battleId.
4. Team generator guarantees at least one damaging move when possible.
5. Faint handling and forced switch to next alive Pokemon.
6. Manual switch validation (not active, not fainted, in range).
7. Winner detection when one side has no alive Pokemon.
8. Basic battle log for each turn event.
9. Frontend can start battle, choose move, and play turn.
10. Frontend can click team sprites to switch Pokemon.
11. Fainted Pokemon are visually grayed out and disabled in switch UI.
12. pokemon-details.json includes sprites, stats, move pools, and abilities.
13. README explains architecture and setup steps.

## Should Have

1. Type effectiveness chart in damage calculation.
2. STAB bonus in damage formula.
3. Accuracy and miss chance per move.
4. Physical vs special stat split in damage formula.
5. Priority move handling in turn order.
6. Basic status moves (burn, poison, paralysis) effects over turns.
7. Better bot move selection than highest raw power.
8. Bot switching logic when matchup is poor.
9. Team preview UI before first turn.
10. Health bar UI and clearer battle feedback animations.
11. Error handling and friendly user messages for invalid actions.
12. Unit tests for mechanics and switching helpers.
13. Integration tests for start, turn, and state endpoints.

## Nice To Have

1. Seeded RNG support for reproducible battles.
2. Persistent battle storage in a database.
3. Save/replay battle logs.
4. Weather and terrain systems.
5. Hazards, stat stages, and entry effects.
6. Ability and item effect framework.
7. Expanded bot AI using expected value and matchup scoring.
8. Ranked difficulty presets for bot behavior.
9. Match history page in UI.
10. Sound effects and advanced visual polish.
11. Spectator mode and shared battle links.
12. Performance profiling and caching improvements.

## Suggested Sprint Plan

### Sprint 1 (Playable Loop)

1. Complete all Must Have items.
2. Verify end-to-end gameplay from start battle to winner.

### Sprint 2 (Core Battle Quality)

1. Add type chart, STAB, and accuracy.
2. Add tests for damage and turn order.

### Sprint 3 (AI and Systems)

1. Improve bot strategy and switching.
2. Add first status effects and polish UI.

### Sprint 4+ (Showdown Depth)

1. Add advanced mechanics (weather, abilities, items, hazards).
2. Add persistence, replay, and quality-of-life features.
