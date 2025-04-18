using Confluent.Kafka;
using KafkaConsumer.Common.Contracts;
using Microsoft.Extensions.Logging;
using System;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using System.Collections.Generic;

namespace KafkaConsumer.Features.UpdateOrder.Handlers;

public class UpdateOrderHandler : IEventHandler
{
    private readonly ILogger<UpdateOrderHandler> _logger;

    public UpdateOrderHandler(ILogger<UpdateOrderHandler> logger)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public string Name => "UpdateOrder";

    public async Task<bool> ProcessEvent(ConsumeResult<string, byte[]> consumeResult)
    {
        try
        {
            var message = consumeResult.Message;
            var topic = consumeResult.Topic;
            var partition = consumeResult.Partition;
            var offset = consumeResult.Offset;

            // Log the raw message for debugging
            var headerStrings = new List<string>();
            foreach (var header in message.Headers)
            {
                headerStrings.Add($"{header.Key}={Encoding.UTF8.GetString(header.GetValueBytes())}");
            }

            _logger.LogInformation(
                "Received message from topic {Topic}, partition {Partition}, offset {Offset}. " +
                "Headers: {Headers}, Value length: {ValueLength} bytes",
                topic, partition, offset,
                string.Join(", ", headerStrings),
                message.Value?.Length ?? 0);

            if (message.Value == null || message.Value.Length == 0)
            {
                _logger.LogWarning("Received empty message from topic {Topic}", topic);
                return false;
            }

            // Try to detect if the message is JSON or binary
            bool isJson = IsJsonMessage(message.Value);
            string content = isJson 
                ? Encoding.UTF8.GetString(message.Value)
                : Convert.ToBase64String(message.Value);

            _logger.LogInformation(
                "Message content ({Format}): {Content}",
                isJson ? "JSON" : "Binary",
                content);

            if (isJson)
            {
                // Handle JSON message
                var orderUpdate = JsonSerializer.Deserialize<OrderUpdate>(content);
                _logger.LogInformation("Processing order update: OrderId={OrderId}, Status={Status}",
                    orderUpdate.OrderId, orderUpdate.Status);
                // Process the order update...
            }
            else
            {
                // Handle binary message
                _logger.LogInformation("Processing binary message of length {Length} bytes", message.Value.Length);
                // Process the binary message...
            }

            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing order update message");
            return false;
        }
    }

    private static bool IsJsonMessage(byte[] data)
    {
        if (data == null || data.Length == 0)
            return false;

        try
        {
            // Check if the message starts with '{' or '['
            char firstChar = (char)data[0];
            if (firstChar != '{' && firstChar != '[')
                return false;

            // Try to parse as JSON to validate
            var jsonString = Encoding.UTF8.GetString(data);
            JsonDocument.Parse(jsonString);
            return true;
        }
        catch
        {
            return false;
        }
    }
}

public class OrderUpdate
{
    public string OrderId { get; set; }
    public string Status { get; set; }
} 