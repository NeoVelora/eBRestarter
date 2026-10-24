using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace eBRestarter.Core.Application.Ports.Outbound.OperatingSystem;

public interface IOutboundPortFileDeletion
{
    Task DeleteFilesAsync(
        List<string> directories,
        IProgress<string> statusReporter,
        IProgress<int> completedDirectoriesReporter,
        CancellationToken token);

    void DeleteSingleFile(string filePath);
}
