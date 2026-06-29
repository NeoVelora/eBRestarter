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
    public class EdgeBrowserTests
    {
        [Fact]
        public void GetPaths_ShouldGenerateCorrectDirectories_ForEdge()
        {
            var mockProcess = new Mock<IOsProcessControlPort>();
            var mockSettings = new Mock<ISettingsPort>();
            var mockFileSystem = new Mock<IFileSystemPort>();
            var mockLogger = new Mock<ILogger<EdgeBrowser>>();
            

            mockFileSystem.Setup(fs => fs.ResolveEnvironmentPath("LocalAppData")).Returns(@"C:\Local");
            mockFileSystem.Setup(fs => fs.CombinePaths(It.IsAny<string[]>())).Returns<string[]>(paths => string.Join(@"\", paths));

            string expectedDefaultProfilePath = @"C:\Local\Microsoft\Edge\User Data\Default";
            mockFileSystem.Setup(fs => fs.DirectoryExists(expectedDefaultProfilePath)).Returns(true);

            var EdgeBrowser = new EdgeBrowser(mockProcess.Object, mockSettings.Object, mockFileSystem.Object, mockLogger.Object);

            var paths = EdgeBrowser.ResolvePaths();

            paths.CacheDirs.ShouldContain($@"{expectedDefaultProfilePath}\GPUCache");
            paths.CookiesDirs.ShouldContain($@"{expectedDefaultProfilePath}\Network");
        }
    }
}


