using eBRestarter.Core.Application.Models.Records;
using eBRestarter.Infrastructure.Services;
using Moq;
using Moq.Protected;
using Shouldly;
using System.Net;
using System.Text;
using Xunit;

namespace eBRestarter.Tests.Infrastructure.Services
{
    public class HttpClientDownloadServiceTests : IDisposable
    {
        private readonly string _tempFilePath;
        private readonly Mock<HttpMessageHandler> _handlerMock;

        public HttpClientDownloadServiceTests()
        {
            _tempFilePath = Path.Combine(Path.GetTempPath(), $"download_test_{Guid.NewGuid()}.exe");
            _handlerMock = new Mock<HttpMessageHandler>(MockBehavior.Strict);
        }

        public void Dispose()
        {
            if (File.Exists(_tempFilePath)) File.Delete(_tempFilePath);
        }


        [Fact]
        public async Task DownloadFileAsync_ShouldWriteContentToFileSystem()
        {
            var testData = "Dies sind simulierte Binärdaten für den Download-Test.";
            var contentBytes = Encoding.UTF8.GetBytes(testData);

            SetupMockResponse(HttpStatusCode.OK, contentBytes);

            var sut = CreateServiceWithMockHandler();

            await sut.DownloadFileAsync(
                "https://example.com/setup.exe",
                _tempFilePath,
                null!,
                CancellationToken.None);

            File.Exists(_tempFilePath).ShouldBeTrue();
            var downloadedData = await File.ReadAllBytesAsync(_tempFilePath);
            downloadedData.ShouldBe(contentBytes);
        }


        [Fact]
        public async Task DownloadFileAsync_ShouldReportProgress_AndReach100Percent()
        {
            var contentSize = 10000;
            var contentBytes = new byte[contentSize];
            new Random().NextBytes(contentBytes);

            SetupMockResponse(HttpStatusCode.OK, contentBytes);

            var progressReports = new List<DownloadProgressStatus>();
            var progressMock = new Progress<DownloadProgressStatus>(p => progressReports.Add(p));

            var sut = CreateServiceWithMockHandler();

            await sut.DownloadFileAsync("https://test.com", _tempFilePath, progressMock, CancellationToken.None);

            await Task.Delay(100);

            progressReports.ShouldNotBeEmpty();
            var lastReport = progressReports.Last();
            lastReport.Percentage.ShouldBe(100.0);
            lastReport.TotalBytes.ShouldBe(contentSize);
        }


        [Fact]
        public async Task DownloadFileAsync_ShouldThrowException_OnHttpError()
        {
            SetupMockResponse(HttpStatusCode.NotFound, Array.Empty<byte>());
            var sut = CreateServiceWithMockHandler();

            await Should.ThrowAsync<HttpRequestException>(async () =>
                await sut.DownloadFileAsync("https://test.com/404", _tempFilePath, null!, CancellationToken.None));
        }

        [Fact]
        public async Task DownloadFileAsync_ShouldRespectCancellationToken()
        {
            SetupMockResponse(HttpStatusCode.OK, new byte[1024]);
            var sut = CreateServiceWithMockHandler();
            var cts = new CancellationTokenSource();
            cts.Cancel();

            await Should.ThrowAsync<OperationCanceledException>(async () =>
                await sut.DownloadFileAsync("https://test.com", _tempFilePath, null!, cts.Token));
        }


        private void SetupMockResponse(HttpStatusCode statusCode, byte[] content)
        {
            _handlerMock
                .Protected()
                .Setup<Task<HttpResponseMessage>>(
                    "SendAsync",
                    ItExpr.IsAny<HttpRequestMessage>(),
                    ItExpr.IsAny<CancellationToken>()
                )
                .ReturnsAsync(new HttpResponseMessage
                {
                    StatusCode = statusCode,
                    Content = new ByteArrayContent(content)
                });
        }

        private HttpClientDownloadService CreateServiceWithMockHandler()
        {
            var service = new HttpClientDownloadService();
            var client = new HttpClient(_handlerMock.Object);

            var field = typeof(HttpClientDownloadService).GetField("_httpClient",
                System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);

            field?.SetValue(service, client);

            return service;
        }
    }
}