using eBRestarter.Core.Application.ObjectArchetypes.DTOs.Update;

namespace eBRestarter.Core.Application.Ports.Outbound.Update;

public interface IOutboundPortUpdate
{
    Task<UpdateInfo> CheckForUpdateAsync(CancellationToken cancellationToken = default);

    Task DownloadAndInstallAsync(UpdateInfo updateInfo, CancellationToken cancellationToken = default);
}