using eBRestarter.Core.Application.ObjectArchetypes.DTOs.Records;

namespace eBRestarter.Core.Application.Ports.Outbound.Interfaces.Update;

public interface IOutboundPortUpdate
{
    Task<UpdateInfo> CheckForUpdateAsync(CancellationToken cancellationToken = default);

    Task DownloadAndInstallAsync(UpdateInfo updateInfo, CancellationToken cancellationToken = default);
}