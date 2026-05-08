using eBRestarter.Core.Application.Interfaces.OperatingSystem.WindowsOS;
using eBRestarter.Infrastructure.Services.WindowsOS;
using Microsoft.Extensions.Logging;
using Moq;
using Shouldly;
using System;
using Xunit;

namespace eBRestarter.Tests.Infrastructure.Services.WindowsOS
{
    public class WindowsSystemInfoServiceTests
    {
        private readonly Mock<ILogger<WindowsSystemInfoService>> _mockLogger;
        private readonly Mock<IWindowsRegistryService> _mockRegistry;
        private readonly WindowsSystemInfoService _sut;

        private const string RegistryPathCurrentVersion = @"SOFTWARE\Microsoft\Windows NT\CurrentVersion";
        private const string RegistryPathUserChoiceHttp = @"Software\Microsoft\Windows\Shell\Associations\UrlAssociations\http\UserChoice";
        private const string RegistryPathUserChoiceHttps = @"Software\Microsoft\Windows\Shell\Associations\UrlAssociations\https\UserChoice";

        public WindowsSystemInfoServiceTests()
        {
            _mockLogger = new Mock<ILogger<WindowsSystemInfoService>>();
            _mockRegistry = new Mock<IWindowsRegistryService>();

            _sut = new WindowsSystemInfoService(_mockLogger.Object, _mockRegistry.Object);
        }

        [Fact]
        public void GetCurrentOsDisplayVersion_ShouldReturnVersion_WhenRegistryKeyExists()
        {
            _mockRegistry.Setup(r => r.GetLocalMachineValue(RegistryPathCurrentVersion, "DisplayVersion"))
                         .Returns("22H2");

            var result = _sut.GetCurrentOsDisplayVersion();

            result.ShouldBe("22H2");
        }

        [Theory]
        [InlineData(null)]
        [InlineData("")]
        [InlineData("   ")]
        public void GetCurrentOsDisplayVersion_ShouldReturnUnknown_WhenKeyIsMissingOrEmpty(object? invalidValue)
        {
            _mockRegistry.Setup(r => r.GetLocalMachineValue(RegistryPathCurrentVersion, "DisplayVersion"))
                         .Returns(invalidValue);

            var result = _sut.GetCurrentOsDisplayVersion();

            result.ShouldBe("Unknown");
        }

        [Fact]
        public void GetCurrentOsDisplayVersion_ShouldReturnError_OnException()
        {
            _mockRegistry.Setup(r => r.GetLocalMachineValue(It.IsAny<string>(), It.IsAny<string>()))
                         .Throws(new UnauthorizedAccessException("No rights"));

            var result = _sut.GetCurrentOsDisplayVersion();

            result.ShouldBe("Error");
        }

        [Fact]
        public void GetCurrentOsBuildVersion_ShouldCombineEnvironmentBuild_WithUbrFromRegistry()
        {
            _mockRegistry.Setup(r => r.GetLocalMachineValue(RegistryPathCurrentVersion, "UBR"))
                         .Returns(1234);

            string expectedBuild = $"{Environment.OSVersion.Version.Build}.1234";

            var result = _sut.GetCurrentOsBuildVersion();

            result.ShouldBe(expectedBuild);
        }

        [Fact]
        public void GetCurrentOsBuildVersion_ShouldDefaultToZero_WhenUbrIsMissing()
        {
            _mockRegistry.Setup(r => r.GetLocalMachineValue(RegistryPathCurrentVersion, "UBR"))
                         .Returns(null);

            string expectedBuild = $"{Environment.OSVersion.Version.Build}.0";

            var result = _sut.GetCurrentOsBuildVersion();

            result.ShouldBe(expectedBuild);
        }

        [Fact]
        public void GetCurrentOsBuildVersion_ShouldReturnError_OnException()
        {
            _mockRegistry.Setup(r => r.GetLocalMachineValue(It.IsAny<string>(), It.IsAny<string>()))
                         .Throws(new Exception("Registry defekt"));

            var result = _sut.GetCurrentOsBuildVersion();

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
            _mockRegistry.Setup(r => r.GetCurrentUserValue(RegistryPathUserChoiceHttp, "ProgId")).Returns(progId);
            _mockRegistry.Setup(r => r.GetCurrentUserValue(RegistryPathUserChoiceHttps, "ProgId")).Returns(progId);

            var result = _sut.GetCurrentStandardBrowserName();

            result.ShouldBe(expectedBrowserName);
        }

        [Fact]
        public void GetCurrentStandardBrowserName_ShouldHandleMismatch_BetweenHttpAndHttps()
        {
            _mockRegistry.Setup(r => r.GetCurrentUserValue(RegistryPathUserChoiceHttp, "ProgId")).Returns("ChromeHTML");
            _mockRegistry.Setup(r => r.GetCurrentUserValue(RegistryPathUserChoiceHttps, "ProgId")).Returns("FirefoxURL");

            var result = _sut.GetCurrentStandardBrowserName();

            result.ShouldBe("Chrome");
        }

        [Theory]
        [InlineData(null, "ChromeHTML")]
        [InlineData("ChromeHTML", "")]
        [InlineData(null, null)]
        public void GetCurrentStandardBrowserName_ShouldReturnDash_WhenAnyProgIdIsMissing(string? httpProgId, string? httpsProgId)
        {
            _mockRegistry.Setup(r => r.GetCurrentUserValue(RegistryPathUserChoiceHttp, "ProgId")).Returns(httpProgId);
            _mockRegistry.Setup(r => r.GetCurrentUserValue(RegistryPathUserChoiceHttps, "ProgId")).Returns(httpsProgId);

            var result = _sut.GetCurrentStandardBrowserName();

            result.ShouldBe("-");
        }

        [Fact]
        public void GetCurrentStandardBrowserName_ShouldReturnDash_OnException()
        {
            _mockRegistry.Setup(r => r.GetCurrentUserValue(It.IsAny<string>(), It.IsAny<string>()))
                         .Throws(new UnauthorizedAccessException("No rights for HKCU"));

            var result = _sut.GetCurrentStandardBrowserName();

            result.ShouldBe("-");
        }
    }
}