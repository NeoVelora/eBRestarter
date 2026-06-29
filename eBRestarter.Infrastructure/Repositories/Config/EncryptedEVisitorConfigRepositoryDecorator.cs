using eBRestarter.Core.Application.Ports.Outbound.Config;
using eBRestarter.Core.Application.Ports.Outbound.Security;
using eBRestarter.Core.Domain.Entities;
using Microsoft.Extensions.Logging;

namespace eBRestarter.Infrastructure.Repositories.Config;

public sealed class EncryptedEVisitorConfigRepositoryDecorator(
    IEVisitorConfigPort inner,
    IEncryptionPort encryptionUseCase,
    ILogger<EncryptedEVisitorConfigRepositoryDecorator> logger) : IEVisitorConfigPort
{
    private readonly IEVisitorConfigPort _inner = inner;
    private readonly IEncryptionPort _encryptionUseCase = encryptionUseCase;
    private readonly ILogger<EncryptedEVisitorConfigRepositoryDecorator> _logger = logger;

    public AppConfig LoadConfig()
    {
        var config = _inner.LoadConfig();

        if (!string.IsNullOrEmpty(config.Settings.ApiKey))
        {
            try
            {
                var decryptedKey = _encryptionUseCase.Decrypt(config.Settings.ApiKey);
                config.Settings.ApiKey = decryptedKey;

                if (string.IsNullOrEmpty(decryptedKey))
                {
                    _logger.LogWarning("API Key could not be decrypted (possibly changed machine). The key must be re-entered.");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error while decrypting the API key.");
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
                config.Settings.ApiKey = _encryptionUseCase.Encrypt(originalKey);
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
