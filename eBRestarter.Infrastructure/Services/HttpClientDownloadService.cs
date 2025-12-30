using eBRestarter.Core.Application.Interfaces.Browser;
using eBRestarter.Core.Domain.Models.Records;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;

namespace eBRestarter.Infrastructure.Services
{
    public class HttpClientDownloadService : IBrowserDownloadService
    {
        private readonly HttpClient _httpClient;

        public HttpClientDownloadService()
        {
            _httpClient = new HttpClient();
        }

        public async Task DownloadFileAsync(string url, string destinationPath, IProgress<DownloadProgressStatus> progress, CancellationToken cancellationToken)
        {
            using var response = await _httpClient.GetAsync(url, HttpCompletionOption.ResponseHeadersRead, cancellationToken);
            response.EnsureSuccessStatusCode();

            var totalBytes = response.Content.Headers.ContentLength ?? -1L;
            var canReportProgress = totalBytes != -1;

            using var contentStream = await response.Content.ReadAsStreamAsync(cancellationToken);

            using var fileStream = new FileStream(destinationPath, FileMode.Create, FileAccess.Write, FileShare.None, 8192, true);

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
}
