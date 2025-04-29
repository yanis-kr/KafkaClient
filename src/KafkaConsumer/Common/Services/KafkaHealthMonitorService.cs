using KafkaConsumer.Common.Contracts;
using Microsoft.Extensions.Logging;
using System;

namespace KafkaConsumer.Common.Services;

public class KafkaHealthMonitorService : IKafkaHealthCheck
{
    private readonly ILogger<KafkaHealthMonitorService> _logger;
    private bool _isHealthy;

    public bool IsHealthy => _isHealthy;

    public KafkaHealthMonitorService(ILogger<KafkaHealthMonitorService> logger)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _isHealthy = false;
    }

    public void SetHealthy(bool isHealthy)
    {
        if (_isHealthy != isHealthy)
        {
            _isHealthy = isHealthy;
            _logger.LogInformation("Kafka health status changed to: {IsHealthy}", isHealthy);
        }
    }
} 