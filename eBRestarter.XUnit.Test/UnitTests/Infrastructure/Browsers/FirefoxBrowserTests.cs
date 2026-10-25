using Microsoft.Extensions.Logging;
using NSubstitute;
using Shouldly;
using Xunit;
using eBRestarter.Core.Application.Ports.Outbound.OperatingSystem;
using eBRestarter.Infrastructure.Adapters.Outbound.BehavioralComponents.Wrappers.Browsers;
using Microsoft.Extensions.Logging.Testing;

namespace eBRestarter.XUnit.Test.UnitTests.Infrastructure.Browsers
{
    [Trait("Category", "Unit")]
    public class FirefoxBrowserTests
    {
        [Fact]
        public void GetPaths_ShouldParseProfilesIni_AndGeneratePathsForRelativeAndAbsoluteProfiles()
        {
            var mockProcess = Substitute.For<IOutboundPortOsProcessControl>();
            var mockSettings = Substitute.For<IOutboundPortSystemConfigurationRepository>();
            var mockFileSystem = Substitute.For<IOutboundPortFileSystem>();
            var mockLogger = new FakeLogger<AdapterFirefoxBrowserWrapper>();

            mockFileSystem.ResolveEnvironmentPath("AppData").Returns(@"C:\Roaming");
            mockFileSystem.ResolveEnvironmentPath("LocalAppData").Returns(@"C:\Local");

            mockFileSystem
                .CombinePaths(Arg.Any<string[]>())
                .Returns(ci => string.Join(@"\", ci.Arg<string[]>()));

            string fakeIniPath = @"C:\Roaming\Mozilla\Firefox\profiles.ini";
            mockFileSystem.FileExists(fakeIniPath).Returns(true);

            string[] fakeIniContent = new[]
            {
                "[Profile0]",
                "Name=default",
                "IsRelative=1",
                "Path=Profiles/abc.default",
                "",
                "[Profile1]",
                "Name=CustomProfile",
                "IsRelative=0",
                @"Path=D:\Custom\FirefoxProfile"
            };

            mockFileSystem.ReadAllLines(fakeIniPath).Returns(fakeIniContent);

            var firefoxBrowser = new AdapterFirefoxBrowserWrapper(mockProcess, mockSettings, mockFileSystem, mockLogger);

            var paths = firefoxBrowser.ResolvePaths();

            paths.CacheDirs.ShouldContain(@"C:\Local\Mozilla\Firefox\Profiles\abc.default\cache2\entries");
            paths.CookiesDirs.ShouldContain(@"C:\Roaming\Mozilla\Firefox\Profiles\abc.default\storage\default");
            paths.ExtensionsDirs.ShouldContain(@"C:\Roaming\Mozilla\Firefox\Profiles\abc.default\extensions");

            paths.CacheDirs.ShouldContain(@"C:\Local\Mozilla\Firefox\Profiles\FirefoxProfile\cache2\entries");
            paths.CookiesDirs.ShouldContain(@"D:\Custom\FirefoxProfile\storage\default");
            paths.ExtensionsDirs.ShouldContain(@"D:\Custom\FirefoxProfile\extensions");
        }

        [Fact]
        public void IsExtensionInstalled_ShouldReturnTrue_WhenXpiFileExists()
        {
            var mockProcess = Substitute.For<IOutboundPortOsProcessControl>();
            var mockSettings = Substitute.For<IOutboundPortSystemConfigurationRepository>();
            var mockFileSystem = Substitute.For<IOutboundPortFileSystem>();
            var mockLogger = new FakeLogger<AdapterFirefoxBrowserWrapper>();

            string fakeRoamingExtensionsDir = @"C:\Roaming\Mozilla\Firefox\Profiles\abc.default\extensions";
            string expectedExtensionId = "{fef425dc-a60f-4484-954d-71ecf2544846}.xpi";
            string fullXpiPath = $@"{fakeRoamingExtensionsDir}\{expectedExtensionId}";

            mockFileSystem.ResolveEnvironmentPath("AppData").Returns(@"C:\Roaming");
            mockFileSystem.ResolveEnvironmentPath("LocalAppData").Returns(@"C:\Local");

            mockFileSystem
                .CombinePaths(Arg.Any<string[]>())
                .Returns(ci => string.Join(@"\", ci.Arg<string[]>()));

            mockFileSystem.FileExists(Arg.Any<string>()).Returns(true);
            mockFileSystem.ReadAllLines(Arg.Any<string>()).Returns(new[] { "[Profile0]", "Path=Profiles/abc.default" });

            mockFileSystem.DirectoryExists(fakeRoamingExtensionsDir).Returns(true);
            mockFileSystem.FileExists(fullXpiPath).Returns(true);

            var firefoxBrowser = new AdapterFirefoxBrowserWrapper(mockProcess, mockSettings, mockFileSystem, mockLogger);

            bool isInstalled = firefoxBrowser.IsExtensionInstalled();

            isInstalled.ShouldBeTrue();
        }

        [Fact]
        public void IsExtensionInstalled_ShouldReturnTrue_WhenExtractedFolderExists()
        {
            var mockProcess = Substitute.For<IOutboundPortOsProcessControl>();
            var mockSettings = Substitute.For<IOutboundPortSystemConfigurationRepository>();
            var mockFileSystem = Substitute.For<IOutboundPortFileSystem>();
            var mockLogger = new FakeLogger<AdapterFirefoxBrowserWrapper>();

            string fakeRoamingExtensionsDir = @"C:\Roaming\Mozilla\Firefox\Profiles\abc.default\extensions";

            mockFileSystem.ResolveEnvironmentPath("AppData").Returns(@"C:\Roaming");
            mockFileSystem.ResolveEnvironmentPath("LocalAppData").Returns(@"C:\Local");

            mockFileSystem
                .CombinePaths(Arg.Any<string[]>())
                .Returns(ci => string.Join(@"\", ci.Arg<string[]>()));

            mockFileSystem.FileExists(Arg.Any<string>()).Returns(true);
            mockFileSystem.ReadAllLines(Arg.Any<string>()).Returns(new[] { "[Profile0]", "Path=Profiles/abc.default" });
            mockFileSystem.DirectoryExists(fakeRoamingExtensionsDir).Returns(true);

            string fullXpiPath = $@"{fakeRoamingExtensionsDir}\{{fef425dc-a60f-4484-954d-71ecf2544846}}.xpi";
            mockFileSystem.FileExists(fullXpiPath).Returns(false);

            string extractedFolderPath = $@"{fakeRoamingExtensionsDir}\{{fef425dc-a60f-4484-954d-71ecf2544846}}";
            mockFileSystem.DirectoryExists(extractedFolderPath).Returns(true);

            var firefoxBrowser = new AdapterFirefoxBrowserWrapper(mockProcess, mockSettings, mockFileSystem, mockLogger);

            bool isInstalled = firefoxBrowser.IsExtensionInstalled();

            isInstalled.ShouldBeTrue();
        }
    }
}
