using Confluent.Kafka;
using System.Text.Json;
using System.Text;
using System;
using Microsoft.Extensions.Logging;
using System.Linq;
using Microsoft.Extensions.Options;
using KafkaConsumer.Common.Configuration;

namespace KafkaConsumer.Common.Extensions;

public static class ConsumeResultExtensions
{
    private static IOptions<TopicSettings>? _topicConfig;

    public static void Initialize(IOptions<TopicSettings> topicConfig)
    {
        _topicConfig = topicConfig;
    }

    public static string ExtractEventType(this ConsumeResult<string, byte[]> consumeResult, ILogger logger = null)
    {
        return ExtractTypeFromHeaders(consumeResult, logger)
               ?? ExtractTypeFromCloudEvent(consumeResult, logger)
               ?? ExtractTypeFromJson(consumeResult, logger)
               ?? ExtractTypeFromConfiguration(consumeResult, logger);
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

    private static string ExtractTypeFromConfiguration(ConsumeResult<string, byte[]> consumeResult, ILogger logger)
    {
        if (_topicConfig == null)
        {
            logger?.LogWarning("Topic configuration not initialized. Cannot extract event type from configuration.");
            return null;
        }

        try
        {
            var messageContent = Encoding.UTF8.GetString(consumeResult.Message.Value);
            var currentSet = _topicConfig.Value.CurrentSet;
            var subscriptions = _topicConfig.Value.Sets[currentSet];

            // Get all unique event types from configuration (excluding wildcard *)
            var eventTypes = subscriptions
                .Where(s => s.TopicName == consumeResult.Topic && s.EventType != "*")
                .Select(s => s.EventType)
                .Distinct()
                .ToList();

            if (!eventTypes.Any())
            {
                logger?.LogDebug("No event types found in configuration for topic {Topic}", consumeResult.Topic);
                return null;
            }

            // Check if message content contains any of the configured event types
            foreach (var eventType in eventTypes)
            {
                if (messageContent.Contains(eventType, StringComparison.OrdinalIgnoreCase))
                {
                    logger?.LogDebug("Found event type in message content: {EventType}", eventType);
                    return eventType;
                }
            }

            logger?.LogDebug("No matching event type found in message content for topic {Topic}", consumeResult.Topic);
            return null;
        }
        catch (Exception ex)
        {
            logger?.LogError(ex, "Error extracting event type from configuration");
            return null;
        }
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

