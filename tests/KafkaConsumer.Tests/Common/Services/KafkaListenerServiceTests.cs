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

namespace KafkaConsumer.Tests.Common.Services;

public class KafkaListenerServiceTests
{
    private readonly Mock<ITopicResolver> _mockTopicResolver;
    private readonly Mock<IOptions<TopicSettings>> _mockTopicConfig;
    private readonly Mock<IOptions<KafkaSettings>> _mockKafkaSettings;
    private readonly Mock<ILogger<KafkaListenerService>> _mockLogger;
    private readonly TopicSettings _topicConfig;
    private readonly KafkaSettings _kafkaSettings;

    public KafkaListenerServiceTests()
    {
        _mockTopicResolver = new Mock<ITopicResolver>();
        _mockTopicConfig = new Mock<IOptions<TopicSettings>>();
        _mockKafkaSettings = new Mock<IOptions<KafkaSettings>>();
        _mockLogger = new Mock<ILogger<KafkaListenerService>>();

        _topicConfig = new TopicSettings
        {
            CurrentSet = "Development",
            Sets = new Dictionary<string, List<TopicConfigEntry>>
            {
                ["Development"] = new List<TopicConfigEntry>
                {
                    new() { TopicName = "topic_1", EventType = "user.created", HandlerName = "UpdateUser" },
                    new() { TopicName = "topic_2", EventType = "order.created", HandlerName = "UpdateOrder" }
                }
            }
        };

        _kafkaSettings = new KafkaSettings
        {
            BootstrapServers = "localhost:9092",
            GroupId = "test-group",
            AutoOffsetReset = "Latest"
        };

        _mockTopicConfig.Setup(x => x.Value).Returns(_topicConfig);
        _mockKafkaSettings.Setup(x => x.Value).Returns(_kafkaSettings);
    }

    [Fact]
    public async Task ExecuteAsync_WhenCancelled_StopsGracefully()
    {
        // Arrange
        var service = new KafkaListenerService(
            _mockTopicResolver.Object, 
            _mockTopicConfig.Object, 
            _mockKafkaSettings.Object, 
            _mockLogger.Object);
        
        var cts = new CancellationTokenSource();
        cts.Cancel(); // Cancel immediately

        // Act
        await service.StartAsync(cts.Token);

        // Assert
        _mockTopicResolver.Verify(x => x.ResolveHandler(It.IsAny<ConsumeResult<string, byte[]>>()), Times.Never);
    }

    [Fact]
    public void Constructor_WithValidDependencies_InitializesSuccessfully()
    {
        // Arrange
        var topicResolver = new Mock<ITopicResolver>().Object;
        var topicConfig = Options.Create(_topicConfig);
        var kafkaSettings = Options.Create(_kafkaSettings);
        var logger = new Mock<ILogger<KafkaListenerService>>().Object;

        // Act
        var service = new KafkaListenerService(
            topicResolver,
            topicConfig,
            kafkaSettings,
            logger);

        // Assert
        Assert.NotNull(service);
    }

    [Fact]
    public void Constructor_WithNullTopicResolver_ThrowsArgumentNullException()
    {
        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => 
            new KafkaListenerService(null, _mockTopicConfig.Object, _mockKafkaSettings.Object, _mockLogger.Object));
    }

    [Fact]
    public void Constructor_WithNullTopicConfig_ThrowsArgumentNullException()
    {
        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => 
            new KafkaListenerService(_mockTopicResolver.Object, null, _mockKafkaSettings.Object, _mockLogger.Object));
    }

    [Fact]
    public void Constructor_WithNullKafkaSettings_ThrowsArgumentNullException()
    {
        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => 
            new KafkaListenerService(_mockTopicResolver.Object, _mockTopicConfig.Object, null, _mockLogger.Object));
    }

    [Fact]
    public void Constructor_WithNullLogger_ThrowsArgumentNullException()
    {
        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => 
            new KafkaListenerService(_mockTopicResolver.Object, _mockTopicConfig.Object, _mockKafkaSettings.Object, null));
    }
} 