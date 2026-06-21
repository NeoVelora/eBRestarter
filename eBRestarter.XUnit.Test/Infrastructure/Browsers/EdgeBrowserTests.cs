using eBRestarter.Core.Application.Interfaces.OperatingSystem;
using eBRestarter.Core.Application.Interfaces.OperatingSystem.WindowsOS;
using eBRestarter.Infrastructure.Browsers;
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
            var mockOs = new Mock<IOperatingSystemFacade>();
            var mockLogger = new Mock<ILogger<EdgeBrowserAdapter>>();
            var mockFileSystem = new Mock<IWindowsFileSystemService>();

            mockFileSystem.Setup(fs => fs.ResolveEnvironmentPath("LocalAppData")).Returns(@"C:\Local");
            mockFileSystem.Setup(fs => fs.CombinePaths(It.IsAny<string[]>())).Returns<string[]>(paths => string.Join(@"\", paths));

            string expectedDefaultProfilePath = @"C:\Local\Microsoft\Edge\User Data\Default";
            mockFileSystem.Setup(fs => fs.DirectoryExists(expectedDefaultProfilePath)).Returns(true);

            mockOs.Setup(os => os.WindowsFileSystemServiceAdapter).Returns(mockFileSystem.Object);
            var EdgeBrowserAdapter = new EdgeBrowserAdapter(mockOs.Object, mockLogger.Object);

            var paths = EdgeBrowserAdapter.ResolvePaths();

            paths.CacheDirs.ShouldContain($@"{expectedDefaultProfilePath}\GPUCache");
            paths.CookiesDirs.ShouldContain($@"{expectedDefaultProfilePath}\Network");
        }
    }
}


