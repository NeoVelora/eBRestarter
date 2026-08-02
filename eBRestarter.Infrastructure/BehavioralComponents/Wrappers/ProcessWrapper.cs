using System;
using System.Diagnostics;

namespace eBRestarter.Infrastructure.BehavioralComponents.Wrappers;

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
        try
        {
            return processes.Length > 0;
        }
        finally
        {
            foreach (var process in processes)
            {
                process.Dispose();
            }
        }
    }

    public void KillProcess(string name)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);

        var processes = Process.GetProcessesByName(name);
        foreach (var process in processes)
        {
            try
            {
                process.Kill();
            }
            finally
            {
                process.Dispose();
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
