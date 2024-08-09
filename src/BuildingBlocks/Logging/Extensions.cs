namespace BuildingBlocks.Logging;

using System.Text;
using BuildingBlocks.Web;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Serilog;
using Serilog.Events;
using Serilog.Exceptions;
using Serilog.Sinks.Elasticsearch;
using Serilog.Sinks.SpectreConsole;

public static class Extensions
{
    public static WebApplicationBuilder AddCustomSerilog(this WebApplicationBuilder builder, IWebHostEnvironment env)
    {
        builder.Host.UseSerilog((context, services, loggerConfiguration) =>
        {
            // Retrieve environment and configuration options
            var environment = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT");
            var logOptions = context.Configuration.GetSection(nameof(LogOptions)).Get<LogOptions>();
            var appOptions = context.Configuration.GetSection(nameof(AppOptions)).Get<AppOptions>();

            // Set log level
            var logLevel = Enum.TryParse<LogEventLevel>(logOptions.Level, true, out var level)
                ? level
                : LogEventLevel.Information;

            // Configure Serilog
            loggerConfiguration
                .MinimumLevel.Is(logLevel)
                .WriteTo.SpectreConsole(logOptions.LogTemplate, logLevel)
                .MinimumLevel.Override("Microsoft", LogEventLevel.Information)
                .MinimumLevel.Override("Microsoft.EntityFrameworkCore", LogEventLevel.Error) // EF Core logs at error level
                .MinimumLevel.Override("Microsoft.AspNetCore", LogEventLevel.Warning) // ASP.NET Core logs at warning level
                .Enrich.WithExceptionDetails()
                .Enrich.FromLogContext()
                .ReadFrom.Configuration(context.Configuration);

            // Configure Elasticsearch sink if enabled
            if (logOptions.Elastic?.Enabled == true)
            {
                loggerConfiguration.WriteTo.Elasticsearch(
                    new ElasticsearchSinkOptions(new Uri(logOptions.Elastic.ElasticServiceUrl))
                    {
                        AutoRegisterTemplate = true,
                        IndexFormat = $"{appOptions.Name}-{environment?.ToLower()}"
                    });
            }

            // Configure Sentry sink if enabled
            if (logOptions.Sentry?.Enabled == true)
            {
                var minimumBreadcrumbLevel = Enum.TryParse<LogEventLevel>(logOptions.Level, true, out var minBreadcrumbLevel)
                    ? minBreadcrumbLevel
                    : LogEventLevel.Information;

                var minimumEventLevel = Enum.TryParse<LogEventLevel>(logOptions.Sentry.MinimumEventLevel, true, out var minEventLevel)
                    ? minEventLevel
                    : LogEventLevel.Error;

                loggerConfiguration.WriteTo.Sentry(o =>
                {
                    o.Dsn = logOptions.Sentry.Dsn;
                    o.MinimumBreadcrumbLevel = minimumBreadcrumbLevel;
                    o.MinimumEventLevel = minEventLevel;
                });
            }

            // Configure File sink if enabled
            if (logOptions.File?.Enabled == true)
            {
                var root = env.ContentRootPath;
                Directory.CreateDirectory(Path.Combine(root, "logs"));

                var path = string.IsNullOrWhiteSpace(logOptions.File.Path) ? "logs/log-.txt" : logOptions.File.Path;
                var interval = Enum.TryParse<RollingInterval>(logOptions.File.Interval, true, out var rollingInterval)
                    ? rollingInterval
                    : RollingInterval.Day;

                loggerConfiguration.WriteTo.File(
                    path,
                    rollingInterval: interval,
                    encoding: Encoding.UTF8,
                    outputTemplate: logOptions.LogTemplate
                );
            }
        });

        return builder;
    }
}
