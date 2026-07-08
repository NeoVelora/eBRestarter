using eBRestarter.Core.Application.ObjectArchetypes.DTOs.Records;

namespace eBRestarter.Core.Application.Ports.Outbound.Interfaces.Update;

public interface IOutboundPortUpdate
{
    Task<UpdateInfo> CheckForUpdateAsync();

    Task DownloadAndInstallAsync(UpdateInfo updateInfo);
}