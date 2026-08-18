using Microsoft.Extensions.Logging;
using System;
using System.Security.Cryptography;
using System.Text;

using eBRestarter.Core.Application.ObjectArchetypes.Constants;
using eBRestarter.Core.Application.Ports.Outbound.Interfaces.Security;

namespace eBRestarter.Infrastructure.Adapters.Outbound.BehavioralComponents.Utility;

public sealed class AdapterWindowsEncryptionUtility(
    ILogger<AdapterWindowsEncryptionUtility> logger) : IOutboundPortEncryption
{
    private const string EncryptionFailedExceptionMessage = "The value could not be encrypted with Windows DPAPI. It was deliberately NOT persisted unprotected.";

    private static readonly byte[] Entropy = "eBRestarter_Secure_Entropy_2026"u8.ToArray();


    private readonly ILogger<AdapterWindowsEncryptionUtility> _logger = logger ?? throw new ArgumentNullException(nameof(logger));


    public string Decrypt(string cipherText)
    {
        if (string.IsNullOrWhiteSpace(cipherText))
        {
            return string.Empty;
        }

        try
        {
            var encryptedBytes = Convert.FromBase64String(cipherText);

            var plainBytes = ProtectedData.Unprotect(encryptedBytes, Entropy, DataProtectionScope.CurrentUser);

            return Encoding.UTF8.GetString(plainBytes);
        }
        catch (CryptographicException exception)
        {
            _logger.LogWarning(
                LogEventIds.Configuration.ApiKeyDecryptionFailed,
                exception,
                "DPAPI could not unprotect the stored value; the profile was most likely copied from another machine or user.");

            return string.Empty;
        }
        catch (FormatException exception)
        {
            _logger.LogWarning(
                LogEventIds.Configuration.ApiKeyDecryptionFailed,
                exception,
                "The stored cipher text is not valid Base64 and could not be decrypted.");

            return string.Empty;
        }
        catch (Exception exception)
        {
            _logger.LogError(
                LogEventIds.Configuration.ApiKeyDecryptionFailed,
                exception,
                "Unexpected failure while decrypting a protected value.");

            return string.Empty;
        }
    }

    public string Encrypt(string plainText)
    {
        if (string.IsNullOrWhiteSpace(plainText))
        {
            return string.Empty;
        }

        try
        {
            var plainBytes = Encoding.UTF8.GetBytes(plainText);

            var encryptedBytes = ProtectedData.Protect(plainBytes, Entropy, DataProtectionScope.CurrentUser);

            return Convert.ToBase64String(encryptedBytes);
        }
        catch (Exception exception)
        {
            throw new InvalidOperationException(EncryptionFailedExceptionMessage, exception);
        }
    }
}