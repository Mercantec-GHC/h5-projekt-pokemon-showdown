# Pokemon Showdown Style Bot Battle (Next.js + C# API)

This README explains exactly how this project was structured so you can rebuild the same architecture yourself.

## 1) Project goal

Build a single-player Pokemon battle game where:

1. Teams are random.
2. Each Pokemon has random moves chosen from valid move pools.
3. The player fights a bot.
4. You can attack or switch Pokemon.
5. Fainted Pokemon cannot be switched into.

## 2) Folder structure used

Core game logic is split by responsibility.

1. Data generation script:
	scripts/generate-pokemon-details.mjs
2. Generated datasets:
	public/pokemon-list.json
	public/pokemon-details.json
3. Domain logic:
	app/lib/game/types.ts
	app/lib/game/data.ts
	app/lib/game/random.ts
	app/lib/game/team.ts
	app/lib/game/bot.ts
	app/lib/game/Switching.ts
	app/lib/game/mechanics.ts
	app/lib/game/session-store.ts
4. C# battle API:
	csharp-api/Program.cs
	csharp-api/Controllers/GameController.cs
	csharp-api/Services/BattleEngine.cs
	csharp-api/Services/BattleStore.cs
	csharp-api/Services/PokemonDataService.cs
	csharp-api/Models/GameModels.cs
5. Next.js API routes (legacy, optional):
	app/api/game/start/route.ts
	app/api/game/turn/route.ts
	app/api/game/state/route.ts
	app/api/game/route.ts (utility route kept for list save/team debug)
6. Frontend test/play UI:
	app/components/GameFetcher.tsx

Why this split matters:

1. Routes stay thin.
2. Mechanics are reusable and testable.
3. You can replace the UI later without rewriting battle logic.

## 3) Data pipeline (what was added)

The generator script reads pokemon-list.json and fetches each Pokemon from PokeAPI.

For each Pokemon, it stores:

1. Basic identity and stats.
2. Types and abilities.
3. Sprites:
	sprites.front_default
	sprites.back_default
4. Moves:
	level_up_moves
	level_up_damaging_moves
	level_up_status_moves
	fallback_attacks (used if a Pokemon has no level-up damaging move)
5. Items.

Important design choice:

1. JSON stores move pools, not a fixed moveset.
2. Random moves are chosen later at team/battle creation time.

## 4) Commands you run

Run inside the pokemon-showdown folder.

1. Install dependencies:
	npm install
2. Generate full details file:
	npm run generate:pokemon-details
3. Run C# API:
	npm run dev:api
4. Run Next.js dev server:
	npm run dev
5. Set frontend API target:
	copy .env.example to .env.local

Required .env.local value:

1. NEXT_PUBLIC_GAME_API_BASE=http://localhost:5108/game

Optional quick generation for testing:

1. npm run generate:pokemon-details -- --limit 8

## 5) Battle flow architecture

### Start battle

POST /game/start

1. Loads pokemon-details.json.
2. Generates random player team and random bot team.
3. Randomly builds each Pokemon moveset from move pools.
4. Stores state in memory.
5. Returns battleId and state.

### Play turn

POST /game/turn

Supported actions:

1. Move action:
	{
	  "battleId": "...",
	  "type": "move",
	  "moveName": "tackle"
	}
2. Switch action:
	{
	  "battleId": "...",
	  "type": "switch",
	  "switchIndex": 2
	}

The resolver:

1. Applies action.
2. Resolves bot response.
3. Applies damage.
4. Handles faint.
5. Forces auto-switch when active faints.
6. Checks winner.
7. Appends battle log events.

### Get current state

GET /game/state?battleId=...

Returns latest stored state for UI refresh/recovery.

## 6) Switching system details

Switching logic lives in app/lib/game/Switching.ts.

It supports:

1. Forced switch to next alive Pokemon after faint.
2. Manual switch to a chosen team index.
3. Validation rules:
	Cannot switch to current active Pokemon.
	Cannot switch to fainted Pokemon.
	Index must be in range.

## 7) Frontend behavior in GameFetcher

The UI in app/components/GameFetcher.tsx now:

1. Starts battles.
2. Plays turns with selected moves.
3. Sends switch actions when a team sprite is clicked.
4. Shows all team sprites.
5. Grays out fainted Pokemon.
6. Disables invalid switch targets.
7. Displays battle log and winner.

## 8) How to rebuild this from scratch

Use this order:

1. Create data generator and produce pokemon-details.json.
2. Create shared types (types.ts).
3. Add data loader/cache (data.ts).
4. Add team generation + random moveset builder (team.ts, random.ts).
5. Add bot move selection (bot.ts).
6. Add turn resolver (mechanics.ts).
7. Add switching helpers (Switching.ts).
8. Add in-memory battle store (session-store.ts).
9. Add start/turn/state API routes.
10. Connect UI to those routes.

## 9) Current limitations (known)

This is an MVP engine, not full Pokemon Showdown simulation yet.

Not fully implemented yet:

1. Real Pokemon damage formula.
2. Type effectiveness chart.
3. Accuracy/evasion.
4. Priority moves.
5. Full status/ability/item effects.
6. Smart switching AI.

## 10) Suggested next improvements

1. Add full type chart and STAB in mechanics.
2. Add move accuracy and crit chance.
3. Add better bot strategy (switch logic + threat checks).
4. Add persistent storage (database) instead of memory store.
5. Add unit tests for mechanics and switching.
