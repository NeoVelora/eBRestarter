using NSubstitute;
using Shouldly;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Xunit;
using eBRestarter.Core.Application.ObjectArchetypes.DTOs.Browser;
using eBRestarter.Core.Application.ObjectArchetypes.Enums;
using eBRestarter.Core.Application.Ports.Inbound.Providers;
using eBRestarter.Core.Application.Ports.Outbound.OperatingSystem;
using eBRestarter.Core.Application.Ports.Outbound.Browser;
using eBRestarter.Core.Application.Ports.Inbound.UseCases.Browser;
using eBRestarter.Infrastructure.BehavioralComponents.Validators;
using NSubstitute.ExceptionExtensions;

using eBRestarter.XUnit.Test.Shared.TestDoubles;

using eBRestarter.Core.Application.ObjectArchetypes.Constants;
using eBRestarter.Core.Application.Ports.Outbound.Logging;

namespace eBRestarter.XUnit.Test.UnitTests.Core.Application.UseCases
{
    [Trait("Category", "Unit")]
    public class DeleteBrowserContentUseCaseTests
    {
        private readonly IOutboundPortBrowserFactory _mockBrowserFactory;
        private readonly IOutboundPortFileDeletion _mockFileDeletionService;
        private readonly IOutboundPortOsProcessControl _mockProcessService;
        private readonly IInboundPortLocalizationProvider _mockLocalizationService;
        private readonly IOutboundPortApplicationLogger<DeleteBrowserContentUseCase> _mockLogger;
        private readonly DeleteBrowserContentUseCase _sut;

        public DeleteBrowserContentUseCaseTests()
        {
            _mockBrowserFactory = Substitute.For<IOutboundPortBrowserFactory>();
            _mockFileDeletionService = Substitute.For<IOutboundPortFileDeletion>();
            _mockProcessService = Substitute.For<IOutboundPortOsProcessControl>();
            _mockLocalizationService = Substitute.For<IInboundPortLocalizationProvider>();
            _mockLogger = Substitute.For<IOutboundPortApplicationLogger<DeleteBrowserContentUseCase>>();

            _mockLocalizationService
                .RetrieveString(Arg.Any<string>())
                .Returns(ci => ci.Arg<string>());

            _sut =
            new DeleteBrowserContentUseCase(
                _mockBrowserFactory,
                _mockFileDeletionService,
                _mockLocalizationService,
                _mockProcessService,
                TimeProvider.System,
                new AdapterFluentValidationWrapper<DeleteBrowserContentRequest>(new DeleteBrowserContentValidator()),
                _mockLogger);
        }

        [Fact]
        public async Task ExecuteAsync_ShouldThrowArgumentNullException_WhenRequestIsNull()
        {
            var progress = new Progress<DeleteBrowserContentProgress>();
            await Should.ThrowAsync<ArgumentNullException>(() => _sut.ExecuteAsync(null!, progress, CancellationToken.None));
        }

        [Fact]
        public async Task ExecuteAsync_ShouldThrowArgumentNullException_WhenProgressIsNull()
        {
            var request = new DeleteBrowserContentRequest(BrowserType.Chrome, true, true, ForceCloseProcess: false);
            await Should.ThrowAsync<ArgumentNullException>(() => _sut.ExecuteAsync(request, null!, CancellationToken.None));
        }

        [Fact]
        public async Task ExecuteAsync_ShouldReturnNoPathsFailure_WhenBrowserPathsAreEmpty()
        {
            var request = new DeleteBrowserContentRequest(BrowserType.Chrome, DeleteCookies: true, DeleteCache: true, ForceCloseProcess: false);
            var progress = new Progress<DeleteBrowserContentProgress>();

            var mockBrowser = Substitute.For<IOutboundPortBrowser>();
            mockBrowser.ProcessName.Returns("chrome");
            mockBrowser.ResolvePaths().Returns(new BrowserPaths([], [], []));
            _mockBrowserFactory.Create(BrowserType.Chrome).Returns(mockBrowser);

            _mockProcessService.IsProcessAlive("chrome").Returns(false);

            var result = await _sut.ExecuteAsync(request, progress, CancellationToken.None);

            (result is DeleteBrowserContentFailure failure && failure.Message == "Cleanup_NoPaths").ShouldBeTrue();
            await _mockFileDeletionService.DidNotReceive().DeleteFilesAsync(Arg.Any<List<string>>(), Arg.Any<IProgress<string>>(), Arg.Any<IProgress<int>>(), Arg.Any<CancellationToken>());
        }

        [Fact]
        public async Task ExecuteAsync_ShouldReturnProcessConflict_WhenBrowserIsStillRunning()
        {
            var request = new DeleteBrowserContentRequest(BrowserType.Chrome, true, true, ForceCloseProcess: false);
            var progress = new Progress<DeleteBrowserContentProgress>();

            var mockBrowser = Substitute.For<IOutboundPortBrowser>();
            mockBrowser.ProcessName.Returns("chrome");
            _mockBrowserFactory.Create(BrowserType.Chrome).Returns(mockBrowser);

            _mockProcessService.IsProcessAlive("chrome").Returns(true);

            var result = await _sut.ExecuteAsync(request, progress, CancellationToken.None);

            (result is DeleteBrowserContentProcessConflict).ShouldBeTrue();

            await _mockFileDeletionService.DidNotReceive().DeleteFilesAsync(Arg.Any<List<string>>(), Arg.Any<IProgress<string>>(), Arg.Any<IProgress<int>>(), Arg.Any<CancellationToken>());
        }

