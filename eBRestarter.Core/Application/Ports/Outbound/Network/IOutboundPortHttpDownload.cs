using eBRestarter.Core.Application.ObjectArchetypes.DTOs.Browser;

namespace eBRestarter.Core.Application.Ports.Outbound.Network;

public interface IOutboundPortHttpDownload
{
    Task DownloadFileAsync(
        string url,
        string destinationPath,
        IProgress<DownloadProgressStatus> progress,
        CancellationToken cancel);
}
