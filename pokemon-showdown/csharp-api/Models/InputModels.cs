using System.Text.Json.Serialization;

namespace PokemonShowdown.Api.Models
{
    public class Input
    {
        [JsonPropertyName("type")]
        public string Type { get; set; } = "";

        [JsonPropertyName("value")]
        public string Value { get; set; } = "";
    }

    public class MqttInput
    {
        [JsonPropertyName("type")]
        public string? Type { get; set; }

        [JsonPropertyName("action")]
        public string? Action { get; set; }

        [JsonPropertyName("direction")]
        public string? Direction { get; set; }
    }
}
