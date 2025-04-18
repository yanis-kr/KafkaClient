using KafkaConsumer.Common.Extensions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Serilog;

namespace KafkaConsumer.Tests.Fixtures;

public class HostFixture : IDisposable
{
    public IHost TestHost { get; }

    public HostFixture()
    {
        TestHost = Host.CreateDefaultBuilder()
            .ConfigureLogging(logging =>
            {
                logging.ClearProviders();
                logging.AddSerilog();
            })
            .ConfigureServices((context, services) =>
            {
                services.AddOptionsConfigurations(new ConfigurationBuilder().Build());
                services.RegisterEventHandlers();
            })
            .Build();
    }

    public void Dispose()
    {
        if (TestHost is IAsyncDisposable asyncDisposable)
        {
            asyncDisposable.DisposeAsync().AsTask().Wait();
        }
        else
        {
            TestHost.Dispose();
        }
    }
}