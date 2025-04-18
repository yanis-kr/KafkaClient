using Confluent.Kafka;
using System.Text.Json;
using System.Text;
using System;
using Microsoft.Extensions.Logging;
using System.Linq;

namespace KafkaConsumer.Common.Extensions;

public static class ConsumeResultExtensions
{
    public static string ExtractEventType(this ConsumeResult<string, byte[]> consumeResult, ILogger logger = null)
    {
        return ExtractTypeFromHeaders(consumeResult, logger)
               ?? ExtractTypeFromCloudEvent(consumeResult, logger)
               ?? ExtractTypeFromJson(consumeResult, logger);
    }

    private static string ExtractTypeFromHeaders(ConsumeResult<string, byte[]> consumeResult, ILogger logger)
    {
        foreach (var header in consumeResult.Message.Headers)
        {
            if (header.Key.Equals("type", StringComparison.OrdinalIgnoreCase) ||
                header.Key.Equals("eventType", StringComparison.OrdinalIgnoreCase))
            {
                var typeFromHeader = Encoding.UTF8.GetString(header.GetValueBytes());
                logger?.LogDebug("Found event type in Kafka headers: {EventType}", typeFromHeader);
                return typeFromHeader;
            }
        }

        logger?.LogDebug("No event type found in Kafka headers.");
        return null;
    }

    private static string ExtractTypeFromCloudEvent(ConsumeResult<string, byte[]> consumeResult, ILogger logger)
    {
        try
        {
            var jsonString = Encoding.UTF8.GetString(consumeResult.Message.Value);
            using var jsonDoc = JsonDocument.Parse(jsonString);
            var root = jsonDoc.RootElement;

            if (root.TryGetProperty("type", out var typeElement) &&
                root.TryGetProperty("source", out _) &&
                root.TryGetProperty("id", out _))
            {
                var type = typeElement.GetString();
                logger?.LogDebug("Found event type in CloudEvent: {EventType}", type);
                return type;
            }
        }
        catch (Exception ex)
        {
            logger?.LogDebug(ex, "Message is not a CloudEvent");
        }

        return null;
    }

    private static string ExtractTypeFromJson(ConsumeResult<string, byte[]> consumeResult, ILogger logger)
    {
        try
        {
            var jsonString = Encoding.UTF8.GetString(consumeResult.Message.Value);
            using var jsonDoc = JsonDocument.Parse(jsonString);
            if (jsonDoc.RootElement.TryGetProperty("type", out var typeElement))
            {
                var typeFromJson = typeElement.GetString();
                logger?.LogDebug("Found event type in JSON: {EventType}", typeFromJson);
                return typeFromJson;
            }
        }
        catch (Exception ex)
        {
            logger?.LogDebug(ex, "Message is not valid JSON or does not contain type field");
        }

        logger?.LogInformation("Could not determine event type.");
        return null;
    }

    public static void LogMessageContent(
      this ConsumeResult<string, byte[]> consumeResult,
      ILogger logger,
      string methodName = "Default")
    {
        if (consumeResult == null) throw new ArgumentNullException(nameof(consumeResult));
        if (logger == null) throw new ArgumentNullException(nameof(logger));

        var messageKey = consumeResult.Message.Key ?? "(null)";
        var messageValue = consumeResult.Message.Value != null
            ? Encoding.UTF8.GetString(consumeResult.Message.Value)
            : "(null)";

        var headers = consumeResult.Message.Headers.Count > 0
            ? string.Join(", ", consumeResult.Message.Headers.Select(h =>
                $"{h.Key}: {Encoding.UTF8.GetString(h.GetValueBytes())}"))
            : "(none)";

        var methodInfo = !string.IsNullOrEmpty(methodName) ? $"Method={methodName}, " : "";

        logger.LogInformation("[{MethodName}] Processing event: Topic={Topic}, Key={Key}, Headers=[{Headers}], Value={Value}",
            methodInfo, consumeResult.Topic, messageKey, headers, messageValue);
    }
}

