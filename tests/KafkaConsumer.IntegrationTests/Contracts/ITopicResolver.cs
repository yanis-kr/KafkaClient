using Confluent.Kafka;
using KafkaConsumer.Common.Contracts;
using System.Collections.Generic;

namespace KafkaConsumer.Common.Contracts
{
    /// <summary>
    /// Interface for resolving handlers for Kafka messages
    /// </summary>
    public interface ITopicResolver
    {
        /// <summary>
        /// Resolves the appropriate event handlers for the given Kafka message.
        /// </summary>
        /// <param name="consumeResult">The Kafka consume result containing the message</param>
        /// <returns>The event handlers if found, empty collection otherwise</returns>
        IEnumerable<IEventHandler> ResolveHandlers(ConsumeResult<string, byte[]> consumeResult);
    }
} 