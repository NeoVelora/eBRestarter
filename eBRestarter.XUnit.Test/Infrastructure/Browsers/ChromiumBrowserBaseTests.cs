using eBRestarter.Core.Application.Interfaces.OperatingSystem;
using eBRestarter.Core.Application.Interfaces.OperatingSystem.WindowsOS;
using eBRestarter.Infrastructure.Browsers;
using Microsoft.Extensions.Logging;
using Moq;
using Shouldly;
using System.Collections.Generic;
using Xunit;

namespace eBRestarter.XUnit.Test.Infrastructure.Browsers
{
    public class ChromiumBrowserBaseTests
    {
        [Fact]
        public void IsExtensionInstalled_ShouldReturnTrue_WhenExtensionFolderExists()
        {
            var mockOs = new Mock<IOperatingSystemFacade>();
            var mockLogger = new Mock<ILogger<ChromeBrowser>>();
            var mockFileSystem = new Mock<IWindowsFileSystemService>();

            string localAppData = @"C:\Users\Test\AppData\Local";
            mockFileSystem.Setup(fs => fs.GetEnvironmentPath("LocalAppData")).Returns(localAppData);

            mockFileSystem
                .Setup(fs => fs.CombinePaths(It.IsAny<string[]>()))
                .Returns<string[]>(paths => string.Join(@"\", paths));

            string defaultProfilePath = $@"{localAppData}\Google\Chrome\User Data\Default";
            mockFileSystem.Setup(fs => fs.DirectoryExists(defaultProfilePath)).Returns(true);

            string chromeExtensionId = "agchmcconfdfcenopioeilpgjngelefk";
            string expectedExtensionDir = $@"{defaultProfilePath}\Extensions\{chromeExtensionId}";

            mockFileSystem.Setup(fs => fs.DirectoryExists(expectedExtensionDir)).Returns(true);

            mockOs.Setup(os => os.WindowsFileSystemService).Returns(mockFileSystem.Object);

            var chromeBrowser = new ChromeBrowser(mockOs.Object, mockLogger.Object);

            bool result = chromeBrowser.IsExtensionInstalled();

            result.ShouldBeTrue();
        }

        [Fact]
        public void IsExtensionInstalled_ShouldReturnFalse_WhenExtensionFolderIsMissing()
        {
            var mockOs = new Mock<IOperatingSystemFacade>();
            var mockLogger = new Mock<ILogger<ChromeBrowser>>();
            var mockFileSystem = new Mock<IWindowsFileSystemService>();

            mockFileSystem.Setup(fs => fs.GetEnvironmentPath(It.IsAny<string>())).Returns(@"C:\FakeAppData");

            mockFileSystem.Setup(fs => fs.DirectoryExists(It.IsAny<string>())).Returns(false);

            mockOs.Setup(os => os.WindowsFileSystemService).Returns(mockFileSystem.Object);

            var chromeBrowser = new ChromeBrowser(mockOs.Object, mockLogger.Object);

            bool result = chromeBrowser.IsExtensionInstalled();

            result.ShouldBeFalse();
        }
    }
}