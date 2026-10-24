namespace eBRestarter.Core.Application.Ports.Outbound.Security;

public interface IOutboundPortEncryption
{
    string Encrypt(string plainText);
    string Decrypt(string cipherText);
}
