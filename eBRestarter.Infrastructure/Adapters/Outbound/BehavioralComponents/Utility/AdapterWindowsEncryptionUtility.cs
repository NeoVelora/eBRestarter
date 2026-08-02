using System;
using System.Security.Cryptography;
using System.Text;

using eBRestarter.Core.Application.Ports.Outbound.Interfaces.Security;

namespace eBRestarter.Infrastructure.Adapters.Outbound.BehavioralComponents.Utility;

public sealed class AdapterWindowsEncryptionUtility : IOutboundPortEncryption
{
    private static readonly byte[] Entropy = "eBRestarter_Secure_Entropy_2026"u8.ToArray();


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
        catch (CryptographicException)
        {
            return string.Empty;
        }
        catch (FormatException)
        {
            return string.Empty;
        }
        catch (Exception)
        {
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
        catch (CryptographicException)
        {
            return string.Empty;
        }
        catch (Exception)
        {
            return string.Empty;
        }
    }
}