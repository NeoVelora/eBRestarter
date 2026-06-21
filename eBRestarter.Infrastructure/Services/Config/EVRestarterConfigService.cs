using eBRestarter.Core.Application.Interfaces;
using eBRestarter.Core.Application.Interfaces.Config;
using eBRestarter.Core.Application.Interfaces.OperatingSystem.WindowsOS;
using eBRestarter.Core.Domain.Entities;
using Microsoft.Extensions.Logging;
using System;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace eBRestarter.Infrastructure.Services.Config;

public class EVisitorConfigService(
    IPathUseCase pathUseCase, 
    IWindowsFileSystemService fileSystem,
    ILogger<EVisitorConfigService> logger) : IEVisitorConfigService
{
    private readonly IPathUseCase _pathUseCase = pathUseCase;
    private readonly IWindowsFileSystemService _fileSystem = fileSystem;
    private readonly ILogger<EVisitorConfigService> _logger = logger;

    private readonly JsonSerializerOptions _jsonOptions = new()
    {
        WriteIndented = true,
        PropertyNameCaseInsensitive = true,
        TypeInfoResolver = AppConfigJsonContext.Default
    };

    public AppConfig LoadConfig()
    {
        var filePath = _pathUseCase.RetrieveConfigFilePath();

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
            _logger.LogError(ex, "Config-Datei ist kein gültiges JSON.");
            return new AppConfig();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Kritischer Fehler beim Laden der Konfiguration.");
            return new AppConfig();
        }
    }

    public void SaveConfig(AppConfig config)
    {
        try
        {
            var filePath = _pathUseCase.RetrieveConfigFilePath();
            var directory = _fileSystem.GetDirectoryName(filePath);

            if (!string.IsNullOrEmpty(directory) && !_fileSystem.DirectoryExists(directory))
            {
                _fileSystem.CreateDirectory(directory);
            }

            var jsonString = JsonSerializer.Serialize(config, _jsonOptions);
            _fileSystem.WriteAllText(filePath, jsonString);

            _logger.LogInformation("Konfiguration gespeichert.");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Fehler beim Speichern der Konfiguration.");
        }
    }

    public void ResetConfig()
    {
        _logger.LogWarning("Konfiguration wird auf Standardwerte zurückgesetzt.");
        var defaultConfig = new AppConfig();
        SaveConfig(defaultConfig);
    }
}

[JsonSerializable(typeof(AppConfig))]
internal partial class AppConfigJsonContext : JsonSerializerContext;

