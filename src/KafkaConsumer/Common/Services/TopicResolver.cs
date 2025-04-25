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
    /// Resolves the appropriate event handlers for the given Kafka message.
    /// </summary>
    /// <param name="consumeResult">The Kafka consume result containing the message</param>
    /// <returns>The event handlers if found, empty collection otherwise</returns>
    IEnumerable<IEventHandler> ResolveHandlers(ConsumeResult<string, byte[]> consumeResult);
}

public class TopicResolver : ITopicResolver
{
    private readonly IOptions<TopicSettings> _topicConfig;
    private readonly ILogger<TopicResolver> _logger;
    private readonly IServiceProvider _serviceProvider;
    private readonly Dictionary<string, Dictionary<string, List<Type>>> _topicEventTypeToHandlers;

    public TopicResolver(
        IOptions<TopicSettings> topicConfig,
        ILogger<TopicResolver> logger,
        IServiceProvider serviceProvider)
    {
        _topicConfig = topicConfig ?? throw new ArgumentNullException(nameof(topicConfig));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _serviceProvider = serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider));
        
        _topicEventTypeToHandlers = BuildTopicEventTypeToHandlerMap();
    }

    private Dictionary<string, Dictionary<string, List<Type>>> BuildTopicEventTypeToHandlerMap()
    {
        var result = new Dictionary<string, Dictionary<string, List<Type>>>(StringComparer.OrdinalIgnoreCase);
        
        var config = _topicConfig.Value;
        string currentSet = config.CurrentSet;
        
        if (string.IsNullOrEmpty(currentSet) || !config.Sets.ContainsKey(currentSet))
        {
            throw new InvalidOperationException($"CurrentSet '{currentSet}' is not defined in configuration.");
        }

        var currentSetSubscriptions = config.Sets[currentSet];
        _logger.LogInformation("Building topic-event type map with {Count} entries", currentSetSubscriptions.Count);

        foreach (var subscription in currentSetSubscriptions)
        {
            // Validate that each subscription has at least one handler
            if (subscription.HandlerNames == null || !subscription.HandlerNames.Any())
            {
                throw new InvalidOperationException(
                    $"Topic '{subscription.TopicName}' must have at least one handler defined.");
            }

            // Validate that each subscription has at least one event type
            if (subscription.EventTypes == null || !subscription.EventTypes.Any())
            {
                throw new InvalidOperationException(
                    $"Topic '{subscription.TopicName}' must have at least one event type defined.");
            }

            if (!result.TryGetValue(subscription.TopicName, out var topicDict))
            {
                topicDict = new Dictionary<string, List<Type>>(StringComparer.OrdinalIgnoreCase);
                result[subscription.TopicName] = topicDict;
            }

            foreach (var eventType in subscription.EventTypes)
            {
                if (!topicDict.TryGetValue(eventType, out var handlerList))
                {
                    handlerList = new List<Type>();
                    topicDict[eventType] = handlerList;
                }

                // Track if at least one valid handler was found for this subscription
                bool hasValidHandler = false;

                foreach (var handlerName in subscription.HandlerNames)
                {
                    var handlerType = Type.GetType(handlerName);
                    if (handlerType == null)
                    {
                        _logger.LogError("Handler type '{HandlerType}' not found for topic '{TopicName}' and event type '{EventType}'", 
                            handlerName, subscription.TopicName, eventType);
                        continue;
                    }

                    // Validate that the handler implements IEventHandler
                    if (!typeof(IEventHandler).IsAssignableFrom(handlerType))
                    {
                        _logger.LogError("Handler type '{HandlerType}' does not implement IEventHandler interface", handlerName);
                        continue;
                    }

                    handlerList.Add(handlerType);
                    hasValidHandler = true;
                    _logger.LogInformation("Mapped topic '{TopicName}' with event type '{EventType}' to handler type '{HandlerType}'", 
                        subscription.TopicName, eventType, handlerType.Name);
                }

                // Validate that at least one valid handler was found
                if (!hasValidHandler)
                {
                    throw new InvalidOperationException(
                        $"No valid handlers found for topic '{subscription.TopicName}' with event type '{eventType}'. " +
                        $"All specified handlers must exist and implement IEventHandler.");
                }
            }
        }

        return result;
    }

    public IEnumerable<IEventHandler> ResolveHandlers(ConsumeResult<string, byte[]> consumeResult)
    {
        if (consumeResult?.Message?.Value == null)
        {
            _logger.LogWarning("Received null message or value");
            return Enumerable.Empty<IEventHandler>();
        }

        var topicName = consumeResult.Topic;
        var eventType = consumeResult.ExtractEventType(_logger);

        _logger.LogInformation("Resolving handlers for topic: {Topic}, event type: {EventType}", 
            topicName, eventType ?? "null");

        var handlers = new List<IEventHandler>();

        // Try to find exact match first
        var configEntries = _topicConfig.Value.Sets[_topicConfig.Value.CurrentSet]
            .Where(x => x.TopicName == topicName && x.EventTypes.Contains(eventType));

        foreach (var configEntry in configEntries)
        {
            _logger.LogInformation("Found exact match for topic {Topic} and event type {EventType}", 
                topicName, eventType);
            foreach (var handlerName in configEntry.HandlerNames)
            {
                var handler = CreateHandler(handlerName);
                if (handler != null)
                {
                    handlers.Add(handler);
                }
            }
        }

        // Try wildcard match
        var wildcardEntries = _topicConfig.Value.Sets[_topicConfig.Value.CurrentSet]
            .Where(x => x.TopicName == topicName && x.EventTypes.Contains("*"));

        foreach (var configEntry in wildcardEntries)
        {
            _logger.LogInformation("Found wildcard match for topic {Topic}", topicName);
            foreach (var handlerName in configEntry.HandlerNames)
            {
                var handler = CreateHandler(handlerName);
                if (handler != null)
                {
                    handlers.Add(handler);
                }
            }
        }

        _logger.LogWarning("Handlers found for topic {Topic} and event type {EventType} : {count}",
            topicName, eventType ?? "null", handlers.Count);
        return handlers;
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
            // First try direct Type.GetType
            var handlerType = Type.GetType(handlerTypeFullName);
            
            // If that fails, try searching through loaded assemblies
            if (handlerType == null)
            {
                handlerType = AppDomain.CurrentDomain.GetAssemblies()
                    .Select(a => a.GetType(handlerTypeFullName))
                    .FirstOrDefault(t => t != null);
            }

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