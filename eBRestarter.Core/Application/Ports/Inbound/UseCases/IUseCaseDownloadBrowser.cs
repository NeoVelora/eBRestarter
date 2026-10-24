using eBRestarter.Core.Application.ObjectArchetypes.DTOs.Browser;

namespace eBRestarter.Core.Application.Ports.Inbound.UseCases;

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
