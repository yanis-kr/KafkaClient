using KafkaConsumer.Common.Configuration;

namespace KafkaConsumer.Tests.Common.Mocks;

public class MockTopicConfigurations : TopicSettings
{
    public MockTopicConfigurations()
    {
        CurrentSet = "Set1";
        Sets = new Dictionary<string, List<TopicSubscription>>
        {
            ["Set1"] = new List<TopicSubscription>
            {
                new TopicSubscription
                {
                    TopicName = "topic1",
                    EventTypes = new[] { "test.event" },
                    HandlerNames = ["TestHandler"]
                }
            }
        };
    }
}