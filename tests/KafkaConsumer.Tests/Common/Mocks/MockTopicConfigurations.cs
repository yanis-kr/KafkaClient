using KafkaConsumer.Common.Configuration;

namespace KafkaConsumer.Tests.Common.Mocks;

public class MockTopicConfigurations : TopicConfigurations
{
    public MockTopicConfigurations()
    {
        CurrentSet = "Set1";
        Sets = new Dictionary<string, List<TopicConfigEntry>>
        {
            ["Set1"] = new List<TopicConfigEntry>
            {
                new TopicConfigEntry
                {
                    TopicName = "topic1",
                    EventType = "test.event",
                    HandlerName = "TestHandler"
                }
            }
        };
    }
} 