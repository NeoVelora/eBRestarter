using System.Diagnostics;

namespace eBRestarter.Infrastructure.BehavioralComponents.Wrappers.WindowsOS;

public interface IProcessWrapper
{
    IProcess? Start(ProcessStartInfo info);

    bool IsProcessRunning(string name);

    void KillProcess(string name);

    IProcess[] GetProcesses();
}

