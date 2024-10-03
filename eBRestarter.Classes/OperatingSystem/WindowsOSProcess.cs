using Serilog;
using System.ComponentModel;
using System.Diagnostics;
using System.Runtime.InteropServices;

namespace eBRestarter.Classes.OperatingSystem
{
    public partial class WindowsOS
    {
        public Process? Process { get; set; }

        public void StartMSIFile(string installerMSIFilePath)
        {
            ProcessStartInfo startInfo = new()
            {
                FileName = "msiexec.exe",
                Arguments = $"/i \"{installerMSIFilePath}\"",
                UseShellExecute = false,
                RedirectStandardOutput = true,
                RedirectStandardError = true
            };

            try
            {
                using Process process = Process.Start(startInfo)!;
                using (StreamReader output = process.StandardOutput)
                {
                    string outputText = output.ReadToEnd();
                }

                using (StreamReader error = process.StandardError)
                {
                    string errorText = error.ReadToEnd();
                }

                process.WaitForExit();
            }
            catch (Exception)
            {
            }
        }

        public void StartExecute(string installerEXEFilePath)
        {
            ProcessStartInfo startInfo = new()
            {
                FileName = installerEXEFilePath,
                Arguments = string.Empty,
                UseShellExecute = true,
                RedirectStandardOutput = false,
                RedirectStandardError = false
            };

            try
            {
                using Process process = Process.Start(startInfo)!;

                process.WaitForExit();
            }
            catch (Exception ex)
            {
                Debug.WriteLine("Fehler beim Starten der Anwendung: " + ex.Message);
            }
        }

        public void OpenLinkWithStandardbrowser(string url)
        {
            try
            {
                Process.Start(new ProcessStartInfo
                {
                    FileName = url,
                    UseShellExecute = true
                });
            }
            catch (Exception ex)
            {
                Debug.WriteLine("An error occurred: " + ex.Message);
            }
        }

        public bool ProcessAlive(string name)
        {
            if (Process.GetProcessesByName(name).Length > 0)
            {
                return true;
            }
            else
            {
                return false;
            }
        }

        public void StopProcess(string nameOfProcess)
        {
            try
            {
                foreach (var process in Process.GetProcessesByName(nameOfProcess))
                {
                    try
                    {
                        process.Kill();
                    }
                    catch (Win32Exception)
                    {
                    }
                }
            }
            catch (IOException e)
            {
                Log.Error(e, "Es ist eine Ausnahme im WindowsOSbereich aufgetreten. Logging...");
            }
        }


        public void CloseApplication(string nameOfApplication)
        {

            if (ProcessAlive(nameOfApplication) == true)
            {
                StopProcess(nameOfApplication);
            }
        }


        public void CloseAllApplications()
        {
            Process me = Process.GetCurrentProcess();
            foreach (Process p in Process.GetProcesses())
            {
                if (p.Id != me.Id && IntPtr.Zero != p.MainWindowHandle && false == p.ProcessName.Equals("explorer", StringComparison.CurrentCultureIgnoreCase))
                {
                    p.CloseMainWindow();
                }
            }
        }


        [DllImport("user32.dll", SetLastError = true)]
        private static extern bool PostMessage(IntPtr hWnd, uint Msg, IntPtr wParam, IntPtr lParam);

        private const uint WM_CLOSE = 0x0010;

        public void CloseAllOpenPrograms()
        {
            var processes = Process.GetProcesses();

            foreach (var process in processes)
            {
                if (process.ProcessName == "System" || process.ProcessName == "Idle")
                    continue;

                try
                {
                    if (process.MainWindowHandle != IntPtr.Zero)
                    {
                        PostMessage(process.MainWindowHandle, WM_CLOSE, IntPtr.Zero, IntPtr.Zero);
                        process.WaitForExit(5000);
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error closing process {process.ProcessName}: {ex.Message}");
                }
            }

        }

        public void ShutdownComputer()
        {
            Process.Start("shutdown", "/r /f /t 0");
        }

        public void CloseProgramsAndShutdown()
        {
            CloseAllOpenPrograms();
            ShutdownComputer();
        }

    }
}
