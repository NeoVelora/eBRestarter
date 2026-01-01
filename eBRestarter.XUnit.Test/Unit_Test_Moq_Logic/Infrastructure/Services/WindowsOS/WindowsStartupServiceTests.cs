using eBRestarter.Core.Application.Interfaces.OperatingSystem.WindowsOS;
using eBRestarter.Infrastructure.Services.WindowsOS;
using eBRestarter.Infrastructure.Wrapper.Interface;
using Microsoft.Extensions.Logging;
using Moq;
using System;
using System.Collections.Generic;
using System.Text;

namespace eBRestarter.XUnit.Test.Unit_Test_Moq_Logic.Infrastructure.Services.WindowsOS
{
    public class WindowsStartupServiceTests
    {
        private readonly Mock<ILogger<WindowsStartupService>> _loggerMock;
        private readonly Mock<IWindowsRegistryService> _registryMock;
        private readonly Mock<IProcessInfoService> _processMock;
        private readonly WindowsStartupService _windowsStartupService;

        public WindowsStartupServiceTests()
        {
            _loggerMock = new Mock<ILogger<WindowsStartupService>>();
            _registryMock = new Mock<IWindowsRegistryService>();
            _processMock = new Mock<IProcessInfoService>();

            _windowsStartupService = new WindowsStartupService(_loggerMock.Object, _registryMock.Object, _processMock.Object);
        }

        [Fact]
        public void EnableAutoStart_ShouldWriteCorrectPathToRegistry()
        {
            string fakePath = @"C:\MyApp\eBRestarter.exe";

            _processMock.Setup(x => x.GetCurrentExecutablePath()).Returns(fakePath);

            _windowsStartupService.EnableAutoStart();

            try
            {
                _registryMock.Verify(x => x.SetCurrentUserValue(
                    It.Is<string>(s => s.Contains("Run")),
                    "eBRestarter",
                    fakePath), Times.Once);
            }
            catch (MockException)
            {
                _loggerMock.Verify(x => x.Log(
                    LogLevel.Error,
                    It.IsAny<EventId>(),
                    It.IsAny<It.IsAnyType>(),
                    It.IsAny<Exception>(),
                    It.IsAny<Func<It.IsAnyType, Exception, string>>()),
                    Times.AtLeastOnce,
                    "Der Test schlug fehl, weil EnableAutoStart eine Exception gefangen hat.");

                throw;
            }
        }

        [Fact]
        public void DisableAutoStart_ShouldDeleteRegistryValue()
        {
            _windowsStartupService.DisableAutoStart();

            _registryMock.Verify(x => x.DeleteCurrentUserValue(
                @"SOFTWARE\Microsoft\Windows\CurrentVersion\Run",
                "eBRestarter"), Times.Once);
        }

        [Fact]
        public void GetStartupEntries_ShouldReturnDictionaryFromRegistry()
        {
            var fakeEntries = new Dictionary<string, object>
        {
            { "Steam", @"C:\Steam.exe" },
            { "Discord", @"C:\Discord.exe" }
        };

            _registryMock.Setup(x => x.GetCurrentUserValues(It.IsAny<string>()))
                         .Returns(fakeEntries);

            var result = _windowsStartupService.GetStartupEntries();

            Assert.Equal(2, result.Count);
            Assert.True(result.ContainsKey("Steam"));
        }

        [Fact]
        public void GetStartupEntries_ShouldReturnEmpty_OnException()
        {
            _registryMock.Setup(x => x.GetCurrentUserValues(It.IsAny<string>()))
                         .Throws(new Exception("Registry kaputt"));

            var result = _windowsStartupService.GetStartupEntries();

            Assert.Empty(result);

            VerifyLog(LogLevel.Error, "Fehlerbeim Abrufen der Autostart-Einträge");
        }

        [Theory]
        [InlineData(true, 1)]
        [InlineData(false, 0)]
        public void SetEdgeStartupBoost_ShouldSetCorrectDWord(bool enable, int expectedValue)
        {
            _windowsStartupService.SetEdgeStartupBoost(enable);

            _registryMock.Verify(x => x.SetLocalMachineValue(
                @"SOFTWARE\Policies\Microsoft\Edge",
                "StartupBoostEnabled",
                expectedValue,
                Microsoft.Win32.RegistryValueKind.DWord), Times.Once);
        }

        [Theory]
        [InlineData(true, 0)]
        [InlineData(false, 2)]
        public void SetAutoLogon_ShouldSetCorrectDWord(bool enable, int expectedValue)
        {
            _windowsStartupService.SetAutoLogon(enable);

            _registryMock.Verify(x => x.SetLocalMachineValue(
                @"SOFTWARE\Microsoft\Windows NT\CurrentVersion\PasswordLess\Device",
                "DevicePasswordLessBuildVersion",
                expectedValue,
                Microsoft.Win32.RegistryValueKind.DWord), Times.Once);
        }

        [Fact]
        public void SetAutoLogon_ShouldLogPermissionError_WhenUnauthorized()
        {
            _registryMock.Setup(x => x.SetLocalMachineValue(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<object>(), It.IsAny<Microsoft.Win32.RegistryValueKind>()))
                         .Throws(new UnauthorizedAccessException("Keine Admin Rechte"));

            _windowsStartupService.SetAutoLogon(true);

            VerifyLog(LogLevel.Error, "Zugriff verweigert");
        }

        private void VerifyLog(LogLevel level, string messagePart)
        {
            _loggerMock.Verify(
                x => x.Log(
                    level,
                    It.IsAny<EventId>(),
                    It.Is<It.IsAnyType>((v, t) => v.ToString().Contains(messagePart)),
                    It.IsAny<Exception>(),
                    It.IsAny<Func<It.IsAnyType, Exception, string>>()),
                Times.Once);
        }
    }
}
