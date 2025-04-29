using Confluent.Kafka;
using KafkaConsumer.Common.Configuration;
using KafkaConsumer.Common.Contracts;
using KafkaConsumer.Common.Services;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using System;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace KafkaConsumer.Tests.Common;

public class KafkaConsumerTests
{
    [Fact]
    public void CreateConsumer_WithValidConfig_ShouldNotThrow()
    {
        // Arrange
        var config = new ConsumerConfig
        {
            BootstrapServers = "localhost:9092",
            GroupId = "test-group",
            AutoOffsetReset = AutoOffsetReset.Latest,
            EnableAutoCommit = true
        };

        // Act & Assert
        var exception = Record.Exception(() =>
        {
            using var consumer = new ConsumerBuilder<string, string>(config).Build();
            consumer.Close();
        });

        Assert.Null(exception);
    }

    [Fact]
    public void CreateConsumer_WithErrorHandler_ShouldNotThrow()
    {
        // Arrange
        var config = new ConsumerConfig
        {
            BootstrapServers = "localhost:9092",
            GroupId = "test-group",
            AutoOffsetReset = AutoOffsetReset.Latest,
            EnableAutoCommit = true
        };

        var loggerMock = new Mock<ILogger<KafkaListenerService>>();
        var healthCheckMock = new Mock<IKafkaHealthCheck>();

        // Act & Assert
        var exception = Record.Exception(() =>
        {
            using var consumer = new ConsumerBuilder<string, string>(config)
                .SetErrorHandler((_, e) =>
                {
                    loggerMock.Object.LogError("Kafka Error: {Reason}", e.Reason);
                })
                .Build();
            consumer.Close();
        });

        Assert.Null(exception);
    }

    [Fact]
    public async Task KafkaListenerService_WithMockDependencies_ShouldInitialize()
    {
        // Arrange
        var topicResolverMock = new Mock<ITopicResolver>();
        var topicConfigMock = new Mock<IOptions<TopicSettings>>();
        var kafkaSettingsMock = new Mock<IOptions<KafkaSettings>>();
        var loggerMock = new Mock<ILogger<KafkaListenerService>>();
        var healthCheckMock = new Mock<IKafkaHealthCheck>();

        topicConfigMock.Setup(x => x.Value).Returns(new TopicSettings
        {
            CurrentSet = "Set1",
            Sets = new System.Collections.Generic.Dictionary<string, System.Collections.Generic.List<TopicSubscription>>
            {
                ["Set1"] = new System.Collections.Generic.List<TopicSubscription>
                {
                    new TopicSubscription
                    {
                        TopicName = "test-topic",
                        EventTypes = new[] { "test.event" },
                        HandlerNames = new[] { "TestHandler" }
                    }
                }
            }
        });

        kafkaSettingsMock.Setup(x => x.Value).Returns(new KafkaSettings
        {
            BootstrapServers = "localhost:9092",
            GroupId = "test-group",
            AutoOffsetReset = "Latest",
            SecurityProtocol = "Plaintext",
            SaslMechanisms = "Plain",
            SessionTimeoutMs = 30000,
            ClientId = "test-client",
            EnableAutoCommit = true
        });

        // Create a service that will be immediately cancelled
        var service = new KafkaListenerService(
            topicResolverMock.Object,
            topicConfigMock.Object,
            kafkaSettingsMock.Object,
            loggerMock.Object,
            healthCheckMock.Object);

        // Act & Assert
        var exception = await Record.ExceptionAsync(async () =>
        {
            // Start the service and immediately stop it
            var cts = new CancellationTokenSource();
            cts.Cancel(); // Cancel immediately to prevent the consumer from starting
            await service.StartAsync(cts.Token);
        });

        Assert.Null(exception);
    }
} 