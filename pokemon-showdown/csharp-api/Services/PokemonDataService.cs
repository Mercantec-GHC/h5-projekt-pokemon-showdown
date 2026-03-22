using System.Text.Json;
using PokemonShowdown.Api.Models;

namespace PokemonShowdown.Api.Services;

public sealed class PokemonDataService
{
    private readonly SemaphoreSlim _loadLock = new(1, 1);
    private PokemonDetailsFile? _cached;

    public async Task<PokemonDetailsFile> GetDetailsAsync(CancellationToken cancellationToken = default)
    {
        if (_cached is not null)
        {
            return _cached;
        }

        await _loadLock.WaitAsync(cancellationToken);
        try
        {
            if (_cached is not null)
            {
                return _cached;
            }

            var filePath = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", "public", "pokemon-details.json"));
            if (!File.Exists(filePath))
            {
                throw new FileNotFoundException("Could not find pokemon-details.json", filePath);
            }

            var json = await File.ReadAllTextAsync(filePath, cancellationToken);
            var parsed = JsonSerializer.Deserialize<PokemonDetailsFile>(json, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            });

            if (parsed is null || parsed.Pokemon.Count == 0)
            {
                throw new InvalidOperationException("pokemon-details.json is invalid or empty");
            }

            _cached = parsed;
            return _cached;
        }
        finally
        {
            _loadLock.Release();
        }
    }
}
