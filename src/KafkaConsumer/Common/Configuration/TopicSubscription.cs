using System.Collections.Generic;

namespace KafkaConsumer.Common.Configuration;

public class TopicSubscription
{
    public string TopicName { get; set; } = string.Empty;
    public IEnumerable<string> EventTypes { get; set; } = new List<string>();
    public IEnumerable<string> HandlerNames { get; set; }
}

