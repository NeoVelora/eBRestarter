using eBRestarter.Core.Application.Interfaces.OperatingSystem.WindowsOS;
using eBRestarter.Infrastructure.Services.WindowsOS;
using eBRestarter.Core.Application.Interfaces.OperatingSystem.WindowsOS.Process;
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
    public class WindowsStartupServiceTests
    {
        private readonly Mock<ILogger<WindowsStartupService>> _mockLogger;
        private readonly Mock<IWindowsRegistryService> _mockRegistry;
        private readonly Mock<IProcessInfoService> _mockProcessInfo;
        private readonly WindowsStartupService _sut;

        private const string RegistryPathRun = @"SOFTWARE\Microsoft\Windows\CurrentVersion\Run";
        private const string RegistryPathEdgePolicies = @"SOFTWARE\Policies\Microsoft\Edge";
        private const string RegistryPathPasswordLess = @"SOFTWARE\Microsoft\Windows NT\CurrentVersion\PasswordLess\Device";

        public WindowsStartupServiceTests()
        {
            _mockLogger = new Mock<ILogger<WindowsStartupService>>();
            _mockRegistry = new Mock<IWindowsRegistryService>();
            _mockProcessInfo = new Mock<IProcessInfoService>();

            _sut = new WindowsStartupService(_mockLogger.Object, _mockRegistry.Object, _mockProcessInfo.Object);
        }


        [Fact]
        public void EnableAutoStart_ShouldSetRegistryKey_WithCurrentExePath()
        {
            string fakeExePath = @"C:\TestApp\eBRestarter.exe";
            _mockProcessInfo.Setup(p => p.GetCurrentExecutablePath()).Returns(fakeExePath);

            _sut.EnableAutoStart();

            _mockRegistry.Verify(r => r.SetCurrentUserValue(
                RegistryPathRun,
                "eBRestarter",
                fakeExePath),
            Times.Once);
        }

        [Fact]
        public void DisableAutoStart_ShouldDeleteRegistryKey()
        {
            _sut.DisableAutoStart();

            _mockRegistry.Verify(r => r.DeleteCurrentUserValue(RegistryPathRun, "eBRestarter"), Times.Once);
        }

        [Fact]
        public void EnableAutoStart_ShouldCatchException_AndNotCrash()
        {
            _mockProcessInfo.Setup(p => p.GetCurrentExecutablePath()).Returns("dummy.exe");
            _mockRegistry.Setup(r => r.SetCurrentUserValue(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<object>()))
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

            _mockRegistry.Setup(r => r.GetCurrentUserValues(RegistryPathRun)).Returns(fakeRegistryEntries);

            bool result = await _sut.IsAutoStartEnabledAsync();

            result.ShouldBe(isEnabledInRegistry);
        }


        [Theory]
        [InlineData(true, 1)]
        [InlineData(false, 0)]
        public void SetEdgeStartupBoost_ShouldWriteCorrectDWordValue(bool enable, int expectedDword)
        {
            _sut.SetEdgeStartupBoost(enable);

            _mockRegistry.Verify(r => r.SetLocalMachineValue(
                RegistryPathEdgePolicies,
                "StartupBoostEnabled",
                expectedDword,
                RegistryValueKind.DWord),
            Times.Once);
        }

        [Theory]
        [InlineData(1, true)]
        [InlineData(0, false)]
        [InlineData(null, true)]
        [InlineData("InvalidString", true)]
        public void IsEdgeStartupBoostEnabled_ShouldReturnExpectedResult(object? registryValue, bool expectedResult)
        {
            _mockRegistry.Setup(r => r.GetLocalMachineValue(RegistryPathEdgePolicies, "StartupBoostEnabled"))
                         .Returns(registryValue);

            bool result = _sut.IsEdgeStartupBoostEnabled();

            result.ShouldBe(expectedResult);
        }


        [Theory]
        [InlineData(true, 0)]
        [InlineData(false, 2)]
        public void SetAutoLogon_ShouldSetInvertedPasswordLessValue(bool enableAutoLogon, int expectedDword)
        {
            _sut.SetAutoLogon(enableAutoLogon);

            _mockRegistry.Verify(r => r.SetLocalMachineValue(
                RegistryPathPasswordLess,
                "DevicePasswordLessBuildVersion",
                expectedDword,
                RegistryValueKind.DWord),
            Times.Once);
        }

        [Fact]
        public void SetAutoLogon_ShouldCatchUnauthorizedAccessException_Gracefully()
        {
            _mockRegistry.Setup(r => r.SetLocalMachineValue(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<object>(), It.IsAny<RegistryValueKind>()))
                         .Throws(new UnauthorizedAccessException("Need Admin Rights"));

            Should.NotThrow(() => _sut.SetAutoLogon(true));
        }
    }
}