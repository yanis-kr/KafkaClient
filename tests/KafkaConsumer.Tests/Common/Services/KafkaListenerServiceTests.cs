using CloudNative.CloudEvents;
using KafkaConsumer.Common.Configuration;
using KafkaConsumer.Common.Contracts;
using KafkaConsumer.Common.Services;
using KafkaConsumer.Tests.Common.Mocks;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;

namespace KafkaConsumer.Tests.Common.Services;

public class KafkaListenerServiceTests
{
    private readonly Mock<IEventDispatcher> _mockDispatcher;
    private readonly Mock<IOptions<TopicSettings>> _mockTopicConfig;
    private readonly Mock<IOptions<KafkaSettings>> _mockKafkaSettings;
    private readonly Mock<ILogger<KafkaListenerService>> _mockLogger;
    private readonly TopicSettings _topicConfig;
    private readonly KafkaSettings _kafkaSettings;

    public KafkaListenerServiceTests()
    {
        _mockDispatcher = new Mock<IEventDispatcher>();
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
        var service = new KafkaListenerService(_mockDispatcher.Object, _mockTopicConfig.Object, _mockKafkaSettings.Object, _mockLogger.Object);
        var cts = new CancellationTokenSource();
        cts.Cancel(); // Cancel immediately

        // Act
        await service.StartAsync(cts.Token);

        // Assert
        _mockDispatcher.Verify(x => x.DispatchEvent(It.IsAny<CloudEvent>()), Times.Never);
    }

    [Fact]
    public void Constructor_WithValidDependencies_InitializesSuccessfully()
    {
        // Arrange
        var dispatcher = new MockEventDispatcher();
        var topicConfig = new MockTopicConfigurations();
        var kafkaSettings = new MockKafkaSettings();
        var logger = new Mock<ILogger<KafkaListenerService>>();

        // Act
        var service = new KafkaListenerService(
            dispatcher,
            Options.Create(topicConfig),
            Options.Create(kafkaSettings),
            logger.Object);

        // Assert
        Assert.NotNull(service);
    }

    [Fact]
    public void Constructor_WithNullDispatcher_ThrowsArgumentNullException()
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
            new KafkaListenerService(_mockDispatcher.Object, null, _mockKafkaSettings.Object, _mockLogger.Object));
    }

    [Fact]
    public void Constructor_WithNullKafkaSettings_ThrowsArgumentNullException()
    {
        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => 
            new KafkaListenerService(_mockDispatcher.Object, _mockTopicConfig.Object, null, _mockLogger.Object));
    }
} 