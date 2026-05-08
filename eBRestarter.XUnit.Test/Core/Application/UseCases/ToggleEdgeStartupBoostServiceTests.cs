using eBRestarter.Core.Application.Interfaces.Browser;
using eBRestarter.Core.Application.Interfaces.OperatingSystem.WindowsOS;
using eBRestarter.Core.Application.UseCases.ToggleEdgeStartupBoost;
using eBRestarter.Core.Application.Enums;
using Moq;
using Shouldly;
using System;
using Xunit;

namespace eBRestarter.Tests.Core.Application.UseCases.ToggleEdgeStartupBoost
{
    public class ToggleEdgeStartupBoostServiceTests
    {
        private readonly Mock<IWindowsStartupManagerService> _mockStartupService;
        private readonly Mock<IBrowserFactory> _mockBrowserFactory;
        private readonly Mock<IBrowser> _mockEdgeBrowser;
        private readonly ToggleEdgeStartupBoostService _sut;

        public ToggleEdgeStartupBoostServiceTests()
        {
            _mockStartupService = new Mock<IWindowsStartupManagerService>();
            _mockBrowserFactory = new Mock<IBrowserFactory>();
            _mockEdgeBrowser = new Mock<IBrowser>();

            _mockBrowserFactory
                .Setup(f => f.Create(BrowserType.Edge))
                .Returns(_mockEdgeBrowser.Object);

            _sut = new ToggleEdgeStartupBoostService(
                _mockStartupService.Object,
                _mockBrowserFactory.Object);
        }

        [Theory]
        [InlineData(true)]
        [InlineData(false)]
        public void IsEnabled_ShouldReturnExactStateFromStartupService(bool expectedState)
        {
            _mockStartupService.Setup(s => s.IsEdgeStartupBoostEnabled()).Returns(expectedState);

            var result = _sut.IsEnabled();

            result.ShouldBe(expectedState);
            _mockStartupService.Verify(s => s.IsEdgeStartupBoostEnabled(), Times.Once);
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

            _mockStartupService.Verify(s => s.SetEdgeStartupBoost(targetState), Times.Once);

            result.Success.ShouldBeTrue();
            result.NewState.ShouldBe(targetState);
            result.ErrorMessage.ShouldBeEmpty();
        }

        [Fact]
        public void Toggle_ShouldCatchException_AndReturnFailureResponse()
        {
            _mockStartupService
                .Setup(s => s.SetEdgeStartupBoost(It.IsAny<bool>()))
                .Throws(new UnauthorizedAccessException("Zugriff auf Registry verweigert."));

            var result = _sut.Toggle(true);

            result.Success.ShouldBeFalse();

            result.NewState.ShouldBeFalse();

            result.ErrorMessage.ShouldBe("Zugriff auf Registry verweigert.");
        }
    }
}