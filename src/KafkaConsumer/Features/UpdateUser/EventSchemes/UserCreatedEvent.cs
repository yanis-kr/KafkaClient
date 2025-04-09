namespace KafkaConsumer.Features.UpdateUser.EventScheme;

class UserCreatedEvent
{
    public string AccountId { get; set; }
    public string CustomerName { get; set; }
    public string Date { get; set; }
}
