using CloudNative.CloudEvents;
using CloudNative.CloudEvents.Kafka;
using CloudNative.CloudEvents.SystemTextJson;
using Confluent.Kafka;
using Microsoft.Extensions.Configuration;
using System;
using System.Threading.Tasks;

internal class Program
{
    public const string TopicName = "topic_1";
    public const string EventType = "user.created";
    private static async Task Main(string[] args)
    {
        var config = new ConfigurationBuilder()
       .AddJsonFile("appsettings.json", optional: false)
       .AddUserSecrets<Program>()
       .Build();

        var kafkaSettings = config.GetSection("Kafka").Get<KafkaSettings>();
        var topicName = TopicName;

        var producerConfig = new ProducerConfig
        {
            BootstrapServers = kafkaSettings.BootstrapServers,
            SecurityProtocol = Enum.Parse<SecurityProtocol>(kafkaSettings.SecurityProtocol),
            SaslMechanism = Enum.Parse<SaslMechanism>(kafkaSettings.SaslMechanisms),
            SaslUsername = kafkaSettings.SaslUsername,
            SaslPassword = kafkaSettings.SaslPassword,
            ClientId = kafkaSettings.ClientId
        };

        using var producer = new ProducerBuilder<string, byte[]>(producerConfig).Build();

        var cloudEvent = new CloudEvent
        {
            Id = Guid.NewGuid().ToString(),
            Source = new Uri("urn:example:producer"),
            Type = EventType,
            Time = DateTimeOffset.UtcNow,
            DataContentType = "application/json",
            Data = new
            {
                AccountId = "123456789",
                CustomerName = "John Doe",
                Date = DateTime.UtcNow
            }
        };

        var formatter = new JsonEventFormatter();
        var kafkaMessage = cloudEvent.ToKafkaMessage(ContentMode.Structured, formatter);

        try
        {
            var result = await producer.ProduceAsync(topicName, kafkaMessage);
            Console.WriteLine($"Event sent successfully to '{result.TopicPartitionOffset}'");
        }
        catch (ProduceException<string, byte[]> ex)
        {
            Console.WriteLine($"Error sending event: {ex.Error.Reason}");
        }
    }
}

internal class KafkaSettings
{
    public string BootstrapServers { get; set; } = "";
    public string SecurityProtocol { get; set; } = "SaslSsl";
    public string SaslMechanisms { get; set; } = "Plain";
    public string SaslUsername { get; set; } = "";
    public string SaslPassword { get; set; } = "";
    public string ClientId { get; set; } = "";
}
