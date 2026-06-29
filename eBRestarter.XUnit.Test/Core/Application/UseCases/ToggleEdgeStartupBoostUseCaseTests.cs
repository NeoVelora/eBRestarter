using eBRestarter.Core.Application.Ports.Outbound.OperatingSystem;
using eBRestarter.Core.Application.Ports.Outbound.Browser;
using eBRestarter.Core.Application.Ports.Outbound.Network;
using eBRestarter.Core.Application.Ports.Outbound.Network;
using eBRestarter.Core.Application.Ports.Inbound.UseCases.ToggleEdgeStartupBoost;
using eBRestarter.Core.Application.UseCases.ToggleEdgeStartupBoost;
using eBRestarter.Core.Application.Models.Errors;
using eBRestarter.Core.Application.Models.Records;
using eBRestarter.Core.Application.Enums;
using Moq;
using Shouldly;
using System;
using Xunit;

namespace eBRestarter.Tests.Core.Application.UseCases.ToggleEdgeStartupBoost
{
    public class ToggleEdgeStartupBoostUseCaseTests
    {
        private readonly Mock<IBrowserConfigPort> _mockStartupService;
        private readonly Mock<IBrowserFactoryPort> _mockBrowserFactory;
        private readonly Mock<IBrowserPort> _mockEdgeBrowser;
        private readonly ToggleEdgeStartupBoostUseCase _sut;

        public ToggleEdgeStartupBoostUseCaseTests()
        {
            _mockStartupService = new Mock<IBrowserConfigPort>();
            _mockBrowserFactory = new Mock<IBrowserFactoryPort>();
            _mockEdgeBrowser = new Mock<IBrowserPort>();

            _mockBrowserFactory
                .Setup(f => f.Create(BrowserType.Edge))
                .Returns(_mockEdgeBrowser.Object);

            _sut = new ToggleEdgeStartupBoostUseCase(
                _mockStartupService.Object,
                _mockBrowserFactory.Object);
        }

        [Theory]
        [InlineData(true)]
        [InlineData(false)]
        public void IsEnabled_ShouldReturnExactStateFromStartupService(bool expectedState)
        {
            _mockStartupService.Setup(s => s.IsBrowserStartupBoostEnabled()).Returns(expectedState);

            var result = _sut.IsEnabled();

            result.ShouldBe(expectedState);
            _mockStartupService.Verify(s => s.IsBrowserStartupBoostEnabled(), Times.Once);
        }

        [Theory]
        [InlineData(true)]
        [InlineData(false)]
        public void IsEdgeInstalled_ShouldReturnStateFromEdgeBrowserInstance(bool isInstalled)
        {
            _mockEdgeBrowser.Setup(b => b.IsInstalled).Returns(isInstalled);

            var result = _sut.IsEdgeInstalled();

            result.ShouldBe(isInstalled);

            _mockBrowserFactory.Verify(f => f.Create(BrowserType.Edge), Times.Once);
        }

        [Theory]
        [InlineData(true)]
        [InlineData(false)]
        public void Toggle_ShouldCallStartupService_AndReturnSuccess(bool targetState)
        {
            var result = _sut.Toggle(targetState);

            _mockStartupService.Verify(s => s.SetBrowserStartupBoost(targetState), Times.Once);

            result.Success.ShouldBeTrue();
            result.NewState.ShouldBe(targetState);
            result.ErrorMessage.ShouldBeEmpty();
        }

        [Fact]
        public void Toggle_ShouldCatchException_AndReturnFailureResponse()
        {
            _mockStartupService
                .Setup(s => s.SetBrowserStartupBoost(It.IsAny<bool>()))
                .Throws(new UnauthorizedAccessException("Zugriff auf Registry verweigert."));

            var result = _sut.Toggle(true);

            result.Success.ShouldBeFalse();

            result.NewState.ShouldBeFalse();

            result.ErrorMessage.ShouldBe("Zugriff auf Registry verweigert.");
        }
    }
}


