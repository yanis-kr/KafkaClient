using KafkaConsumer.Common.Configuration;
using Microsoft.Extensions.Options;
using System;
using System.Collections.Generic;
using System.Linq;

namespace KafkaConsumer.Common.Utls;

public class ExternalUrlResolver(IOptions<ExternalSystemsSettings> options)
{
    private readonly Dictionary<string, ApiDefinition> _apiMap =
        options.Value.ApiDefinitions.ToDictionary(x => x.Name, StringComparer.OrdinalIgnoreCase);

    public string GetAbsoluteUrl(string apiName, string relativeUrlKey)
    {
        if (!_apiMap.TryGetValue(apiName, out var api))
        {
            return null;
        }

        if (!api.RelativeUrls.TryGetValue(relativeUrlKey, out var relativePath))
        {
            return null;
        }
        return $"{api.BaseUrl.TrimEnd('/')}/{relativePath.TrimStart('/')}";
    }
}

