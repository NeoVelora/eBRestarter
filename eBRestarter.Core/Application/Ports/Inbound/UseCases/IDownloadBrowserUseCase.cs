using eBRestarter.Core.Application.Models.Records;

namespace eBRestarter.Core.Application.Ports.Inbound.UseCases;

public interface IDownloadBrowserUseCase
{
    Task<string> DownloadInstallerAsync(
        string browserName,
        string downloadUrl,
        IProgress<DownloadProgressStatus> progress,
        CancellationToken cancellationToken);

    Task StartInstallerAsync(string installerPath);

    void CleanupPartialDownload(string browserName);
}
