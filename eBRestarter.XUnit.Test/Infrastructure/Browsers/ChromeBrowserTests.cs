using eBRestarter.Core.Application.Interfaces.OperatingSystem;
using eBRestarter.Core.Application.Interfaces.OperatingSystem.WindowsOS;
using eBRestarter.Infrastructure.Browsers;
using Microsoft.Extensions.Logging;
using Moq;
using Shouldly;
using Xunit;

namespace eBRestarter.XUnit.Test.Infrastructure.Browsers
{
    public class ChromeBrowserTests
    {
        [Fact]
        public void GetPaths_ShouldGenerateCorrectCacheAndCookieDirectories_ForDefaultProfile()
        {
            var mockOs = new Mock<IOperatingSystemFacade>();
            var mockLogger = new Mock<ILogger<ChromeBrowser>>();
            var mockFileSystem = new Mock<IWindowsFileSystemService>();

            mockFileSystem.Setup(fs => fs.ResolveEnvironmentPath("LocalAppData")).Returns(@"C:\Users\Test\AppData\Local");

            mockFileSystem
                .Setup(fs => fs.CombinePaths(It.IsAny<string[]>()))
                .Returns<string[]>(paths => string.Join(@"\", paths));

            string expectedDefaultProfilePath = @"C:\Users\Test\AppData\Local\Google\Chrome\User Data\Default";
            mockFileSystem.Setup(fs => fs.DirectoryExists(expectedDefaultProfilePath)).Returns(true);

            mockOs.Setup(os => os.WindowsFileSystemService).Returns(mockFileSystem.Object);
            var chromeBrowser = new ChromeBrowser(mockOs.Object, mockLogger.Object);

            var paths = chromeBrowser.ResolvePaths();

            paths.CacheDirs.ShouldContain($@"{expectedDefaultProfilePath}\Cache\Cache_Data");
            paths.CacheDirs.ShouldContain($@"{expectedDefaultProfilePath}\Service Worker");

            paths.CookiesDirs.ShouldContain($@"{expectedDefaultProfilePath}\IndexedDB");
            paths.CookiesDirs.ShouldContain($@"{expectedDefaultProfilePath}\Local Storage");
        }
    }
}
