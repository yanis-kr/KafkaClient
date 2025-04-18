using CloudNative.CloudEvents;
using Confluent.Kafka;
using KafkaConsumer.Common.Contracts;
using KafkaConsumer.Common.Extensions;
using Microsoft.Extensions.Logging;
using System;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;

namespace KafkaConsumer.Features.UpdateUser.Handlers;

public class UpdateUserHandler : IEventHandler
{
    private readonly ILogger<UpdateUserHandler> _logger;

    public UpdateUserHandler(ILogger<UpdateUserHandler> logger)
    {
        _logger = logger;
    }

    public string Name => "UpdateUser";

    public Task<bool> ProcessEvent(ConsumeResult<string, byte[]> consumeResult)
    {
        ArgumentNullException.ThrowIfNull(consumeResult, nameof(consumeResult));

        consumeResult.LogMessageContent(_logger, this.Name);

        var messageKey = consumeResult.Message.Key;
        var messageValue = Encoding.UTF8.GetString(consumeResult.Message.Value);
        var headers = string.Join(", ", consumeResult.Message.Headers.Select(h => $"{h.Key}: {Encoding.UTF8.GetString(h.GetValueBytes())}"));

        _logger.LogInformation("[{HandlerName}] Processing event: Topic={Topic}, Key={Key}, Headers=[{Headers}], Value={Value}",
            Name, consumeResult.Topic, messageKey, headers, messageValue);

        return Task.FromResult(true);
    }
}
