using System.Collections.Concurrent;
using PokemonShowdown.Api.Models;

namespace PokemonShowdown.Api.Services;

public sealed class BattleStore
{
    private readonly ConcurrentDictionary<string, BattleState> _store = new();

    public BattleState Create(BattleState state)
    {
        _store[state.BattleId] = state;
        return state;
    }

    public BattleState? Get(string battleId)
    {
        _store.TryGetValue(battleId, out var state);
        return state;
    }

    public BattleState Update(BattleState state)
    {
        _store[state.BattleId] = state;
        return state;
    }
}
