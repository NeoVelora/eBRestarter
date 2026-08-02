using eBRestarter.Core.Application.Ports.Outbound.Network;
using eBRestarter.Core.Application.Ports.Outbound.Network;
using Microsoft.Extensions.Logging;
using Moq;
using Shouldly;
using System;
using Xunit;
using eBRestarter.Core.Application.Ports.Outbound.Interfaces.OperatingSystem;
using eBRestarter.Infrastructure.Adapters.Outbound.BehavioralComponents.Provider.WindowsOS;

namespace eBRestarter.Tests.Infrastructure.Adapters.WindowsOS
{
    public class WindowsSystemInfoProviderTests
    {
        private readonly Mock<ILogger<AdapterWindowsSystemInfoProvider>> _mockLogger;
        private readonly Mock<IOutboundPortSystemConfigurationRepository> _mockRegistry;
        private readonly AdapterWindowsSystemInfoProvider _sut;

        private const string RegistryPathCurrentVersion = @"SOFTWARE\Microsoft\Windows NT\CurrentVersion";
        private const string RegistryPathUserChoiceHttp = @"Software\Microsoft\Windows\Shell\Associations\UrlAssociations\http\UserChoice";
        private const string RegistryPathUserChoiceHttps = @"Software\Microsoft\Windows\Shell\Associations\UrlAssociations\https\UserChoice";

        public WindowsSystemInfoProviderTests()
        {
            _mockLogger = new Mock<ILogger<AdapterWindowsSystemInfoProvider>>();
            _mockRegistry = new Mock<IOutboundPortSystemConfigurationRepository>();

            _sut = new AdapterWindowsSystemInfoProvider(_mockLogger.Object, _mockRegistry.Object);
        }

        [Fact]
        public void GetCurrentOsDisplayVersion_ShouldReturnVersion_WhenRegistryKeyExists()
        {
            _mockRegistry.Setup(r => r.GetSystemValue(RegistryPathCurrentVersion, "DisplayVersion"))
                         .Returns("22H2");

            var result = _sut.RetrieveCurrentOsDisplayVersion();

            result.ShouldBe("22H2");
        }

        [Theory]
        [InlineData(null)]
        [InlineData("")]
        [InlineData("   ")]
        public void GetCurrentOsDisplayVersion_ShouldReturnUnknown_WhenKeyIsMissingOrEmpty(object? invalidValue)
        {
            _mockRegistry.Setup(r => r.GetSystemValue(RegistryPathCurrentVersion, "DisplayVersion"))
                         .Returns(invalidValue);

            var result = _sut.RetrieveCurrentOsDisplayVersion();

            result.ShouldBe("Unknown");
        }

        [Fact]
        public void GetCurrentOsDisplayVersion_ShouldReturnError_OnException()
        {
            _mockRegistry.Setup(r => r.GetSystemValue(It.IsAny<string>(), It.IsAny<string>()))
                         .Throws(new UnauthorizedAccessException("No rights"));

            var result = _sut.RetrieveCurrentOsDisplayVersion();

            result.ShouldBe("Error");
        }

        [Fact]
        public void GetCurrentOsBuildVersion_ShouldCombineEnvironmentBuild_WithUbrFromRegistry()
        {
            _mockRegistry.Setup(r => r.GetSystemValue(RegistryPathCurrentVersion, "UBR"))
                         .Returns(1234);

            string expectedBuild = $"{Environment.OSVersion.Version.Build}.1234";

            var result = _sut.RetrieveCurrentOsBuildVersion();

            result.ShouldBe(expectedBuild);
        }

        [Fact]
        public void GetCurrentOsBuildVersion_ShouldDefaultToZero_WhenUbrIsMissing()
        {
            _mockRegistry.Setup(r => r.GetSystemValue(RegistryPathCurrentVersion, "UBR"))
                         .Returns(null);

            string expectedBuild = $"{Environment.OSVersion.Version.Build}.0";

            var result = _sut.RetrieveCurrentOsBuildVersion();

            result.ShouldBe(expectedBuild);
        }

        [Fact]
        public void GetCurrentOsBuildVersion_ShouldReturnError_OnException()
        {
            _mockRegistry.Setup(r => r.GetSystemValue(It.IsAny<string>(), It.IsAny<string>()))
                         .Throws(new Exception("Registry defekt"));

            var result = _sut.RetrieveCurrentOsBuildVersion();

            result.ShouldBe("Error");
        }

        [Theory]
        [InlineData("ChromeHTML", "Chrome")]
        [InlineData("FirefoxURL-308046B0AF4A39CB", "Firefox")]
        [InlineData("MSEdgeHTM", "Edge")]
        [InlineData("OperaStable", "Opera")]
        [InlineData("BraveHTML", "Brave")]
        [InlineData("UnbekannterBrowserXYZ", "-")]
        public void GetCurrentStandardBrowserName_ShouldMapProgIdCorrectly(string progId, string expectedBrowserName)
        {
            _mockRegistry.Setup(r => r.GetUserValue(RegistryPathUserChoiceHttp, "ProgId")).Returns(progId);
            _mockRegistry.Setup(r => r.GetUserValue(RegistryPathUserChoiceHttps, "ProgId")).Returns(progId);

            var result = _sut.RetrieveCurrentStandardBrowserName();

            result.ShouldBe(expectedBrowserName);
        }

        [Fact]
        public void GetCurrentStandardBrowserName_ShouldHandleMismatch_BetweenHttpAndHttps()
        {
            _mockRegistry.Setup(r => r.GetUserValue(RegistryPathUserChoiceHttp, "ProgId")).Returns("ChromeHTML");
            _mockRegistry.Setup(r => r.GetUserValue(RegistryPathUserChoiceHttps, "ProgId")).Returns("FirefoxURL");

            var result = _sut.RetrieveCurrentStandardBrowserName();

            result.ShouldBe("Chrome");
        }

        [Theory]
        [InlineData(null, "ChromeHTML")]
        [InlineData("ChromeHTML", "")]
        [InlineData(null, null)]
        public void GetCurrentStandardBrowserName_ShouldReturnDash_WhenAnyProgIdIsMissing(string? httpProgId, string? httpsProgId)
        {
            _mockRegistry.Setup(r => r.GetUserValue(RegistryPathUserChoiceHttp, "ProgId")).Returns(httpProgId);
            _mockRegistry.Setup(r => r.GetUserValue(RegistryPathUserChoiceHttps, "ProgId")).Returns(httpsProgId);

            var result = _sut.RetrieveCurrentStandardBrowserName();

            result.ShouldBe("-");
        }

        [Fact]
        public void GetCurrentStandardBrowserName_ShouldReturnDash_OnException()
        {
            _mockRegistry.Setup(r => r.GetUserValue(It.IsAny<string>(), It.IsAny<string>()))
                         .Throws(new UnauthorizedAccessException("No rights for HKCU"));

            var result = _sut.RetrieveCurrentStandardBrowserName();

            result.ShouldBe("-");
        }
    }
}


