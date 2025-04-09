namespace KafkaConsumer.Common.Configuration;

public class TopicConfigEntry
{
    public string TopicName { get; set; } = string.Empty;
    public string EventType { get; set; } = string.Empty;
    public string HandlerName { get; set; } = string.Empty;
}

