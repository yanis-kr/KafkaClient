using CloudNative.CloudEvents;
using KafkaConsumer.Common.Contracts;

namespace KafkaConsumer.Tests.Common.Mocks;

public class MockEventDispatcher : IEventDispatcher
{
    public bool DispatchEvent(CloudEvent cloudEvent)
    {
        return true;
    }
} 