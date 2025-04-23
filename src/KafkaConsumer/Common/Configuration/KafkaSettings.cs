namespace KafkaConsumer.Common.Configuration;

public class KafkaSettings
{
    public string BootstrapServers { get; set; } = string.Empty;
    public string GroupId { get; set; } = string.Empty;
    public string AutoOffsetReset { get; set; } = "Latest";  // "Latest" or "Earliest"
    public string SecurityProtocol { get; set; } = "Plaintext";  // "Plaintext" or "SaslSsl"
    public string SaslMechanisms { get; set; } = "Plain";  // "Plain" or "ScramSha256"
    public string SaslUsername { get; set; } = string.Empty;
    public string SaslPassword { get; set; } = string.Empty;

    public int SessionTimeoutMs { get; set; } = 30000;  // Default session timeout
    public string ClientId { get; set; } = "KafkaConsumerClient";  // Default client ID

    public bool EnableAutoCommit { get; set; } = true;  // Default auto-commit setting

    // Schema Registry settings
    public string SchemaRegistryUrl { get; set; } = string.Empty;
    public string SchemaRegistryAuthKey { get; set; } = string.Empty;
    public string SchemaRegistryAuthSecret { get; set; } = string.Empty;
}

