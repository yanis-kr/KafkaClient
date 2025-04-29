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

public class KafkaListenerService : BackgroundService, IKafkaHealthCheck
{
    private readonly ITopicResolver _topicResolver;
    private readonly IOptions<TopicSettings> _topicConfig;
    private readonly IOptions<KafkaSettings> _kafkaSettings;
    private readonly ILogger<KafkaListenerService> _logger;
    private IConsumer<string, byte[]> _consumer;
    private bool _isHealthy;

    public bool IsHealthy => _isHealthy;

    public KafkaListenerService(
        ITopicResolver topicResolver,
        IOptions<TopicSettings> topicConfig,
        IOptions<KafkaSettings> kafkaSettings,
        ILogger<KafkaListenerService> logger)
    {
        _topicResolver = topicResolver ?? throw new ArgumentNullException(nameof(topicResolver));
        _topicConfig = topicConfig ?? throw new ArgumentNullException(nameof(topicConfig));
        _kafkaSettings = kafkaSettings ?? throw new ArgumentNullException(nameof(kafkaSettings));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _isHealthy = false;
    }

    public void SetHealthy(bool isHealthy)
    {
        _isHealthy = isHealthy;
        _logger.LogInformation("Kafka health status changed to: {IsHealthy}", isHealthy);
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        // Build consumer configuration from settings
        ConsumerConfig consumerConfig = BuildKafkaConfig();

        try
        {
            // Create the Kafka consumer (ignoring message key, using byte[] for value)
            _consumer = new ConsumerBuilder<string, byte[]>(consumerConfig)
                .SetErrorHandler((_, e) =>
                {
                    _logger.LogError("Kafka Error: {Reason}", e.Reason);
                    SetHealthy(false);
                })
                .Build();

            // Determine topics to subscribe (from active config set)
            string currentSet = _topicConfig.Value.CurrentSet;
            var topics = _topicConfig.Value.Sets[currentSet].Select(t => t.TopicName).Distinct();
            _consumer.Subscribe(topics);

            _logger.LogInformation("Subscribed to topics: {Topics} (set: {CurrentSet})",
                string.Join(", ", topics), currentSet);

            SetHealthy(true);

            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    ConsumeResult<string, byte[]> consumeResult = _consumer.Consume(stoppingToken);
                    if (consumeResult?.Message == null)
                    {
                        continue;
                    }

                    var handlers = _topicResolver.ResolveHandlers(consumeResult);
                    if (!handlers.Any())
                    {
                        _logger.LogWarning("No handlers found for message from topic {Topic}", consumeResult.Topic);
                        continue;
                    }

                    foreach (var handler in handlers)
                    {
                        try
                        {
                            await handler.ProcessEvent(consumeResult);
                        }
                        catch (Exception ex)
                        {
                            _logger.LogError(ex, "Error processing message with handler {HandlerType}", handler.GetType().Name);
                            SetHealthy(false);
                        }
                    }

                    // Commit the offset if auto-commit is disabled
                    _consumer.Commit(consumeResult);
                }
                catch (ConsumeException ex)
                {
                    _logger.LogError(ex, "Consume error: {Reason}", ex.Error.Reason);
                    SetHealthy(false);
                }
                catch (OperationCanceledException)
                {
                    // Normal shutdown
                    _logger.LogInformation("Service shutdown requested");
                    break;
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error processing message");
                    SetHealthy(false);
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Critical error in Kafka consumer");
            SetHealthy(false);
            throw;
        }
        finally
        {
            _consumer?.Close();
        }
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

