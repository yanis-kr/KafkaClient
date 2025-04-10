using CloudNative.CloudEvents;
using KafkaConsumer.Common.Contracts;
using Microsoft.Extensions.Logging;
using System;

namespace KafkaConsumer.Features.UpdateUser.Handlers;

public class UpdateUserHandler : IEventHandler
{
    private readonly ILogger<UpdateUserHandler> _logger;

    public UpdateUserHandler(ILogger<UpdateUserHandler> logger)
    {
        _logger = logger;
    }

    public string Name => "UpdateUser";

    public bool ProcessEvent(CloudEvent e)
    {
        ArgumentNullException.ThrowIfNull(e, nameof(e));
        _logger.LogInformation("[{HandlerName}] Processing event: {EventType}, ID={EventId}", Name, e.Type, e.Id);
        return true;
    }
}
