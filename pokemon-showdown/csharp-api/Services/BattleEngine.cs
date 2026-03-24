<<<<<<< Updated upstream
=======
using System;
using System.Linq;
using System.Collections.Generic;
>>>>>>> Stashed changes
using PokemonShowdown.Api.Models;

namespace PokemonShowdown.Api.Services;

public sealed class BattleEngine
{
<<<<<<< Updated upstream
    public BattleState CreateBattle(PokemonDetailsFile details, int teamSize, int movesPerPokemon)
    {
        // TODO: Build random player and bot teams from details.Pokemon.
        // TODO: Build randomized movesets (at least one damaging move if possible).
        // TODO: Initialize battle state (battleId, active indexes, turn, log, winner).
        throw new NotImplementedException("CreateBattle is intentionally left empty for manual implementation.");
=======
    private readonly MovesService _movesService;

    public BattleEngine(MovesService movesService)
    {
        _movesService = movesService;
    }

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
            // build a pool of moves preferring level-up damaging moves, then status, then fallbacks
            var pool = new List<Move>();
            if (entry.LevelUpDamagingMoves?.Count > 0) pool.AddRange(entry.LevelUpDamagingMoves);
            if (entry.LevelUpStatusMoves?.Count > 0) pool.AddRange(entry.LevelUpStatusMoves);
            if (pool.Count == 0 && entry.FallbackAttacks?.Count > 0) pool.AddRange(entry.FallbackAttacks);

            var take = Math.Min(movesPerPokemon, Math.Max(1, pool.Count));
            var moveset = pool.OrderBy(_ => rng.Next()).Take(take)
                .Select(m =>
                {
                    // prefer enriched move data from MovesService when available
                    var full = _movesService.FindByName(m.Name);
                    if (full is not null) return full;

                    return new Move
                    {
                        Name = m.Name,
                        Level = m.Level,
                        DamageClass = m.DamageClass,
                        Power = m.Power,
                        Type = m.Type,
                        Fallback = m.Fallback
                    };
                })
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
>>>>>>> Stashed changes
    }

    public ResolveTurnResponse ResolveTurn(BattleState state, TurnRequest action)
    {
<<<<<<< Updated upstream
        // TODO: Validate move/switch action payload.
        // TODO: Resolve turn order, damage, switch behavior, and faint handling.
        // TODO: Update winner and append turn events to battle log.
        throw new NotImplementedException("ResolveTurn is intentionally left empty for manual implementation.");
=======
        if (state == null) throw new ArgumentNullException(nameof(state));
        if (action == null) throw new ArgumentNullException(nameof(action));

        var events = new List<string>();
        var rng = new Random();

        // ensure active indices are valid
        var playerSide = state.Player ?? new BattleSide();
        var botSide = state.Bot ?? new BattleSide();

        if (playerSide.Team.Count == 0 || botSide.Team.Count == 0)
        {
            return HandleNoPokemon(state, events);
        }

        var playerMon = playerSide.Team.ElementAtOrDefault(playerSide.ActiveIndex) ?? playerSide.Team[0];
        var botMon = botSide.Team.ElementAtOrDefault(botSide.ActiveIndex) ?? botSide.Team[0];

        if (state.PlayerMustSwitch && action.Type != "switch")
        {
            return HandleMustSwitch(state, events);
        }

        if (action.Type == "move")
        {
            return HandleMoveAction(state, action, events, rng, playerSide, botSide, playerMon, botMon);
        }
        else if (action.Type == "switch")
        {
            return HandleSwitchAction(state, action, events, playerSide);
        }

        return HandleUnknownAction(state, action, events);
    }

    private ResolveTurnResponse HandleNoPokemon(BattleState state, List<string> events)
    {
        var msg = "One side has no Pokemon; cannot resolve turn.";
        state.Log.Add(msg);
        events.Add(msg);
        return new ResolveTurnResponse { State = state, Events = events };
    }

    private ResolveTurnResponse HandleMustSwitch(BattleState state, List<string> events)
    {
        var must = "You must switch to a new Pokémon before continuing.";
        state.Log.Add(must);
        events.Add(must);
        return new ResolveTurnResponse { State = state, Events = events };
    }

