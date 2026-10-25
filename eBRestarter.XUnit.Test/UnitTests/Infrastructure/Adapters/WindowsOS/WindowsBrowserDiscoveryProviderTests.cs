using NSubstitute;
using Shouldly;
using System;
using System.Linq;
using System.Threading.Tasks;
using Xunit;
using eBRestarter.Core.Application.Common.Results;
using eBRestarter.Core.Application.ObjectArchetypes.Constants;
using eBRestarter.Core.Application.ObjectArchetypes.Enums;
using eBRestarter.Core.Application.ObjectArchetypes.Models;
using eBRestarter.Core.Application.Ports.Outbound.Browser;
using eBRestarter.Infrastructure.Adapters.Outbound.BehavioralComponents.Providers.WindowsOS;
using NSubstitute.ExceptionExtensions;

namespace eBRestarter.XUnit.Test.UnitTests.Infrastructure.Adapters.WindowsOS
{
    [Trait("Category", "Unit")]
    public class WindowsBrowserDiscoveryProviderTests
    {
        private readonly IOutboundPortBrowserFactory _mockBrowserFactory;

        public WindowsBrowserDiscoveryProviderTests()
        {
            _mockBrowserFactory = Substitute.For<IOutboundPortBrowserFactory>();
        }

        [Fact]
        public void Constructor_ShouldThrowArgumentNullException_WhenFactoryIsNull()
        {
            var logger = Microsoft.Extensions.Logging.Abstractions.NullLogger<AdapterWindowsBrowserDiscoveryProvider>.Instance;

            Action act = () => new AdapterWindowsBrowserDiscoveryProvider(null!, logger);

            act.ShouldThrow<ArgumentNullException>().ParamName.ShouldBe("browserFactory");
        }

        [Fact]
        public async Task GetInstalledBrowsersAsync_ShouldMapInstalledBrowserCorrectly()
        {
            var service = new AdapterWindowsBrowserDiscoveryProvider(_mockBrowserFactory, Microsoft.Extensions.Logging.Abstractions.NullLogger<AdapterWindowsBrowserDiscoveryProvider>.Instance);
            int enumCount = Enum.GetValues<BrowserType>().Length;

            var mockInstalledBrowser = Substitute.For<IOutboundPortBrowser>();
            mockInstalledBrowser.IsInstalled.Returns(true);
            mockInstalledBrowser.BrowserVersion.Returns("120.0.6099.109");
            mockInstalledBrowser.DisplayName.Returns("Mocked Browser");
            mockInstalledBrowser.IconPath.Returns(@"C:\icon.exe");
            mockInstalledBrowser.DownloadUrl.Returns("https://download.com");

            _mockBrowserFactory
                .Create(Arg.Any<BrowserType>())
                .Returns(mockInstalledBrowser);

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
            var service = new AdapterWindowsBrowserDiscoveryProvider(_mockBrowserFactory, Microsoft.Extensions.Logging.Abstractions.NullLogger<AdapterWindowsBrowserDiscoveryProvider>.Instance);

            var mockUninstalledBrowser = Substitute.For<IOutboundPortBrowser>();
            mockUninstalledBrowser.IsInstalled.Returns(false);
            mockUninstalledBrowser.BrowserVersion.Returns("ShouldBeIgnored");
            mockUninstalledBrowser.DisplayName.Returns("Missing Browser");

            _mockBrowserFactory
                .Create(Arg.Any<BrowserType>())
                .Returns(mockUninstalledBrowser);

            var result = await service.FindInstalledBrowsersAsync();

            var firstBrowser = result.First();
            firstBrowser.IsInstalled.ShouldBeFalse();
            firstBrowser.Version.ShouldBe("Not installed");
        }

        [Fact]
        public async Task GetInstalledBrowsersAsync_ShouldSkipBrowser_WhenFactoryThrowsNotSupportedException()
        {
            var service = new AdapterWindowsBrowserDiscoveryProvider(_mockBrowserFactory, Microsoft.Extensions.Logging.Abstractions.NullLogger<AdapterWindowsBrowserDiscoveryProvider>.Instance);

            _mockBrowserFactory
                .Create(Arg.Any<BrowserType>())
                .Throws<NotSupportedException>();

            var result = await service.FindInstalledBrowsersAsync();

            result.ShouldBeEmpty();
        }

        [Fact]
        public async Task GetInstalledBrowsersAsync_ShouldHandleGeneralExceptions_Gracefully()
        {
            var service = new AdapterWindowsBrowserDiscoveryProvider(_mockBrowserFactory, Microsoft.Extensions.Logging.Abstractions.NullLogger<AdapterWindowsBrowserDiscoveryProvider>.Instance);

            var buggyBrowserMock = Substitute.For<IOutboundPortBrowser>();
            buggyBrowserMock.IsInstalled.Throws(new InvalidOperationException("Registry Error"));

            _mockBrowserFactory
                .Create(Arg.Any<BrowserType>())
                .Returns(buggyBrowserMock);

            var result = await service.FindInstalledBrowsersAsync();

            result.ShouldBeEmpty();
        }

        [Fact]
        public async Task FindInstalledBrowsersAsync_ShouldTrackCacheHitsAndMissesAccurately_WhenInvokedAcrossTtlWindow()
        {
            var fakeTimeProvider = new Microsoft.Extensions.Time.Testing.FakeTimeProvider();
            using var service = new AdapterWindowsBrowserDiscoveryProvider(
                _mockBrowserFactory,
                Microsoft.Extensions.Logging.Abstractions.NullLogger<AdapterWindowsBrowserDiscoveryProvider>.Instance,
                fakeTimeProvider);

            var mockBrowser = Substitute.For<IOutboundPortBrowser>();
            mockBrowser.IsInstalled.Returns(true);
            mockBrowser.BrowserVersion.Returns("130.0");
            mockBrowser.DisplayName.Returns("Chrome");
            mockBrowser.IconPath.Returns(@"C:\chrome.exe");
            mockBrowser.DownloadUrl.Returns("https://google.com");

            _mockBrowserFactory
                .Create(Arg.Any<BrowserType>())
                .Returns(mockBrowser);

            service.CacheHits.ShouldBe(0);
            service.CacheMisses.ShouldBe(0);

            var result1 = await service.FindInstalledBrowsersAsync();
            result1.ShouldNotBeEmpty();

            service.CacheMisses.ShouldBe(1);
            service.CacheHits.ShouldBe(0);

            fakeTimeProvider.Advance(TimeSpan.FromSeconds(10));
            var result2 = await service.FindInstalledBrowsersAsync();
            result2.ShouldNotBeEmpty();

            service.CacheMisses.ShouldBe(1);
            service.CacheHits.ShouldBe(1);

            fakeTimeProvider.Advance(TimeSpan.FromSeconds(10));
            var result3 = await service.FindInstalledBrowsersAsync();
            result3.ShouldNotBeEmpty();

            service.CacheMisses.ShouldBe(1);
            service.CacheHits.ShouldBe(2);

            fakeTimeProvider.Advance(TimeSpan.FromSeconds(15));
            var result4 = await service.FindInstalledBrowsersAsync();
            result4.ShouldNotBeEmpty();

            service.CacheMisses.ShouldBe(2);
            service.CacheHits.ShouldBe(2);

            fakeTimeProvider.Advance(TimeSpan.FromSeconds(5));
            var result5 = await service.FindInstalledBrowsersAsync();
            result5.ShouldNotBeEmpty();

            service.CacheMisses.ShouldBe(2);
            service.CacheHits.ShouldBe(3);
        }
    }
}


