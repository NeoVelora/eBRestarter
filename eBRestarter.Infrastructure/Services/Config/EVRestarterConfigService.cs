using eBRestarter.Core.Application.Interfaces;
using eBRestarter.Core.Application.Interfaces.Config;
using eBRestarter.Core.Application.Interfaces.Security;
using eBRestarter.Core.Application.Models.Config;
using Microsoft.Extensions.Logging;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace eBRestarter.Infrastructure.Services.Config;

public class EVisitorConfigService(IPathService pathService, IEncryptionService encryptionService, ILogger<EVisitorConfigService> logger) : IEVisitorConfigService
{
    private readonly IPathService _pathService = pathService;
    private readonly IEncryptionService _encryptionService = encryptionService;
    private readonly ILogger<EVisitorConfigService> _logger = logger;

    private readonly JsonSerializerOptions _jsonOptions = new()
    {
        WriteIndented = true,
        PropertyNameCaseInsensitive = true,
        TypeInfoResolver = AppConfigJsonContext.Default
    };

    public AppConfig LoadConfig()
    {
        var filePath = _pathService.GetConfigFilePath();

        if (!File.Exists(filePath))
        {
            var defaultConfig = new AppConfig();
            SaveConfig(defaultConfig);
            return defaultConfig;
        }

        try
        {
            string jsonString = File.ReadAllText(filePath);

            var config = JsonSerializer.Deserialize<AppConfig>(jsonString, _jsonOptions) ?? new AppConfig();

            if (!string.IsNullOrEmpty(config.Settings.ApiKey))
            {
                var decryptedKey = _encryptionService.Decrypt(config.Settings.ApiKey);


                config = config with
                {
                    Settings = config.Settings with { ApiKey = decryptedKey }
                };

                if (string.IsNullOrEmpty(decryptedKey))
                {
                    _logger.LogWarning("API Key konnte nicht entschlüsselt werden (evtl. PC gewechselt). Der Key muss neu eingegeben werden.");
                }
            }

            return config;
        }
        catch (JsonException)
        {
            _logger.LogError("Config-Datei ist kein gültiges JSON.");
            return new AppConfig();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Kritischer Fehler beim Laden.");
            return new AppConfig();
        }
    }

    public void SaveConfig(AppConfig config)
    {
        try
        {
            var filePath = _pathService.GetConfigFilePath();
            var directory = Path.GetDirectoryName(filePath);

            if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
            {
                Directory.CreateDirectory(directory);
            }

            var encryptedKey = _encryptionService.Encrypt(config.Settings.ApiKey);

            var configToSave = config with
            {
                Settings = config.Settings with { ApiKey = encryptedKey }
            };

            string jsonString = JsonSerializer.Serialize(configToSave, _jsonOptions);
            File.WriteAllText(filePath, jsonString);

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
internal partial class AppConfigJsonContext : JsonSerializerContext
{
}