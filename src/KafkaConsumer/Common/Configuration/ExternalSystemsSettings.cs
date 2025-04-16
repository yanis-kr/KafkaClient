using System.Collections.Generic;

namespace KafkaConsumer.Common.Configuration;

public class ExternalSystemsSettings
{
    public AuthSettings SharedAuthentication { get; set; } = new();
    public List<ApiDefinition> ApiDefinitions { get; set; } = new();
}
