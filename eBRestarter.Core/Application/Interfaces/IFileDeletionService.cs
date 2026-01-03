using System;
using System.Collections.Generic;
using System.Text;

namespace eBRestarter.Core.Application.Interfaces
{
    public interface IFileDeletionService
    {
        Task<int> CountFilesAsync(List<string> directories);

        Task DeleteFilesAsync(List<string> directories, IProgress<string> statusReporter, IProgress<int> valueReporter, CancellationToken token);

        void DeleteSingleFile(string filePath);
    }
}
