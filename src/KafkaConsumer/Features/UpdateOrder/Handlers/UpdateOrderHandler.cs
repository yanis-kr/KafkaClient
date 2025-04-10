using CloudNative.CloudEvents;
using KafkaConsumer.Common.Contracts;
using Microsoft.Extensions.Logging;
using System;

namespace KafkaConsumer.Features.UpdateOrder.Handlers;

public class UpdateOrderHandler : IEventHandler
{
    private readonly ILogger<UpdateOrderHandler> _logger;

    public UpdateOrderHandler(ILogger<UpdateOrderHandler> logger)
    {
        _logger = logger;
    }

    public string Name => "UpdateOrder";

    public bool ProcessEvent(CloudEvent e)
    {
        ArgumentNullException.ThrowIfNull(e, nameof(e));
        _logger.LogInformation("[{HandlerName}] Processing event: {EventType}, ID={EventId}", Name, e.Type, e.Id);
        return true;
    }
}
