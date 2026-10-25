using Microsoft.Extensions.Logging;
using NSubstitute;
using Shouldly;
using System.Collections.Generic;
using Xunit;
using eBRestarter.Core.Application.ObjectArchetypes.Constants;
using eBRestarter.Core.Application.Ports.Outbound.OperatingSystem;
using eBRestarter.Infrastructure.Adapters.Outbound.BehavioralComponents.Wrappers.Browsers;
using Microsoft.Extensions.Logging.Testing;

namespace eBRestarter.XUnit.Test.UnitTests.Infrastructure.Browsers
{
    [Trait("Category", "Unit")]
    public class ChromiumBrowserBaseTests
    {
        [Fact]
        public void IsExtensionInstalled_ShouldReturnTrue_WhenExtensionFolderExists()
        {
            var mockProcess = Substitute.For<IOutboundPortOsProcessControl>();
            var mockSettings = Substitute.For<IOutboundPortSystemConfigurationRepository>();
            var mockFileSystem = Substitute.For<IOutboundPortFileSystem>();
            var mockLogger = new FakeLogger<AdapterChromeBrowserWrapper>();

            string localAppData = @"C:\Users\Test\AppData\Local";
            mockFileSystem.ResolveEnvironmentPath("LocalAppData").Returns(localAppData);

            mockFileSystem
                .CombinePaths(Arg.Any<string[]>())
                .Returns(ci => string.Join(@"\", ci.Arg<string[]>()));

            string defaultProfilePath = $@"{localAppData}\Google\Chrome\User Data\Default";
            mockFileSystem.DirectoryExists(defaultProfilePath).Returns(true);

            string chromeExtensionId = "agchmcconfdfcenopioeilpgjngelefk";
            string expectedExtensionDir = $@"{defaultProfilePath}\Extensions\{chromeExtensionId}";

            mockFileSystem.DirectoryExists(expectedExtensionDir).Returns(true);

            var chromeBrowser = new AdapterChromeBrowserWrapper(mockProcess, mockSettings, mockFileSystem, mockLogger);

            bool result = chromeBrowser.IsExtensionInstalled();

            result.ShouldBeTrue();
        }

        [Fact]
        public void IsExtensionInstalled_ShouldReturnFalse_WhenExtensionFolderIsMissing()
        {
            var mockProcess = Substitute.For<IOutboundPortOsProcessControl>();
            var mockSettings = Substitute.For<IOutboundPortSystemConfigurationRepository>();
            var mockFileSystem = Substitute.For<IOutboundPortFileSystem>();
            var mockLogger = new FakeLogger<AdapterChromeBrowserWrapper>();

            mockFileSystem.ResolveEnvironmentPath(Arg.Any<string>()).Returns(@"C:\FakeAppData");
            mockFileSystem.DirectoryExists(Arg.Any<string>()).Returns(false);

            var chromeBrowser = new AdapterChromeBrowserWrapper(mockProcess, mockSettings, mockFileSystem, mockLogger);

            bool result = chromeBrowser.IsExtensionInstalled();

            result.ShouldBeFalse();
        }
    }
}
