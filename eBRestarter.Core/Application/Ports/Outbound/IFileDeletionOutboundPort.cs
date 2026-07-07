namespace eBRestarter.Core.Application.Ports.Outbound;

public interface IFileDeletionOutboundPort
{
    Task<int> CountFilesAsync(List<string> directories);

    Task DeleteFilesAsync(List<string> directories, IProgress<string> statusReporter, IProgress<int> valueReporter, CancellationToken token);

    void DeleteSingleFile(string filePath);
}
