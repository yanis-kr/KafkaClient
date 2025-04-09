using KafkaConsumer.Common.Configuration;
using KafkaConsumer.Common.Contracts;
using KafkaConsumer.Extensions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using System.Collections.Generic;
using System.Linq;
using Xunit;

namespace KafkaConsumer.Tests.Extensions;

public class ServiceCollectionExtensionsTests
{
    [Fact]
    public void RegisterEventHandlers_RegistersAllEventHandlerImplementations()
    {
        // Arrange
        var services = new ServiceCollection();

        // Act
        services.RegisterEventHandlers();
        var serviceProvider = services.BuildServiceProvider();

        // Assert
        var handlers = serviceProvider.GetServices<IEventHandler>().ToList();
        Assert.NotEmpty(handlers);
        Assert.Contains(handlers, h => h.Name == "UpdateOrder");
        Assert.Contains(handlers, h => h.Name == "UpdateUser");
    }

    [Fact]
    public void AddOptionsConfigurations_RegistersConfigurationOptions()
    {
        // Arrange
        var services = new ServiceCollection();
        var configurationValues = new Dictionary<string, string>
        {
            { "TopicConfigurations:CurrentSet", "Development" },
            { "TopicConfigurations:Sets:Development:0:TopicName", "topic_1" },
            { "TopicConfigurations:Sets:Development:0:EventType", "user.created" },
            { "TopicConfigurations:Sets:Development:0:HandlerName", "UpdateUser" },
            { "Kafka:BootstrapServers", "localhost:9092" },
            { "Kafka:GroupId", "test-group" }
        };

        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(configurationValues)
            .Build();

        // Act
        services.AddOptionsConfigurations(configuration);
        var serviceProvider = services.BuildServiceProvider();

        // Assert
        var topicConfig = serviceProvider.GetService<Microsoft.Extensions.Options.IOptions<TopicConfigurations>>();
        var kafkaSettings = serviceProvider.GetService<Microsoft.Extensions.Options.IOptions<KafkaSettings>>();

        Assert.NotNull(topicConfig);
        Assert.NotNull(kafkaSettings);
        Assert.Equal("Development", topicConfig.Value.CurrentSet);
        Assert.Equal("localhost:9092", kafkaSettings.Value.BootstrapServers);
    }
} 