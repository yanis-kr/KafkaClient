using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Hosting;
using System.Text.Json;
using System.Linq;
using Microsoft.AspNetCore.Http;
using KafkaConsumer.Common.Services;

namespace KafkaConsumer.Common.Extensions;

public static class WebHostExtensions
{
    public static IHostBuilder ConfigureWebHostWithHealthChecks(this IHostBuilder builder)
    {
        return builder.ConfigureWebHostDefaults(webBuilder =>
        {
            webBuilder.ConfigureServices((context, services) =>
            {
                services.AddHealthChecks()
                    .AddCheck<KafkaHealthCheck>("kafka_health");
            });

            webBuilder.Configure(app =>
            {
                app.UseRouting();
                app.UseEndpoints(endpoints =>
                {
                    endpoints.MapHealthChecks("/health", new HealthCheckOptions
                    {
                        ResponseWriter = async (context, report) =>
                        {
                            context.Response.ContentType = "application/json";
                            var result = JsonSerializer.Serialize(new
                            {
                                status = report.Status.ToString(),
                                checks = report.Entries.Select(e => new
                                {
                                    name = e.Key,
                                    status = e.Value.Status.ToString(),
                                    description = e.Value.Description
                                })
                            });
                            await context.Response.WriteAsync(result);
                        }
                    });

                    endpoints.MapHealthChecks("/live", new HealthCheckOptions
                    {
                        Predicate = _ => false
                    });

                    endpoints.MapHealthChecks("/ready", new HealthCheckOptions
                    {
                        Predicate = _ => true
                    });
                });
            });
        });
    }
} 