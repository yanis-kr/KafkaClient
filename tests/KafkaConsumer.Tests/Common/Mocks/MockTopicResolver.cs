using CloudNative.CloudEvents;
using Confluent.Kafka;
using KafkaConsumer.Common.Contracts;
using KafkaConsumer.Common.Services;

namespace KafkaConsumer.Tests.Common.Mocks;

public class MockTopicResolver : ITopicResolver
{
    public bool DispatchEvent(CloudEvent cloudEvent)
    {
        return true;
    }

    public IEventHandler? ResolveHandler(ConsumeResult<string, byte[]> consumeResult)
    {
        return null;
    }
} 