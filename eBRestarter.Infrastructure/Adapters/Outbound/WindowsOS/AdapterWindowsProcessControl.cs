using eBRestarter.Core.Application.Ports.Outbound.Interfaces.OperatingSystem;
using eBRestarter.Infrastructure.BehavioralComponents.Wrappers;
using Microsoft.Extensions.Logging;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Runtime.Versioning;

namespace eBRestarter.Infrastructure.Adapters.WindowsOS;

[SupportedOSPlatform("windows")]
public sealed partial class AdapterWindowsProcessControl(ILogger<AdapterWindowsProcessControl> logger, IProcessWrapper processWrapper) : IOutboundPortOsProcessControl
{
    private readonly ILogger<AdapterWindowsProcessControl> _logger = logger;

    private readonly IProcessWrapper _processWrapper = processWrapper;

    public void StartExecutable(string exeFilePath)
    {
        try
        {
            _processWrapper.Start(new ProcessStartInfo
            {
                FileName = exeFilePath,
                UseShellExecute = true
            });

            if (_logger.IsEnabled(LogLevel.Information))
            {
                _logger.LogInformation("Executable started: {Path}", exeFilePath);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error starting the EXE: {Path}", exeFilePath);
        }
    }

    public void RunInstaller(string installerPath)
    {
        string systemFolder = Environment.GetFolderPath(Environment.SpecialFolder.System);
        string msiExecPath = Path.Combine(systemFolder, "msiexec.exe");

        var startInfo = new ProcessStartInfo
        {
            FileName = msiExecPath,
            Arguments = $"/i \"{installerPath}\"",

            UseShellExecute = false,
            RedirectStandardOutput = true,
            RedirectStandardError = true,

            CreateNoWindow = true
        };

        try
        {
            using var process = _processWrapper.Start(startInfo);

            if (process == null)
            {
                if (_logger.IsEnabled(LogLevel.Warning))
                {
                    _logger.LogWarning("MSI process could not be started (null): {Path}", installerPath);
                }
                return;
            }

            string output = process.StandardOutput.ReadToEnd();
            string error = process.StandardError.ReadToEnd();

            process.WaitForExit();

            if (!string.IsNullOrWhiteSpace(error) && _logger.IsEnabled(LogLevel.Warning))
            {
                _logger.LogWarning("MSI Installer error output: {Error}", error);
            }

            if (!string.IsNullOrWhiteSpace(output) && _logger.IsEnabled(LogLevel.Debug))
            {
                _logger.LogDebug("MSI Installer output: {Output}", output);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error starting the MSI installer: {Path}", installerPath);
        }
    }

    public void OpenUrlInBrowser(string exeFilePath, string arguments)
    {
        try
        {
            var startInfo = new ProcessStartInfo
            {
                FileName = exeFilePath,
                Arguments = arguments,
                UseShellExecute = true
            };

            _processWrapper.Start(startInfo);

            if (_logger.IsEnabled(LogLevel.Information))
            {
                _logger.LogInformation("Executable started: {Path} with arguments: {Args}", exeFilePath, arguments);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error starting the EXE: {Path}", exeFilePath);
        }
    }

    public void ShutdownComputer()
    {
        try
        {
            _logger.LogInformation("Shutting down computer (Restart)...");

            string systemFolder = Environment.GetFolderPath(Environment.SpecialFolder.System);
            string shutdownPath = Path.Combine(systemFolder, "shutdown.exe");

            _processWrapper.Start(new ProcessStartInfo(shutdownPath, "/r /f /t 0") { UseShellExecute = true });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error attempting to shut down the computer.");
        }
    }

    public bool IsProcessAlive(string processName)
    {
        return _processWrapper.IsProcessRunning(processName);
    }

    public void CloseApplication(string processName)
    {
        try
        {
            if (_processWrapper.IsProcessRunning(processName))
            {
                _processWrapper.KillProcess(processName);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error terminating {Name}", processName);
        }
    }

    public async Task CloseAllOpenProgramsAsync(int timeoutMilliseconds)
    {
        var processes = _processWrapper.GetProcesses();
        var pendingTasks = new List<Task>();
        var processesToDispose = new List<IProcess>();

        string currentProcessName = Process.GetCurrentProcess().ProcessName;

        foreach (var process in processes)
        {
            if (ShouldIgnoreProcess(process, currentProcessName))
            {
                process.Dispose();
                continue;
            }

            TryCloseProcess(process, pendingTasks, processesToDispose);
        }

        if (pendingTasks.Count > 0)
        {
            await WaitForProcessesToCloseAsync(pendingTasks, processesToDispose, timeoutMilliseconds);
        }
    }

    private static bool ShouldIgnoreProcess(IProcess process, string currentProcessName)
    {
        return process.ProcessName == "System"
                     || process.ProcessName == "Idle"
                     || process.ProcessName.Equals("explorer", StringComparison.OrdinalIgnoreCase)
                     || process.ProcessName.Equals(currentProcessName, StringComparison.OrdinalIgnoreCase);
    }

    private void TryCloseProcess(IProcess process, List<Task> pendingTasks, List<IProcess> processesToDispose)
    {
        try
        {
            if (process.MainWindowHandle != IntPtr.Zero)
            {
                PostMessage(process.MainWindowHandle, WM_CLOSE, IntPtr.Zero, IntPtr.Zero);
                pendingTasks.Add(process.WaitForExitAsync());
                processesToDispose.Add(process);
            }
            else
            {
                process.Dispose();
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error sending WM_CLOSE to process {Name}", process.ProcessName);
            process.Dispose();
        }
    }

    private async Task WaitForProcessesToCloseAsync(List<Task> pendingTasks, List<IProcess> processesToDispose, int timeoutMilliseconds)
    {
        if (_logger.IsEnabled(LogLevel.Information))
        {
            _logger.LogInformation("Waiting for the closure of {Count} programs (Timeout: {Timeout}ms)...", pendingTasks.Count, timeoutMilliseconds);
        }

        var allTasksFinished = Task.WhenAll(pendingTasks);
        var timeoutTask = Task.Delay(timeoutMilliseconds);

        var finishedTask = await Task.WhenAny(allTasksFinished, timeoutTask);

        if (finishedTask == timeoutTask)
        {
            if (_logger.IsEnabled(LogLevel.Warning))
            {
                _logger.LogWarning("Timeout reached while waiting for programs to terminate.");
            }
        }
        else
        {
            if (_logger.IsEnabled(LogLevel.Information))
            {
                _logger.LogInformation("All programs were successfully closed gracefully.");
            }
        }

        foreach (var process in processesToDispose)
        {
            process.Dispose();
        }
    }

    [LibraryImport("user32.dll", EntryPoint = "PostMessageW", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static partial bool PostMessage(IntPtr hWnd, uint Msg, IntPtr wParam, IntPtr lParam);

    public async Task StartExecutableAsync(string exeFilePath)
    {
        try
        {
            var startInfo = new ProcessStartInfo
            {
                FileName = exeFilePath,
                UseShellExecute = true
            };

            using var process = _processWrapper.Start(startInfo);

            if (process != null)
            {
                if (_logger.IsEnabled(LogLevel.Information))
                {
                    _logger.LogInformation("Executable started and waiting for termination: {Path}", exeFilePath);
                }

                await process.WaitForExitAsync();

                if (_logger.IsEnabled(LogLevel.Information))
                {
                    _logger.LogInformation("Executable was terminated: {Path}", exeFilePath);
                }
            }
            else
            {
                if (_logger.IsEnabled(LogLevel.Warning))
                {
                    _logger.LogWarning("Process could not be started (received null): {Path}", exeFilePath);
                }
            }
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException($"Error starting/waiting for the EXE: {exeFilePath}", ex);
        }
    }

    public void OpenDirectoryInFileBrowser(string folderPath)
    {
        try
        {
            string args = $"\"{folderPath}\"";

            var windowsFolder = Environment.GetFolderPath(Environment.SpecialFolder.Windows);
            var explorerPath = Path.Combine(windowsFolder, "explorer.exe");

            _processWrapper.Start(new ProcessStartInfo
            {
                FileName = explorerPath,
                Arguments = args,
                UseShellExecute = true
            });

            if (_logger.IsEnabled(LogLevel.Information))
            {
                _logger.LogInformation("Explorer opened in: {Path}", folderPath);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error opening Explorer: {Path}", folderPath);
        }
    }

    private const uint WM_CLOSE = 0x0010;
}
