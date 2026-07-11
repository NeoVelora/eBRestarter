using eBRestarter.Core.Application.Ports.Outbound.Interfaces.Config;
using eBRestarter.Core.Application.Ports.Outbound.Interfaces.Security;
using eBRestarter.Core.Domain.ValueObjects;
using Microsoft.Extensions.Logging;

namespace eBRestarter.Infrastructure.Repositories.Config;

public sealed class EncryptedEVisitorConfigRepositoryDecorator(
    IOutboundPortEVisitorConfigRepository inner,
    IOutboundPortEncryption encryptionUseCase,
    ILogger<EncryptedEVisitorConfigRepositoryDecorator> logger) : IOutboundPortEVisitorConfigRepository
{
    private readonly IOutboundPortEVisitorConfigRepository _inner = inner;
    private readonly IOutboundPortEncryption _encryptionUseCase = encryptionUseCase;
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
