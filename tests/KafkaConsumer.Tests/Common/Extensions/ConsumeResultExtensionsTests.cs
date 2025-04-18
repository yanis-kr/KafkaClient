using Confluent.Kafka;
using KafkaConsumer.Common.Extensions;
using Microsoft.Extensions.Logging.Abstractions;
using System.Text;

namespace KafkaConsumer.Tests.Common.Extensions;

public class ConsumeResultExtensionsTests
{
    [Fact]
    public void ExtractEventType_FromHeaders_ReturnsType()
    {
        var headers = new Headers { new Header("type", Encoding.UTF8.GetBytes("headerType")) };
        var result = CreateConsumeResult(Encoding.UTF8.GetBytes("{}"), headers);

        var eventType = result.ExtractEventType(NullLogger<string>.Instance);

        Assert.Equal("headerType", eventType);
    }

    [Fact]
    public void ExtractEventType_FromCloudEvent_ReturnsType()
    {
        var cloudEventJson = "{\"id\":\"123\",\"source\":\"source\",\"type\":\"cloudEventType\"}";
        var result = CreateConsumeResult(Encoding.UTF8.GetBytes(cloudEventJson));

        var eventType = result.ExtractEventType(NullLogger<string>.Instance);

        Assert.Equal("cloudEventType", eventType);
    }

    [Fact]
    public void ExtractEventType_FromJson_ReturnsType()
    {
        var json = "{\"type\":\"jsonType\"}";
        var result = CreateConsumeResult(Encoding.UTF8.GetBytes(json));

        var eventType = result.ExtractEventType(NullLogger<string>.Instance);

        Assert.Equal("jsonType", eventType);
    }

    [Fact]
    public void ExtractEventType_NoTypeFound_ReturnsNull()
    {
        var json = "{\"noType\":\"data\"}";
        var result = CreateConsumeResult(Encoding.UTF8.GetBytes(json));

        var eventType = result.ExtractEventType(NullLogger<string>.Instance);

        Assert.Null(eventType);
    }

    private ConsumeResult<string, byte[]> CreateConsumeResult(byte[] value, Headers? headers = null)
    {
        return new ConsumeResult<string, byte[]>
        {
            Message = new Message<string, byte[]>
            {
                Value = value,
                Headers = headers ?? new Headers()
            }
        };
    }
}
