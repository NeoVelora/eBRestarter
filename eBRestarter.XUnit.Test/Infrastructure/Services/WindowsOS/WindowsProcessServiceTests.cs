using eBRestarter.Infrastructure.Services.WindowsOS;
using eBRestarter.Core.Application.Interfaces.OperatingSystem.WindowsOS.Process;
using Microsoft.Extensions.Logging;
using Moq;
using Shouldly;
using System;
using System.Diagnostics;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using Xunit;

namespace eBRestarter.Tests.Infrastructure.Services.WindowsOS
{
    public class WindowsProcessServiceTests
    {
        private readonly Mock<ILogger<WindowsProcessService>> _mockLogger;
        private readonly Mock<IProcessWrapper> _mockProcessWrapper;
        private readonly WindowsProcessService _sut;

        public WindowsProcessServiceTests()
        {
            _mockLogger = new Mock<ILogger<WindowsProcessService>>();
            _mockProcessWrapper = new Mock<IProcessWrapper>();

            _sut = new WindowsProcessService(_mockLogger.Object, _mockProcessWrapper.Object);
        }


        [Fact]
        public void StartExecutable_ShouldCallWrapper_WithCorrectStartInfo()
        {
            string exePath = @"C:\TestApp\app.exe";

            _sut.StartExecutable(exePath);

            _mockProcessWrapper.Verify(w => w.Start(It.Is<ProcessStartInfo>(info =>
                info.FileName == exePath &&
                info.UseShellExecute == true
            )), Times.Once);
        }


        [Fact]
        public void StartMsiFile_ShouldConfigureMsiExec_AndReadStreams()
        {
            string msiPath = @"C:\Install\setup.msi";
            string expectedSystemFolder = Environment.GetFolderPath(Environment.SpecialFolder.System);
            string expectedMsiExecPath = Path.Combine(expectedSystemFolder, "msiexec.exe");

            var mockProcess = new Mock<IProcess>();

            var outStream = new MemoryStream(Encoding.UTF8.GetBytes("Installation OK"));
            var errStream = new MemoryStream(Encoding.UTF8.GetBytes(""));
            mockProcess.Setup(p => p.StandardOutput).Returns(new StreamReader(outStream));
            mockProcess.Setup(p => p.StandardError).Returns(new StreamReader(errStream));

            _mockProcessWrapper.Setup(w => w.Start(It.IsAny<ProcessStartInfo>())).Returns(mockProcess.Object);

            _sut.StartMsiFile(msiPath);

            _mockProcessWrapper.Verify(w => w.Start(It.Is<ProcessStartInfo>(info =>
                info.FileName == expectedMsiExecPath &&
                info.Arguments == $"/i \"{msiPath}\"" &&
                info.UseShellExecute == false &&
                info.RedirectStandardOutput == true &&
                info.CreateNoWindow == true
            )), Times.Once);

            mockProcess.Verify(p => p.WaitForExit(), Times.Once);
        }


        [Fact]
        public void OpenUrlInBrowser_ShouldPassUrlAsArgument()
        {
            string browserPath = @"C:\Program Files\Google\Chrome\Application\chrome.exe";
            string url = "https://www.google.com";

            _sut.OpenUrlInBrowser(browserPath, url);

            _mockProcessWrapper.Verify(w => w.Start(It.Is<ProcessStartInfo>(info =>
                info.FileName == browserPath &&
                info.Arguments == url &&
                info.UseShellExecute == true
            )), Times.Once);
        }

        [Fact]
        public void ShutdownComputer_ShouldCallShutdownExe_WithForceParams()
        {
            string expectedSystemFolder = Environment.GetFolderPath(Environment.SpecialFolder.System);
            string expectedShutdownPath = Path.Combine(expectedSystemFolder, "shutdown.exe");

            _sut.ShutdownComputer();

            _mockProcessWrapper.Verify(w => w.Start(It.Is<ProcessStartInfo>(info =>
                info.FileName == expectedShutdownPath &&
                info.Arguments == "/r /f /t 0"
            )), Times.Once);
        }

