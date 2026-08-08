using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Configuration;
using Serilog;
using Serilog.Events;
using Serilog.Sinks.SystemConsole.Themes;

namespace Billing.Infrastructure.Logging;

/// <summary>
/// Centralised Serilog bootstrap. Configures console + rolling file sinks with
/// enrichers for tenant, user, and correlation id.
/// </summary>
public static class SerilogConfiguration
{
    public static LoggerConfiguration Configure(LoggerConfiguration configuration, IConfiguration config)
    {
        var logLevel = config.GetValue<string>("Serilog:MinimumLevel") ?? "Information";
        if (!Enum.TryParse(logLevel, out LogEventLevel level))
            level = LogEventLevel.Information;

        var logPath = config.GetValue<string>("Serilog:FilePath") ?? "logs/billing-.log";
        var rollingInterval = config.GetValue<string>("Serilog:RollingInterval") ?? "Day";
        var interval = Enum.TryParse<RollingInterval>(rollingInterval, out var ri) ? ri : RollingInterval.Day;
        var retainedFileCount = config.GetValue<int?>("Serilog:RetainedFileCountLimit") ?? 31;

        configuration
            .MinimumLevel.Is(level)
            .MinimumLevel.Override("Microsoft", LogEventLevel.Warning)
            .MinimumLevel.Override("Microsoft.Hosting.Lifetime", LogEventLevel.Information)
            .MinimumLevel.Override("System", LogEventLevel.Warning)
            .Enrich.FromLogContext()
            .Enrich.WithProperty("Application", "BillingSystem")
            .WriteTo.Async(a => a.Console(
                outputTemplate: "[{Timestamp:HH:mm:ss} {Level:u3}] {Message:lj} {Properties:j}{NewLine}{Exception}",
                theme: SystemConsoleTheme.Literate))
            .WriteTo.Async(a => a.File(
                path: logPath,
                rollingInterval: interval,
                retainedFileCountLimit: retainedFileCount,
                outputTemplate: "{Timestamp:yyyy-MM-dd HH:mm:ss.fff zzz} [{Level:u3}] {Message:lj} {Properties:j}{NewLine}{Exception}"));

        return configuration;
    }

    public static IServiceCollection AddBillingSerilog(this IServiceCollection services)
    {
        services.AddSingleton<ILogger>(sp => Log.Logger);
        return services;
    }
}
