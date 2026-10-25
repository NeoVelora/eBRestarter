using System;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading;
using Microsoft.Extensions.Logging;

using eBRestarter.Core.Application.Ports.Inbound.Providers;
using eBRestarter.Core.Application.Ports.Outbound.Config;
using eBRestarter.Core.Application.Ports.Outbound.OperatingSystem;
using eBRestarter.Core.Domain.ValueObjects;
using eBRestarter.Core.Application.ObjectArchetypes.Constants;

namespace eBRestarter.Infrastructure.Adapters.Outbound.BehavioralComponents.Repositories.Config;

public sealed class AdapterEVRestarterConfigRepository(
    IInboundPortOsAppPathProvider pathProvider,
    IOutboundPortFileSystem fileSystem,
    ILogger<AdapterEVRestarterConfigRepository> logger)
    : IOutboundPortEVisitorConfigRepository
{

    private readonly IOutboundPortFileSystem _fileSystem = fileSystem ?? throw new ArgumentNullException(nameof(fileSystem));
    private readonly ILogger<AdapterEVRestarterConfigRepository> _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    private readonly IInboundPortOsAppPathProvider _pathProvider = pathProvider ?? throw new ArgumentNullException(nameof(pathProvider));

    private static readonly JsonSerializerOptions _jsonOptions = new()
    {
        WriteIndented = true,
        PropertyNameCaseInsensitive = true,
        TypeInfoResolver = AppConfigJsonContext.Default
    };

    private readonly Lock _cacheLock = new();
    private string? _cachedConfigJson;


    public AppConfig LoadConfig()
    {
        var filePath = _pathProvider.RetrieveConfigFilePath();

        if (!_fileSystem.FileExists(filePath))
        {
            var defaultConfig = new AppConfig();
            SaveConfig(defaultConfig);
            return defaultConfig;
        }

        try
        {
            var jsonString = ReadConfigJsonCached(filePath);
            return JsonSerializer.Deserialize<AppConfig>(jsonString, _jsonOptions)
                ?? throw new InvalidOperationException($"Configuration file at '{filePath}' deserialized to null.");
        }
        catch (JsonException exception)
        {
            InvalidateCache();
            throw new InvalidOperationException($"Configuration file at '{filePath}' is corrupted and contains invalid JSON.", exception);
        }
        catch (Exception)
        {
            InvalidateCache();
            throw;
        }
    }

    public void ResetConfig()
    {
        _logger.LogWarning(LogEventIds.Configuration.ConfigurationReset, "Configuration is being reset to default values.");
        var defaultConfig = new AppConfig();
        SaveConfig(defaultConfig);
    }

    public void SaveConfig(AppConfig config)
    {
        ArgumentNullException.ThrowIfNull(config);

        var filePath = _pathProvider.RetrieveConfigFilePath();

        try
        {
            var directory = _fileSystem.GetDirectoryName(filePath);

            if (!string.IsNullOrEmpty(directory) && !_fileSystem.DirectoryExists(directory))
            {
                _fileSystem.CreateDirectory(directory);
            }

            var jsonString = JsonSerializer.Serialize(config, _jsonOptions);

            _fileSystem.WriteAllText(filePath, jsonString);

            lock (_cacheLock)
            {
                _cachedConfigJson = jsonString;
            }

            if (_logger.IsEnabled(LogLevel.Debug))
            {
                _logger.LogDebug(LogEventIds.Configuration.ConfigurationSaved, "Configuration saved successfully to {Path}.", filePath);
            }
        }
        catch (Exception exception)
        {
            InvalidateCache();
            throw new InvalidOperationException($"Failed to save configuration to '{filePath}'.", exception);
        }
    }

    private void InvalidateCache()
    {
        lock (_cacheLock)
        {
            _cachedConfigJson = null;
        }
    }

    private string ReadConfigJsonCached(string filePath)
    {
        lock (_cacheLock)
        {
            _cachedConfigJson ??= _fileSystem.ReadAllText(filePath);
            return _cachedConfigJson;
        }
    }
}

[JsonSerializable(typeof(AppConfig))]
internal partial class AppConfigJsonContext : JsonSerializerContext;

