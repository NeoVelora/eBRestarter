using eBRestarter.Core.Application.Interfaces.OperatingSystem.WindowsOS.Process;

namespace eBRestarter.Infrastructure.Wrapper;

public class ProcessInfoService : IProcessInfoService
{
    public string GetCurrentExecutablePath()
    {
        return Environment.ProcessPath!;
    }
}
