using eBRestarter.Application.Services.Ports.Interfaces;
using eBRestarter.Infrastructure.Wrapper.Interface;
using Microsoft.Extensions.Logging;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;
using System.Runtime.Versioning;

namespace eBRestarter.Infrastructure.Services.WindowsOS
{
    [SupportedOSPlatform("windows")]
    public class WindowsProcessService : IProcessControlService
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
            var startInfo = new ProcessStartInfo
            {
                FileName = "msiexec.exe",
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

        public void OpenUrlInBrowser(string url)
        {
            try
            {
                _processWrapper.Start(new ProcessStartInfo
                {
                    FileName = url,
                    UseShellExecute = true
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Fehler beim Öffnen der URL: {Url}", url);
            }
        }

        public void ShutdownComputer()
        {
            try
            {
                _logger.LogInformation("Fahre Computer herunter (Neustart)...");
                _processWrapper.Start(new ProcessStartInfo("shutdown", "/r /f /t 0") { UseShellExecute = true });
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


        [DllImport("user32.dll", SetLastError = true)]
        private static extern bool PostMessage(IntPtr hWnd, uint Msg, IntPtr wParam, IntPtr lParam);

        private const uint WM_CLOSE = 0x0010;
    }
}
