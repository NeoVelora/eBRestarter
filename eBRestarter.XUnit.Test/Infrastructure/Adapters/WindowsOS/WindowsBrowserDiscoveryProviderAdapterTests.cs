using eBRestarter.Core.Application.Ports.Outbound.Browser;
using eBRestarter.Infrastructure.Adapters.WindowsOS;
using eBRestarter.Core.Application.Enums;
using eBRestarter.Core.Application.Models;
using Moq;
using Shouldly;
using System;
using System.Linq;
using System.Threading.Tasks;
using Xunit;

namespace eBRestarter.Tests.Infrastructure.Adapters.WindowsOS
{
    public class WindowsBrowserDiscoveryProviderAdapterTests
    {
        private readonly Mock<IBrowserFactoryPort> _mockBrowserFactory;

        public WindowsBrowserDiscoveryProviderAdapterTests()
        {
            _mockBrowserFactory = new Mock<IBrowserFactoryPort>();
        }

        [Fact]
        public void Constructor_ShouldThrowArgumentNullException_WhenFactoryIsNull()
        {
            Action act = () => new WindowsBrowserDiscoveryProviderAdapter(null!, Microsoft.Extensions.Logging.Abstractions.NullLogger<WindowsBrowserDiscoveryProviderAdapter>.Instance);

            act.ShouldThrow<ArgumentNullException>().ParamName.ShouldBe("BrowserFactory");
        }

        [Fact]
        public async Task GetInstalledBrowsersAsync_ShouldMapInstalledBrowserCorrectly()
        {
            var service = new WindowsBrowserDiscoveryProviderAdapter(_mockBrowserFactory.Object, Microsoft.Extensions.Logging.Abstractions.NullLogger<WindowsBrowserDiscoveryProviderAdapter>.Instance);
            int enumCount = Enum.GetValues<BrowserType>().Length;

            var mockInstalledBrowser = new Mock<IBrowserPort>();
            mockInstalledBrowser.Setup(b => b.IsInstalled).Returns(true);
            mockInstalledBrowser.Setup(b => b.BrowserVersion).Returns("120.0.6099.109");
            mockInstalledBrowser.Setup(b => b.DisplayName).Returns("Mocked Browser");
            mockInstalledBrowser.Setup(b => b.IconPath).Returns(@"C:\icon.exe");
            mockInstalledBrowser.Setup(b => b.DownloadUrl).Returns("https://download.com");

            _mockBrowserFactory
                .Setup(f => f.Create(It.IsAny<BrowserType>()))
                .Returns(mockInstalledBrowser.Object);

            var result = await service.FindInstalledBrowsersAsync();

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
            var service = new WindowsBrowserDiscoveryProviderAdapter(_mockBrowserFactory.Object, Microsoft.Extensions.Logging.Abstractions.NullLogger<WindowsBrowserDiscoveryProviderAdapter>.Instance);

            var mockUninstalledBrowser = new Mock<IBrowserPort>();
            mockUninstalledBrowser.Setup(b => b.IsInstalled).Returns(false);
            mockUninstalledBrowser.Setup(b => b.BrowserVersion).Returns("Sollte ignoriert werden");
            mockUninstalledBrowser.Setup(b => b.DisplayName).Returns("Missing Browser");

            _mockBrowserFactory
                .Setup(f => f.Create(It.IsAny<BrowserType>()))
                .Returns(mockUninstalledBrowser.Object);

            var result = await service.FindInstalledBrowsersAsync();

            var firstBrowser = result.First();
            firstBrowser.IsInstalled.ShouldBeFalse();

            firstBrowser.Version.ShouldBe("Not installed");
        }

        [Fact]
        public async Task GetInstalledBrowsersAsync_ShouldSkipBrowser_WhenFactoryThrowsNotSupportedException()
        {
            var service = new WindowsBrowserDiscoveryProviderAdapter(_mockBrowserFactory.Object, Microsoft.Extensions.Logging.Abstractions.NullLogger<WindowsBrowserDiscoveryProviderAdapter>.Instance);

            _mockBrowserFactory
                .Setup(f => f.Create(It.IsAny<BrowserType>()))
                .Throws<NotSupportedException>();

            var result = await service.FindInstalledBrowsersAsync();

            result.ShouldBeEmpty();
        }

        [Fact]
        public async Task GetInstalledBrowsersAsync_ShouldHandleGeneralExceptions_Gracefully()
        {
            var service = new WindowsBrowserDiscoveryProviderAdapter(_mockBrowserFactory.Object, Microsoft.Extensions.Logging.Abstractions.NullLogger<WindowsBrowserDiscoveryProviderAdapter>.Instance);

            var buggyBrowserMock = new Mock<IBrowserPort>();
            buggyBrowserMock.Setup(b => b.IsInstalled).Throws(new InvalidOperationException("Registry Error"));

            _mockBrowserFactory
                .Setup(f => f.Create(It.IsAny<BrowserType>()))
                .Returns(buggyBrowserMock.Object);

            var result = await service.FindInstalledBrowsersAsync();

            result.ShouldBeEmpty();
        }
    }
}


