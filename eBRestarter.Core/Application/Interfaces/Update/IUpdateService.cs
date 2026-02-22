using eBRestarter.Core.Domain.Models.Records;

namespace eBRestarter.Core.Application.Interfaces.Update;

public interface IUpdateService
{
    Task<UpdateInfo> CheckForUpdateAsync();

    Task DownloadAndInstallAsync(UpdateInfo updateInfo);
}
