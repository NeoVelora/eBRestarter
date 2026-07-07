using eBRestarter.Infrastructure.Adapters.WindowsOS;
using eBRestarter.Core.Application.Ports.Outbound.Network;
using eBRestarter.Core.Application.Ports.Outbound.Network;
using eBRestarter.Core.Application.Enums;
using eBRestarter.Core.Application.Models.Records;
using Microsoft.Extensions.Logging;
using Moq;
using Shouldly;
using System.Collections.Generic;
using Xunit;
using eBRestarter.Core.Application.Ports.Outbound.Interfaces.OperatingSystem;
using eBRestarter.Infrastructure.Adapters.Outbound.Browsers;

namespace eBRestarter.XUnit.Test.Infrastructure.Browsers
{
    public class BrowserBaseTests
    {

        private class TestDummyBrowser : AdapterBrowserBase
        {
            public TestDummyBrowser(IOutboundPortOsProcessControl os, IOutboundPortSystemConfigurationRepository settings, IOutboundPortFileSystem fs, ILogger logger) : base(os, settings, fs, logger) { }

            public override BrowserType Type => BrowserType.Firefox;
            public override string DisplayName => "TestBrowser";
            public override string IconPath => "";
            public override string DownloadUrl => "";
            public override string ExtensionInstallUrl => "";
            public override string ProcessName => "testbrowser";
            protected override string RegistryKeyVersion => @"Software\Test";

            protected override List<string> ExecutablePaths => new() { @"C:\FakePath\browser.exe" };

            public override bool IsExtensionInstalled(string? extensionId = null) => false;
            public override BrowserPaths ResolvePaths() => new(new List<string>(), new List<string>(), new List<string>());

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
            var mockProcess = new Mock<IOutboundPortOsProcessControl>();
            var mockSettings = new Mock<IOutboundPortSystemConfigurationRepository>();
            var mockFileSystem = new Mock<IOutboundPortFileSystem>();
            var mockLogger = new Mock<ILogger>();
            var dummyBrowser = new TestDummyBrowser(mockProcess.Object, mockSettings.Object, mockFileSystem.Object, mockLogger.Object);

            var result = dummyBrowser.ExposeCleanVersionString(rawVersion);

            result.ShouldBe(expectedResult);
        }

        [Fact]
        public void Start_ShouldFindExecutableAndCallProcessService_WithUrlAndArguments()
        {
            var mockProcess = new Mock<IOutboundPortOsProcessControl>();
            var mockSettings = new Mock<IOutboundPortSystemConfigurationRepository>();
            
            var mockLogger = new Mock<ILogger>();

            var mockFileSystem = new Mock<IOutboundPortFileSystem>();
            mockFileSystem.
                Setup(fs => fs.FileExists(@"C:\FakePath\browser.exe")).
                Returns(true);

            
            var dummyBrowser = new TestDummyBrowser(mockProcess.Object, mockSettings.Object, mockFileSystem.Object, mockLogger.Object);

            const string testUrl = "https://www.ebesucher.com/surfbar/test";
            const string testArgs = "--incognito";

            dummyBrowser.Start(testUrl, testArgs);

            mockProcess.Verify(p => p.OpenUrlInBrowser(
                @"C:\FakePath\browser.exe",
                "https://www.ebesucher.com/surfbar/test --incognito"
            ), Times.Once);
        }

        [Fact]
        public void Start_ShouldLogError_WhenExecutableIsNotFound()
        {
            var mockProcess = new Mock<IOutboundPortOsProcessControl>();
            var mockSettings = new Mock<IOutboundPortSystemConfigurationRepository>();
            
            var mockLogger = new Mock<ILogger>();

            var mockFileSystem = new Mock<IOutboundPortFileSystem>();
            mockFileSystem
                .Setup(fs => fs.FileExists(It.IsAny<string>()))
                .Returns(false);

            
            var dummyBrowser = new TestDummyBrowser(mockProcess.Object, mockSettings.Object, mockFileSystem.Object, mockLogger.Object);

            dummyBrowser.Start("https://test.com");

            mockProcess.Verify(p => p.OpenUrlInBrowser(It.IsAny<string>(), It.IsAny<string>()), Times.Never);
        }
    }
}


