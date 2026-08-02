using eBRestarter.Infrastructure.Adapters.WindowsOS;
using eBRestarter.Core.Application.Ports.Outbound.Network;
using eBRestarter.Core.Application.Ports.Outbound.Network;
using Microsoft.Extensions.Logging;
using Moq;
using Shouldly;
using Xunit;
using eBRestarter.Core.Application.Ports.Outbound.Interfaces.OperatingSystem;
using eBRestarter.Infrastructure.Adapters.Outbound.BehavioralComponents.Wrapper.Browsers;

namespace eBRestarter.XUnit.Test.Infrastructure.Browsers
{
    public class ChromeBrowserTests
    {
        [Fact]
        public void GetPaths_ShouldGenerateCorrectCacheAndCookieDirectories_ForDefaultProfile()
        {
            var mockProcess = new Mock<IOutboundPortOsProcessControl>();
            var mockSettings = new Mock<IOutboundPortSystemConfigurationRepository>();
            var mockFileSystem = new Mock<IOutboundPortFileSystem>();
            var mockLogger = new Mock<ILogger<AdapterChromeBrowserWrapper>>();
            

            mockFileSystem.Setup(fs => fs.ResolveEnvironmentPath("LocalAppData")).Returns(@"C:\Users\Test\AppData\Local");

            mockFileSystem
                .Setup(fs => fs.CombinePaths(It.IsAny<string[]>()))
                .Returns<string[]>(paths => string.Join(@"\", paths));

            string expectedDefaultProfilePath = @"C:\Users\Test\AppData\Local\Google\Chrome\User Data\Default";
            mockFileSystem.Setup(fs => fs.DirectoryExists(expectedDefaultProfilePath)).Returns(true);

            var ChromeBrowser = new AdapterChromeBrowserWrapper(mockProcess.Object, mockSettings.Object, mockFileSystem.Object, mockLogger.Object);

            var paths = ChromeBrowser.ResolvePaths();

            paths.CacheDirs.ShouldContain($@"{expectedDefaultProfilePath}\Cache\Cache_Data");
            paths.CacheDirs.ShouldContain($@"{expectedDefaultProfilePath}\Service Worker");

            paths.CookiesDirs.ShouldContain($@"{expectedDefaultProfilePath}\IndexedDB");
            paths.CookiesDirs.ShouldContain($@"{expectedDefaultProfilePath}\Local Storage");
        }
    }
}