    private ResolveTurnResponse HandleMoveAction(BattleState state, TurnRequest action, List<string> events, Random rng, BattleSide playerSide, BattleSide botSide, BattlePokemon playerMon, BattlePokemon botMon)
    {
        var requested = action.MoveName ?? string.Empty;
        var playerMove = playerMon.Moveset.FirstOrDefault(m => string.Equals(m.Name, requested, StringComparison.OrdinalIgnoreCase))
                         ?? playerMon.Moveset.FirstOrDefault();
        var botMove = botMon.Moveset.OrderBy(_ => rng.Next()).FirstOrDefault();

        AddMoveLog(state, events, state.Turn, playerSide.Trainer, playerMon.Name, playerMove?.Name);
        AddMoveLog(state, events, state.Turn, botSide.Trainer, botMon.Name, botMove?.Name);

        bool playerFirst = DetermineMoveOrder(playerMon, botMon, rng);

        if (playerFirst)
        {
            if (!botMon.Fainted)
            {
                PerformAttack(state, events, rng, playerMon, botMon, playerMove, playerSide, botSide);
                if (HandleBotFaint(state, events, botMon, botSide, playerSide))
                {
                    state.Turn++;
                    return new ResolveTurnResponse { State = state, Events = events };
                }
            }
            if (!playerMon.Fainted && botMove is not null && !botMon.Fainted)
            {
                PerformAttack(state, events, rng, botMon, playerMon, botMove, botSide, playerSide);
                if (HandlePlayerFaint(state, events, playerMon, playerSide))
                {
                    state.Turn++;
                    return new ResolveTurnResponse { State = state, Events = events };
                }
            }
        }
        else
        {
            if (!playerMon.Fainted && botMove is not null)
            {
                PerformAttack(state, events, rng, botMon, playerMon, botMove, botSide, playerSide);
                if (HandlePlayerFaint(state, events, playerMon, playerSide))
                {
                    state.Turn++;
                    return new ResolveTurnResponse { State = state, Events = events };
                }
            }
            if (!botMon.Fainted)
            {
                PerformAttack(state, events, rng, playerMon, botMon, playerMove, playerSide, botSide);
                if (HandleBotFaint(state, events, botMon, botSide, playerSide))
                {
                    state.Turn++;
                    return new ResolveTurnResponse { State = state, Events = events };
                }
            }
        }

        ApplyResidual(state, events, playerMon, playerSide);
        ApplyResidual(state, events, botMon, botSide);

        if (botMon.Fainted && state.Winner is null)
        {
            if (HandleBotFaint(state, events, botMon, botSide, playerSide))
            {
                state.Turn++;
                return new ResolveTurnResponse { State = state, Events = events };
            }
        }
        if (playerMon.Fainted)
        {
            state.PlayerMustSwitch = true;
        }
        state.Turn++;
        return new ResolveTurnResponse { State = state, Events = events };
    }

    // Helper methods for move action logic
    private void AddMoveLog(BattleState state, List<string> events, int turn, string trainer, string monName, string? moveName)
    {
        var msg = $"Turn {turn}: {trainer}'s {monName} used {moveName ?? "(no move)"}.";
        state.Log.Add(msg);
        events.Add(msg);
    }

    private bool DetermineMoveOrder(BattlePokemon playerMon, BattlePokemon botMon, Random rng)
    {
        double effPlayerSpeed = EffectiveStat(playerMon, "speed");
        double effBotSpeed = EffectiveStat(botMon, "speed");
        if (playerMon.Status == "par") effPlayerSpeed *= 0.5;
        if (botMon.Status == "par") effBotSpeed *= 0.5;
        return effPlayerSpeed > effBotSpeed || (Math.Abs(effPlayerSpeed - effBotSpeed) < 0.0001 && rng.Next(2) == 0);
    }

    private void PerformAttack(BattleState state, List<string> events, Random rng, BattlePokemon attacker, BattlePokemon defender, Move? mv, BattleSide atkSide, BattleSide defSide)
    {
        if (mv is null) return;
        if (mv.Accuracy is not null)
        {
            var accRoll = rng.Next(100) + 1;
            if (accRoll > mv.Accuracy.Value)
            {
                var miss = $"{attacker.Name}'s {mv.Name} missed!";
                state.Log.Add(miss);
                events.Add(miss);
                return;
            }
        }
        if (IsParalyzed(attacker, rng))
        {
            var parMsg = $"{attacker.Name} is paralyzed and can't move!";
            state.Log.Add(parMsg);
            events.Add(parMsg);
            return;
        }
        var cat = GetCategory(mv);
        if (cat != "status")
        {
            var dmg = ComputeDamage(mv, attacker, defender);
            defender.CurrentHp -= dmg;
            var dmgMsg = $"{defender.Name} took {dmg} damage (HP {Math.Max(0, defender.CurrentHp)}/{defender.MaxHp}).";
            state.Log.Add(dmgMsg);
            events.Add(dmgMsg);
        }
        if (mv.StatChanges is not null && mv.StatChanges.Count > 0)
        {
            if (mv.Target == "self") ApplyStatChanges(state, attacker, mv.StatChanges);
            else ApplyStatChanges(state, defender, mv.StatChanges);
        }
        if (mv.Status is not null)
        {
            var applied = TryApplyStatus(attacker, defender, mv, rng);
            if (applied is not null)
            {
                var sMsg = $"{applied.Item1} is now {applied.Item2}!";
                state.Log.Add(sMsg);
                events.Add(sMsg);
            }
        }
    }

