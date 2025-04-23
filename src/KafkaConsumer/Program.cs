using KafkaConsumer.Common.Extensions;
using KafkaConsumer.Common.Services;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

var builder = Host.CreateDefaultBuilder(args)
    .ConfigureAppConfigurationWithUserSecrets()
    .ConfigureLogging()
    .ConfigureServices((context, services) => services
        .AddOptionsConfigurations(context.Configuration)
        .RegisterEventHandlers()
        .AddOktaAuthentication()
        .AddApiClients(context.Configuration)
        .AddSingleton<ITopicResolver, TopicResolver>()
        .AddHostedService<KafkaListenerService>());

var app = builder.Build();
await app.RunAsync();
