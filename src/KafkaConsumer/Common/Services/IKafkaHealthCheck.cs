using System;

namespace KafkaConsumer.Common.Services;

public interface IKafkaHealthCheck
{
    bool IsHealthy { get; }
    void SetHealthy(bool isHealthy);
} 