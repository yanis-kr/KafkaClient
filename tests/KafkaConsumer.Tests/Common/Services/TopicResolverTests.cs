using Confluent.Kafka;
using KafkaConsumer.Common.Configuration;
using KafkaConsumer.Common.Contracts;
using KafkaConsumer.Common.Services;
using KafkaConsumer.Features.UpdateOrder.Handlers;
using KafkaConsumer.Features.UpdateUser.Handlers;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using System;
using System.Collections.Generic;
using System.Linq;
using Xunit;

namespace KafkaConsumer.Tests.Common.Services;

public class TopicResolverTests
{
    private readonly Mock<IOptions<TopicSettings>> _mockTopicConfig;
    private readonly Mock<ILogger<TopicResolver>> _mockLogger;
    private readonly Mock<IServiceProvider> _mockServiceProvider;
    private readonly TopicResolver _resolver;

    public TopicResolverTests()
    {
        _mockTopicConfig = new Mock<IOptions<TopicSettings>>();
        _mockLogger = new Mock<ILogger<TopicResolver>>();
        _mockServiceProvider = new Mock<IServiceProvider>();

        var topicSettings = new TopicSettings
        {
            CurrentSet = "TestSet",
            Sets = new Dictionary<string, List<TopicSubscription>>
            {
                ["TestSet"] = new List<TopicSubscription>
                {
                    new() 
                    { 
                        TopicName = "test-topic", 
                        EventTypes = new[] { "test.event" }, 
                        HandlerNames = new[] 
                        { 
                            "KafkaConsumer.Features.UpdateOrder.Handlers.UpdateOrderHandler",
                            "KafkaConsumer.Features.UpdateUser.Handlers.UpdateUserHandler"
                        } 
                    },
                    new() 
                    { 
                        TopicName = "wildcard-topic", 
                        EventTypes = new[] { "*" }, 
                        HandlerNames = new[] 
                        { 
                            "KafkaConsumer.Features.UpdateUser.Handlers.UpdateUserHandler" 
                        } 
                    },
                    new() 
                    { 
                        TopicName = "multi-event-topic", 
                        EventTypes = new[] { "event.one", "event.two" }, 
                        HandlerNames = new[] 
                        { 
                            "KafkaConsumer.Features.UpdateOrder.Handlers.UpdateOrderHandler" 
                        } 
                    }
                }
            }
        };

        _mockTopicConfig.Setup(x => x.Value).Returns(topicSettings);
        _resolver = new TopicResolver(_mockTopicConfig.Object, _mockLogger.Object, _mockServiceProvider.Object);
    }

    [Fact]
    public void ResolveHandlers_WithExactMatch_ReturnsHandlers()
    {
        // Arrange
        var mockOrderHandler = new Mock<IEventHandler>();
        var mockUserHandler = new Mock<IEventHandler>();
        
        _mockServiceProvider.Setup(x => x.GetService(typeof(UpdateOrderHandler)))
            .Returns(mockOrderHandler.Object);
        _mockServiceProvider.Setup(x => x.GetService(typeof(UpdateUserHandler)))
            .Returns(mockUserHandler.Object);

        var consumeResult = CreateConsumeResult("test-topic", "test.event");

        // Act
        var results = _resolver.ResolveHandlers(consumeResult).ToList();

        // Assert
        Assert.Equal(2, results.Count);
        Assert.Contains(mockOrderHandler.Object, results);
        Assert.Contains(mockUserHandler.Object, results);
    }

    [Fact]
    public void ResolveHandlers_WithWildcardMatch_ReturnsHandlers()
    {
        // Arrange
        var mockHandler = new Mock<IEventHandler>();
        _mockServiceProvider.Setup(x => x.GetService(typeof(UpdateUserHandler)))
            .Returns(mockHandler.Object);

        var consumeResult = CreateConsumeResult("wildcard-topic", "test.something");

        // Act
        var results = _resolver.ResolveHandlers(consumeResult).ToList();

        // Assert
        Assert.Single(results);
        Assert.Same(mockHandler.Object, results[0]);
    }

    [Fact]
    public void ResolveHandlers_WithMultipleEventTypes_ReturnsCorrectHandlers()
    {
        // Arrange
        var mockHandler = new Mock<IEventHandler>();
        _mockServiceProvider.Setup(x => x.GetService(typeof(UpdateOrderHandler)))
            .Returns(mockHandler.Object);

        // Test with first event type
        var consumeResult1 = CreateConsumeResult("multi-event-topic", "event.one");
        var results1 = _resolver.ResolveHandlers(consumeResult1).ToList();
        Assert.Single(results1);
        Assert.Same(mockHandler.Object, results1[0]);

        // Test with second event type
        var consumeResult2 = CreateConsumeResult("multi-event-topic", "event.two");
        var results2 = _resolver.ResolveHandlers(consumeResult2).ToList();
        Assert.Single(results2);
        Assert.Same(mockHandler.Object, results2[0]);
    }

    [Fact]
    public void ResolveHandlers_WithNoMatch_ReturnsEmptyCollection()
    {
        // Arrange
        var consumeResult = CreateConsumeResult("unknown-topic", "unknown.event");

        // Act
        var results = _resolver.ResolveHandlers(consumeResult).ToList();

        // Assert
        Assert.Empty(results);
    }

