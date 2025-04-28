using Microsoft.Extensions.Diagnostics.HealthChecks;
using System.Threading;
using System.Threading.Tasks;

namespace KafkaConsumer.Common.Services;

public class KafkaHealthCheck : IHealthCheck
{
    private readonly IKafkaHealthCheck _kafkaHealthCheck;

    public KafkaHealthCheck(IKafkaHealthCheck kafkaHealthCheck)
    {
        _kafkaHealthCheck = kafkaHealthCheck;
    }

    public Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken cancellationToken = default)
    {
        return Task.FromResult(_kafkaHealthCheck.IsHealthy
            ? HealthCheckResult.Healthy("Kafka consumer is healthy")
            : HealthCheckResult.Unhealthy("Kafka consumer is unhealthy"));
    }
} 