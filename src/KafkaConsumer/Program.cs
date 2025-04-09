using KafkaConsumer.Common.Contracts;
using KafkaConsumer.Common.Services;
using KafkaConsumer.Extensions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using System.IO;

var builder = Host.CreateDefaultBuilder(args)
    .ConfigureAppConfigurationWithUserSecrets()
    .ConfigureServices((context, services) => services
        .AddOptionsConfigurations(context.Configuration)
        .RegisterEventHandlers()
        .AddSingleton<IEventDispatcher, EventDispatcher>()
        .AddHostedService<KafkaListenerService>());

var app = builder.Build();
await app.RunAsync();
