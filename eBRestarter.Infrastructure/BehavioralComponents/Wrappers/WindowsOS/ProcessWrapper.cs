using System;
using System.Diagnostics;

namespace eBRestarter.Infrastructure.BehavioralComponents.Wrappers.WindowsOS;

public sealed class ProcessWrapper : IProcessWrapper
{

    public IProcess[] GetProcesses()
    {
        var processes = Process.GetProcesses();

        return Array.ConvertAll(processes, static process => (IProcess)new ProcessAdapter(process));
    }

    public bool IsProcessRunning(string name)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);

        var processes = Process.GetProcessesByName(name);

        int runningCount = processes.Length;

        foreach (var process in processes)
        {
            process.Dispose();
        }

        return runningCount > 0;
    }

    public void KillProcess(string name)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);

        var processes = Process.GetProcessesByName(name);

        foreach (var process in processes)
        {
            using (process)
            {
                process.Kill();
            }
        }
    }

    public IProcess? Start(ProcessStartInfo info)
    {
        ArgumentNullException.ThrowIfNull(info);

        var process = Process.Start(info);

        return process is not null ? new ProcessAdapter(process) : null;
    }
}

