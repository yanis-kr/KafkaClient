using CloudNative.CloudEvents;
using KafkaConsumer.Common.Contracts;
using System;

namespace KafkaConsumer.Features.UpdateUser.Handlers;

public class UpdateUserHandler : IEventHandler
{
    public string Name => "UpdateUser";

    public bool ProcessEvent(CloudEvent e)
    {
        if (e == null) throw new ArgumentNullException(nameof(e));
        Console.WriteLine($"[{Name}] Processing event: {e.Type}, ID={e.Id}");
        return true;
    }
}