    private bool HandleBotFaint(BattleState state, List<string> events, BattlePokemon botMon, BattleSide botSide, BattleSide playerSide)
    {
        if (botMon.CurrentHp <= 0 && !botMon.Fainted)
        {
            botMon.Fainted = true;
            botMon.CurrentHp = 0;
            var faintMsg = $"{botSide.Trainer}'s {botMon.Name} fainted!";
            state.Log.Add(faintMsg);
            events.Add(faintMsg);
            var botAlive = botSide.Team.FirstOrDefault(p => !p.Fainted);
            if (botAlive is null)
            {
                state.Winner = playerSide.Trainer;
                var winMsg = $"{playerSide.Trainer} wins the battle!";
                state.Log.Add(winMsg);
                events.Add(winMsg);
                return true;
            }
            else
            {
                var newIdx = botSide.Team.FindIndex(p => !p.Fainted);
                botSide.ActiveIndex = newIdx >= 0 ? newIdx : 0;
                var sw = $"{botSide.Trainer} sent out {botSide.Team[botSide.ActiveIndex].Name}.";
                state.Log.Add(sw);
                events.Add(sw);
            }
        }
        return false;
    }

    private bool HandlePlayerFaint(BattleState state, List<string> events, BattlePokemon playerMon, BattleSide playerSide)
    {
        if (playerMon.CurrentHp <= 0)
        {
            playerMon.Fainted = true;
            playerMon.CurrentHp = 0;
            var faintMsg2 = $"{playerSide.Trainer}'s {playerMon.Name} fainted!";
            state.Log.Add(faintMsg2);
            events.Add(faintMsg2);
            state.PlayerMustSwitch = true;
            var must = "Your active Pokémon fainted — choose a new Pokémon (switch).";
            state.Log.Add(must);
            events.Add(must);
            return true;
        }
        return false;
    }

    private void ApplyResidual(BattleState state, List<string> events, BattlePokemon mon, BattleSide side)
    {
        if (mon.Fainted) return;
        if (string.IsNullOrWhiteSpace(mon.Status)) return;
        if (mon.Status == "brn")
        {
            var dmg = Math.Max(1, mon.MaxHp / 16);
            mon.CurrentHp -= dmg;
            var m = $"{mon.Name} is hurt by its burn for {dmg} HP (HP {Math.Max(0, mon.CurrentHp)}/{mon.MaxHp}).";
            state.Log.Add(m);
            events.Add(m);
        }
        else if (mon.Status == "psn")
        {
            var dmg = Math.Max(1, mon.MaxHp / 8);
            mon.CurrentHp -= dmg;
            var m = $"{mon.Name} is hurt by poison for {dmg} HP (HP {Math.Max(0, mon.CurrentHp)}/{mon.MaxHp}).";
            state.Log.Add(m);
            events.Add(m);
        }
        if (mon.CurrentHp <= 0)
        {
            mon.Fainted = true;
            mon.CurrentHp = 0;
            var faint = $"{side.Trainer}'s {mon.Name} fainted from residual damage!";
            state.Log.Add(faint);
            events.Add(faint);
        }
    }

    // Utility methods for stat, status, and damage
    private double EffectiveStat(BattlePokemon mon, string stat)
    {
        double StageMultiplier(int stage)
        {
            if (stage >= 0) return (2.0 + stage) / 2.0;
            return 2.0 / (2 - stage);
        }
        stat = stat.ToLowerInvariant();
        return stat switch
        {
            "attack" => mon.Stats.Attack * StageMultiplier(mon.AttackStage),
            "defense" => mon.Stats.Defense * StageMultiplier(mon.DefenseStage),
            "special_attack" => mon.Stats.SpecialAttack * StageMultiplier(mon.SpecialAttackStage),
            "special_defense" => mon.Stats.SpecialDefense * StageMultiplier(mon.SpecialDefenseStage),
            "speed" => mon.Stats.Speed * StageMultiplier(mon.SpeedStage),
            _ => 1
        };
    }

