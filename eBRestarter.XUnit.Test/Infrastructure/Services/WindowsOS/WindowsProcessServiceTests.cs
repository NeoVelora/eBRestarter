using Microsoft.Extensions.Logging;
using NSubstitute;
using Shouldly;
using System;
using System.Diagnostics;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using Xunit;
using eBRestarter.Core.Application.Common.Results;
using eBRestarter.Core.Application.ObjectArchetypes.Constants;
using eBRestarter.Infrastructure.Adapters.Outbound.BehavioralComponents.Wrapper.WindowsOS;
using eBRestarter.Infrastructure.BehavioralComponents.Wrappers;
using NSubstitute.ExceptionExtensions;
using Microsoft.Extensions.Logging.Testing;

namespace eBRestarter.Tests.Infrastructure.Services.WindowsOS
{
    public class WindowsProcessServiceTests
    {
        private readonly FakeLogger<AdapterWindowsProcessControlWrapper> _mockLogger;
        private readonly IProcessWrapper _mockProcessWrapper;
        private readonly AdapterWindowsProcessControlWrapper _sut;

        public WindowsProcessServiceTests()
        {
            _mockLogger = new FakeLogger<AdapterWindowsProcessControlWrapper>();
            _mockProcessWrapper = Substitute.For<IProcessWrapper>();

            _sut = new AdapterWindowsProcessControlWrapper(_mockLogger, _mockProcessWrapper);
        }

        [Fact]
        public void StartExecutable_ShouldCallWrapper_WithCorrectStartInfo()
        {
            string exePath = @"C:\TestApp\app.exe";

            _sut.StartExecutable(exePath);

            _mockProcessWrapper.Received(1).Start(Arg.Is<ProcessStartInfo>(info =>
                info.FileName == exePath &&
                info.UseShellExecute == true
            ));
        }

        [Fact]
        public void RunInstaller_ShouldConfigureMsiExec_AndReadStreams()
        {
            string msiPath = @"C:\Install\setup.msi";
            string expectedSystemFolder = Environment.GetFolderPath(Environment.SpecialFolder.System);
            string expectedMsiExecPath = Path.Combine(expectedSystemFolder, "msiexec.exe");
            var mockProcess = Substitute.For<IProcess>();

            var outStream = new MemoryStream(Encoding.UTF8.GetBytes("Installation OK"));
            var errStream = new MemoryStream(Encoding.UTF8.GetBytes(""));
            mockProcess.StandardOutput.Returns(new StreamReader(outStream));
            mockProcess.StandardError.Returns(new StreamReader(errStream));

            _mockProcessWrapper.Start(Arg.Any<ProcessStartInfo>()).Returns(mockProcess);

            _sut.RunInstaller(msiPath);

            _mockProcessWrapper.Received(1).Start(Arg.Is<ProcessStartInfo>(info =>
                info.FileName == expectedMsiExecPath &&
                info.Arguments == $"/i \"{msiPath}\"" &&
                info.UseShellExecute == false &&
                info.RedirectStandardOutput == true &&
                info.CreateNoWindow == true
            ));

            mockProcess.Received(1).WaitForExit();
        }

        [Fact]
        public void OpenUrlInBrowser_ShouldPassUrlAsArgument()
        {
            string browserPath = @"C:\Program Files\Google\Chrome\Application\chrome.exe";
            string url = "https://www.google.com";

            _sut.OpenUrlInBrowser(browserPath, url);

            _mockProcessWrapper.Received(1).Start(Arg.Is<ProcessStartInfo>(info =>
                info.FileName == browserPath &&
                info.Arguments == url &&
                info.UseShellExecute == true
            ));
        }

        [Fact]
        public void ShutdownComputer_ShouldCallShutdownExe_WithForceParams()
        {
            string expectedSystemFolder = Environment.GetFolderPath(Environment.SpecialFolder.System);
            string expectedShutdownPath = Path.Combine(expectedSystemFolder, "shutdown.exe");

            _sut.ShutdownComputer();

            _mockProcessWrapper.Received(1).Start(Arg.Is<ProcessStartInfo>(info =>
                info.FileName == expectedShutdownPath &&
                info.Arguments == "/r /f /t 0"
            ));
        }

        [Fact]
        public void OpenDirectoryInFileBrowser_ShouldCallExplorerExe_WithQuotedPath()
        {
            string targetFolder = @"C:\My Folder With Spaces";
            string expectedWindowsFolder = Environment.GetFolderPath(Environment.SpecialFolder.Windows);
            string expectedExplorerPath = Path.Combine(expectedWindowsFolder, "explorer.exe");

            _sut.OpenDirectoryInFileBrowser(targetFolder);

            _mockProcessWrapper.Received(1).Start(Arg.Is<ProcessStartInfo>(info =>
                info.FileName == expectedExplorerPath &&
                info.Arguments == $"\"{targetFolder}\""
            ));
        }

        [Fact]
        public void CloseApplication_ShouldCallKill_WhenProcessIsRunning()
        {
            string processName = "notepad";
            _mockProcessWrapper.IsProcessRunning(processName).Returns(true);

            _sut.CloseApplication(processName);

            _mockProcessWrapper.Received(1).KillProcess(processName);
        }

        [Fact]
        public void CloseApplication_ShouldDoNothing_WhenProcessIsNotRunning()
        {
            string processName = "notepad";
            _mockProcessWrapper.IsProcessRunning(processName).Returns(false);

            _sut.CloseApplication(processName);

            _mockProcessWrapper.DidNotReceive().KillProcess(Arg.Any<string>());
        }

        [Fact]
        public async Task CloseAllOpenProgramsAsync_ShouldIgnoreSystemAndHeadlessProcesses()
        {
            var systemMock = Substitute.For<IProcess>();
            systemMock.ProcessName.Returns("System");

            var headlessMock = Substitute.For<IProcess>();
            headlessMock.ProcessName.Returns("BackgroundService");
            headlessMock.MainWindowHandle.Returns(IntPtr.Zero);

            var validAppMock = Substitute.For<IProcess>();
            validAppMock.ProcessName.Returns("Notepad");
            validAppMock.MainWindowHandle.Returns(new IntPtr(1234));
            validAppMock.WaitForExitAsync().Returns(Task.CompletedTask);
            _mockProcessWrapper.GetProcesses().Returns(new IProcess[]
            {
                systemMock,
                headlessMock,
                validAppMock
            });

            await _sut.CloseAllOpenProgramsAsync(5000);

            await systemMock.DidNotReceive().WaitForExitAsync();
            await headlessMock.DidNotReceive().WaitForExitAsync();
            await validAppMock.Received(1).WaitForExitAsync();
            validAppMock.Received(1).Dispose();
        }

        [Fact]
        public async Task StartExecutableAsync_ShouldAwaitProcessExit()
        {
            var mockProcess = Substitute.For<IProcess>();
            mockProcess.WaitForExitAsync().Returns(Task.CompletedTask);

            _mockProcessWrapper.Start(Arg.Any<ProcessStartInfo>()).Returns(mockProcess);

            await _sut.StartExecutableAsync("test.exe");

            await mockProcess.Received(1).WaitForExitAsync();
        }

        [Fact]
        public async Task StartExecutableAsync_ShouldRethrowException()
        {
            _mockProcessWrapper
                .Start(Arg.Any<ProcessStartInfo>())
                .Throws(new InvalidOperationException("Access Denied"));

            Func<Task> act = async () => await _sut.StartExecutableAsync("test.exe");

            await act.ShouldThrowAsync<InvalidOperationException>();
        }
    }
}
