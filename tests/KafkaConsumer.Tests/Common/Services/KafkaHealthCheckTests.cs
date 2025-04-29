using KafkaConsumer.Common.Contracts;
using KafkaConsumer.Common.Services;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Moq;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace KafkaConsumer.Tests.Common.Services;

public class KafkaHealthCheckTests
{
    private readonly Mock<IKafkaHealthCheck> _kafkaHealthCheckMock;
    private readonly KafkaHealthCheck _kafkaHealthCheck;
    private readonly HealthCheckContext _healthCheckContext;

    public KafkaHealthCheckTests()
    {
        _kafkaHealthCheckMock = new Mock<IKafkaHealthCheck>();
        _kafkaHealthCheck = new KafkaHealthCheck(_kafkaHealthCheckMock.Object);
        _healthCheckContext = new HealthCheckContext();
    }

    [Fact]
    public async Task CheckHealthAsync_WhenKafkaIsHealthy_ReturnsHealthyResult()
    {
        // Arrange
        _kafkaHealthCheckMock.Setup(x => x.IsHealthy).Returns(true);

        // Act
        var result = await _kafkaHealthCheck.CheckHealthAsync(_healthCheckContext);

        // Assert
        Assert.Equal(HealthStatus.Healthy, result.Status);
        Assert.Equal("Kafka consumer is healthy", result.Description);
    }

    [Fact]
    public async Task CheckHealthAsync_WhenKafkaIsUnhealthy_ReturnsUnhealthyResult()
    {
        // Arrange
        _kafkaHealthCheckMock.Setup(x => x.IsHealthy).Returns(false);

        // Act
        var result = await _kafkaHealthCheck.CheckHealthAsync(_healthCheckContext);

        // Assert
        Assert.Equal(HealthStatus.Unhealthy, result.Status);
        Assert.Equal("Kafka consumer is unhealthy", result.Description);
    }
} 