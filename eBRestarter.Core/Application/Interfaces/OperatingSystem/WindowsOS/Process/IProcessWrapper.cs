using System.Diagnostics;

namespace eBRestarter.Core.Application.Interfaces.OperatingSystem.WindowsOS.Process;

public interface IProcessWrapper
{
    IProcess? Start(ProcessStartInfo info);

    bool IsProcessRunning(string name);

    void KillProcess(string name);

    IProcess[] GetProcesses();
}
