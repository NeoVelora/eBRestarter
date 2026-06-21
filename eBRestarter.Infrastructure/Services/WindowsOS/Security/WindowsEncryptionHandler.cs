using eBRestarter.Core.Application.Interfaces.Security;
using System.Security.Cryptography;
using System.Text;

namespace eBRestarter.Infrastructure.Services.WindowsOS.Security;

public class WindowsEncryptionHandler : IEncryptionUseCase
{
    private static readonly byte[] _entropy = Encoding.UTF8.GetBytes("eBRestarter_Secure_Entropy_2026");

    public string Encrypt(string plainText)
    {
        if (string.IsNullOrEmpty(plainText)) return string.Empty;

        try
        {
            var plainBytes = Encoding.UTF8.GetBytes(plainText);

            var encryptedBytes = ProtectedData.Protect(plainBytes, _entropy, DataProtectionScope.CurrentUser);

            return Convert.ToBase64String(encryptedBytes);
        }
        catch
        {
            return string.Empty;
        }
    }

    public string Decrypt(string cipherText)
    {
        if (string.IsNullOrEmpty(cipherText)) return string.Empty;

        try
        {
            var encryptedBytes = Convert.FromBase64String(cipherText);

            var plainBytes = ProtectedData.Unprotect(encryptedBytes, _entropy, DataProtectionScope.CurrentUser);

            return Encoding.UTF8.GetString(plainBytes);
        }
        catch
        {
            return string.Empty;
        }
    }
}

