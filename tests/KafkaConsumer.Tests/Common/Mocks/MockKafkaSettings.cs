using KafkaConsumer.Common.Configuration;

namespace KafkaConsumer.Tests.Common.Mocks;

public class MockKafkaSettings : KafkaSettings
{
    public MockKafkaSettings()
    {
        BootstrapServers = "localhost:9092";
        GroupId = "test-group";
        SecurityProtocol = "Plaintext";
        SaslMechanisms = "Plain";
        SaslUsername = "test-user";
        SaslPassword = "test-password";
        AutoOffsetReset = "Latest";
        SessionTimeoutMs = 30000;
        ClientId = "KafkaConsumerClient";
        EnableAutoCommit = true;
    }
} 