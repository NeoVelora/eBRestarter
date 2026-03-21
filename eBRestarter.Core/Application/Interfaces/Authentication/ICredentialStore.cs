using eBRestarter.Core.Domain.Models.Records;

namespace eBRestarter.Core.Application.Interfaces.Authentication;

public interface ICredentialStore
{
    void SaveCredentials(ApiCredentials credentials);

    ApiCredentials? LoadCredentials();

    void ClearCredentials();

    ApiCredentials? ImportFromLegacyFile(string filePath);
}
