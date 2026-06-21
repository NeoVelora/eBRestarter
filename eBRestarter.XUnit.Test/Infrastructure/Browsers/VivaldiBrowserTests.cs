using eBRestarter.Core.Application.Interfaces.OperatingSystem;
using eBRestarter.Core.Application.Interfaces.OperatingSystem.WindowsOS;
using eBRestarter.Infrastructure.Browsers;
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
            var mockOs = new Mock<IOperatingSystemFacade>();
            var mockLogger = new Mock<ILogger<VivaldiBrowserAdapter>>();
            var mockRegistry = new Mock<IWindowsRegistryService>();

            mockRegistry
                .Setup(reg => reg.RetrieveCurrentUserValue(It.IsAny<string>(), "DisplayVersion"))
                .Returns(null);

            mockRegistry
                .Setup(reg => reg.RetrieveLocalMachineValue(It.IsAny<string>(), "DisplayVersion"))
                .Returns("5.3.2679.55 (Stable channel)");

            mockOs.Setup(os => os.WindowsRegistryAdapter).Returns(mockRegistry.Object);

            var VivaldiBrowserAdapter = new VivaldiBrowserAdapter(mockOs.Object, mockLogger.Object);

            string version = VivaldiBrowserAdapter.BrowserVersion;

            version.ShouldBe("5.3.2679.55");
        }

        [Fact]
        public void GetPaths_ShouldGenerateCorrectDirectories_ForVivaldi()
        {
            var mockOs = new Mock<IOperatingSystemFacade>();
            var mockLogger = new Mock<ILogger<VivaldiBrowserAdapter>>();
            var mockFileSystem = new Mock<IWindowsFileSystemService>();

            mockFileSystem.Setup(fs => fs.ResolveEnvironmentPath("LocalAppData")).Returns(@"C:\Local");
            mockFileSystem.Setup(fs => fs.CombinePaths(It.IsAny<string[]>())).Returns<string[]>(paths => string.Join(@"\", paths));

            string expectedDefaultProfilePath = @"C:\Local\Vivaldi\User Data\Default";
            mockFileSystem.Setup(fs => fs.DirectoryExists(expectedDefaultProfilePath)).Returns(true);

            mockOs.Setup(os => os.WindowsFileSystemServiceAdapter).Returns(mockFileSystem.Object);
            var VivaldiBrowserAdapter = new VivaldiBrowserAdapter(mockOs.Object, mockLogger.Object);

            var paths = VivaldiBrowserAdapter.ResolvePaths();

            paths.CacheDirs.ShouldContain($@"{expectedDefaultProfilePath}\Service Worker");
            paths.CookiesDirs.ShouldContain($@"{expectedDefaultProfilePath}\IndexedDB");
        }
    }
}


