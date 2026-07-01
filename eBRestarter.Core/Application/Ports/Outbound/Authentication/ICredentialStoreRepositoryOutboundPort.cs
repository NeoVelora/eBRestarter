using eBRestarter.Core.Application.Models.Records;

namespace eBRestarter.Core.Application.Ports.Outbound.Authentication;

public interface ICredentialStoreRepositoryOutboundPort
{
    void SaveCredentials(ApiCredentials credentials);

    ApiCredentials? LoadCredentials();

    void ClearCredentials();

    ApiCredentials? ImportFromLegacyFile(string filePath);
}


