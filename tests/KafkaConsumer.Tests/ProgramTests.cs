using KafkaConsumer.Common.Configuration;
using KafkaConsumer.Common.Contracts;
using KafkaConsumer.Common.Extensions;
using KafkaConsumer.Common.Services;
using KafkaConsumer.Tests.Common.Handlers;
using KafkaConsumer.Tests.Common.Mocks;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using Xunit;

namespace KafkaConsumer.Tests;

public class ProgramTests
{
    [Fact]
    public void ServiceConfiguration_RegistersAllRequiredServices()
    {
        // Arrange
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string>
            {
                ["TopicConfigurations:CurrentSet"] = "Set1",
                ["TopicConfigurations:Sets:Set1:0:TopicName"] = "topic1",
                ["TopicConfigurations:Sets:Set1:0:EventType"] = "test.event",
                ["TopicConfigurations:Sets:Set1:0:HandlerName"] = "TestHandler",
                ["Kafka:BootstrapServers"] = "localhost:9092",
                ["Kafka:GroupId"] = "test-group",
                ["Kafka:SecurityProtocol"] = "Plaintext",
                ["Kafka:SaslMechanisms"] = "Plain",
                ["Kafka:SaslUsername"] = "test-user",
                ["Kafka:SaslPassword"] = "test-password"
            })
            .Build();

        var services = new ServiceCollection();
        
        // Register configuration
        services.AddSingleton<IConfiguration>(configuration);
        
        // Register options
        services.AddOptionsConfigurations(configuration);
        
        // Register event handlers
        services.RegisterEventHandlers();
        services.AddSingleton<IEventHandler, TestHandler>();
        
        // Register services
        services.AddSingleton<IEventDispatcher, MockEventDispatcher>();
        services.AddHostedService<KafkaListenerService>();

        // Act
        var serviceProvider = services.BuildServiceProvider();

        // Assert
        // Verify EventDispatcher is registered
        var dispatcher = serviceProvider.GetService<IEventDispatcher>();
        Assert.NotNull(dispatcher);
        Assert.IsType<MockEventDispatcher>(dispatcher);

        // Verify at least one event handler is registered
        var handlers = serviceProvider.GetServices<IEventHandler>();
        Assert.NotEmpty(handlers);

        // Verify configuration is properly bound
        var topicConfig = serviceProvider.GetService<IOptions<TopicConfigurations>>();
        Assert.NotNull(topicConfig);
        Assert.Equal("Set1", topicConfig.Value.CurrentSet);
        Assert.Single(topicConfig.Value.Sets["Set1"]);

        var kafkaSettings = serviceProvider.GetService<IOptions<KafkaSettings>>();
        Assert.NotNull(kafkaSettings);
        Assert.Equal("localhost:9092", kafkaSettings.Value.BootstrapServers);
        Assert.Equal("test-group", kafkaSettings.Value.GroupId);
    }
} 