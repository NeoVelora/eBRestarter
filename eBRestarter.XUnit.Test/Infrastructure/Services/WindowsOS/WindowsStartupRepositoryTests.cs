using eBRestarter.Core.Application.Ports.Outbound.OperatingSystem;
using eBRestarter.Infrastructure.Adapters.WindowsOS;
using eBRestarter.Core.Application.Ports.Outbound.Network;
using eBRestarter.Core.Application.Ports.Outbound.Network;
using Microsoft.Extensions.Logging;
using Microsoft.Win32;
using Moq;
using Shouldly;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Xunit;

namespace eBRestarter.Tests.Infrastructure.Services.WindowsOS
{
    public class WindowsStartupRepositoryTests
    {
        private readonly Mock<ILogger<WindowsStartupRepository>> _mockLogger;
        private readonly Mock<ISettingsRepositoryOutboundPort> _mockRegistry;
        private readonly Mock<IProcessInfoProviderOutboundPort> _mockProcessInfo;
        private readonly WindowsStartupRepository _sut;

        private const string RegistryPathRun = @"SOFTWARE\Microsoft\Windows\CurrentVersion\Run";
        private const string RegistryPathEdgePolicies = @"SOFTWARE\Policies\Microsoft\Edge";
        private const string RegistryPathPasswordLess = @"SOFTWARE\Microsoft\Windows NT\CurrentVersion\PasswordLess\Device";

        public WindowsStartupRepositoryTests()
        {
            _mockLogger = new Mock<ILogger<WindowsStartupRepository>>();
            _mockRegistry = new Mock<ISettingsRepositoryOutboundPort>();
            _mockProcessInfo = new Mock<IProcessInfoProviderOutboundPort>();

            _sut = new WindowsStartupRepository(_mockLogger.Object, _mockRegistry.Object, _mockProcessInfo.Object);
        }

        [Fact]
        public void EnableAutoStart_ShouldSetRegistryKey_WithCurrentExePath()
        {
            string fakeExePath = @"C:\TestApp\eBRestarter.exe";
            _mockProcessInfo.Setup(p => p.GetCurrentExecutablePath()).Returns(fakeExePath);

            _sut.EnableAutoStart();

            _mockRegistry.Verify(r => r.SetUserValue(
                RegistryPathRun,
                "eBRestarter",
                fakeExePath),
            Times.Once);
        }

        [Fact]
        public void DisableAutoStart_ShouldDeleteRegistryKey()
        {
            _sut.DisableAutoStart();

            _mockRegistry.Verify(r => r.DeleteUserValue(RegistryPathRun, "eBRestarter"), Times.Once);
        }

        [Fact]
        public void EnableAutoStart_ShouldCatchException_AndNotCrash()
        {
            _mockProcessInfo.Setup(p => p.GetCurrentExecutablePath()).Returns("dummy.exe");
            _mockRegistry.Setup(r => r.SetUserValue(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<object>()))
                         .Throws(new UnauthorizedAccessException("Access Denied"));

            Should.NotThrow(() => _sut.EnableAutoStart());
        }

        [Theory]
        [InlineData(true)]
        [InlineData(false)]
        public async Task IsAutoStartEnabledAsync_ShouldReturnCorrectStatus(bool isEnabledInRegistry)
        {
            var fakeRegistryEntries = new Dictionary<string, object>();

            if (isEnabledInRegistry)
            {
                fakeRegistryEntries.Add("eBRestarter", @"C:\Path\eB.exe");
            }
            fakeRegistryEntries.Add("SomeOtherApp", @"C:\Other\app.exe");

            _mockRegistry.Setup(r => r.GetUserValues(RegistryPathRun)).Returns(fakeRegistryEntries);

            bool result = await _sut.IsAutoStartEnabledAsync();

            result.ShouldBe(isEnabledInRegistry);
        }

        [Theory]
        [InlineData(true, 1)]
        [InlineData(false, 0)]
        public void SetBrowserStartupBoost_ShouldWriteCorrectDWordValue(bool enable, int expectedDword)
        {
            _sut.SetBrowserStartupBoost(enable);

            _mockRegistry.Verify(r => r.SetSystemValue(
                RegistryPathEdgePolicies,
                "StartupBoostEnabled",
                expectedDword),
            Times.Once);
        }

        [Theory]
        [InlineData(1, true)]
        [InlineData(0, false)]
        [InlineData(null, true)]
        [InlineData("InvalidString", true)]
        public void IsBrowserStartupBoostEnabled_ShouldReturnExpectedResult(object? registryValue, bool expectedResult)
        {
            _mockRegistry.Setup(r => r.GetSystemValue(RegistryPathEdgePolicies, "StartupBoostEnabled"))
                         .Returns(registryValue);

            bool result = _sut.IsBrowserStartupBoostEnabled();

            result.ShouldBe(expectedResult);
        }

        [Theory]
        [InlineData(true, 0)]
        [InlineData(false, 2)]
        public void SetAutoLogon_ShouldSetInvertedPasswordLessValue(bool enableAutoLogon, int expectedDword)
        {
            _sut.SetAutoLogon(enableAutoLogon);

            _mockRegistry.Verify(r => r.SetSystemValue(
                RegistryPathPasswordLess,
                "DevicePasswordLessBuildVersion",
                expectedDword),
            Times.Once);
        }

        [Fact]
        public void SetAutoLogon_ShouldCatchUnauthorizedAccessException_Gracefully()
        {
            _mockRegistry.Setup(r => r.SetSystemValue(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<object>()))
                         .Throws(new UnauthorizedAccessException("Need Admin Rights"));

            Should.NotThrow(() => _sut.SetAutoLogon(true));
        }
    }
}


