using System;
using System.Diagnostics;
using System.IO;
using System.Threading.Tasks;

namespace eBRestarter.Infrastructure.BehavioralComponents.Wrappers;

public sealed class ProcessAdapter(
    Process process)
    : IProcess
{

    private readonly Process _process = process ?? throw new ArgumentNullException(nameof(process));


    public string ProcessName => _process.ProcessName;

    public IntPtr MainWindowHandle => _process.MainWindowHandle;

    public StreamReader StandardError => _process.StandardError;

    public StreamReader StandardOutput => _process.StandardOutput;


    public void Dispose() => _process.Dispose();

    public void WaitForExit() => _process.WaitForExit();

    public bool WaitForExit(int milliseconds) => _process.WaitForExit(milliseconds);

    public Task WaitForExitAsync() => _process.WaitForExitAsync();
}
