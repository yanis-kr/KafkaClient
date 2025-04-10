using Microsoft.Extensions.Hosting;
using Serilog;
using System.Text.RegularExpressions;

namespace KafkaConsumer.Common.Logging;

public static class LoggingConfiguration
{
    public static IHostBuilder ConfigureLogging(this IHostBuilder hostBuilder)
    {
        return hostBuilder.UseSerilog((hostingContext, loggerConfiguration) =>
        {
            loggerConfiguration
                .ReadFrom.Configuration(hostingContext.Configuration)
                .Enrich.FromLogContext()
                .WriteTo.Console(
                    outputTemplate: "[{Timestamp:HH:mm:ss} {Level:u3}] {Message:lj}{NewLine}{Exception}");
        });
    }

    //TBD if need this
    //public static string MaskSensitiveData(string input)
    //{
    //    if (string.IsNullOrEmpty(input))
    //        return input;

    //    // Common patterns for sensitive data
    //    var patterns = new[]
    //    {
    //        (@"password=([^&\s]+)", "password=****"),
    //        (@"api[_-]?key=([^&\s]+)", "api_key=****"),
    //        (@"secret=([^&\s]+)", "secret=****"),
    //        (@"token=([^&\s]+)", "token=****"),
    //        (@"auth=([^&\s]+)", "auth=****"),
    //        (@"credential=([^&\s]+)", "credential=****")
    //    };

    //    var result = input;
    //    foreach (var (pattern, replacement) in patterns)
    //    {
    //        result = Regex.Replace(result, pattern, replacement, RegexOptions.IgnoreCase);
    //    }

    //    return result;
    //}
} 