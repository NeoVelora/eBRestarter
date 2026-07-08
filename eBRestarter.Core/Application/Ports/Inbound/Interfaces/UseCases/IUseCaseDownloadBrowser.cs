using eBRestarter.Core.Application.ObjectArchetypes.DTOs.Records;

namespace eBRestarter.Core.Application.Ports.Inbound.Interfaces.UseCases;

public interface IUseCaseDownloadBrowser
{
    Task<string> DownloadInstallerAsync(
        string browserName,
        string downloadUrl,
        IProgress<DownloadProgressStatus> progress,
        CancellationToken cancellationToken);

    Task StartInstallerAsync(string installerPath);

    void CleanupPartialDownload(string browserName);
}
