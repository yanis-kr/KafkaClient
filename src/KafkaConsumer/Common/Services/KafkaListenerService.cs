using CloudNative.CloudEvents;
using CloudNative.CloudEvents.Kafka;
using CloudNative.CloudEvents.SystemTextJson;
using Confluent.Kafka;
using KafkaConsumer.Common.Configuration;
using KafkaConsumer.Common.Contracts;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace KafkaConsumer.Common.Services;

public class KafkaListenerService : BackgroundService
{
    private readonly IEventDispatcher _dispatcher;
    private readonly IOptions<TopicSettings> _topicConfig;
    private readonly IOptions<KafkaSettings> _kafkaSettings;
    private readonly ILogger<KafkaListenerService> _logger;

    public KafkaListenerService(IEventDispatcher dispatcher,
                                IOptions<TopicSettings> topicConfig,
                                IOptions<KafkaSettings> kafkaSettings,
                                ILogger<KafkaListenerService> logger)
    {
        _dispatcher = dispatcher ?? throw new ArgumentNullException(nameof(dispatcher));
        _topicConfig = topicConfig ?? throw new ArgumentNullException(nameof(topicConfig));
        _kafkaSettings = kafkaSettings ?? throw new ArgumentNullException(nameof(kafkaSettings));
        _logger = logger;
    }

    protected override Task ExecuteAsync(CancellationToken stoppingToken)
    {
        // Build consumer configuration from settings
        ConsumerConfig consumerConfig = BuildKafkaConfig();

        // Create the Kafka consumer (ignoring message key, using byte[] for value)
        using var consumer = new ConsumerBuilder<string, byte[]>(consumerConfig)
            .SetErrorHandler((_, e) =>
                _logger.LogError("Kafka Error: {Reason}", e.Reason))
            .Build();

        // Determine topics to subscribe (from active config set)
        string currentSet = _topicConfig.Value.CurrentSet;
        var topics = _topicConfig.Value.Sets[currentSet].Select(t => t.TopicName).Distinct();
        consumer.Subscribe(topics);

        _logger.LogInformation("Subscribed to topics: {Topics} (set: {CurrentSet})", 
            string.Join(", ", topics), currentSet);

        try
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    var consumeResult = consumer.Consume(stoppingToken);
                    var cloudEventFormatter = new JsonEventFormatter();
                    var cloudEvent = consumeResult.Message.ToCloudEvent(cloudEventFormatter);

                    if (!_dispatcher.DispatchEvent(cloudEvent))
                    {
                        _logger.LogWarning("Event (Type={EventType}, Id={EventId}) was not handled.", 
                            cloudEvent.Type, cloudEvent.Id);
                    }
                }
                catch (ConsumeException ex)
                {
                    _logger.LogError(ex, "Consume error: {Reason}", ex.Error.Reason);
                }
            }
        }
        catch (OperationCanceledException)
        {
            // Normal shutdown
            _logger.LogInformation("Service shutdown requested");
        }
        finally
        {
            consumer.Close();
        }

        return Task.CompletedTask;
    }

    private ConsumerConfig BuildKafkaConfig()
    {
        var settings = _kafkaSettings.Value;
        return new ConsumerConfig
        {
            BootstrapServers = settings.BootstrapServers,
            GroupId = settings.GroupId,
            AutoOffsetReset = Enum.Parse<AutoOffsetReset>(settings.AutoOffsetReset),
            SecurityProtocol = Enum.Parse<SecurityProtocol>(settings.SecurityProtocol),
            SaslMechanism = Enum.Parse<SaslMechanism>(settings.SaslMechanisms),
            SaslUsername = settings.SaslUsername,
            SaslPassword = settings.SaslPassword,
            SessionTimeoutMs = settings.SessionTimeoutMs,
            ClientId = settings.ClientId,
            EnableAutoCommit = settings.EnableAutoCommit
        };
    }
}

