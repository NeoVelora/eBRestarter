using eBRestarter.Core.Application.Interfaces.Config;
using eBRestarter.Core.Application.Interfaces.Security;
using eBRestarter.Core.Domain.Entities;
using Microsoft.Extensions.Logging;

namespace eBRestarter.Infrastructure.Services.Config;

public class EncryptedEVisitorConfigServiceDecorator(
    IEVisitorConfigService inner,
    IEncryptionService encryptionService,
    ILogger<EncryptedEVisitorConfigServiceDecorator> logger) : IEVisitorConfigService
{
    private readonly IEVisitorConfigService _inner = inner;
    private readonly IEncryptionService _encryptionService = encryptionService;
    private readonly ILogger<EncryptedEVisitorConfigServiceDecorator> _logger = logger;

    public AppConfig LoadConfig()
    {
        var config = _inner.LoadConfig();

        if (!string.IsNullOrEmpty(config.Settings.ApiKey))
        {
            try
            {
                var decryptedKey = _encryptionService.Decrypt(config.Settings.ApiKey);
                config.Settings.ApiKey = decryptedKey;

                if (string.IsNullOrEmpty(decryptedKey))
                {
                    _logger.LogWarning("API Key konnte nicht entschlüsselt werden (evtl. PC gewechselt). Der Key muss neu eingegeben werden.");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Fehler beim Entschlüsseln des API-Keys.");
                config.Settings.ApiKey = string.Empty;
            }
        }

        return config;
    }

    public void SaveConfig(AppConfig config)
    {
        var originalKey = config.Settings.ApiKey;
        try
        {
            if (!string.IsNullOrEmpty(originalKey))
            {
                config.Settings.ApiKey = _encryptionService.Encrypt(originalKey);
            }

            _inner.SaveConfig(config);
        }
        finally
        {
            config.Settings.ApiKey = originalKey;
        }
    }

    public void ResetConfig()
    {
        _inner.ResetConfig();
    }
}
