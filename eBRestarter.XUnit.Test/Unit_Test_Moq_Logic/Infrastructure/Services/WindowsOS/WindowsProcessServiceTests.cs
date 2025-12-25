using eBRestarter.Infrastructure.Services.WindowsOS;
using eBRestarter.Infrastructure.Wrapper.Interface;
using Microsoft.Extensions.Logging;
using Moq;
using Shouldly;
using System.Diagnostics;

namespace eBRestarter.XUnit.Test.Unit_Test_Moq_Logic.Infrastructure.Services.Windows
{
    public class WindowsProcessServiceTests
    {
        private readonly Mock<ILogger<WindowsProcessService>> _loggerMock;
        private readonly Mock<IProcessWrapper> _wrapperMock;

        private readonly WindowsProcessService _sut;

        public WindowsProcessServiceTests()
        {
            _loggerMock = new Mock<ILogger<WindowsProcessService>>();
            _wrapperMock = new Mock<IProcessWrapper>();

            _sut = new WindowsProcessService(_loggerMock.Object, _wrapperMock.Object);
        }

        #region StartExecutable Tests

        [Fact]
        public void StartExecutable_Should_CallWrapper_With_Correct_Arguments()
        {
            string testPath = @"C:\Apps\test.exe";

            _sut.StartExecutable(testPath);

            _wrapperMock.Verify(x => x.Start(It.Is<ProcessStartInfo>(p =>
                p.FileName == testPath &&
                p.UseShellExecute == true
            )), Times.Once);

            VerifyLog(LogLevel.Information, "Executable gestartet");
        }

        [Fact]
        public void StartExecutable_Should_Catch_Exception_And_Log_Error()
        {
            _wrapperMock.Setup(x => x.Start(It.IsAny<ProcessStartInfo>()))
                        .Throws(new System.ComponentModel.Win32Exception("Access denied"));

            var action = () => _sut.StartExecutable("test.exe");

            action.ShouldNotThrow();
            VerifyLog(LogLevel.Error, "Fehler beim Starten");
        }

        #endregion

        #region ShutdownComputer Tests

        [Fact]
        public void ShutdownComputer_Should_Call_Shutdown_With_Reboot_Arguments()
        {
            _sut.ShutdownComputer();

            _wrapperMock.Verify(x => x.Start(It.Is<ProcessStartInfo>(p =>
                p.FileName == "shutdown" &&
                p.Arguments.Contains("/r") &&
                p.Arguments.Contains("/f") &&
                p.Arguments.Contains("/t 0")
            )), Times.Once);
        }

        #endregion

        #region CloseApplication Tests (Logik-Test!)

        [Fact]
        public void CloseApplication_Should_Kill_Process_If_It_Is_Running()
        {
            string appName = "Notepad";
            _wrapperMock.Setup(x => x.IsProcessRunning(appName)).Returns(true);

            _sut.CloseApplication(appName);

            _wrapperMock.Verify(x => x.KillProcess(appName), Times.Once);
        }

        [Fact]
        public void CloseApplication_Should_Do_Nothing_If_Process_Is_Not_Running()
        {
            string appName = "GhostApp";
            _wrapperMock.Setup(x => x.IsProcessRunning(appName)).Returns(false);

            _sut.CloseApplication(appName);

            _wrapperMock.Verify(x => x.KillProcess(It.IsAny<string>()), Times.Never);
        }

        #endregion

        #region StartMsiFile Tests

        [Fact]
        public void StartMsiFile_Should_Log_Warning_If_Process_Is_Null()
        {
            _wrapperMock.Setup(x => x.Start(It.IsAny<ProcessStartInfo>())).Returns((Process?)null);

            _sut.StartMsiFile("installer.msi");

            VerifyLog(LogLevel.Warning, "konnte nicht gestartet werden");
        }


        #endregion

        #region IsProcessAlive Tests

        [Theory]
        [InlineData(true)]
        [InlineData(false)]
        public void IsProcessAlive_Should_Return_Result_From_Wrapper(bool isRunning)
        {
            _wrapperMock.Setup(x => x.IsProcessRunning("test")).Returns(isRunning);

            bool result = _sut.IsProcessAlive("test");

            result.ShouldBe(isRunning);
        }

        #endregion

        private void VerifyLog(LogLevel level, string messageFragment)
        {
            _loggerMock.Verify(
                x => x.Log(
                    level,
                    It.IsAny<EventId>(),
                    It.Is<It.IsAnyType>((v, t) => v.ToString().Contains(messageFragment)),
                    It.IsAny<Exception>(),
                    It.IsAny<Func<It.IsAnyType, Exception, string>>()),
                Times.Once);
        }
    }
}
