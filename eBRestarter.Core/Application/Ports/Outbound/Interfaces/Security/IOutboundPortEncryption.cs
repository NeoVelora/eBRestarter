namespace eBRestarter.Core.Application.Ports.Outbound.Interfaces.Security;

public interface IOutboundPortEncryption
{
    string Encrypt(string plainText);
    string Decrypt(string cipherText);
}
