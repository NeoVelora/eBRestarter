using Microsoft.Extensions.Logging;
using NSubstitute;
using Shouldly;
using System;
using Xunit;
using eBRestarter.Core.Application.Common.Results;
using eBRestarter.Core.Application.ObjectArchetypes.Constants;
using eBRestarter.Core.Application.Ports.Outbound.OperatingSystem;
using eBRestarter.Infrastructure.Adapters.Outbound.BehavioralComponents.Providers.WindowsOS;
using NSubstitute.ExceptionExtensions;
using Microsoft.Extensions.Logging.Testing;

namespace eBRestarter.XUnit.Test.UnitTests.Infrastructure.Adapters.WindowsOS
{
    [Trait("Category", "Unit")]
    public class WindowsSystemInfoProviderTests
    {
        private readonly FakeLogger<AdapterWindowsSystemInfoProvider> _mockLogger;
        private readonly IOutboundPortSystemConfigurationRepository _mockRegistry;
        private readonly AdapterWindowsSystemInfoProvider _sut;

        private const string RegistryPathCurrentVersion = @"SOFTWARE\Microsoft\Windows NT\CurrentVersion";
        private const string RegistryPathUserChoiceHttp = @"Software\Microsoft\Windows\Shell\Associations\UrlAssociations\http\UserChoice";
        private const string RegistryPathUserChoiceHttps = @"Software\Microsoft\Windows\Shell\Associations\UrlAssociations\https\UserChoice";

        public WindowsSystemInfoProviderTests()
        {
            _mockLogger = new FakeLogger<AdapterWindowsSystemInfoProvider>();
            _mockRegistry = Substitute.For<IOutboundPortSystemConfigurationRepository>();

            _sut = new AdapterWindowsSystemInfoProvider(_mockLogger, _mockRegistry);
        }

        [Fact]
        public void GetCurrentOsDisplayVersion_ShouldReturnVersion_WhenRegistryKeyExists()
        {
            _mockRegistry.GetSystemValue(RegistryPathCurrentVersion, "DisplayVersion")
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
            _mockRegistry.GetSystemValue(RegistryPathCurrentVersion, "DisplayVersion")
                         .Returns(invalidValue);

            var result = _sut.RetrieveCurrentOsDisplayVersion();

            result.ShouldBe("Unknown");
        }

        [Fact]
        public void GetCurrentOsDisplayVersion_ShouldReturnError_OnException()
        {
            _mockRegistry.GetSystemValue(Arg.Any<string>(), Arg.Any<string>())
                         .Throws(new UnauthorizedAccessException("No rights"));

            var result = _sut.RetrieveCurrentOsDisplayVersion();

            result.ShouldBe("Error");
        }

        [Fact]
        public void GetCurrentOsBuildVersion_ShouldCombineEnvironmentBuild_WithUbrFromRegistry()
        {
            _mockRegistry.GetSystemValue(RegistryPathCurrentVersion, "UBR")
                         .Returns(1234);

            string expectedBuild = $"{Environment.OSVersion.Version.Build}.1234";

            var result = _sut.RetrieveCurrentOsBuildVersion();

            result.ShouldBe(expectedBuild);
        }

        [Fact]
        public void GetCurrentOsBuildVersion_ShouldDefaultToZero_WhenUbrIsMissing()
        {
            _mockRegistry.GetSystemValue(RegistryPathCurrentVersion, "UBR")
                         .Returns(null);

            string expectedBuild = $"{Environment.OSVersion.Version.Build}.0";

            var result = _sut.RetrieveCurrentOsBuildVersion();

            result.ShouldBe(expectedBuild);
        }

        [Fact]
        public void GetCurrentOsBuildVersion_ShouldReturnError_OnException()
        {
            _mockRegistry.GetSystemValue(Arg.Any<string>(), Arg.Any<string>())
                         .Throws(new Exception("Registry error"));

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
            _mockRegistry.GetUserValue(RegistryPathUserChoiceHttp, "ProgId").Returns(progId);
            _mockRegistry.GetUserValue(RegistryPathUserChoiceHttps, "ProgId").Returns(progId);

            var result = _sut.RetrieveCurrentStandardBrowserName();

            result.ShouldBe(expectedBrowserName);
        }

        [Fact]
        public void GetCurrentStandardBrowserName_ShouldHandleMismatch_BetweenHttpAndHttps()
        {
            _mockRegistry.GetUserValue(RegistryPathUserChoiceHttp, "ProgId").Returns("ChromeHTML");
            _mockRegistry.GetUserValue(RegistryPathUserChoiceHttps, "ProgId").Returns("FirefoxURL");

            var result = _sut.RetrieveCurrentStandardBrowserName();

            result.ShouldBe("Chrome");
        }

        [Theory]
        [InlineData(null, "ChromeHTML")]
        [InlineData("ChromeHTML", "")]
        [InlineData(null, null)]
        public void GetCurrentStandardBrowserName_ShouldReturnDash_WhenAnyProgIdIsMissing(string? httpProgId, string? httpsProgId)
        {
            _mockRegistry.GetUserValue(RegistryPathUserChoiceHttp, "ProgId").Returns(httpProgId);
            _mockRegistry.GetUserValue(RegistryPathUserChoiceHttps, "ProgId").Returns(httpsProgId);

            var result = _sut.RetrieveCurrentStandardBrowserName();

            result.ShouldBe("-");
        }

        [Fact]
        public void GetCurrentStandardBrowserName_ShouldReturnDash_OnException()
        {
            _mockRegistry.GetUserValue(Arg.Any<string>(), Arg.Any<string>())
                         .Throws(new UnauthorizedAccessException("No rights for HKCU"));

            var result = _sut.RetrieveCurrentStandardBrowserName();

            result.ShouldBe("-");
        }
    }
}
