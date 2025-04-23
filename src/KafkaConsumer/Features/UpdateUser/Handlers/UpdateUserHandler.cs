using Avro.Generic;
using CloudNative.CloudEvents;
using Confluent.Kafka;
using Confluent.SchemaRegistry;
using Confluent.SchemaRegistry.Serdes;
using KafkaConsumer.Common.Contracts;
using Microsoft.Extensions.Logging;
using System;
using System.Threading.Tasks;

namespace KafkaConsumer.Features.UpdateUser.Handlers;

public class UpdateUserHandler : IEventHandler
{
    private readonly ILogger<UpdateUserHandler> _logger;
    private readonly ISchemaRegistryClient _schemaRegistryClient;
    private readonly AvroDeserializer<GenericRecord> _avroDeserializer;

    public UpdateUserHandler(
        ILogger<UpdateUserHandler> logger,
        ISchemaRegistryClient schemaRegistryClient)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _schemaRegistryClient = schemaRegistryClient ?? throw new ArgumentNullException(nameof(schemaRegistryClient));
        _avroDeserializer = new AvroDeserializer<GenericRecord>(_schemaRegistryClient);
    }

    //public string Name => "UpdateUser";

    public async Task<bool> ProcessEvent(ConsumeResult<string, byte[]> consumeResult)
    {
        ArgumentNullException.ThrowIfNull(consumeResult, nameof(consumeResult));

        try
        {
            // Deserialize the message as a GenericRecord
            var genericRecord = await _avroDeserializer.DeserializeAsync(
                consumeResult.Message.Value,
                false,
                new SerializationContext(MessageComponentType.Value, consumeResult.Topic, consumeResult.Message.Headers));

            // Create a CloudEvent from the GenericRecord
            var cloudEvent = new CloudEvent
            {
                //Type = genericRecord.GetValue("type")?.ToString(),
                //Source = new Uri(genericRecord.GetValue("source")?.ToString() ?? "unknown://source"),
                //Id = genericRecord.GetValue("id")?.ToString(),
                //Time = DateTime.Parse(genericRecord.GetValue("time")?.ToString() ?? DateTime.UtcNow.ToString("O")),
                //DataContentType = genericRecord.GetValue("datacontenttype")?.ToString(),
                //DataSchema = genericRecord.GetValue("dataschema")?.ToString() != null 
                //    ? new Uri(genericRecord.GetValue("dataschema").ToString()) 
                //    : null,
                //Subject = genericRecord.GetValue("subject")?.ToString(),
                //Data = genericRecord.GetValue("data")
            };

            // Log the CloudEvent details
            _logger.LogInformation(
                "Processing CloudEvent: Type={Type}, Source={Source}, Id={Id}, Time={Time}",
                cloudEvent.Type,
                cloudEvent.Source,
                cloudEvent.Id,
                cloudEvent.Time);

            // Extract and process the data
            if (cloudEvent.Data is GenericRecord dataRecord)
            {
                // Process the user data from the GenericRecord
                //var userId = dataRecord.GetValue("userId")?.ToString();
                //var userName = dataRecord.GetValue("userName")?.ToString();
                //var email = dataRecord.GetValue("email")?.ToString();
                var userId = dataRecord.GetValue(0)?.ToString();
                var userName = "dataRecord.GetValue(userName)?.ToString();";
                var email = "dataRecord.GetValue(email)?.ToString();";

                _logger.LogInformation(
                    "User data: Id={UserId}, Name={UserName}, Email={Email}",
                    userId,
                    userName,
                    email);

                // TODO: Add your business logic here to process the user data
            }
            else
            {
                _logger.LogWarning("CloudEvent data is not a GenericRecord");
            }

            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing Avro message from topic {Topic}", consumeResult.Topic);
            return false;
        }
    }
}
