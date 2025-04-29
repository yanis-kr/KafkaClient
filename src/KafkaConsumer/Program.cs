using KafkaConsumer.Common.Extensions;
using KafkaConsumer.Common.Services;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using System.Text.Json;
using System.Linq;
using System.IO;
using Microsoft.AspNetCore.Http;
using KafkaConsumer.Common.Contracts;

var builder = Host.CreateDefaultBuilder(args)
    .ConfigureAppConfigurationWithUserSecrets()
    .ConfigureLogging()
    .ConfigureWebHostWithHealthChecks();

builder.ConfigureServices((context, services) => services
    .AddOptionsConfigurations(context.Configuration)
    .RegisterEventHandlers()
    .AddOktaAuthentication()
    .AddApiClients(context.Configuration)
    .AddSingleton<ITopicResolver, TopicResolver>()
    .AddSingleton<IKafkaHealthCheck, KafkaHealthMonitorService>()
    .AddHostedService<KafkaListenerService>());

var app = builder.Build();
await app.RunAsync();