        [Fact]
        public void OpenExplorer_ShouldCallExplorerExe_WithQuotedPath()
        {
            string targetFolder = @"C:\My Folder With Spaces";
            string expectedWindowsFolder = Environment.GetFolderPath(Environment.SpecialFolder.Windows);
            string expectedExplorerPath = Path.Combine(expectedWindowsFolder, "explorer.exe");

            _sut.OpenExplorer(targetFolder);

            _mockProcessWrapper.Verify(w => w.Start(It.Is<ProcessStartInfo>(info =>
                info.FileName == expectedExplorerPath &&
                info.Arguments == $"\"{targetFolder}\""
            )), Times.Once);
        }


        [Fact]
        public void CloseApplication_ShouldCallKill_WhenProcessIsRunning()
        {
            string processName = "notepad";
            _mockProcessWrapper.Setup(w => w.IsProcessRunning(processName)).Returns(true);

            _sut.CloseApplication(processName);

            _mockProcessWrapper.Verify(w => w.KillProcess(processName), Times.Once);
        }

        [Fact]
        public void CloseApplication_ShouldDoNothing_WhenProcessIsNotRunning()
        {
            string processName = "notepad";
            _mockProcessWrapper.Setup(w => w.IsProcessRunning(processName)).Returns(false);

            _sut.CloseApplication(processName);

            _mockProcessWrapper.Verify(w => w.KillProcess(It.IsAny<string>()), Times.Never);
        }

        [Fact]
        public async Task CloseAllOpenProgramsAsync_ShouldIgnoreSystemAndHeadlessProcesses()
        {
            var systemMock = new Mock<IProcess>();
            systemMock.Setup(p => p.ProcessName).Returns("System");

            var headlessMock = new Mock<IProcess>();
            headlessMock.Setup(p => p.ProcessName).Returns("BackgroundService");
            headlessMock.Setup(p => p.MainWindowHandle).Returns(IntPtr.Zero);

            var validAppMock = new Mock<IProcess>();
            validAppMock.Setup(p => p.ProcessName).Returns("Notepad");
            validAppMock.Setup(p => p.MainWindowHandle).Returns(new IntPtr(1234));
            validAppMock.Setup(p => p.WaitForExitAsync()).Returns(Task.CompletedTask);

            _mockProcessWrapper.Setup(w => w.GetProcesses()).Returns(new IProcess[]
            {
                systemMock.Object,
                headlessMock.Object,
                validAppMock.Object
            });

            await _sut.CloseAllOpenProgramsAsync(5000);

            systemMock.Verify(p => p.WaitForExitAsync(), Times.Never);

            headlessMock.Verify(p => p.WaitForExitAsync(), Times.Never);

            validAppMock.Verify(p => p.WaitForExitAsync(), Times.Once);

            validAppMock.Verify(p => p.Dispose(), Times.Once);
        }


        [Fact]
        public async Task StartExecutableAsync_ShouldAwaitProcessExit()
        {
            var mockProcess = new Mock<IProcess>();
            mockProcess.Setup(p => p.WaitForExitAsync()).Returns(Task.CompletedTask);

            _mockProcessWrapper.Setup(w => w.Start(It.IsAny<ProcessStartInfo>())).Returns(mockProcess.Object);

            await _sut.StartExecutableAsync("test.exe");

            mockProcess.Verify(p => p.WaitForExitAsync(), Times.Once);
        }

        [Fact]
        public async Task StartExecutableAsync_ShouldRethrowException()
        {
            _mockProcessWrapper
                .Setup(w => w.Start(It.IsAny<ProcessStartInfo>()))
                .Throws(new InvalidOperationException("Access Denied"));

            Func<Task> act = async () => await _sut.StartExecutableAsync("test.exe");

            await act.ShouldThrowAsync<InvalidOperationException>();
        }
    }
}