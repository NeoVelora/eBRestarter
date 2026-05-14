using eBRestarter.Core.Application.Interfaces.OperatingSystem.WindowsOS;
using eBRestarter.Core.Application.Interfaces.OperatingSystem.WindowsOS.Process;
using Microsoft.Extensions.Logging;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Runtime.Versioning;

namespace eBRestarter.Infrastructure.Services.WindowsOS;

[SupportedOSPlatform("windows")]
public partial class WindowsProcessService(ILogger<WindowsProcessService> logger, IProcessWrapper processWrapper) : IWindowsProcessControlService
{
    private readonly ILogger<WindowsProcessService> _logger = logger;

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
                _logger.LogInformation("Executable gestartet: {Path}", exeFilePath);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Fehler beim Starten der EXE: {Path}", exeFilePath);
        }
    }

    public void StartMsiFile(string path)
    {
        string systemFolder = Environment.GetFolderPath(Environment.SpecialFolder.System);
        string msiExecPath = Path.Combine(systemFolder, "msiexec.exe");

        var startInfo = new ProcessStartInfo
        {
            FileName = msiExecPath,
            Arguments = $"/i \"{path}\"",

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
                    _logger.LogWarning("MSI Prozess konnte nicht gestartet werden (null): {Path}", path);
                }
                return;
            }

            string output = process.StandardOutput.ReadToEnd();
            string error = process.StandardError.ReadToEnd();

            process.WaitForExit();

            if (!string.IsNullOrWhiteSpace(error) && _logger.IsEnabled(LogLevel.Warning))
            {
                _logger.LogWarning("MSI Installer Fehler-Output: {Error}", error);
            }

            if (!string.IsNullOrWhiteSpace(output) && _logger.IsEnabled(LogLevel.Debug))
            {
                _logger.LogDebug("MSI Installer Output: {Output}", output);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Fehler beim Starten des MSI-Installers: {Path}", path);
        }
    }

    public void OpenUrlInBrowser(string exeFilePath, string arguments = "")
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
                _logger.LogInformation("Executable gestartet: {Path} mit Arguments: {Args}", exeFilePath, arguments);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Fehler beim Starten der EXE: {Path}", exeFilePath);
        }
    }

    public void ShutdownComputer()
    {
        try
        {
            _logger.LogInformation("Fahre Computer herunter (Neustart)...");

            string systemFolder = Environment.GetFolderPath(Environment.SpecialFolder.System);
            string shutdownPath = Path.Combine(systemFolder, "shutdown.exe");

            _processWrapper.Start(new ProcessStartInfo(shutdownPath, "/r /f /t 0") { UseShellExecute = true });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Fehler beim Versuch, den Computer herunterzufahren.");
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
            _logger.LogError(ex, "Fehler beim Beenden von {Name}", processName);
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
            _logger.LogError(ex, "Fehler beim Senden von WM_CLOSE an Prozess {Name}", process.ProcessName);
            process.Dispose();
        }
    }

    private async Task WaitForProcessesToCloseAsync(List<Task> pendingTasks, List<IProcess> processesToDispose, int timeoutMilliseconds)
    {
        if (_logger.IsEnabled(LogLevel.Information))
        {
            _logger.LogInformation("Warte auf das Schließen von {Count} Programmen (Timeout: {Timeout}ms)...", pendingTasks.Count, timeoutMilliseconds);
        }

        var allTasksFinished = Task.WhenAll(pendingTasks);
        var timeoutTask = Task.Delay(timeoutMilliseconds);

        var finishedTask = await Task.WhenAny(allTasksFinished, timeoutTask);

        if (finishedTask == timeoutTask)
        {
            if (_logger.IsEnabled(LogLevel.Warning))
            {
                _logger.LogWarning("Timeout beim Warten auf das Beenden der Programme erreicht.");
            }
        }
        else
        {
            if (_logger.IsEnabled(LogLevel.Information))
            {
                _logger.LogInformation("Alle Programme wurden erfolgreich sanft geschlossen.");
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
                    _logger.LogInformation("Executable gestartet und warte auf Beendeung: {Path}", exeFilePath);
                }

                await process.WaitForExitAsync();

                if (_logger.IsEnabled(LogLevel.Information))
                {
                    _logger.LogInformation("Executable wurde beendet: {Path}", exeFilePath);
                }
            }
            else
            {
                if (_logger.IsEnabled(LogLevel.Warning))
                {
                    _logger.LogWarning("Prozess konnte nicht gestartet werden (null zurückerhalten): {Path}", exeFilePath);
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Fehler beim Starten/Warten der EXE: {Path}", exeFilePath);
            throw;
        }
    }

    public void OpenExplorer(string folderPath)
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
                _logger.LogInformation("Explorer geöffnet in: {Path}", folderPath);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Fehler beim Öffnen des Explorers: {Path}", folderPath);
        }
    }

    private const uint WM_CLOSE = 0x0010;
}
