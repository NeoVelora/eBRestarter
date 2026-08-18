using System;
using System.Threading;
using Microsoft.Extensions.Logging;

using eBRestarter.Core.Application.Ports.Outbound.Interfaces.Config;
using eBRestarter.Core.Application.Ports.Outbound.Interfaces.Security;
using eBRestarter.Core.Domain.ValueObjects;
using eBRestarter.Core.Application.ObjectArchetypes.Constants;

namespace eBRestarter.Infrastructure.BehavioralComponents.Repositories.Config;

public sealed class EncryptedEVisitorConfigRepositoryDecorator(
    IOutboundPortEVisitorConfigRepository inner,
    IOutboundPortEncryption encryptionUseCase,
    ILogger<EncryptedEVisitorConfigRepositoryDecorator> logger)
    : IOutboundPortEVisitorConfigRepository
{

    private const string SaveAbortedExceptionMessage = "The configuration was not saved because the API key could not be encrypted.";


    private readonly IOutboundPortEncryption _encryptionUseCase = encryptionUseCase ?? throw new ArgumentNullException(nameof(encryptionUseCase));
    private readonly IOutboundPortEVisitorConfigRepository _inner = inner ?? throw new ArgumentNullException(nameof(inner));
    private readonly ILogger<EncryptedEVisitorConfigRepositoryDecorator> _logger = logger ?? throw new ArgumentNullException(nameof(logger));

    private readonly Lock _decryptionCacheLock = new();
    private string? _cachedCipherText;
    private string? _cachedPlainText;


    public AppConfig LoadConfig()
    {
        var config = _inner.LoadConfig();

        if (string.IsNullOrEmpty(config.Settings.ApiKey))
        {
            return config;
        }

        try
        {
            var decryptedKey = DecryptCached(config.Settings.ApiKey);
            config.Settings.ApiKey = decryptedKey;

            if (string.IsNullOrEmpty(decryptedKey))
            {
                _logger.LogWarning(LogEventIds.Configuration.ApiKeyDecryptionFailed, "API Key could not be decrypted (possibly changed machine). The key must be re-entered.");
            }
        }
        catch (Exception exception)
        {
            _logger.LogError(LogEventIds.Configuration.ApiKeyDecryptionFailed, exception, "Error while decrypting the API key.");
            config.Settings.ApiKey = string.Empty;
        }

        return config;
    }

    public void ResetConfig()
    {
        _inner.ResetConfig();
    }

    public void SaveConfig(AppConfig config)
    {
        ArgumentNullException.ThrowIfNull(config);

        var originalKey = config.Settings.ApiKey;

        try
        {
            if (!string.IsNullOrEmpty(originalKey))
            {
                var cipherText = _encryptionUseCase.Encrypt(originalKey);
                config.Settings.ApiKey = cipherText;

                StoreDecryptionCache(cipherText, originalKey);
            }
            else
            {
                InvalidateDecryptionCache();
            }

            _inner.SaveConfig(config);
        }
        catch (Exception exception)
        {
            InvalidateDecryptionCache();

            throw new InvalidOperationException(SaveAbortedExceptionMessage, exception);
        }
        finally
        {
            config.Settings.ApiKey = originalKey;
        }
    }

    private string DecryptCached(string cipherText)
    {
        lock (_decryptionCacheLock)
        {
            if (_cachedPlainText is not null && string.Equals(_cachedCipherText, cipherText, StringComparison.Ordinal))
            {
                return _cachedPlainText;
            }
        }

        var plainText = _encryptionUseCase.Decrypt(cipherText);

        if (!string.IsNullOrEmpty(plainText))
        {
            StoreDecryptionCache(cipherText, plainText);
        }

        return plainText;
    }

    private void InvalidateDecryptionCache()
    {
        lock (_decryptionCacheLock)
        {
            _cachedCipherText = null;
            _cachedPlainText = null;
        }
    }

    private void StoreDecryptionCache(string cipherText, string plainText)
    {
        lock (_decryptionCacheLock)
        {
            _cachedCipherText = cipherText;
            _cachedPlainText = plainText;
        }
    }
}
