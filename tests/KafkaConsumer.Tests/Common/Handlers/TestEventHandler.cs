using CloudNative.CloudEvents;
using KafkaConsumer.Common.Contracts;

namespace KafkaConsumer.Tests.Common.Handlers;

public class TestHandler : IEventHandler
{
    public string Name => "TestHandler";

    public bool ProcessEvent(CloudEvent e)
    {
        return true;
    }
} 