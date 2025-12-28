using eBRestarter.Core.Application.Interfaces;
using eBRestarter.Core.Application.Interfaces.Config;
using eBRestarter.Core.Domain.Models.Records.Config;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Text;
using System.Text.Json;

namespace eBRestarter.Infrastructure.Services.Config
{
    public class EVRestarterConfigService : IEVisitorConfigService
    {
        private readonly IPathService _pathService;
        private readonly ILogger<EVRestarterConfigService> _logger;

        private readonly JsonSerializerOptions _jsonOptions = new()
        {
            WriteIndented = true,
            PropertyNameCaseInsensitive = true
        };

        public EVRestarterConfigService(IPathService pathService, ILogger<EVRestarterConfigService> logger)
        {
            _pathService = pathService;
            _logger = logger;
        }

        public AppConfig LoadConfig()
        {
            var filePath = _pathService.GetConfigFilePath();

            if (!File.Exists(filePath))
            {
                _logger.LogInformation("Konfigurationsdatei '{Path}' nicht gefunden. Erstelle Standardwerte.", filePath);

                var defaultConfig = new AppConfig();
                SaveConfig(defaultConfig);

                return defaultConfig;
            }

            try
            {
                string jsonString = File.ReadAllText(filePath);

                var config = JsonSerializer.Deserialize<AppConfig>(jsonString, _jsonOptions);

                return config ?? new AppConfig();
            }
            catch (JsonException jsonEx)
            {
                _logger.LogError(jsonEx, "Die Konfigurationsdatei ist beschädigt (ungültiges JSON). Nutze Standardwerte.");
                return new AppConfig();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Allgemeiner Fehler beim Laden der Konfiguration.");
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

                string jsonString = JsonSerializer.Serialize(config, _jsonOptions);

                File.WriteAllText(filePath, jsonString);

                _logger.LogInformation("Konfiguration erfolgreich unter '{Path}' gespeichert.", filePath);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Kritischer Fehler beim Speichern der Konfiguration. Änderungen gingen verloren.");
            }
        }

        public void ResetConfig()
        {
            _logger.LogWarning("Konfiguration wird auf Standardwerte zurückgesetzt.");

            var defaultConfig = new AppConfig();
            SaveConfig(defaultConfig);
        }
    }
}
