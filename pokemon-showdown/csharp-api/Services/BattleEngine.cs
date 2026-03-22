using PokemonShowdown.Api.Models;

namespace PokemonShowdown.Api.Services;

public sealed class BattleEngine
{
    public BattleState CreateBattle(PokemonDetailsFile details, int teamSize, int movesPerPokemon)
    {
        // TODO: Build random player and bot teams from details.Pokemon.
        // TODO: Build randomized movesets (at least one damaging move if possible).
        // TODO: Initialize battle state (battleId, active indexes, turn, log, winner).
        throw new NotImplementedException("CreateBattle is intentionally left empty for manual implementation.");
    }

    public ResolveTurnResponse ResolveTurn(BattleState state, TurnRequest action)
    {
        // TODO: Validate move/switch action payload.
        // TODO: Resolve turn order, damage, switch behavior, and faint handling.
        // TODO: Update winner and append turn events to battle log.
        throw new NotImplementedException("ResolveTurn is intentionally left empty for manual implementation.");
    }
}
