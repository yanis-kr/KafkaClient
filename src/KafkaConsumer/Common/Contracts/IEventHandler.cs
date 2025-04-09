using CloudNative.CloudEvents;

namespace KafkaConsumer.Common.Contracts;

public interface IEventHandler
{
    string Name { get; }
    bool ProcessEvent(CloudEvent e);
}
