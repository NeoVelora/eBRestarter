using eBRestarter.Infrastructure.Wrapper.Interface;
using System.Diagnostics;

namespace eBRestarter.Infrastructure.Wrapper;

public class ProcessInfoService : IProcessInfoService
{
    public string GetCurrentExecutablePath()
    {
        return Process.GetCurrentProcess().MainModule!.FileName;
    }
}