    [Fact]
    public void Constructor_WithEmptyHandlerNames_ThrowsException()
    {
        // Arrange
        var topicSettings = new TopicSettings
        {
            CurrentSet = "TestSet",
            Sets = new Dictionary<string, List<TopicSubscription>>
            {
                ["TestSet"] = new List<TopicSubscription>
                {
                    new() 
                    { 
                        TopicName = "test-topic", 
                        EventTypes = new[] { "test.event" }, 
                        HandlerNames = new List<string>() // Empty list
                    }
                }
            }
        };

        _mockTopicConfig.Setup(x => x.Value).Returns(topicSettings);

        // Act & Assert
        var exception = Assert.Throws<InvalidOperationException>(() => 
            new TopicResolver(_mockTopicConfig.Object, _mockLogger.Object, _mockServiceProvider.Object));
        
        Assert.Contains("must have at least one handler defined", exception.Message);
    }

    [Fact]
    public void Constructor_WithNullHandlerNames_ThrowsException()
    {
        // Arrange
        var topicSettings = new TopicSettings
        {
            CurrentSet = "TestSet",
            Sets = new Dictionary<string, List<TopicSubscription>>
            {
                ["TestSet"] = new List<TopicSubscription>
                {
                    new() 
                    { 
                        TopicName = "test-topic", 
                        EventTypes = new[] { "test.event" }, 
                        HandlerNames = null // Null list
                    }
                }
            }
        };

        _mockTopicConfig.Setup(x => x.Value).Returns(topicSettings);

        // Act & Assert
        var exception = Assert.Throws<InvalidOperationException>(() => 
            new TopicResolver(_mockTopicConfig.Object, _mockLogger.Object, _mockServiceProvider.Object));
        
        Assert.Contains("must have at least one handler defined", exception.Message);
    }

    [Fact]
    public void Constructor_WithEmptyEventTypes_ThrowsException()
    {
        // Arrange
        var topicSettings = new TopicSettings
        {
            CurrentSet = "TestSet",
            Sets = new Dictionary<string, List<TopicSubscription>>
            {
                ["TestSet"] = new List<TopicSubscription>
                {
                    new() 
                    { 
                        TopicName = "test-topic", 
                        EventTypes = new List<string>(), // Empty list
                        HandlerNames = new[] { "KafkaConsumer.Features.UpdateOrder.Handlers.UpdateOrderHandler" }
                    }
                }
            }
        };

        _mockTopicConfig.Setup(x => x.Value).Returns(topicSettings);

        // Act & Assert
        var exception = Assert.Throws<InvalidOperationException>(() => 
            new TopicResolver(_mockTopicConfig.Object, _mockLogger.Object, _mockServiceProvider.Object));
        
        Assert.Contains("must have at least one event type defined", exception.Message);
    }

    [Fact]
    public void Constructor_WithNullEventTypes_ThrowsException()
    {
        // Arrange
        var topicSettings = new TopicSettings
        {
            CurrentSet = "TestSet",
            Sets = new Dictionary<string, List<TopicSubscription>>
            {
                ["TestSet"] = new List<TopicSubscription>
                {
                    new() 
                    { 
                        TopicName = "test-topic", 
                        EventTypes = null, // Null list
                        HandlerNames = new[] { "KafkaConsumer.Features.UpdateOrder.Handlers.UpdateOrderHandler" }
                    }
                }
            }
        };

        _mockTopicConfig.Setup(x => x.Value).Returns(topicSettings);

        // Act & Assert
        var exception = Assert.Throws<InvalidOperationException>(() => 
            new TopicResolver(_mockTopicConfig.Object, _mockLogger.Object, _mockServiceProvider.Object));
        
        Assert.Contains("must have at least one event type defined", exception.Message);
    }

    [Fact]
    public void Constructor_WithNonExistentHandlerType_ThrowsException()
    {
        // Arrange
        var topicSettings = new TopicSettings
        {
            CurrentSet = "TestSet",
            Sets = new Dictionary<string, List<TopicSubscription>>
            {
                ["TestSet"] = new List<TopicSubscription>
                {
                    new() 
                    { 
                        TopicName = "test-topic", 
                        EventTypes = new[] { "test.event" }, 
                        HandlerNames = new[] { "NonExistentHandlerType" }
                    }
                }
            }
        };

        _mockTopicConfig.Setup(x => x.Value).Returns(topicSettings);

        // Act & Assert
        var exception = Assert.Throws<InvalidOperationException>(() => 
            new TopicResolver(_mockTopicConfig.Object, _mockLogger.Object, _mockServiceProvider.Object));
        
        Assert.Contains("No valid handlers found", exception.Message);
    }

    [Fact]
    public void Constructor_WithHandlerNotImplementingIEventHandler_ThrowsException()
    {
        // Arrange
        var topicSettings = new TopicSettings
        {
            CurrentSet = "TestSet",
            Sets = new Dictionary<string, List<TopicSubscription>>
            {
                ["TestSet"] = new List<TopicSubscription>
                {
                    new() 
                    { 
                        TopicName = "test-topic", 
                        EventTypes = new[] { "test.event" }, 
                        HandlerNames = new[] { typeof(string).AssemblyQualifiedName } // String doesn't implement IEventHandler
                    }
                }
            }
        };

        _mockTopicConfig.Setup(x => x.Value).Returns(topicSettings);

        // Act & Assert
        var exception = Assert.Throws<InvalidOperationException>(() => 
            new TopicResolver(_mockTopicConfig.Object, _mockLogger.Object, _mockServiceProvider.Object));
        
        Assert.Contains("No valid handlers found", exception.Message);
    }

    private static ConsumeResult<string, byte[]> CreateConsumeResult(string topic, string eventType)
    {
        var headers = new Headers();
        headers.Add("type", System.Text.Encoding.UTF8.GetBytes(eventType));

        return new ConsumeResult<string, byte[]>
        {
            Topic = topic,
            Message = new Message<string, byte[]>
            {
                Headers = headers,
                Value = System.Text.Encoding.UTF8.GetBytes("{\"key\":\"value\"}")
            },
        };
    }
}