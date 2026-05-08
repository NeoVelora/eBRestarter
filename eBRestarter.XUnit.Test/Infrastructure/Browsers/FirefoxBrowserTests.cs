using eBRestarter.Core.Application.Interfaces.OperatingSystem;
using eBRestarter.Core.Application.Interfaces.OperatingSystem.WindowsOS;
using eBRestarter.Infrastructure.Browsers;
using Microsoft.Extensions.Logging;
using Moq;
using Shouldly;
using Xunit;

namespace eBRestarter.XUnit.Test.Infrastructure.Browsers
{
    public class FirefoxBrowserTests
    {
        [Fact]
        public void GetPaths_ShouldParseProfilesIni_AndGeneratePathsForRelativeAndAbsoluteProfiles()
        {
            var mockOs = new Mock<IOperatingSystemFacade>();
            var mockLogger = new Mock<ILogger<FirefoxBrowser>>();
            var mockFileSystem = new Mock<IWindowsFileSystemService>();

            mockFileSystem.Setup(fs => fs.GetEnvironmentPath("AppData")).Returns(@"C:\Roaming");
            mockFileSystem.Setup(fs => fs.GetEnvironmentPath("LocalAppData")).Returns(@"C:\Local");

            mockFileSystem
                .Setup(fs => fs.CombinePaths(It.IsAny<string[]>()))
                .Returns<string[]>(paths => string.Join(@"\", paths));

            string fakeIniPath = @"C:\Roaming\Mozilla\Firefox\profiles.ini";
            mockFileSystem.Setup(fs => fs.FileExists(fakeIniPath)).Returns(true);

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

            mockFileSystem.Setup(fs => fs.ReadAllLines(fakeIniPath)).Returns(fakeIniContent);

            mockOs.Setup(os => os.WindowsFileSystemService).Returns(mockFileSystem.Object);
            var firefoxBrowser = new FirefoxBrowser(mockOs.Object, mockLogger.Object);
            var paths = firefoxBrowser.GetPaths();

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
            var mockOs = new Mock<IOperatingSystemFacade>();
            var mockLogger = new Mock<ILogger<FirefoxBrowser>>();
            var mockFileSystem = new Mock<IWindowsFileSystemService>();

            string fakeRoamingExtensionsDir = @"C:\Roaming\Mozilla\Firefox\Profiles\abc.default\extensions";
            string expectedExtensionId = "{fef425dc-a60f-4484-954d-71ecf2544846}.xpi";
            string fullXpiPath = $@"{fakeRoamingExtensionsDir}\{expectedExtensionId}";

            mockFileSystem.Setup(fs => fs.GetEnvironmentPath("AppData")).Returns(@"C:\Roaming");
            mockFileSystem.Setup(fs => fs.GetEnvironmentPath("LocalAppData")).Returns(@"C:\Local");

            mockFileSystem
                .Setup(fs => fs.CombinePaths(It.IsAny<string[]>()))
                .Returns<string[]>(paths => string.Join(@"\", paths));

            mockFileSystem.Setup(fs => fs.FileExists(It.IsAny<string>())).Returns(true);
            mockFileSystem.Setup(fs => fs.ReadAllLines(It.IsAny<string>())).Returns(new[] { "[Profile0]", "Path=Profiles/abc.default" });

            mockFileSystem.Setup(fs => fs.DirectoryExists(fakeRoamingExtensionsDir)).Returns(true);
            mockFileSystem.Setup(fs => fs.FileExists(fullXpiPath)).Returns(true);

            mockOs.Setup(os => os.WindowsFileSystemService).Returns(mockFileSystem.Object);
            var firefoxBrowser = new FirefoxBrowser(mockOs.Object, mockLogger.Object);
            bool isInstalled = firefoxBrowser.IsExtensionInstalled();
            isInstalled.ShouldBeTrue();
        }

        [Fact]
        public void IsExtensionInstalled_ShouldReturnTrue_WhenExtractedFolderExists()
        {
            var mockOs = new Mock<IOperatingSystemFacade>();
            var mockLogger = new Mock<ILogger<FirefoxBrowser>>();
            var mockFileSystem = new Mock<IWindowsFileSystemService>();

            string fakeRoamingExtensionsDir = @"C:\Roaming\Mozilla\Firefox\Profiles\abc.default\extensions";

            mockFileSystem.Setup(fs => fs.GetEnvironmentPath("AppData")).Returns(@"C:\Roaming");
            mockFileSystem.Setup(fs => fs.GetEnvironmentPath("LocalAppData")).Returns(@"C:\Local");

            mockFileSystem
                .Setup(fs => fs.CombinePaths(It.IsAny<string[]>()))
                .Returns<string[]>(paths => string.Join(@"\", paths));

            mockFileSystem.Setup(fs => fs.FileExists(It.IsAny<string>())).Returns(true);
            mockFileSystem.Setup(fs => fs.ReadAllLines(It.IsAny<string>())).Returns(new[] { "[Profile0]", "Path=Profiles/abc.default" });
            mockFileSystem.Setup(fs => fs.DirectoryExists(fakeRoamingExtensionsDir)).Returns(true);

            string fullXpiPath = $@"{fakeRoamingExtensionsDir}\{{fef425dc-a60f-4484-954d-71ecf2544846}}.xpi";
            mockFileSystem.Setup(fs => fs.FileExists(fullXpiPath)).Returns(false);

            string extractedFolderPath = $@"{fakeRoamingExtensionsDir}\{{fef425dc-a60f-4484-954d-71ecf2544846}}";
            mockFileSystem.Setup(fs => fs.DirectoryExists(extractedFolderPath)).Returns(true);

            mockOs.Setup(os => os.WindowsFileSystemService).Returns(mockFileSystem.Object);
            var firefoxBrowser = new FirefoxBrowser(mockOs.Object, mockLogger.Object);
            bool isInstalled = firefoxBrowser.IsExtensionInstalled();
            isInstalled.ShouldBeTrue();
        }
    }
}