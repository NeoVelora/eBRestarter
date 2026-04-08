using eBRestarter.Core.Application.Interfaces;

namespace eBRestarter.Infrastructure.Services;

public class FileDeletionService : IFileDeletionService
{
    public async Task<int> CountFilesAsync(List<string> directories)
    {
        return await Task.Run(() =>
        {
            int count = 0;
            foreach (var dir in directories)
            {
                if (Directory.Exists(dir))
                {
                    try
                    {
                        count += Directory.GetFiles(dir, "*", SearchOption.AllDirectories).Length;
                    }
                    catch { }
                }
            }
            return count;
        });
    }

    public async Task DeleteFilesAsync(List<string> directories, IProgress<string> statusReporter, IProgress<int> valueReporter, CancellationToken token)
    {
        await Task.Run(() =>
        {
            int deletedCount = 0;
            const int reportInterval = 10;
            int updateCounter = 0;

            foreach (var dir in directories)
            {
                if (!Directory.Exists(dir)) continue;

                string[] files;

                try { files = Directory.GetFiles(dir, "*", SearchOption.AllDirectories); }

                catch { continue; }

                foreach (var file in files)
                {
                    if (token.IsCancellationRequested) return;

                    try
                    {
                        if (file.Contains("moz-extension")) continue;

                        File.Delete(file);
                        deletedCount++;
                        updateCounter++;

                        if (updateCounter >= reportInterval)
                        {
                            statusReporter.Report($"Lösche: {Path.GetFileName(file)}");
                            valueReporter.Report(deletedCount);
                            updateCounter = 0;
                        }
                    }
                    catch { }
                }

                if (token.IsCancellationRequested) return;

                try { Directory.Delete(dir, false); } catch { }
            }

            statusReporter.Report("Abschließe Bereinigung...");
            valueReporter.Report(deletedCount);

        }, token);
    }

    public void DeleteSingleFile(string filePath)
    {
        if (File.Exists(filePath))
        {
            try { File.Delete(filePath); } catch { }
        }
    }
}

