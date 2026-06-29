using eBRestarter.Core.Application.Ports.Outbound.OperatingSystem;
using eBRestarter.Infrastructure.Adapters.WindowsOS;
using eBRestarter.Core.Application.Ports.Outbound.Network;
using eBRestarter.Core.Application.Ports.Outbound.Network;
using eBRestarter.Infrastructure.Adapters.Browsers;
using Microsoft.Extensions.Logging;
using Moq;
using Shouldly;
using Xunit;

namespace eBRestarter.XUnit.Test.Infrastructure.Browsers
{
    public class VivaldiBrowserTests
    {
        [Fact]
        public void BrowserVersion_ShouldReturnVersionFromUninstallKey_AndCleanIt()
        {
            var mockProcess = new Mock<IOsProcessControlPort>();
            var mockSettings = new Mock<ISettingsPort>();
            var mockFileSystem = new Mock<IFileSystemPort>();
            var mockLogger = new Mock<ILogger<VivaldiBrowser>>();
            

            mockSettings.Setup(reg => reg.GetUserValue(It.IsAny<string>(), "DisplayVersion"))
                .Returns(null);

            mockSettings.Setup(reg => reg.GetSystemValue(It.IsAny<string>(), "DisplayVersion"))
                .Returns("5.3.2679.55 (Stable channel)");

            var VivaldiBrowser = new VivaldiBrowser(mockProcess.Object, mockSettings.Object, mockFileSystem.Object, mockLogger.Object);

            string version = VivaldiBrowser.BrowserVersion;

            version.ShouldBe("5.3.2679.55");
        }

        [Fact]
        public void GetPaths_ShouldGenerateCorrectDirectories_ForVivaldi()
        {
            var mockProcess = new Mock<IOsProcessControlPort>();
            var mockSettings = new Mock<ISettingsPort>();
            
            var mockLogger = new Mock<ILogger<VivaldiBrowser>>();
            var mockFileSystem = new Mock<IFileSystemPort>();

            mockFileSystem.Setup(fs => fs.ResolveEnvironmentPath("LocalAppData")).Returns(@"C:\Local");
            mockFileSystem.Setup(fs => fs.CombinePaths(It.IsAny<string[]>())).Returns<string[]>(paths => string.Join(@"\", paths));

            string expectedDefaultProfilePath = @"C:\Local\Vivaldi\User Data\Default";
            mockFileSystem.Setup(fs => fs.DirectoryExists(expectedDefaultProfilePath)).Returns(true);

            var VivaldiBrowser = new VivaldiBrowser(mockProcess.Object, mockSettings.Object, mockFileSystem.Object, mockLogger.Object);

            var paths = VivaldiBrowser.ResolvePaths();

            paths.CacheDirs.ShouldContain($@"{expectedDefaultProfilePath}\Service Worker");
            paths.CookiesDirs.ShouldContain($@"{expectedDefaultProfilePath}\IndexedDB");
        }
    }
}


