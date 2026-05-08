using eBRestarter.Core.Application.Interfaces.Browser;
using eBRestarter.Core.Application.Enums;
using eBRestarter.Core.Application.Models;
using eBRestarter.Infrastructure.Services.WindowsOS;
using Moq;
using Shouldly;
using System;
using System.Linq;
using System.Threading.Tasks;
using Xunit;

namespace eBRestarter.Tests.Infrastructure.Services.WindowsOS
{
    public class WindowsBrowserServiceTests
    {
        private readonly Mock<IBrowserFactory> _mockBrowserFactory;

        public WindowsBrowserServiceTests()
        {
            _mockBrowserFactory = new Mock<IBrowserFactory>();
        }

        [Fact]
        public void Constructor_ShouldThrowArgumentNullException_WhenFactoryIsNull()
        {
            Action act = () => new WindowsBrowserService(null!);

            act.ShouldThrow<ArgumentNullException>().ParamName.ShouldBe("browserFactory");
        }

        [Fact]
        public async Task GetInstalledBrowsersAsync_ShouldMapInstalledBrowserCorrectly()
        {
            var service = new WindowsBrowserService(_mockBrowserFactory.Object);
            int enumCount = Enum.GetValues<BrowserType>().Length;

            var mockInstalledBrowser = new Mock<IBrowser>();
            mockInstalledBrowser.Setup(b => b.IsInstalled).Returns(true);
            mockInstalledBrowser.Setup(b => b.BrowserVersion).Returns("120.0.6099.109");
            mockInstalledBrowser.Setup(b => b.DisplayName).Returns("Mocked Browser");
            mockInstalledBrowser.Setup(b => b.IconPath).Returns(@"C:\icon.exe");
            mockInstalledBrowser.Setup(b => b.DownloadUrl).Returns("https://download.com");

            _mockBrowserFactory
                .Setup(f => f.Create(It.IsAny<BrowserType>()))
                .Returns(mockInstalledBrowser.Object);

            var result = await service.GetInstalledBrowsersAsync();

            var browserList = result.ToList();

            browserList.Count.ShouldBe(enumCount);

            var firstBrowser = browserList.First();
            firstBrowser.IsInstalled.ShouldBeTrue();
            firstBrowser.Version.ShouldBe("120.0.6099.109");
            firstBrowser.Name.ShouldBe("Mocked Browser");
            firstBrowser.IconPath.ShouldBe(@"C:\icon.exe");
            firstBrowser.DownloadUrl.ShouldBe("https://download.com");
        }

        [Fact]
        public async Task GetInstalledBrowsersAsync_ShouldMapUninstalledBrowserCorrectly()
        {
            var service = new WindowsBrowserService(_mockBrowserFactory.Object);

            var mockUninstalledBrowser = new Mock<IBrowser>();
            mockUninstalledBrowser.Setup(b => b.IsInstalled).Returns(false);
            mockUninstalledBrowser.Setup(b => b.BrowserVersion).Returns("Sollte ignoriert werden");
            mockUninstalledBrowser.Setup(b => b.DisplayName).Returns("Missing Browser");

            _mockBrowserFactory
                .Setup(f => f.Create(It.IsAny<BrowserType>()))
                .Returns(mockUninstalledBrowser.Object);

            var result = await service.GetInstalledBrowsersAsync();

            var firstBrowser = result.First();
            firstBrowser.IsInstalled.ShouldBeFalse();

            firstBrowser.Version.ShouldBe("Nicht installiert");
        }

        [Fact]
        public async Task GetInstalledBrowsersAsync_ShouldSkipBrowser_WhenFactoryThrowsNotSupportedException()
        {
            var service = new WindowsBrowserService(_mockBrowserFactory.Object);

            _mockBrowserFactory
                .Setup(f => f.Create(It.IsAny<BrowserType>()))
                .Throws<NotSupportedException>();

            var result = await service.GetInstalledBrowsersAsync();

            result.ShouldBeEmpty();
        }

        [Fact]
        public async Task GetInstalledBrowsersAsync_ShouldHandleGeneralExceptions_Gracefully()
        {
            var service = new WindowsBrowserService(_mockBrowserFactory.Object);

            var buggyBrowserMock = new Mock<IBrowser>();
            buggyBrowserMock.Setup(b => b.IsInstalled).Throws(new InvalidOperationException("Registry Error"));

            _mockBrowserFactory
                .Setup(f => f.Create(It.IsAny<BrowserType>()))
                .Returns(buggyBrowserMock.Object);

            var result = await service.GetInstalledBrowsersAsync();

            result.ShouldBeEmpty();
        }
    }
}