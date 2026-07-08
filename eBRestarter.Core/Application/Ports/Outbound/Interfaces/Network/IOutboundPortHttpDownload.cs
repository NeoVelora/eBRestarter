using eBRestarter.Core.Application.ObjectArchetypes.DTOs.Records;

namespace eBRestarter.Core.Application.Ports.Outbound.Interfaces.Network;

public interface IOutboundPortHttpDownload
{
    Task DownloadFileAsync(
        string url,
        string destinationPath,
        IProgress<DownloadProgressStatus> progress,
        CancellationToken cancel);
}
