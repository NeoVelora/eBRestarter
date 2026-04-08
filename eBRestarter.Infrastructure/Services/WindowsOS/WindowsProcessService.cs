using eBRestarter.Core.Application.Interfaces.OperatingSystem.WindowsOS;
using eBRestarter.Infrastructure.Wrapper.Interface;
using Microsoft.Extensions.Logging;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Runtime.Versioning;

namespace eBRestarter.Infrastructure.Services.WindowsOS;

[SupportedOSPlatform("windows")]
public class WindowsProcessService : IWindowsProcessControlService
{
    private readonly ILogger<WindowsProcessService> _logger;

    private readonly IProcessWrapper _processWrapper;

    public WindowsProcessService(ILogger<WindowsProcessService> logger, IProcessWrapper processWrapper)
    {
        _logger = logger;
        _processWrapper = processWrapper;
    }

    public void StartExecutable(string exeFilePath)
    {
        try
        {
            _processWrapper.Start(new ProcessStartInfo
            {
                FileName = exeFilePath,
                UseShellExecute = true
            });
            _logger.LogInformation("Executable gestartet: {Path}", exeFilePath);
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
                _logger.LogWarning("MSI Prozess konnte nicht gestartet werden (null): {Path}", path);
                return;
            }

            string output = process.StandardOutput.ReadToEnd();
            string error = process.StandardError.ReadToEnd();

            process.WaitForExit();

            if (!string.IsNullOrWhiteSpace(error))
            {
                _logger.LogWarning("MSI Installer Fehler-Output: {Error}", error);
            }

            if (!string.IsNullOrWhiteSpace(output))
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
            _logger.LogInformation("Executable gestartet: {Path} mit Arguments: {Args}", exeFilePath, arguments);
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

    public void CloseAllOpenPrograms()
    {
        var processes = _processWrapper.GetProcesses();

        foreach (var process in processes)
        {
            using (process)
            {
                if (process.ProcessName == "System" || process.ProcessName == "Idle")
                    continue;

                try
                {
                    if (process.MainWindowHandle != IntPtr.Zero)
                    {
                        PostMessage(process.MainWindowHandle, WM_CLOSE, IntPtr.Zero, IntPtr.Zero);
                        bool exited = process.WaitForExit(5000);

                        if (!exited)
                        {
                            _logger.LogWarning("Prozess {Name} hat auf WM_CLOSE nicht reagiert.", process.ProcessName);
                        }
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Fehler beim Schließen von Prozess {Name}", process.ProcessName);
                }
            }
        }
    }


    [DllImport("user32.dll", SetLastError = true)]
    private static extern bool PostMessage(IntPtr hWnd, uint Msg, IntPtr wParam, IntPtr lParam);

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
                _logger.LogInformation("Executable gestartet und warte auf Beendeung: {Path}", exeFilePath);

                await process.WaitForExitAsync();

                _logger.LogInformation("Executable wurde beendet: {Path}", exeFilePath);
            }
            else
            {
                _logger.LogWarning("Prozess konnte nicht gestartet werden (null zurückerhalten): {Path}", exeFilePath);
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

            string windowsFolder = Environment.GetFolderPath(Environment.SpecialFolder.Windows);
            string explorerPath = Path.Combine(windowsFolder, "explorer.exe");

            _processWrapper.Start(new ProcessStartInfo
            {
                FileName = explorerPath,
                Arguments = args,
                UseShellExecute = true
            });

            _logger.LogInformation("Explorer geöffnet in: {Path}", folderPath);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Fehler beim Öffnen des Explorers: {Path}", folderPath);
        }
    }

    private const uint WM_CLOSE = 0x0010;
}