    private string GetCategory(Move mv)
    {
        if (!string.IsNullOrWhiteSpace(mv.DamageClass)) return mv.DamageClass!.ToLowerInvariant();
        if (mv.Power is null) return "status";
        return "physical";
    }

    private int ComputeDamage(Move mv, BattlePokemon attacker, BattlePokemon defender)
    {
        if (mv?.Power is null) return 0;
        var category = GetCategory(mv);
        double atk = category == "special" ? EffectiveStat(attacker, "special_attack") : EffectiveStat(attacker, "attack");
        double def = category == "special" ? EffectiveStat(defender, "special_defense") : EffectiveStat(defender, "defense");
        double burnMult = (attacker.Status == "brn" && category == "physical") ? 0.5 : 1.0;
        var raw = (mv.Power.Value * (atk / Math.Max(1.0, def)) / 2.0) * burnMult;
        var dmg = Math.Max(1, (int)Math.Round(raw));
        return dmg;
    }

    private void ApplyStatChanges(BattleState state, BattlePokemon target, List<StatChange>? changes)
    {
        if (changes is null) return;
        foreach (var sc in changes)
        {
            var s = sc.Stat?.ToLowerInvariant() ?? string.Empty;
            var before = 0;
            switch (s)
            {
                case "attack": before = target.AttackStage; target.AttackStage = ClampStage(target.AttackStage + sc.Stages); break;
                case "defense": before = target.DefenseStage; target.DefenseStage = ClampStage(target.DefenseStage + sc.Stages); break;
                case "special_attack": before = target.SpecialAttackStage; target.SpecialAttackStage = ClampStage(target.SpecialAttackStage + sc.Stages); break;
                case "special_defense": before = target.SpecialDefenseStage; target.SpecialDefenseStage = ClampStage(target.SpecialDefenseStage + sc.Stages); break;
                case "speed": before = target.SpeedStage; target.SpeedStage = ClampStage(target.SpeedStage + sc.Stages); break;
                default: break;
            }
            state.Log.Add($"{target.Name}'s {sc.Stat} changed by {sc.Stages} stages ({before} -> {before + sc.Stages}).");
        }
    }

    private int ClampStage(int stage) => Math.Max(-6, Math.Min(6, stage));

    private bool IsParalyzed(BattlePokemon mon, Random rng)
    {
        if (mon.Status != "par") return false;
        return (rng.Next(100) < 25);
    }

    private Tuple<string, string>? TryApplyStatus(BattlePokemon attacker, BattlePokemon defender, Move mv, Random rng)
    {
        var cat = GetCategory(mv);
        var target = (cat == "self") ? attacker : defender;
        var status = mv.Status;
        if (status is null) return null;
        if (string.IsNullOrWhiteSpace(status.Name)) return null;
        var roll = rng.Next(100) + 1;
        if (roll <= status.Chance && string.IsNullOrWhiteSpace(target.Status))
        {
            target.Status = status.Name;
            return Tuple.Create(target.Name, status.Name);
        }
        return null;
    }

    private ResolveTurnResponse HandleSwitchAction(BattleState state, TurnRequest action, List<string> events, BattleSide playerSide)
    {
        var idx = action.SwitchIndex ?? playerSide.ActiveIndex;
        if (idx < 0 || idx >= playerSide.Team.Count)
        {
            var msg = $"Invalid switch index {idx}.";
            state.Log.Add(msg);
            events.Add(msg);
            return new ResolveTurnResponse { State = state, Events = events };
        }

        playerSide.ActiveIndex = idx;
        // clearing the must-switch flag when player switches
        state.PlayerMustSwitch = false;
        var msgSw = $"{playerSide.Trainer} switched to {playerSide.Team[idx].Name}.";
        state.Log.Add(msgSw);
        events.Add(msgSw);
        state.Turn++;
        return new ResolveTurnResponse { State = state, Events = events };
    }

    private ResolveTurnResponse HandleUnknownAction(BattleState state, TurnRequest action, List<string> events)
    {
        var unknown = $"Unknown action type: {action.Type}";
        state.Log.Add(unknown);
        events.Add(unknown);
        return new ResolveTurnResponse { State = state, Events = events };
>>>>>>> Stashed changes
    }
}
