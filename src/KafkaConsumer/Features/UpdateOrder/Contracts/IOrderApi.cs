using Refit;
using System.Threading.Tasks;

namespace KafkaConsumer.Features.UpdateOrder.Contracts;

/// <summary>
/// API contract for order operations
/// </summary>
public interface IOrderApi
{
    [Put("/api/orders/{orderId}")]
    Task UpdateOrderAsync(string orderId, [Body] OrderUpdateRequest request);
}

/// <summary>
/// Request model for updating an order
/// </summary>
public class OrderUpdateRequest
{
    public string Status { get; set; }
    public string OrderNumber { get; set; }
    public decimal? Amount { get; set; }
    // Add other properties as needed
} 