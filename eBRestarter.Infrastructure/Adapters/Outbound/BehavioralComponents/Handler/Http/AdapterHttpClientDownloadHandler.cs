using System;
using System.Buffers;
using System.Diagnostics;
using System.IO;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;

using eBRestarter.Core.Application.ObjectArchetypes.DTOs.Records;
using eBRestarter.Core.Application.Ports.Outbound.Interfaces.Network;

namespace eBRestarter.Infrastructure.Adapters.Outbound.BehavioralComponents.Handler.Http;

public sealed class AdapterHttpClientDownloadHandler : IOutboundPortHttpDownload
{
    private const int BufferSizeBytes = 8192;
    private const string DefaultUserAgentHeaderValue = "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/144.0.0.0 Safari/537.36";
    private const double PercentageMultiplier = 100.0;
    private const int UpdateIntervalFrequencyDivider = 10;
    private const long UnknownContentLength = -1L;
    private const string UserAgentHeaderName = "User-Agent";

    private readonly HttpClient _httpClient;


    public AdapterHttpClientDownloadHandler(HttpClient? httpClient = null)
    {
        _httpClient = httpClient ?? new HttpClient();

        if (!_httpClient.DefaultRequestHeaders.Contains(UserAgentHeaderName))
        {
            _httpClient.DefaultRequestHeaders.Add(UserAgentHeaderName, DefaultUserAgentHeaderValue);
        }
    }


    public Task DownloadFileAsync(
        string url,
        string destinationPath,
        IProgress<DownloadProgressStatus> progress,
        CancellationToken cancel)
    {
        ArgumentNullException.ThrowIfNull(url);
        ArgumentNullException.ThrowIfNull(destinationPath);

        return DownloadFileCoreAsync(url, destinationPath, progress, cancel);
    }

    private async Task DownloadFileCoreAsync(
        string url,
        string destinationPath,
        IProgress<DownloadProgressStatus> progress,
        CancellationToken cancel)
    {
        using var response = await _httpClient.GetAsync(url, HttpCompletionOption.ResponseHeadersRead, cancel).ConfigureAwait(false);
        response.EnsureSuccessStatusCode();

        var totalBytes = response.Content.Headers.ContentLength ?? UnknownContentLength;
        var canReportProgress = totalBytes != UnknownContentLength;

        var contentStream = await response.Content.ReadAsStreamAsync(cancel).ConfigureAwait(false);
        await using (contentStream.ConfigureAwait(false))
        {
            var fileStream = new FileStream(destinationPath, FileMode.Create, FileAccess.Write, FileShare.None, BufferSizeBytes, useAsync: true);
            await using (fileStream.ConfigureAwait(false))
            {
                byte[] buffer = ArrayPool<byte>.Shared.Rent(BufferSizeBytes);
                try
                {
                    long totalRead = 0;
                    int bytesRead;

                    long startTimestamp = Stopwatch.GetTimestamp();
                    long lastUpdateTicks = 0;
                    long updateIntervalTicks = Stopwatch.Frequency / UpdateIntervalFrequencyDivider;

                    while ((bytesRead = await contentStream.ReadAsync(buffer.AsMemory(0, BufferSizeBytes), cancel).ConfigureAwait(false)) > 0)
                    {
                        await fileStream.WriteAsync(buffer.AsMemory(0, bytesRead), cancel).ConfigureAwait(false);

                        totalRead += bytesRead;

                        if (canReportProgress && progress is not null)
                        {
                            long currentTicks = Stopwatch.GetElapsedTime(startTimestamp).Ticks;

                            if (currentTicks - lastUpdateTicks > updateIntervalTicks || totalRead == totalBytes)
                            {
                                lastUpdateTicks = currentTicks;

                                var percentage = (double)totalRead / totalBytes * PercentageMultiplier;

                                progress.Report(new DownloadProgressStatus(totalRead, totalBytes, percentage));
                            }
                        }
                    }
                }
                finally
                {
                    ArrayPool<byte>.Shared.Return(buffer);
                }
            }
        }
    }
}
