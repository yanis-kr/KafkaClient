using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using System.IO;

namespace KafkaConsumer.Extensions;

public static class HostBuilderExtensions
{
    public static IHostBuilder ConfigureAppConfigurationWithUserSecrets(this IHostBuilder builder)
    {
        return builder.ConfigureAppConfiguration((hostContext, config) =>
        {
            var env = hostContext.HostingEnvironment;
            config.SetBasePath(Directory.GetCurrentDirectory())
                  .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
                  .AddJsonFile($"appsettings.{env.EnvironmentName}.json", optional: true, reloadOnChange: true)
                  .AddUserSecrets<Program>()
                  .AddEnvironmentVariables();
        });
    }
} 