using NSubstitute;
using Shouldly;
using System;
using Xunit;
using eBRestarter.Core.Application.ObjectArchetypes.Constants;
using eBRestarter.Core.Application.ObjectArchetypes.Enums;
using eBRestarter.Core.Application.Ports.Outbound.Browser;
using eBRestarter.Core.Application.Ports.Outbound.OperatingSystem;
using eBRestarter.Core.Application.Ports.Inbound.UseCases.System;
using NSubstitute.ExceptionExtensions;
using eBRestarter.Core.Application.Ports.Outbound.Logging;

namespace eBRestarter.XUnit.Test.UnitTests.Core.Application.UseCases
{
    [Trait("Category", "Unit")]
    public class ToggleEdgeStartupBoostUseCaseTests
    {
        private readonly IOutboundPortBrowserConfigRepository _mockStartupService;
        private readonly IOutboundPortBrowserFactory _mockBrowserFactory;
        private readonly IOutboundPortBrowser _mockEdgeBrowser;
        private readonly IOutboundPortApplicationLogger<ToggleEdgeStartupBoostUseCase> _mockLogger;
        private readonly ToggleEdgeStartupBoostUseCase _sut;

        public ToggleEdgeStartupBoostUseCaseTests()
        {
            _mockStartupService = Substitute.For<IOutboundPortBrowserConfigRepository>();
            _mockBrowserFactory = Substitute.For<IOutboundPortBrowserFactory>();
            _mockEdgeBrowser = Substitute.For<IOutboundPortBrowser>();
            _mockLogger = Substitute.For<IOutboundPortApplicationLogger<ToggleEdgeStartupBoostUseCase>>();

            _mockBrowserFactory
                .Create(BrowserType.Edge)
                .Returns(_mockEdgeBrowser);

            _sut = new ToggleEdgeStartupBoostUseCase(
                _mockBrowserFactory,
                _mockLogger,
                _mockStartupService);
        }

        [Theory]
        [InlineData(true)]
        [InlineData(false)]
        public void IsEnabled_ShouldReturnExactStateFromStartupService(bool expectedState)
        {
            _mockStartupService.IsBrowserStartupBoostEnabled().Returns(expectedState);

            var result = _sut.IsEnabled();

            result.ShouldBe(expectedState);
            _mockStartupService.Received(1).IsBrowserStartupBoostEnabled();
        }

        [Theory]
        [InlineData(true)]
        [InlineData(false)]
        public void IsEdgeInstalled_ShouldReturnStateFromEdgeBrowserInstance(bool isInstalled)
        {
            _mockEdgeBrowser.IsInstalled.Returns(isInstalled);

            var result = _sut.IsEdgeInstalled();

            result.ShouldBe(isInstalled);

            _mockBrowserFactory.Received(1).Create(BrowserType.Edge);
        }

        [Theory]
        [InlineData(true)]
        [InlineData(false)]
        public void Toggle_ShouldCallStartupService_AndReturnSuccess(bool targetState)
        {
            var result = _sut.Toggle(targetState);

            _mockStartupService.Received(1).SetBrowserStartupBoost(targetState);

            result.Success.ShouldBeTrue();
            result.NewState.ShouldBe(targetState);
            result.ErrorMessage.ShouldBeEmpty();
        }

        [Fact]
        public void Toggle_ShouldCatchException_AndReturnFailureResponse()
        {
            _mockStartupService
                .When(s => s.SetBrowserStartupBoost(Arg.Any<bool>()))
                .Do(_ => throw new UnauthorizedAccessException("Zugriff auf Registry verweigert."));

            var result = _sut.Toggle(true);

            result.Success.ShouldBeFalse();

            result.NewState.ShouldBeFalse();

            result.ErrorMessage.ShouldBe("Edge startup boost configuration could not be changed.");

            _mockLogger.Received(1).LogError(
                LogEventIds.OperatingSystem.EdgeStartupBoostChanged,
                Arg.Any<UnauthorizedAccessException>(),
                Arg.Any<string>(),
                Arg.Any<object[]>());
        }
    }
}


