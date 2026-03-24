
using System;
using System.Linq;
using System.Collections.Generic;

using PokemonShowdown.Api.Models;

namespace PokemonShowdown.Api.Services;

public sealed class BattleEngine
{

    public BattleState CreateBattle(PokemonDetailsFile details, int teamSize, int movesPerPokemon)
    {
        if (details is null) throw new ArgumentNullException(nameof(details));

        var rng = new Random();

        var available = details.Pokemon ?? new List<PokemonDetailsEntry>();
        if (available.Count == 0) throw new InvalidOperationException("No pokemon available to build teams.");

        teamSize = Math.Max(1, Math.Min(teamSize, available.Count));
        movesPerPokemon = Math.Max(1, movesPerPokemon);

        List<PokemonDetailsEntry> Sample(int count)
        {
            return available.OrderBy(_ => rng.Next()).Take(count).ToList();
        }

        BattlePokemon BuildBattlePokemon(PokemonDetailsEntry entry)
        {
            // Only use damaging moves, always pick 4 (or as many as available)
            var pool = entry.LevelUpDamagingMoves ?? new List<Move>();
            var moveset = pool.OrderBy(_ => rng.Next()).Take(4)
                .Select(m => new Move { Name = m.Name, DamageClass = m.DamageClass })
                .ToList();
            var maxHp = Math.Max(1, entry.Stats?.Hp ?? 1);
            return new BattlePokemon
            {
                Id = entry.Id,
                Name = entry.Name,
                Types = entry.Types is null ? new List<string>() : new List<string>(entry.Types),
                Sprites = entry.Sprites,
                Stats = entry.Stats ?? new(),
                CurrentHp = maxHp,
                MaxHp = maxHp,
                Moveset = moveset,
                Fainted = false
            };
        }

        var playerTeam = Sample(teamSize).Select(BuildBattlePokemon).ToList();
        var botTeam = Sample(teamSize).Select(BuildBattlePokemon).ToList();

        var state = new BattleState
        {
            BattleId = Guid.NewGuid().ToString("N"),
            Turn = 1,
            Player = new BattleSide
            {
                Trainer = "Player",
                Team = playerTeam,
                ActiveIndex = 0
            },
            Bot = new BattleSide
            {
                Trainer = "Bot",
                Team = botTeam,
                ActiveIndex = 0
            },
            Winner = null,
            Log = new List<string> { $"Battle {DateTime.UtcNow:O} started between Player and Bot." }
        };

        return state;
    }

    public ResolveTurnResponse ResolveTurn(BattleState state, TurnRequest action)
    {
        var events = new List<string>();
        var player = state.Player;
        var bot = state.Bot;
        if (player.Team.Count == 0 || bot.Team.Count == 0)
            return SimpleResp(state, events, "One side has no Pokemon; cannot resolve turn.");
        var p = player.Team[player.ActiveIndex];
        var b = bot.Team[bot.ActiveIndex];
        if (state.PlayerMustSwitch && action.Type != "switch")
            return SimpleResp(state, events, "You must switch to a new Pokémon before continuing.");
        if (action.Type == "move")
        {
            b.CurrentHp -= Math.Max(1, p.Stats.Attack);
            events.Add($"{p.Name} attacked {b.Name} for {Math.Max(1, p.Stats.Attack)} damage.");
            if (b.CurrentHp <= 0)
            {
                b.Fainted = true; b.CurrentHp = 0;
                events.Add($"{b.Name} fainted!");
                var next = bot.Team.FirstOrDefault(x => !x.Fainted);
                if (next == null)
                {
                    state.Winner = player.Trainer;
                    events.Add($"{player.Trainer} wins the battle!");
                    state.Turn++;
                    state.Log.AddRange(events);
                    return new ResolveTurnResponse { State = state, Events = events };
                }
                bot.ActiveIndex = bot.Team.FindIndex(x => !x.Fainted);
                events.Add($"Bot sent out {bot.Team[bot.ActiveIndex].Name}.");
            }
            if (!b.Fainted)
            {
                p.CurrentHp -= Math.Max(1, b.Stats.Attack);
                events.Add($"{b.Name} attacked {p.Name} for {Math.Max(1, b.Stats.Attack)} damage.");
                if (p.CurrentHp <= 0)
                {
                    p.Fainted = true; p.CurrentHp = 0;
                    events.Add($"{p.Name} fainted!");
                    state.PlayerMustSwitch = true;
                }
            }
            state.Turn++;
            state.Log.AddRange(events);
            return new ResolveTurnResponse { State = state, Events = events };
        }
        if (action.Type == "switch")
        {
            var idx = action.SwitchIndex ?? player.ActiveIndex;
            if (idx < 0 || idx >= player.Team.Count)
                return SimpleResp(state, events, $"Invalid switch index {idx}.");
            player.ActiveIndex = idx;
            state.PlayerMustSwitch = false;
            events.Add($"{player.Trainer} switched to {player.Team[idx].Name}.");
            state.Turn++;
            state.Log.AddRange(events);
            return new ResolveTurnResponse { State = state, Events = events };
        }
        return SimpleResp(state, events, $"Unknown action type: {action.Type}");
    }

    private ResolveTurnResponse SimpleResp(BattleState state, List<string> events, string msg)
    {
        events.Add(msg);
        state.Log.AddRange(events);
        return new ResolveTurnResponse { State = state, Events = events };
    }
}
