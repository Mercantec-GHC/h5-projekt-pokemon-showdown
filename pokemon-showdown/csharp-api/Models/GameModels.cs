using System.Text.Json.Serialization;

namespace PokemonShowdown.Api.Models;

public sealed class Move
{
    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;


    [JsonPropertyName("damage_class")]
    public string DamageClass { get; set; } = string.Empty;
}



public sealed class SpriteSet
{
    [JsonPropertyName("front_default")]
    public string? FrontDefault { get; set; }

    [JsonPropertyName("back_default")]
    public string? BackDefault { get; set; }
}

public sealed class PokemonStats
{
    [JsonPropertyName("hp")]
    public int Hp { get; set; }

    [JsonPropertyName("attack")]
    public int Attack { get; set; }
}

public sealed class PokemonDetailsEntry
{
    [JsonPropertyName("id")]
    public int Id { get; set; }

    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;

    [JsonPropertyName("types")]
    public List<string> Types { get; set; } = [];

    [JsonPropertyName("sprites")]
    public SpriteSet? Sprites { get; set; }

    [JsonPropertyName("stats")]
    public PokemonStats Stats { get; set; } = new();

    [JsonPropertyName("level_up_damaging_moves")]
    public List<Move> LevelUpDamagingMoves { get; set; } = [];

}

public sealed class PokemonDetailsFile
{
    [JsonPropertyName("generatedAt")]
    public string GeneratedAt { get; set; } = string.Empty;

    [JsonPropertyName("source")]
    public string Source { get; set; } = string.Empty;

    [JsonPropertyName("count")]
    public int Count { get; set; }

    [JsonPropertyName("pokemon")]
    public List<PokemonDetailsEntry> Pokemon { get; set; } = [];
}

public sealed class BattlePokemon
{
    [JsonPropertyName("id")]
    public int Id { get; set; }

    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;

    [JsonPropertyName("types")]
    public List<string> Types { get; set; } = [];

    [JsonPropertyName("sprites")]
    public SpriteSet? Sprites { get; set; }

    [JsonPropertyName("stats")]
    public PokemonStats Stats { get; set; } = new();

    [JsonPropertyName("currentHp")]
    public int CurrentHp { get; set; }

    [JsonPropertyName("maxHp")]
    public int MaxHp { get; set; }

    [JsonPropertyName("moveset")]
    public List<Move> Moveset { get; set; } = [];

    [JsonPropertyName("fainted")]
    public bool Fainted { get; set; }


}

public sealed class BattleSide
{
    [JsonPropertyName("trainer")]
    public string Trainer { get; set; } = string.Empty;

    [JsonPropertyName("team")]
    public List<BattlePokemon> Team { get; set; } = [];

    [JsonPropertyName("activeIndex")]
    public int ActiveIndex { get; set; }
}

public sealed class BattleState
{
    [JsonPropertyName("battleId")]
    public string BattleId { get; set; } = string.Empty;

    [JsonPropertyName("turn")]
    public int Turn { get; set; }

    [JsonPropertyName("player")]
    public BattleSide Player { get; set; } = new();

    [JsonPropertyName("bot")]
    public BattleSide Bot { get; set; } = new();

    [JsonPropertyName("winner")]
    public string? Winner { get; set; }

    [JsonPropertyName("log")]
    public List<string> Log { get; set; } = [];

    [JsonPropertyName("playerMustSwitch")]
    public bool PlayerMustSwitch { get; set; }
}

public sealed class StartBattleRequest
{
    [JsonPropertyName("teamSize")]
    public int TeamSize { get; set; } = 6;

    [JsonPropertyName("movesPerPokemon")]
    public int MovesPerPokemon { get; set; } = 4;
}

public sealed class TurnRequest
{
    [JsonPropertyName("battleId")]
    public string? BattleId { get; set; }

    [JsonPropertyName("type")]
    public string? Type { get; set; }

    [JsonPropertyName("moveName")]
    public string? MoveName { get; set; }

    [JsonPropertyName("switchIndex")]
    public int? SwitchIndex { get; set; }
}

public sealed class StartBattleResponse
{
    [JsonPropertyName("battleId")]
    public string BattleId { get; set; } = string.Empty;

    [JsonPropertyName("state")]
    public BattleState State { get; set; } = new();
}

public sealed class ResolveTurnResponse
{
    [JsonPropertyName("state")]
    public BattleState State { get; set; } = new();

    [JsonPropertyName("events")]
    public List<string> Events { get; set; } = [];
}

public sealed class StateResponse
{
    [JsonPropertyName("state")]
    public BattleState State { get; set; } = new();
}
