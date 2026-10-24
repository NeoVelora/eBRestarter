using eBRestarter.Core.Application.ObjectArchetypes.DTOs.System;

namespace eBRestarter.Core.Application.Ports.Outbound.Authentication;

public interface IOutboundPortCredentialStoreRepository
{
    void SaveCredentials(ApiCredentials credentials);

    ApiCredentials? LoadCredentials();

    void ClearCredentials();

    ApiCredentials? ImportFromLegacyFile(string filePath);
}
