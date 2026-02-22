using eBRestarter.Infrastructure.Wrapper.Interface;
using System.Diagnostics;

namespace eBRestarter.Infrastructure.Wrapper;

public class RealProcessWrapper : IProcessWrapper
{
    public Process? Start(ProcessStartInfo info) => Process.Start(info);

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

    public Process[] GetProcesses() => Process.GetProcesses();
}
