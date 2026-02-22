using eBRestarter.Core.Application.Interfaces;
using eBRestarter.Core.Application.Interfaces.Config;
using eBRestarter.Core.Application.Interfaces.Security;
using eBRestarter.Core.Domain.Models.Records.Config;
using Microsoft.Extensions.Logging;
using System.Text.Json;

namespace eBRestarter.Infrastructure.Services.Config;

public class EVisitorConfigService : IEVisitorConfigService
{
    private readonly IPathService _pathService;
    private readonly IEncryptionService _encryptionService;
    private readonly ILogger<EVisitorConfigService> _logger;

    private readonly JsonSerializerOptions _jsonOptions = new()
    {
        WriteIndented = true,
        PropertyNameCaseInsensitive = true
    };

    public EVisitorConfigService(IPathService pathService, IEncryptionService encryptionService, ILogger<EVisitorConfigService> logger)
    {
        _encryptionService = encryptionService;
        _pathService = pathService;
        _logger = logger;
    }

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
