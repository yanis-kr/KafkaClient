using Confluent.Kafka;
using System;
using System.Text;
using System.Text.Json;

namespace KafkaConsumer.IntegrationTests.Helpers;

public static class FakeKafkaMessageHelper
{
    /// <summary>
    /// Creates a fake Kafka ConsumeResult with the given JSON payload
    /// </summary>
    public static ConsumeResult<string, byte[]> CreateFakeConsumeResult<T>(
        T payload, 
        string topic = "order-updates", 
        string key = null,
        int partition = 0,
        long offset = 0) where T : class
    {
        // Convert payload to JSON
        var json = JsonSerializer.Serialize(payload);
        var messageValue = Encoding.UTF8.GetBytes(json);
        
        // Create message headers
        var headers = new Headers
        {
            { "content-type", Encoding.UTF8.GetBytes("application/json") },
            { "source", Encoding.UTF8.GetBytes("integration-test") },
            { "timestamp", Encoding.UTF8.GetBytes(DateTimeOffset.UtcNow.ToString("o")) }
        };
        
        // Create the ConsumeResult
        return new ConsumeResult<string, byte[]>
        {
            Message = new Message<string, byte[]>
            {
                Key = key ?? Guid.NewGuid().ToString(),
                Value = messageValue,
                Headers = headers,
                Timestamp = new Timestamp(DateTimeOffset.UtcNow)
            },
            Topic = topic,
            Partition = new Partition(partition),
            Offset = new Offset(offset)
        };
    }
    
    /// <summary>
    /// Creates a fake Kafka ConsumeResult with an invalid (non-JSON) payload
    /// </summary>
    public static ConsumeResult<string, byte[]> CreateInvalidConsumeResult(
        string topic = "order-updates",
        string key = null,
        int partition = 0, 
        long offset = 0)
    {
        return new ConsumeResult<string, byte[]>
        {
            Message = new Message<string, byte[]>
            {
                Key = key ?? Guid.NewGuid().ToString(),
                Value = Encoding.UTF8.GetBytes("This is not valid JSON"),
                Headers = new Headers(),
                Timestamp = new Timestamp(DateTimeOffset.UtcNow)
            },
            Topic = topic,
            Partition = new Partition(partition),
            Offset = new Offset(offset)
        };
    }
    
    /// <summary>
    /// Creates a fake Kafka ConsumeResult with an empty payload
    /// </summary>
    public static ConsumeResult<string, byte[]> CreateEmptyConsumeResult(
        string topic = "order-updates",
        string key = null,
        int partition = 0,
        long offset = 0)
    {
        return new ConsumeResult<string, byte[]>
        {
            Message = new Message<string, byte[]>
            {
                Key = key ?? Guid.NewGuid().ToString(),
                Value = Array.Empty<byte>(),
                Headers = new Headers(),
                Timestamp = new Timestamp(DateTimeOffset.UtcNow)
            },
            Topic = topic,
            Partition = new Partition(partition),
            Offset = new Offset(offset)
        };
    }
} 