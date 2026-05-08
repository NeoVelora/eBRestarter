using eBRestarter.Core.Application.Interfaces.OperatingSystem;
using eBRestarter.Core.Application.Interfaces.OperatingSystem.WindowsOS;
using eBRestarter.Core.Application.Enums;
using eBRestarter.Core.Application.Models.Records;
using eBRestarter.Infrastructure.Browsers.Abstract;
using Microsoft.Extensions.Logging;
using Moq;
using Shouldly;
using System.Collections.Generic;
using Xunit;

namespace eBRestarter.XUnit.Test.Infrastructure.Browsers
{
    public class BrowserBaseTests
    {

        private class TestDummyBrowser : BrowserBase
        {
            public TestDummyBrowser(IOperatingSystemFacade os, ILogger logger) : base(os, logger) { }

            public override BrowserType Type => BrowserType.Firefox;
            public override string DisplayName => "TestBrowser";
            public override string IconPath => "";
            public override string DownloadUrl => "";
            public override string ExtensionInstallUrl => "";
            protected override string ProcessName => "testbrowser";
            protected override string RegistryKeyVersion => @"Software\Test";

            protected override List<string> ExecutablePaths => new() { @"C:\FakePath\browser.exe" };

            public override bool IsExtensionInstalled(string? extensionId = null) => false;
            public override BrowserPaths GetPaths() => new(new List<string>(), new List<string>(), new List<string>());

            public string ExposeCleanVersionString(string? raw)
            {
                return CleanVersionString(raw);
            }
        }

        [Theory]
        [InlineData("120.0.1 (x64 de)", "120.0.1")]
        [InlineData("121.0.6167.161 (Official Build) (64-bit)", "121.0.6167.161")]
        [InlineData("  115.0.3  ", "115.0.3")]
        [InlineData("10.0", "10.0")]
        [InlineData(null, "Unknown")]
        [InlineData("", "Unknown")]
        public void CleanVersionString_ShouldExtractVersionNumberCorrectly(string? rawVersion, string expectedResult)
        {
            var mockOs = new Mock<IOperatingSystemFacade>();
            var mockLogger = new Mock<ILogger>();
            var dummyBrowser = new TestDummyBrowser(mockOs.Object, mockLogger.Object);

            var result = dummyBrowser.ExposeCleanVersionString(rawVersion);

            result.ShouldBe(expectedResult);
        }

        [Fact]
        public void Start_ShouldFindExecutableAndCallProcessService_WithUrlAndArguments()
        {
            var mockOs = new Mock<IOperatingSystemFacade>();
            var mockLogger = new Mock<ILogger>();

            var mockFileSystem = new Mock<IWindowsFileSystemService>();
            mockFileSystem.
                Setup(fs => fs.FileExists(@"C:\FakePath\browser.exe")).
                Returns(true);

            mockOs.Setup(os => os.WindowsFileSystemService).Returns(mockFileSystem.Object);

            var mockProcessService = new Mock<IWindowsProcessControlService>();
            mockOs.Setup(os => os.WindowsProcessControlService).Returns(mockProcessService.Object);

            var dummyBrowser = new TestDummyBrowser(mockOs.Object, mockLogger.Object);

            const string testUrl = "https://www.ebesucher.com/surfbar/test";
            const string testArgs = "--incognito";

            dummyBrowser.Start(testUrl, testArgs);

            mockProcessService.Verify(p => p.OpenUrlInBrowser(
                @"C:\FakePath\browser.exe",
                "https://www.ebesucher.com/surfbar/test --incognito"
            ), Times.Once);
        }

        [Fact]
        public void Start_ShouldLogError_WhenExecutableIsNotFound()
        {
            var mockOs = new Mock<IOperatingSystemFacade>();
            var mockLogger = new Mock<ILogger>();

            var mockFileSystem = new Mock<IWindowsFileSystemService>();
            mockFileSystem
                .Setup(fs => fs.FileExists(It.IsAny<string>()))
                .Returns(false);

            mockOs.Setup(os => os.WindowsFileSystemService).Returns(mockFileSystem.Object);

            var mockProcessService = new Mock<IWindowsProcessControlService>();
            mockOs.Setup(os => os.WindowsProcessControlService).Returns(mockProcessService.Object);

            var dummyBrowser = new TestDummyBrowser(mockOs.Object, mockLogger.Object);

            dummyBrowser.Start("https://test.com");

            mockProcessService.Verify(p => p.OpenUrlInBrowser(It.IsAny<string>(), It.IsAny<string>()), Times.Never);
        }
    }
}