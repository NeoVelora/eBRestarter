using eBRestarter.Core.Application.Interfaces.OperatingSystem.WindowsOS.Process;
using System.Diagnostics;

namespace eBRestarter.Infrastructure.Wrapper;

public class RealProcessWrapper : IProcessWrapper
{
    public IProcess? Start(ProcessStartInfo info)
    {
        var process = Process.Start(info);

        if (process != null)
        {
            return new ProcessAdapter(process);
        }

        return null;
    }

    public bool IsProcessRunning(string name)
    {
        return Process.GetProcessesByName(name).Length > 0;
    }

    public void KillProcess(string name)
    {
        var processes = Process.GetProcessesByName(name);
        foreach (var process in processes)
        {
            process.Kill();
        }
    }

    public IProcess[] GetProcesses()
    {
        var processes = Process.GetProcesses();

        return processes.Select(p => (IProcess)new ProcessAdapter(p)).ToArray();
    }
}
