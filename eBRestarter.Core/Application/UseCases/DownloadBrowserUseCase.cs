using System;
using System.Diagnostics;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

using eBRestarter.Core.Application.ObjectArchetypes.DTOs.Records;
using eBRestarter.Core.Application.Ports.Inbound.Interfaces.Providers;
using eBRestarter.Core.Application.Ports.Inbound.Interfaces.UseCases;
using eBRestarter.Core.Application.Ports.Outbound.Interfaces.Network;
using eBRestarter.Core.Application.Ports.Outbound.Interfaces.OperatingSystem;

namespace eBRestarter.Core.Application.UseCases;

public sealed class DownloadBrowserUseCase(
    IOutboundPortHttpDownload downloadService,
    IOutboundPortFileSystem fileSystemPort,
    IInboundPortOsAppPathProvider pathProvider,
    IOutboundPortOsProcessControl processControlPort)
    : IUseCaseDownloadBrowser
{

    private const string InstallerFileNameSuffix = "_Installer.exe";


    private readonly IOutboundPortHttpDownload _downloadService = downloadService ?? throw new ArgumentNullException(nameof(downloadService));
    private readonly IOutboundPortFileSystem _fileSystemPort = fileSystemPort ?? throw new ArgumentNullException(nameof(fileSystemPort));
    private readonly IInboundPortOsAppPathProvider _pathProvider = pathProvider ?? throw new ArgumentNullException(nameof(pathProvider));
    private readonly IOutboundPortOsProcessControl _processControlPort = processControlPort ?? throw new ArgumentNullException(nameof(processControlPort));


    public void CleanupPartialDownload(string browserName)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(browserName);

        try
        {
            var downloadPath = ResolveDownloadPath(browserName);

            if (_fileSystemPort.FileExists(downloadPath))
            {
                _fileSystemPort.DeleteFile(downloadPath);
            }
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException or ObjectDisposedException)
        {
            Debug.WriteLine(exception);
        }
    }

    public Task<string> DownloadInstallerAsync(
        string browserName,
        string downloadUrl,
        IProgress<DownloadProgressStatus> progress,
        CancellationToken cancellationToken)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(browserName);
        ArgumentException.ThrowIfNullOrWhiteSpace(downloadUrl);
        ArgumentNullException.ThrowIfNull(progress);

        return DownloadInstallerCoreAsync(browserName, downloadUrl, progress, cancellationToken);
    }

    public Task StartInstallerAsync(string installerPath)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(installerPath);

        return StartInstallerCoreAsync(installerPath);
    }

    private async Task<string> DownloadInstallerCoreAsync(
        string browserName,
        string downloadUrl,
        IProgress<DownloadProgressStatus> progress,
        CancellationToken cancellationToken)
    {
        var downloadPath = ResolveDownloadPath(browserName);

        await _downloadService.DownloadFileAsync(
            downloadUrl,
            downloadPath,
            progress,
            cancellationToken).ConfigureAwait(false);

        return downloadPath;
    }

    private string ResolveDownloadPath(string browserName)
    {
        var downloadsFolder = _pathProvider.RetrieveDownloadsPath();
        var fileName = $"{browserName}{InstallerFileNameSuffix}";

        return _fileSystemPort.CombinePaths(downloadsFolder, fileName);
    }

    private async Task StartInstallerCoreAsync(string installerPath)
    {
        await _processControlPort.StartExecutableAsync(installerPath).ConfigureAwait(false);
    }
}
