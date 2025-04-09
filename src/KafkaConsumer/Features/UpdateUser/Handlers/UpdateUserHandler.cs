using CloudNative.CloudEvents;
using KafkaConsumer.Common.Contracts;
using System;

namespace KafkaConsumer.Features.UpdateUser.Handlers;

class UpdateUserHandler : IEventHandler
{
    public string Name => "UpdateUser";

    public bool ProcessEvent(CloudEvent e)
    {
        Console.WriteLine($"[{Name}] Processing event: {e.Type}, ID={e.Id}");
        return true;
    }
}
