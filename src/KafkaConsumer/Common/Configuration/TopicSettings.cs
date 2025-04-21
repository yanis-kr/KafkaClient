using System.Collections.Generic;

namespace KafkaConsumer.Common.Configuration;

public class TopicSettings
{
    public string CurrentSet { get; set; } = string.Empty;
    public Dictionary<string, List<TopicSubscription>> Sets { get; set; } = new();
}

