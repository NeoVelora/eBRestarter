using eBRestarter.Core.Application.Ports.Outbound.OperatingSystem;
using eBRestarter.Infrastructure.Adapters.WindowsOS;
using eBRestarter.Core.Application.Ports.Outbound.Network;
using eBRestarter.Core.Application.Ports.Outbound.Network;
using eBRestarter.Infrastructure.Adapters.Browsers;
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
            var mockProcess = new Mock<IOsProcessControlPort>();
            var mockSettings = new Mock<ISettingsPort>();
            var mockFileSystem = new Mock<IFileSystemPort>();
            var mockLogger = new Mock<ILogger<ChromeBrowser>>();
            

            string localAppData = @"C:\Users\Test\AppData\Local";
            mockFileSystem.Setup(fs => fs.ResolveEnvironmentPath("LocalAppData")).Returns(localAppData);

            mockFileSystem
                .Setup(fs => fs.CombinePaths(It.IsAny<string[]>()))
                .Returns<string[]>(paths => string.Join(@"\", paths));

            string defaultProfilePath = $@"{localAppData}\Google\Chrome\User Data\Default";
            mockFileSystem.Setup(fs => fs.DirectoryExists(defaultProfilePath)).Returns(true);

            string chromeExtensionId = "agchmcconfdfcenopioeilpgjngelefk";
            string expectedExtensionDir = $@"{defaultProfilePath}\Extensions\{chromeExtensionId}";

            mockFileSystem.Setup(fs => fs.DirectoryExists(expectedExtensionDir)).Returns(true);

            var ChromeBrowser = new ChromeBrowser(mockProcess.Object, mockSettings.Object, mockFileSystem.Object, mockLogger.Object);

            bool result = ChromeBrowser.IsExtensionInstalled();

            result.ShouldBeTrue();
        }

        [Fact]
        public void IsExtensionInstalled_ShouldReturnFalse_WhenExtensionFolderIsMissing()
        {
            var mockProcess = new Mock<IOsProcessControlPort>();
            var mockSettings = new Mock<ISettingsPort>();
            var mockFileSystem = new Mock<IFileSystemPort>();
            var mockLogger = new Mock<ILogger<ChromeBrowser>>();
            

            mockFileSystem.Setup(fs => fs.ResolveEnvironmentPath(It.IsAny<string>())).Returns(@"C:\FakeAppData");

            mockFileSystem.Setup(fs => fs.DirectoryExists(It.IsAny<string>())).Returns(false);

            var ChromeBrowser = new ChromeBrowser(mockProcess.Object, mockSettings.Object, mockFileSystem.Object, mockLogger.Object);

            bool result = ChromeBrowser.IsExtensionInstalled();

            result.ShouldBeFalse();
        }
    }
}


