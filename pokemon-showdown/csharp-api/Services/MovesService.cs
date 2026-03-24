using System;
using System.IO;
using System.Linq;
using System.Text.Json;
using PokemonShowdown.Api.Models;

namespace PokemonShowdown.Api.Services;

public sealed class MovesService
{
    private readonly object _loadLock = new();
    private List<Move>? _cached;

    public List<Move> GetMoves()
    {
        if (_cached is not null) return _cached;
        lock (_loadLock)
        {
            if (_cached is not null) return _cached;

            var filePath = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", "public", "pokemon-details.json"));
            if (!File.Exists(filePath))
            {
                _cached = new List<Move>();
                return _cached;
            }

            var json = File.ReadAllText(filePath);
            var parsed = JsonSerializer.Deserialize<List<Move>>(json, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            });

            _cached = parsed ?? new List<Move>();
            return _cached;
        }
    }

    public Move? FindByName(string name)
    {
        if (string.IsNullOrWhiteSpace(name)) return null;
        return GetMoves().FirstOrDefault(m => string.Equals(m.Name, name, StringComparison.OrdinalIgnoreCase));
    }
}
