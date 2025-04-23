using System;
using System.Text.Json.Serialization;

namespace KafkaConsumer.Features.UpdateOrder.Models;

/// <summary>
/// Event model for order updates
/// </summary>
public class UpdateOrderEvent
{
    [JsonPropertyName("orderId")]
    public string OrderId { get; set; }
    
    [JsonPropertyName("orderNumber")]
    public string OrderNumber { get; set; }
    
    [JsonPropertyName("status")]
    public string Status { get; set; }
    
    [JsonPropertyName("amount")]
    public decimal? Amount { get; set; }
    
    [JsonPropertyName("timestamp")]
    public DateTimeOffset Timestamp { get; set; }
} 