using KafkaConsumer.Common.Services;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using System.Threading.Tasks;

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
                    // returns the status of all registered health checks
                    endpoints.MapHealthChecks("/health", new HealthCheckOptions
                    {
                        ResponseWriter = WriteHealthCheckResponse
                    });

                    // liveness probe - no actual health checks are executed
                    // Just confirms that the app is running and able to respond to HTTP requests
                    endpoints.MapHealthChecks("/live", new HealthCheckOptions
                    {
                        Predicate = _ => false,
                        ResponseWriter = WriteHealthCheckResponse
                    });

                    // eadiness probe - executes all registered health checks
                    // Ensures that dependencies (like Kafka) are healthy and app is ready to serve traffic
                    endpoints.MapHealthChecks("/ready", new HealthCheckOptions
                    {
                        Predicate = _ => true,
                        ResponseWriter = WriteHealthCheckResponse
                    });
                });
            });
        });
    }

    private static Task WriteHealthCheckResponse(HttpContext context, HealthReport report)
    {
        context.Response.ContentType = "text/plain";
        return context.Response.WriteAsync(report.Status.ToString());
    }
} 