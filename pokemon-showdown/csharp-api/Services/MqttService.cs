using MQTTnet;
using MQTTnet.Client;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace PokemonShowdown.Api.Services;

public class MqttService
{
    private readonly IMqttClient _mqttClient;
    private bool _isConnected = false;
    public string? LastDirection { get; private set; }

    public MqttService()
    {
        var factory = new MqttFactory();
        _mqttClient = factory.CreateMqttClient();

        var options = new MqttClientOptionsBuilder()
            .WithTcpServer("5cd80438d8ff49d99af9926fe3f099c1.s1.eu.hivemq.cloud", 8883)
            .WithTls()
            .WithCredentials("Silasbillum", "Silasbillum1")
            .Build();

        try
        {
            _ = Task.Run(async () =>
            {
                await _mqttClient.ConnectAsync(options);
                _isConnected = true;
                Console.WriteLine("🟢 MQTT Connected");
                
                // Subscribe to input and status topics
                await _mqttClient.SubscribeAsync("game/input");
                await _mqttClient.SubscribeAsync("game/status");
                Console.WriteLine("📡 Subscribed to game/input and game/status");
                
                // Add message handler
                _mqttClient.DisconnectedAsync += async e =>
                {
                    _isConnected = false;
                    Console.WriteLine("MQTT Disconnected");
                };

                _mqttClient.ApplicationMessageReceivedAsync += async e =>
                {
                    try
                    {
                        var payload = Encoding.UTF8.GetString(e.ApplicationMessage.Payload);
                        Console.WriteLine($"MQTT [{e.ApplicationMessage.Topic}] {payload}");

                        if (e.ApplicationMessage.Topic == "game/input")
                        {
                            
                            if (payload.Contains("\"direction\""))
                            {
                                var direction = JsonSerializer.Deserialize<InputMessage>(payload);
                                if (direction?.Direction != null)
                                {
                                    LastDirection = direction.Direction;
                                    Console.WriteLine($"Input: {direction.Direction}");
                                }
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"Message Parse Error: {ex.Message}");
                    }
                };
            });
        }
        catch (Exception ex)
        {
            Console.WriteLine($"MQTT Error: {ex.Message}");
        }
    }

    public void SetDirection(string direction)
    {
        LastDirection = direction;
        Console.WriteLine($"Direction: {direction}");
    }

    public string? GetDirection()
    {
        var dir = LastDirection;
        LastDirection = null;
        return dir;
    }

    public async Task PublishAsync(string message)
    {
        try
        {
            if (!_isConnected) return;

            var msg = new MqttApplicationMessageBuilder()
                .WithTopic("game/status")
                .WithPayload(message)
                .Build();

            await _mqttClient.PublishAsync(msg);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"MQTT Publish Error: {ex.Message}");
        }
    }
}

public class InputMessage
{
    [JsonPropertyName("direction")]
    public string? Direction { get; set; }
}


