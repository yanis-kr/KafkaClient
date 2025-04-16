using System.Collections.Generic;

namespace KafkaConsumer.Common.Configuration;

public class ApiDefinition
{
    public string Name { get; set; }
    public string BaseUrl { get; set; }
    public Dictionary<string, string> RelativeUrls { get; set; } = new();
}
