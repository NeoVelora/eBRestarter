using eBRestarter.Core.Application.Models.Records;

namespace eBRestarter.Core.Application.Ports.Outbound.Authentication;

public interface ICredentialStorePort
{
    void SaveCredentials(ApiCredentials credentials);

    ApiCredentials? LoadCredentials();

    void ClearCredentials();

    ApiCredentials? ImportFromLegacyFile(string filePath);
}


