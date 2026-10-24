using NSubstitute;
using Shouldly;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Xunit;
using eBRestarter.Core.Application.ObjectArchetypes.DTOs.Browser;
using eBRestarter.Infrastructure.Adapters.Outbound.BehavioralComponents.Handler.Http;
using eBRestarter.Tests.TestDoubles;

namespace eBRestarter.Tests.Infrastructure.Handlers
{
    public class HttpClientDownloadHandlerAdapterTests : IDisposable
    {
        private const int RandomSeed = 20260811;

        private readonly string _tempFilePath;

        public HttpClientDownloadHandlerAdapterTests()
        {
            _tempFilePath = Path.Combine(Path.GetTempPath(), $"download_test_{Guid.NewGuid()}.exe");
        }

        public void Dispose()
        {
            if (File.Exists(_tempFilePath))
            {
                File.Delete(_tempFilePath);
            }

            GC.SuppressFinalize(this);
        }

        [Fact]
        public async Task DownloadFileAsync_ServerReturnsContent_WritesExactBytesToDisk()
        {
            var contentBytes = Encoding.UTF8.GetBytes("Dies sind simulierte Binärdaten für den Download-Test.");
            var sut = CreateSut(HttpStatusCode.OK, contentBytes);

            await sut.DownloadFileAsync("https://example.com/setup.exe", _tempFilePath, null!, TestContext.Current.CancellationToken);

            File.Exists(_tempFilePath).ShouldBeTrue();
            (await File.ReadAllBytesAsync(_tempFilePath, TestContext.Current.CancellationToken)).ShouldBe(contentBytes);
        }

        [Fact]
        public async Task DownloadFileAsync_ContentSpansMultipleChunks_ReportsProgressReaching100Percent()
        {
            const int contentSize = 10000;
            var contentBytes = new byte[contentSize];
            new Random(RandomSeed).NextBytes(contentBytes);

            var progressReports = new List<DownloadProgressStatus>();
            var progress = new SynchronousProgress<DownloadProgressStatus>(progressReports.Add);
            var sut = CreateSut(HttpStatusCode.OK, contentBytes);

            await sut.DownloadFileAsync("https://test.com", _tempFilePath, progress, TestContext.Current.CancellationToken);

            progressReports.ShouldNotBeEmpty();
            progressReports.Last().Percentage.ShouldBe(100.0);
            progressReports.Last().TotalBytes.ShouldBe(contentSize);
        }

        [Fact]
        public async Task DownloadFileAsync_ServerReturnsNotFound_ThrowsHttpRequestException()
        {
            var sut = CreateSut(HttpStatusCode.NotFound, []);

            var act = () => sut.DownloadFileAsync("https://test.com/404", _tempFilePath, null!, CancellationToken.None);

            await Should.ThrowAsync<HttpRequestException>(act);
        }

        [Fact]
        public async Task DownloadFileAsync_TokenAlreadyCancelled_ThrowsOperationCanceledException()
        {
            var sut = CreateSut(HttpStatusCode.OK, new byte[1024]);
            using var cts = new CancellationTokenSource();
            cts.Cancel();

            var act = () => sut.DownloadFileAsync("https://test.com", _tempFilePath, null!, cts.Token);

            await Should.ThrowAsync<OperationCanceledException>(act);
        }

        [Fact]
        public async Task DownloadFileAsync_WithHttpClientFactory_DownloadsSuccessfully()
        {
            var contentBytes = Encoding.UTF8.GetBytes("Download via IHttpClientFactory integration test payload.");
            var handler = new StubHttpMessageHandler(HttpStatusCode.OK, contentBytes);
            using var httpClient = new HttpClient(handler);
            var factoryMock = Substitute.For<IHttpClientFactory>();
            factoryMock.CreateClient(AdapterHttpClientDownloadHandler.HttpClientName).Returns(httpClient);

            var sut = new AdapterHttpClientDownloadHandler(factoryMock);

            await sut.DownloadFileAsync("https://example.com/payload.bin", _tempFilePath, null!, TestContext.Current.CancellationToken);

            File.Exists(_tempFilePath).ShouldBeTrue();
            (await File.ReadAllBytesAsync(_tempFilePath, TestContext.Current.CancellationToken)).ShouldBe(contentBytes);
        }

        [Fact]
        public void Constructor_NullHttpClientFactory_ThrowsArgumentNullException()
        {
            var act = () => new AdapterHttpClientDownloadHandler((IHttpClientFactory)null!);

            Should.Throw<ArgumentNullException>(act);
        }

        [Fact]
        public void Constructor_NullHttpClient_ThrowsArgumentNullException()
        {
            var act = () => new AdapterHttpClientDownloadHandler((HttpClient)null!);

            Should.Throw<ArgumentNullException>(act);
        }

        private static AdapterHttpClientDownloadHandler CreateSut(HttpStatusCode statusCode, byte[] content)
        {
            var handler = new StubHttpMessageHandler(statusCode, content);

            return new AdapterHttpClientDownloadHandler(new HttpClient(handler));
        }

        private sealed class StubHttpMessageHandler(HttpStatusCode statusCode, byte[] content) : HttpMessageHandler
        {
            protected override Task<HttpResponseMessage> SendAsync(
                HttpRequestMessage request,
                CancellationToken cancellationToken)
            {
                cancellationToken.ThrowIfCancellationRequested();

                return Task.FromResult(new HttpResponseMessage
                {
                    StatusCode = statusCode,
                    Content = new ByteArrayContent(content)
                });
            }
        }
    }
}
