using CloudNative.CloudEvents;
using Confluent.Kafka;
using KafkaConsumer.Common.Contracts;

namespace KafkaConsumer.Tests.Common.Handlers;

public class TestHandler : IEventHandler
{
    public string Name => "TestHandler";

    //public bool ProcessEvent(CloudEvent e)
    //{
    //    return true;
    //}

    public Task<bool> ProcessEvent(ConsumeResult<string, byte[]> consumeResult)
    {
        return Task.FromResult(true);
    }
} 