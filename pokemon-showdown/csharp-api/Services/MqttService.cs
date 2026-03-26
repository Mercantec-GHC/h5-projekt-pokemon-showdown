using MQTTnet;
using MQTTnet.Client;
using System.Text;

namespace PokemonShowdown.Api.Services;

public class MqttService
{
    private readonly IMqttClient _mqttClient;
    private bool _isConnected = false;

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
                Console.WriteLine("MQTT Connected to HiveMQ Cloud");
            });
        }
        catch (Exception ex)
        {
            Console.WriteLine($"MQTT Connection Error: {ex.Message}");
        }
    }

    public async Task PublishAsync(string message)
    {
        try
        {
            if (!_isConnected)
            {
                Console.WriteLine("MQTT Not connected yet");
                return;
            }

            var msg = new MqttApplicationMessageBuilder()
                .WithTopic("game/status")
                .WithPayload(message)
                .Build();

            await _mqttClient.PublishAsync(msg);
            Console.WriteLine($"MQTT: {message}");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"MQTT Error: {ex.Message}");
        }
    }
}
