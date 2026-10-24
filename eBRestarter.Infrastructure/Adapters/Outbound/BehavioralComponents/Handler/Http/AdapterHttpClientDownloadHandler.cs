using System;
using System.Buffers;
using System.Diagnostics;
using System.IO;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;

using Microsoft.Extensions.DependencyInjection;

using eBRestarter.Core.Application.ObjectArchetypes.DTOs.Browser;
using eBRestarter.Core.Application.Ports.Outbound.Network;

namespace eBRestarter.Infrastructure.Adapters.Outbound.BehavioralComponents.Handler.Http;

public sealed class AdapterHttpClientDownloadHandler : IOutboundPortHttpDownload
{
    private const int BufferSizeBytes = 8192;
    private const string DefaultUserAgentHeaderValue = "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/144.0.0.0 Safari/537.36";
    private const double PercentageMultiplier = 100.0;
    private const int UpdateIntervalFrequencyDivider = 10;
    private const long UnknownContentLength = -1L;
    private const string UserAgentHeaderName = "User-Agent";

    private const string InsecureSchemeExceptionMessage = "Only HTTPS downloads are permitted. Rejected URL scheme: ";

    private const long MaximumDownloadSizeBytes = 512L * 1024 * 1024;

    private const string SizeLimitExceededExceptionMessage = "The download exceeds the permitted maximum size of 512 MB and was aborted.";

    public const string HttpClientName = "eBRestarter.DownloadHandler";

    private readonly HttpClient? _httpClient;
    private readonly IHttpClientFactory? _httpClientFactory;


    [ActivatorUtilitiesConstructor]
    public AdapterHttpClientDownloadHandler(IHttpClientFactory httpClientFactory)
    {
        ArgumentNullException.ThrowIfNull(httpClientFactory);
        _httpClientFactory = httpClientFactory;
    }

    public AdapterHttpClientDownloadHandler(HttpClient httpClient)
    {
        ArgumentNullException.ThrowIfNull(httpClient);
        _httpClient = httpClient;

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

        if (!Uri.TryCreate(url, UriKind.Absolute, out Uri? requestUri)
            || !string.Equals(requestUri.Scheme, Uri.UriSchemeHttps, StringComparison.Ordinal))
        {
            throw new ArgumentException($"{InsecureSchemeExceptionMessage}{url}", nameof(url));
        }

        return DownloadFileCoreAsync(url, destinationPath, progress, cancel);
    }

    private HttpClient CreateHttpClient()
    {
        if (_httpClient is not null)
        {
            return _httpClient;
        }

        HttpClient client = _httpClientFactory!.CreateClient(HttpClientName);
        if (!client.DefaultRequestHeaders.Contains(UserAgentHeaderName))
        {
            client.DefaultRequestHeaders.Add(UserAgentHeaderName, DefaultUserAgentHeaderValue);
        }

        return client;
    }

    private async Task DownloadFileCoreAsync(
        string url,
        string destinationPath,
        IProgress<DownloadProgressStatus> progress,
        CancellationToken cancel)
    {
        HttpClient httpClient = CreateHttpClient();

        using var response = await httpClient.GetAsync(url, HttpCompletionOption.ResponseHeadersRead, cancel).ConfigureAwait(false);
        response.EnsureSuccessStatusCode();

        var totalBytes = response.Content.Headers.ContentLength ?? UnknownContentLength;
        var canReportProgress = totalBytes != UnknownContentLength;

        if (totalBytes > MaximumDownloadSizeBytes)
        {
            throw new InvalidOperationException(SizeLimitExceededExceptionMessage);
        }

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
                        totalRead += bytesRead;

                        if (totalRead > MaximumDownloadSizeBytes)
                        {
                            throw new InvalidOperationException(SizeLimitExceededExceptionMessage);
                        }

                        await fileStream.WriteAsync(buffer.AsMemory(0, bytesRead), cancel).ConfigureAwait(false);

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
