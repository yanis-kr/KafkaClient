using KafkaConsumer.Common.Contracts;
using KafkaConsumer.Common.Services;
using KafkaConsumer.Extensions;
using KafkaConsumer.Common.Logging;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

var builder = Host.CreateDefaultBuilder(args)
    .ConfigureAppConfigurationWithUserSecrets()
    .ConfigureLogging()
    .ConfigureServices((context, services) => services
        .AddOptionsConfigurations(context.Configuration)
        .RegisterEventHandlers()
        .AddSingleton<IEventDispatcher, EventDispatcher>()
        .AddHostedService<KafkaListenerService>());

var app = builder.Build();
await app.RunAsync();
