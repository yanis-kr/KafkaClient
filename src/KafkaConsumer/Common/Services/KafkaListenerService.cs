using CloudNative.CloudEvents;
using CloudNative.CloudEvents.Kafka;
using CloudNative.CloudEvents.SystemTextJson;
using Confluent.Kafka;
using KafkaConsumer.Common.Configuration;
using KafkaConsumer.Common.Contracts;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace KafkaConsumer.Common.Services;

public class KafkaListenerService : BackgroundService
{
    private readonly IEventDispatcher _dispatcher;
    private readonly IOptions<TopicConfigurations> _topicConfig;
    private readonly IOptions<KafkaSettings> _kafkaSettings;

    public KafkaListenerService(IEventDispatcher dispatcher,
                                IOptions<TopicConfigurations> topicConfig,
                                IOptions<KafkaSettings> kafkaSettings)
    {
        _dispatcher = dispatcher;
        _topicConfig = topicConfig;
        _kafkaSettings = kafkaSettings;
    }

    protected override Task ExecuteAsync(CancellationToken stoppingToken)
    {
        // Build consumer configuration from settings
        ConsumerConfig consumerConfig = BuildKafkaConfig();

        // Create the Kafka consumer (ignoring message key, using byte[] for value)
        using var consumer = new ConsumerBuilder<string, byte[]>(consumerConfig)
            .SetErrorHandler((_, e) =>
                Console.Error.WriteLine($"Kafka Error: {e.Reason}"))
            .Build();

        // Determine topics to subscribe (from active config set)
        string currentSet = _topicConfig.Value.CurrentSet;
        var topics = _topicConfig.Value.Sets[currentSet].Select(t => t.TopicName).Distinct();
        consumer.Subscribe(topics);

        Console.WriteLine($"KafkaListenerService subscribed to topics: {string.Join(", ", topics)} (set: {currentSet})");

        try
        {
            var formatter = new JsonEventFormatter(); // CloudEvents JSON formatter
            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    // Poll for the next message (blocking until one is received or cancellation)
                    var consumeResult = consumer.Consume(stoppingToken);
                    if (consumeResult == null)
                    {
                        // in case of timeout
                        continue;
                    }

                    // Convert Kafka message to CloudEvent
                    CloudEvent cloudEvent = consumeResult.Message.ToCloudEvent(formatter);
                    // (The CloudEvents Kafka extension will parse headers or JSON
                    // to construct the CloudEvent object

                    // Dispatch the event to the appropriate handler
                    bool handled = _dispatcher.DispatchEvent(cloudEvent);
                    if (!handled)
                    {
                        // If not handled (no handler or handler returned false), decide how to handle it.
                        // For now, just log; could move to a dead-letter queue or retry mechanism if needed.
                        Console.WriteLine($"Event (Type={cloudEvent.Type}, Id={cloudEvent.Id}) was not handled.");
                    }

                    // Commit offset if processing succeeded (if using manual commit)
                    // consumer.Commit(consumeResult);
                }
                catch (ConsumeException ex)
                {
                    // Log and continue on consume error (e.g., server issues, deserialization issues)
                    Console.Error.WriteLine($"Consume error: {ex.Error.Reason}");
                }
            }
        }
        catch (OperationCanceledException)
        {
            // Triggered when stoppingToken is cancelled – normal shutdown
        }
        finally
        {
            // Ensure the consumer leaves the group cleanly on shutdown
            consumer.Close();
        }

        return Task.CompletedTask;
    }

    private ConsumerConfig BuildKafkaConfig()
    {
        var kafkaConfig = _kafkaSettings.Value;
        var consumerConfig = new ConsumerConfig
        {
            BootstrapServers = kafkaConfig.BootstrapServers,
            GroupId = kafkaConfig.GroupId,
            // Set auto offset reset behavior
            AutoOffsetReset = kafkaConfig.AutoOffsetReset?.ToLower() == "earliest"
                                ? AutoOffsetReset.Earliest
                                : AutoOffsetReset.Latest,
            EnableAutoCommit = true,  // auto-commit offsets after each consume (configurable)

            // Security settings
            SecurityProtocol = kafkaConfig.SecurityProtocol == "SaslSsl"
                ? SecurityProtocol.SaslSsl
                : SecurityProtocol.Plaintext,
            SaslMechanism = kafkaConfig.SaslMechanisms == "Plain"
                ? SaslMechanism.Plain
                : SaslMechanism.ScramSha256,
            SaslUsername = kafkaConfig.SaslUsername,
            SaslPassword = kafkaConfig.SaslPassword,

            // Additional settings
            SessionTimeoutMs = kafkaConfig.SessionTimeoutMs,
            ClientId = kafkaConfig.ClientId
        };
        return consumerConfig;
    }
}

