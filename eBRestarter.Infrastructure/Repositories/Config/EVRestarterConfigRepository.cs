using eBRestarter.Core.Application.Ports.Inbound.Interfaces.Providers;
using eBRestarter.Core.Application.Ports.Outbound.Interfaces.Config;
using eBRestarter.Core.Application.Ports.Outbound.Interfaces.OperatingSystem;
using eBRestarter.Core.Domain.Entities;
using Microsoft.Extensions.Logging;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace eBRestarter.Infrastructure.Repositories.Config;

public sealed class EVRestarterConfigRepository(
    IInboundPortOsAppPathProvider pathProvider,
    IOutboundPortFileSystem fileSystem,
    ILogger<EVRestarterConfigRepository> logger) : IOutboundPortEVisitorConfigRepository
{
    private readonly IInboundPortOsAppPathProvider _pathProvider = pathProvider;
    private readonly IOutboundPortFileSystem _fileSystem = fileSystem;
    private readonly ILogger<EVRestarterConfigRepository> _logger = logger;

    private readonly JsonSerializerOptions _jsonOptions = new()
    {
        WriteIndented = true,
        PropertyNameCaseInsensitive = true,
        TypeInfoResolver = AppConfigJsonContext.Default
    };

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
            var jsonString = _fileSystem.ReadAllText(filePath);
            return JsonSerializer.Deserialize<AppConfig>(jsonString, _jsonOptions) ?? new AppConfig();
        }
        catch (JsonException ex)
        {
            _logger.LogError(ex, "Configuration file is not valid JSON.");
            return new AppConfig();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Critical error occurred while loading the configuration.");
            return new AppConfig();
        }
    }

    public void SaveConfig(AppConfig config)
    {
        try
        {
            var filePath = _pathProvider.RetrieveConfigFilePath();

            var directory = _fileSystem.GetDirectoryName(filePath);

            if (!string.IsNullOrEmpty(directory) && !_fileSystem.DirectoryExists(directory))
            {
                _fileSystem.CreateDirectory(directory);
            }

            var jsonString = JsonSerializer.Serialize(config, _jsonOptions);

            _fileSystem.WriteAllText(filePath, jsonString);

            _logger.LogInformation("Configuration saved successfully.");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error while saving the configuration.");
        }
    }

    public void ResetConfig()
    {
        _logger.LogWarning("Configuration is being reset to default values.");
        var defaultConfig = new AppConfig();
        SaveConfig(defaultConfig);
    }
}

[JsonSerializable(typeof(AppConfig))]
internal partial class AppConfigJsonContext : JsonSerializerContext;


