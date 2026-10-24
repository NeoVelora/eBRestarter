using Microsoft.Extensions.Logging;
using NSubstitute;
using Shouldly;
using Xunit;
using eBRestarter.Core.Application.Ports.Outbound.OperatingSystem;
using eBRestarter.Infrastructure.Adapters.Outbound.BehavioralComponents.Wrapper.Browsers;
using Microsoft.Extensions.Logging.Testing;

namespace eBRestarter.XUnit.Test.Infrastructure.Browsers
{
    public class VivaldiBrowserTests
    {
        [Fact]
        public void BrowserVersion_ShouldReturnVersionFromUninstallKey_AndCleanIt()
        {
            var mockProcess = Substitute.For<IOutboundPortOsProcessControl>();
            var mockSettings = Substitute.For<IOutboundPortSystemConfigurationRepository>();
            var mockFileSystem = Substitute.For<IOutboundPortFileSystem>();
            var mockLogger = new FakeLogger<AdapterVivaldiBrowserWrapper>();

            mockSettings.GetUserValue(Arg.Any<string>(), "DisplayVersion")
                .Returns(null);

            mockSettings.GetSystemValue(Arg.Any<string>(), "DisplayVersion")
                .Returns("5.3.2679.55 (Stable channel)");

            var vivaldiBrowser = new AdapterVivaldiBrowserWrapper(mockProcess, mockSettings, mockFileSystem, mockLogger);

            string version = vivaldiBrowser.BrowserVersion;

            version.ShouldBe("5.3.2679.55");
        }

        [Fact]
        public void GetPaths_ShouldGenerateCorrectDirectories_ForVivaldi()
        {
            var mockProcess = Substitute.For<IOutboundPortOsProcessControl>();
            var mockSettings = Substitute.For<IOutboundPortSystemConfigurationRepository>();
            var mockLogger = new FakeLogger<AdapterVivaldiBrowserWrapper>();
            var mockFileSystem = Substitute.For<IOutboundPortFileSystem>();

            mockFileSystem.ResolveEnvironmentPath("LocalAppData").Returns(@"C:\Local");
            mockFileSystem.CombinePaths(Arg.Any<string[]>()).Returns(ci => string.Join(@"\", ci.Arg<string[]>()));

            string expectedDefaultProfilePath = @"C:\Local\Vivaldi\User Data\Default";
            mockFileSystem.DirectoryExists(expectedDefaultProfilePath).Returns(true);

            var vivaldiBrowser = new AdapterVivaldiBrowserWrapper(mockProcess, mockSettings, mockFileSystem, mockLogger);

            var paths = vivaldiBrowser.ResolvePaths();

            paths.CacheDirs.ShouldContain($@"{expectedDefaultProfilePath}\Service Worker");
            paths.CookiesDirs.ShouldContain($@"{expectedDefaultProfilePath}\IndexedDB");
        }
    }
}
