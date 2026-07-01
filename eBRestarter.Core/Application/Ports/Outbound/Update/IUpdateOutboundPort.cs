using eBRestarter.Core.Application.Models.Records;

namespace eBRestarter.Core.Application.Ports.Outbound.Update;

public interface IUpdateOutboundPort
{
    Task<UpdateInfo> CheckForUpdateAsync();

    Task DownloadAndInstallAsync(UpdateInfo updateInfo);
}