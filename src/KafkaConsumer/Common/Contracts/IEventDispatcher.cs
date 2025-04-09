using CloudNative.CloudEvents;

namespace KafkaConsumer.Common.Contracts;

public interface IEventDispatcher
{
    bool DispatchEvent(CloudEvent cloudEvent);
}
