using eBRestarter.Core.Application.Interfaces.Browser;
using eBRestarter.Core.Application.Models.Records;
using System.Diagnostics;

namespace eBRestarter.Infrastructure.Services;

public class HttpClientDownloadService : IBrowserDownloadService
{
    private readonly HttpClient _httpClient;

    public HttpClientDownloadService()
    {
        _httpClient = new HttpClient();

        _httpClient.DefaultRequestHeaders.Add("User-Agent", "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/144.0.0.0 Safari/537.36");

    }

    public async Task DownloadFileAsync(string url, string destinationPath, IProgress<DownloadProgressStatus> progress, CancellationToken cancellationToken)
    {
        using var response = await _httpClient.GetAsync(url, HttpCompletionOption.ResponseHeadersRead, cancellationToken);
        response.EnsureSuccessStatusCode();

        var totalBytes = response.Content.Headers.ContentLength ?? -1L;
        var canReportProgress = totalBytes != -1;

        await using var contentStream = await response.Content.ReadAsStreamAsync(cancellationToken);

        await using var fileStream = new FileStream(destinationPath, FileMode.Create, FileAccess.Write, FileShare.None, 8192, true);

        var buffer = new byte[8192];
        long totalRead = 0;
        int bytesRead;

        var stopwatch = new Stopwatch();
        stopwatch.Start();
        long lastUpdateTicks = 0;
        long updateIntervalTicks = Stopwatch.Frequency / 10;

        while ((bytesRead = await contentStream.ReadAsync(buffer, cancellationToken)) > 0)
        {
            await fileStream.WriteAsync(buffer.AsMemory(0, bytesRead), cancellationToken);
            totalRead += bytesRead;

            if (canReportProgress && progress != null)
            {
                long currentTicks = stopwatch.ElapsedTicks;

                if (currentTicks - lastUpdateTicks > updateIntervalTicks || totalRead == totalBytes)
                {
                    lastUpdateTicks = currentTicks;

                    var percentage = (double)totalRead / totalBytes * 100;
                    progress.Report(new DownloadProgressStatus(totalRead, totalBytes, percentage));
                }
            }
        }
    }
}
