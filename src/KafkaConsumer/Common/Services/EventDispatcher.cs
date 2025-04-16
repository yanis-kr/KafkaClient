using CloudNative.CloudEvents;
using KafkaConsumer.Common.Configuration;
using KafkaConsumer.Common.Contracts;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System;
using System.Collections.Generic;
using System.Linq;

namespace KafkaConsumer.Common.Services;

public class EventDispatcher : IEventDispatcher
{
    private readonly Dictionary<string, IEventHandler> _handlersByEventType;
    private readonly ILogger<EventDispatcher> _logger;

    public EventDispatcher(IOptions<TopicSettings> configOptions,
                         IEnumerable<IEventHandler> handlers,
                         ILogger<EventDispatcher> logger)
    {
        var config = configOptions.Value;
        _logger = logger;
        
        _logger.LogInformation("Current configuration set: {CurrentSet}", config.CurrentSet);
        _logger.LogInformation("Available sets: {Sets}", string.Join(", ", config.Sets.Keys));
        
        string currentSet = config.CurrentSet;
        if (string.IsNullOrEmpty(currentSet) || !config.Sets.ContainsKey(currentSet))
        {
            throw new InvalidOperationException($"CurrentSet '{currentSet}' is not defined in configuration.");
        }

        var activeTopics = config.Sets[currentSet];
        _logger.LogInformation("Active topics count: {Count}", activeTopics.Count);
        
        _handlersByEventType = new Dictionary<string, IEventHandler>(StringComparer.OrdinalIgnoreCase);

        foreach (var entry in activeTopics)
        {
            _logger.LogInformation("Processing topic {TopicName} with event type {EventType}", 
                entry.TopicName, entry.EventType);

            var handler = handlers.FirstOrDefault(h => h.Name.Equals(entry.HandlerName, 
                StringComparison.OrdinalIgnoreCase));

            if (handler == null)
            {
                throw new InvalidOperationException(
                    $"No handler found with name '{entry.HandlerName}' for event type '{entry.EventType}'");
            }

            _handlersByEventType[entry.EventType] = handler;
            _logger.LogInformation("Successfully mapped {EventType} to handler {HandlerName}", 
                entry.EventType, handler.Name);
        }
    }

    public bool DispatchEvent(CloudEvent cloudEvent)
    {
        if (cloudEvent == null)
        {
            throw new ArgumentNullException(nameof(cloudEvent));
        }

        string eventType = cloudEvent.Type;
        if (!_handlersByEventType.TryGetValue(eventType, out var handler))
        {
            _logger.LogWarning("No handler configured for event type '{EventType}'. Event ignored.", eventType);
            return false;
        }

        try
        {
            return handler.ProcessEvent(cloudEvent);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing event {EventType} with handler {HandlerName}", 
                eventType, handler.Name);
            return false;
        }
    }
}

