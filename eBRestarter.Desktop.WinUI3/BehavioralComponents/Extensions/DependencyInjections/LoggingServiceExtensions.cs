using System;
using System.IO;

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

using Serilog;
using Serilog.Events;

namespace eBRestarter.Desktop.WinUI3.BehavioralComponents.Extensions.DependencyInjections;

public static class LoggingServiceExtensions
{
    private const string AppFolderName = "eBRestarter";
    private const int DefaultRetainedFileCount = 7;
    private const long DefaultFileSizeLimitBytes = 10L * 1024 * 1024;
    private const string LogFileNameTemplate = "ebrestarter-.log";
    private const string LogFolderName = "logs";

    private const string MinimumLevelConfigurationKey = "Logging:MinimumLevel";

    private const string OutputTemplate =
        "{Timestamp:yyyy-MM-dd HH:mm:ss.fff zzz} [{Level:u3}] ({SourceContext}) {Message:lj}{NewLine}{Properties:j}{NewLine}{Exception}";

    private const string SkylarFolderName = "Skylar";

    public static IServiceCollection AddApplicationLogging(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(configuration);

        var logDirectory = ResolveLogDirectory();
        Directory.CreateDirectory(logDirectory);

        var logFilePath = Path.Combine(logDirectory, LogFileNameTemplate);
        var minimumLevel = ResolveMinimumLevel(configuration);

        var serilogLogger = new LoggerConfiguration()
            .MinimumLevel.Is(minimumLevel)
            .Enrich.FromLogContext()
            .WriteTo.File(
                path: logFilePath,
                outputTemplate: OutputTemplate,
                rollingInterval: RollingInterval.Day,
                fileSizeLimitBytes: DefaultFileSizeLimitBytes,
                rollOnFileSizeLimit: true,
                retainedFileCountLimit: DefaultRetainedFileCount,
                shared: true)
            .CreateLogger();

        services.AddLogging(builder =>
        {
            builder.ClearProviders();
            builder.AddDebug();

            builder.AddSerilog(serilogLogger, dispose: true);
            builder.SetMinimumLevel(ToMicrosoftLevel(minimumLevel));
        });

        return services;
    }

    private static string ResolveLogDirectory()
    {
        var localAppData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);

        return Path.Combine(localAppData, SkylarFolderName, AppFolderName, LogFolderName);
    }

    private static LogEventLevel ResolveMinimumLevel(IConfiguration configuration)
    {
        var configuredValue = configuration[MinimumLevelConfigurationKey];

        return Enum.TryParse(configuredValue, ignoreCase: true, out LogEventLevel parsedLevel)
            ? parsedLevel
            : LogEventLevel.Information;
    }

    private static LogLevel ToMicrosoftLevel(LogEventLevel level) => level switch
    {
        LogEventLevel.Verbose => LogLevel.Trace,
        LogEventLevel.Debug => LogLevel.Debug,
        LogEventLevel.Information => LogLevel.Information,
        LogEventLevel.Warning => LogLevel.Warning,
        LogEventLevel.Error => LogLevel.Error,
        LogEventLevel.Fatal => LogLevel.Critical,
        _ => LogLevel.Information
    };
}
