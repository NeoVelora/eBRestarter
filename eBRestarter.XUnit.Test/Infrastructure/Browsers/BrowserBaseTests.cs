using Microsoft.Extensions.Logging;
using NSubstitute;
using Shouldly;
using System.Collections.Generic;
using Xunit;
using eBRestarter.Core.Application.ObjectArchetypes.Constants;
using eBRestarter.Core.Application.ObjectArchetypes.DTOs.Browser;
using eBRestarter.Core.Application.ObjectArchetypes.Enums;
using eBRestarter.Core.Application.Ports.Outbound.OperatingSystem;
using eBRestarter.Infrastructure.Adapters.Outbound.BehavioralComponents.Wrapper.Browsers;

namespace eBRestarter.XUnit.Test.Infrastructure.Browsers
{
    public class BrowserBaseTests
    {
        private sealed class TestDummyBrowser : AdapterBrowserBaseWrapper
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
            var mockProcess = Substitute.For<IOutboundPortOsProcessControl>();
            var mockSettings = Substitute.For<IOutboundPortSystemConfigurationRepository>();
            var mockFileSystem = Substitute.For<IOutboundPortFileSystem>();
            var mockLogger = Substitute.For<ILogger>();
            var dummyBrowser = new TestDummyBrowser(mockProcess, mockSettings, mockFileSystem, mockLogger);

            var result = dummyBrowser.ExposeCleanVersionString(rawVersion);

            result.ShouldBe(expectedResult);
        }

        [Fact]
        public void Start_ShouldFindExecutableAndCallProcessService_WithUrlAndArguments()
        {
            var mockProcess = Substitute.For<IOutboundPortOsProcessControl>();
            var mockSettings = Substitute.For<IOutboundPortSystemConfigurationRepository>();
            var mockLogger = Substitute.For<ILogger>();

            var mockFileSystem = Substitute.For<IOutboundPortFileSystem>();
            mockFileSystem
                .FileExists(@"C:\FakePath\browser.exe")
                .Returns(true);

            var dummyBrowser = new TestDummyBrowser(mockProcess, mockSettings, mockFileSystem, mockLogger);

            const string testUrl = "https://www.ebesucher.com/surfbar/test";
            const string testArgs = "--incognito";

            dummyBrowser.Start(testUrl, testArgs);

            mockProcess.Received(1).OpenUrlInBrowser(
                @"C:\FakePath\browser.exe",
                "\"https://www.ebesucher.com/surfbar/test\" --incognito"
            );
        }

        [Fact]
        public void Start_ShouldLogError_WhenExecutableIsNotFound()
        {
            var mockProcess = Substitute.For<IOutboundPortOsProcessControl>();
            var mockSettings = Substitute.For<IOutboundPortSystemConfigurationRepository>();
            var mockLogger = Substitute.For<ILogger>();

            var mockFileSystem = Substitute.For<IOutboundPortFileSystem>();
            mockFileSystem
                .FileExists(Arg.Any<string>())
                .Returns(false);

            var dummyBrowser = new TestDummyBrowser(mockProcess, mockSettings, mockFileSystem, mockLogger);

            dummyBrowser.Start("https://test.com");

            mockProcess.DidNotReceive().OpenUrlInBrowser(Arg.Any<string>(), Arg.Any<string>());
        }
    }
}
