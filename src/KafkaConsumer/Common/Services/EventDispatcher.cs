using CloudNative.CloudEvents;
using KafkaConsumer.Common.Configuration;
using KafkaConsumer.Common.Contracts;
using Microsoft.Extensions.Options;
using System;
using System.Collections.Generic;
using System.Linq;

namespace KafkaConsumer.Common.Services;

public class EventDispatcher : IEventDispatcher
{
    private readonly Dictionary<string, IEventHandler> _handlersByEventType;

    public EventDispatcher(IOptions<TopicConfigurations> configOptions,
                           IEnumerable<IEventHandler> handlers)
    {
        var config = configOptions.Value;
        Console.WriteLine($"EventDispatcher: Current configuration set: {config.CurrentSet}");
        Console.WriteLine($"EventDispatcher: Available sets: {string.Join(", ", config.Sets.Keys)}");
        
        string currentSet = config.CurrentSet;
        if (string.IsNullOrEmpty(currentSet) || !config.Sets.ContainsKey(currentSet))
        {
            throw new InvalidOperationException($"CurrentSet '{currentSet}' is not defined in configuration.");
        }

        var activeTopics = config.Sets[currentSet];
        Console.WriteLine($"EventDispatcher: Active topics count: {activeTopics.Count}");
        
        _handlersByEventType = new Dictionary<string, IEventHandler>(StringComparer.OrdinalIgnoreCase);

        // Build mapping from EventType -> Handler instance
        foreach (var entry in activeTopics)
        {
            Console.WriteLine($"EventDispatcher: Processing topic {entry.TopicName} with event type {entry.EventType}");
            // Find the handler with matching Name (case-insensitive)
            var handler = handlers.FirstOrDefault(h =>
                string.Equals(h.Name, entry.HandlerName, StringComparison.OrdinalIgnoreCase));
            if (handler == null)
            {
                throw new InvalidOperationException($"No handler implementation found for '{entry.HandlerName}' (needed for EventType='{entry.EventType}').");
            }
            if (_handlersByEventType.ContainsKey(entry.EventType))
            {
                throw new InvalidOperationException($"Multiple handlers configured for the same EventType '{entry.EventType}' - each event type must be unique.");
            }
            _handlersByEventType[entry.EventType] = handler;
            Console.WriteLine($"EventDispatcher: Successfully mapped {entry.EventType} to handler {handler.Name}");
        }
    }

    public bool DispatchEvent(CloudEvent cloudEvent)
    {
        if (cloudEvent == null) throw new ArgumentNullException(nameof(cloudEvent));
        string eventType = cloudEvent.Type;
        if (_handlersByEventType.TryGetValue(eventType, out var handler))
        {
            // Found a matching handler, invoke it
            try
            {
                return handler.ProcessEvent(cloudEvent);
            }
            catch (Exception ex)
            {
                // Handle exceptions from processing (logging, etc.)
                Console.Error.WriteLine($"Error in handler '{handler.Name}' for event '{eventType}': {ex}");
                return false;
            }
        }
        else
        {
            // No handler for this event type
            Console.WriteLine($"Warning: No handler configured for event type '{eventType}'. Event ignored.");
            return false;
        }
    }
}

