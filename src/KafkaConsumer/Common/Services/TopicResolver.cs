using Confluent.Kafka;
using KafkaConsumer.Common.Configuration;
using KafkaConsumer.Common.Contracts;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using CloudNative.CloudEvents;
using CloudNative.CloudEvents.SystemTextJson;
using CloudNative.CloudEvents.Kafka;
using System.Text.Json;
using System.Linq;
using System.Text;
using KafkaConsumer.Common.Extensions;

namespace KafkaConsumer.Common.Services;

public interface ITopicResolver
{
    /// <summary>
    /// Resolves the appropriate event handler for the given Kafka message.
    /// </summary>
    /// <param name="consumeResult">The Kafka consume result containing the message</param>
    /// <returns>The event handler if found, null otherwise</returns>
    IEventHandler ResolveHandler(ConsumeResult<string, byte[]> consumeResult);
}

public class TopicResolver : ITopicResolver
{
    private readonly IOptions<TopicSettings> _topicConfig;
    private readonly ILogger<TopicResolver> _logger;
    private readonly IServiceProvider _serviceProvider;
    private readonly Dictionary<string, Dictionary<string, Type>> _topicEventTypeToHandler;
    private readonly Dictionary<string, Type> _handlerTypes;

    public TopicResolver(
        IOptions<TopicSettings> topicConfig, 
        ILogger<TopicResolver> logger,
        IServiceProvider serviceProvider)
    {
        _topicConfig = topicConfig ?? throw new ArgumentNullException(nameof(topicConfig));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _serviceProvider = serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider));
        
        _topicEventTypeToHandler = BuildTopicEventTypeToHandlerMap();
        _handlerTypes = new Dictionary<string, Type>();
    }

    private Dictionary<string, Dictionary<string, Type>> BuildTopicEventTypeToHandlerMap()
    {
        var result = new Dictionary<string, Dictionary<string, Type>>(StringComparer.OrdinalIgnoreCase);
        
        var config = _topicConfig.Value;
        string currentSet = config.CurrentSet;
        
        if (string.IsNullOrEmpty(currentSet) || !config.Sets.ContainsKey(currentSet))
        {
            throw new InvalidOperationException($"CurrentSet '{currentSet}' is not defined in configuration.");
        }

        var activeTopics = config.Sets[currentSet];
        _logger.LogInformation("Building topic-event type map with {Count} entries", activeTopics.Count);

        foreach (var entry in activeTopics)
        {
            if (!result.ContainsKey(entry.TopicName))
            {
                result[entry.TopicName] = new Dictionary<string, Type>(StringComparer.OrdinalIgnoreCase);
            }
            
            var handlerType = Type.GetType(entry.HandlerName);
            if (handlerType == null)
            {
                _logger.LogWarning("Handler type '{HandlerType}' not found for topic '{TopicName}'", 
                    entry.HandlerName, entry.TopicName);
                continue;
            }

            result[entry.TopicName][entry.EventType] = handlerType;
            _logger.LogInformation("Mapped topic '{TopicName}' with event type '{EventType}' to handler type '{HandlerType}'", 
                entry.TopicName, entry.EventType, handlerType.Name);
        }

        return result;
    }

    public IEventHandler ResolveHandler(ConsumeResult<string, byte[]> consumeResult)
    {
        if (consumeResult?.Message?.Value == null)
        {
            _logger.LogWarning("Received null message or value");
            return null;
        }

        var topicName = consumeResult.Topic;
        var eventType = consumeResult.ExtractEventType(_logger);

        _logger.LogInformation("Resolving handler for topic: {Topic}, event type: {EventType}", 
            topicName, eventType ?? "null");

        // Try to find exact match first
        var configEntry = _topicConfig.Value.Sets[_topicConfig.Value.CurrentSet]
            .FirstOrDefault(x => x.TopicName == topicName && x.EventType == eventType);

        if (configEntry != null)
        {
            _logger.LogInformation("Found exact match for topic {Topic} and event type {EventType}", 
                topicName, eventType);
            return CreateHandler(configEntry.HandlerName);
        }

        // If no exact match, try wildcard match
        configEntry = _topicConfig.Value.Sets[_topicConfig.Value.CurrentSet]
            .FirstOrDefault(x => x.TopicName == topicName && x.EventType == "*");

        if (configEntry != null)
        {
            _logger.LogInformation("Found wildcard match for topic {Topic}", topicName);
            return CreateHandler(configEntry.HandlerName);
        }

        _logger.LogWarning("No handler found for topic {Topic} and event type {EventType}", 
            topicName, eventType ?? "null");
        return null;
    }

    private IEventHandler CreateHandler(string handlerTypeFullName)
    {
        if (string.IsNullOrEmpty(handlerTypeFullName))
        {
            _logger.LogError("Handler type name is null or empty");
            return null;
        }

        try
        {
            var handlerType = Type.GetType(handlerTypeFullName);
            if (handlerType == null)
            { 
                _logger.LogError("Handler type not found for name: {HandlerTypeName}", handlerTypeFullName);
                return null;
            }

            return (IEventHandler)_serviceProvider.GetRequiredService(handlerType);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating handler instance for {HandlerName}", handlerTypeFullName);
            return null;
        }
    }
} 