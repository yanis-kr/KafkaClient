using System.Collections.Generic;

namespace KafkaConsumer.Common.Configuration;

public class TopicSubscription
{
    public string TopicName { get; set; } = string.Empty;
    public string EventType { get; set; } = string.Empty;
    public IEnumerable<string> HandlerNames { get; set; }
}

