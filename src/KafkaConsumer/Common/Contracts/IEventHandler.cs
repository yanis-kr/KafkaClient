using Confluent.Kafka;
using System.Threading.Tasks;

namespace KafkaConsumer.Common.Contracts;

/// <summary>
/// Interface for handling Kafka events
/// </summary>
public interface IEventHandler
{
    public string Name { get; }
    /// <summary>
    /// Processes a Kafka message
    /// </summary>
    /// <param name="consumeResult">The Kafka consume result containing the message</param>
    /// <returns>A task representing the asynchronous operation</returns>
    Task<bool> ProcessEvent(ConsumeResult<string, byte[]> consumeResult);
}
