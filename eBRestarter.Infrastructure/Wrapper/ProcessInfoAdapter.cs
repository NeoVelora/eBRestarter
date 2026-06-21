using eBRestarter.Core.Application.Interfaces.OperatingSystem.WindowsOS.Process;

namespace eBRestarter.Infrastructure.Wrapper;

public class ProcessInfoAdapter : IProcessInfoUseCase
{
    public ProcessInfoAdapter()
    {
    }

    public string GetCurrentExecutablePath()
    {
        return Environment.ProcessPath!;
    }
}
