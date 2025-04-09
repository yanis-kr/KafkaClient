using System.Collections.Generic;

namespace KafkaConsumer.Common.Configuration;

public class TopicConfigurations
{
    public string CurrentSet { get; set; } = string.Empty;
    public Dictionary<string, List<TopicConfigEntry>> Sets { get; set; } = new();
}

