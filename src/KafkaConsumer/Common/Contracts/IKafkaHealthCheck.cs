using System;

namespace KafkaConsumer.Common.Contracts;

public interface IKafkaHealthCheck
{
    bool IsHealthy { get; }
    void SetHealthy(bool isHealthy);
} 