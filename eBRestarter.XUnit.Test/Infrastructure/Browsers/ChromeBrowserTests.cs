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
    public class ChromeBrowserTests
    {
        [Fact]
        public void GetPaths_ShouldGenerateCorrectCacheAndCookieDirectories_ForDefaultProfile()
        {
            var mockProcess = new Mock<IOsProcessControlOutboundPort>();
            var mockSettings = new Mock<ISettingsRepositoryOutboundPort>();
            var mockFileSystem = new Mock<IFileSystemOutboundPort>();
            var mockLogger = new Mock<ILogger<ChromeBrowser>>();
            

            mockFileSystem.Setup(fs => fs.ResolveEnvironmentPath("LocalAppData")).Returns(@"C:\Users\Test\AppData\Local");

            mockFileSystem
                .Setup(fs => fs.CombinePaths(It.IsAny<string[]>()))
                .Returns<string[]>(paths => string.Join(@"\", paths));

            string expectedDefaultProfilePath = @"C:\Users\Test\AppData\Local\Google\Chrome\User Data\Default";
            mockFileSystem.Setup(fs => fs.DirectoryExists(expectedDefaultProfilePath)).Returns(true);

            var ChromeBrowser = new ChromeBrowser(mockProcess.Object, mockSettings.Object, mockFileSystem.Object, mockLogger.Object);

            var paths = ChromeBrowser.ResolvePaths();

            paths.CacheDirs.ShouldContain($@"{expectedDefaultProfilePath}\Cache\Cache_Data");
            paths.CacheDirs.ShouldContain($@"{expectedDefaultProfilePath}\Service Worker");

            paths.CookiesDirs.ShouldContain($@"{expectedDefaultProfilePath}\IndexedDB");
            paths.CookiesDirs.ShouldContain($@"{expectedDefaultProfilePath}\Local Storage");
        }
    }
}


