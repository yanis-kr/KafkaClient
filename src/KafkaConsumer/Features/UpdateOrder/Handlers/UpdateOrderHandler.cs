using Confluent.Kafka;
using KafkaConsumer.Common.Contracts;
using KafkaConsumer.Features.UpdateOrder.Contracts;
using KafkaConsumer.Features.UpdateOrder.Models;
using Microsoft.Extensions.Logging;
using System;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using System.Collections.Generic;
using KafkaConsumer.Common.Extensions;

namespace KafkaConsumer.Features.UpdateOrder.Handlers;

public class UpdateOrderHandler : IEventHandler
{
    private readonly ILogger<UpdateOrderHandler> _logger;
    private readonly IOrderApi _orderApi;

    public UpdateOrderHandler(
        IOrderApi orderApi,
        ILogger<UpdateOrderHandler> logger)
    {
        _orderApi = orderApi ?? throw new ArgumentNullException(nameof(orderApi));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    //public string Name => "UpdateOrder";

    public async Task<bool> ProcessEvent(ConsumeResult<string, byte[]> consumeResult)
    {
        consumeResult.LogMessageContent(_logger, this.ToString());

        try
        {
            var message = consumeResult.Message;
            var topic = consumeResult.Topic;
            
            if (message.Value == null || message.Value.Length == 0)
            {
                _logger.LogWarning("Received empty message from topic {Topic}", topic);
                return false;
            }

            // Try to detect if the message is JSON or binary
            bool isJson = IsJsonMessage(message.Value);
            if (!isJson)
            {
                _logger.LogWarning("Received non-JSON message, cannot process");
                return false;
            }
            
            string content = Encoding.UTF8.GetString(message.Value);
            
            _logger.LogInformation("Message content: {Content}", content);

            // Deserialize the update order event
            var orderUpdate = JsonSerializer.Deserialize<UpdateOrderEvent>(content);
            
            _logger.LogInformation("Processing order update: OrderId={OrderId}, Status={Status}",
                orderUpdate.OrderId, orderUpdate.Status);
            
            // Call the external API to update the order
            await _orderApi.UpdateOrderAsync(orderUpdate.OrderId, new OrderUpdateRequest
            {
                Status = orderUpdate.Status,
                OrderNumber = orderUpdate.OrderNumber,
                Amount = orderUpdate.Amount
            });
            
            _logger.LogInformation("Successfully updated order {OrderId} with status {Status}", 
                orderUpdate.OrderId, orderUpdate.Status);

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