        [Fact]
        public async Task ExecuteAsync_ShouldForceCloseProcess_WhenRequested()
        {
            var request = new DeleteBrowserContentRequest(BrowserType.Edge, true, true, ForceCloseProcess: true);
            var progressUpdates = new List<DeleteBrowserContentProgress>();
            var progress = new Progress<DeleteBrowserContentProgress>(p => progressUpdates.Add(p));

            var mockBrowser = Substitute.For<IOutboundPortBrowser>();
            mockBrowser.ProcessName.Returns("msedge");

            mockBrowser.ResolvePaths().Returns(new BrowserPaths(
                new List<string> { "C:\\Cache" },
                new List<string>(),
                new List<string>()));

            _mockBrowserFactory.Create(BrowserType.Edge).Returns(mockBrowser);

            _mockProcessService.IsProcessAlive("msedge").Returns(false);

            var result = await _sut.ExecuteAsync(request, progress, CancellationToken.None);

            _mockProcessService.Received(1).CloseApplication("msedge");

            progressUpdates.ShouldContain(p => p.StatusMessage == "Cleanup_ClosingBrowser");
        }

        [Fact]
        public async Task ExecuteAsync_ShouldReturnNoPathsError_WhenNothingIsSelectedOrAvailable()
        {
            var request = new DeleteBrowserContentRequest(BrowserType.Firefox, DeleteCookies: false, DeleteCache: false, ForceCloseProcess: false);
            var progress = new Progress<DeleteBrowserContentProgress>();

            var mockBrowser = Substitute.For<IOutboundPortBrowser>();
            mockBrowser.ProcessName.Returns("firefox");

            mockBrowser.ResolvePaths().Returns(new BrowserPaths(
                new List<string> { "C:\\Cache" },
                new List<string> { "C:\\Cookies" },
                new List<string>()));

            _mockBrowserFactory.Create(BrowserType.Firefox).Returns(mockBrowser);

            _mockProcessService.IsProcessAlive("firefox").Returns(false);

            var result = await _sut.ExecuteAsync(request, progress, CancellationToken.None);

            (result is DeleteBrowserContentFailure failure && failure.Message == "At least one deletion option (Cache or Cookies) must be selected.").ShouldBeTrue();
        }

        [Fact]
        public async Task ExecuteAsync_ShouldCallDeletionService_WithCombinedPaths()
        {
            var request = new DeleteBrowserContentRequest(BrowserType.Brave, DeleteCookies: true, DeleteCache: true, ForceCloseProcess: false);
            var progressUpdates = new List<DeleteBrowserContentProgress>();
            var progress = new SynchronousProgress<DeleteBrowserContentProgress>(progressUpdates.Add);

            var mockBrowser = Substitute.For<IOutboundPortBrowser>();
            mockBrowser.ProcessName.Returns("brave");

            mockBrowser.ResolvePaths().Returns(new BrowserPaths(
                new List<string> { "C:\\Brave\\Cache" },
                new List<string> { "C:\\Brave\\Cookies" },
                new List<string>()));

            _mockBrowserFactory.Create(BrowserType.Brave).Returns(mockBrowser);

            _mockProcessService.IsProcessAlive("brave").Returns(false);

            var result = await _sut.ExecuteAsync(request, progress, CancellationToken.None);

            (result is DeleteBrowserContentSuccess).ShouldBeTrue();

            await _mockFileDeletionService.Received(1).DeleteFilesAsync(
                Arg.Is<List<string>>(list => list.Contains("C:\\Brave\\Cache") && list.Contains("C:\\Brave\\Cookies") && list.Count == 2),
                Arg.Any<IProgress<string>>(),
                Arg.Any<IProgress<int>>(),
                Arg.Any<CancellationToken>());

            progressUpdates.ShouldContain(p => p.StatusMessage == "Cleanup_Finished" && p.TotalSteps == 2 && p.CompletedSteps == 2);
        }

        [Fact]
        public async Task ExecuteAsync_ShouldCatchExceptions_AndReturnFailure()
        {
            var request = new DeleteBrowserContentRequest(BrowserType.Vivaldi, true, true, false);
            var progress = new Progress<DeleteBrowserContentProgress>();
            var expectedException = new UnauthorizedAccessException("Zugriff verweigert");

            _mockBrowserFactory.Create(Arg.Any<BrowserType>()).Throws(expectedException);

            var result = await _sut.ExecuteAsync(request, progress, CancellationToken.None);

            (result is DeleteBrowserContentFailure failure && failure.Message == "Cleanup_Failed").ShouldBeTrue();
            _mockLogger.Received(1).LogError(
                LogEventIds.Browser.BrowserCleanupFailed,
                expectedException,
                Arg.Any<string>(),
                Arg.Any<object[]>());
        }
    }
}


