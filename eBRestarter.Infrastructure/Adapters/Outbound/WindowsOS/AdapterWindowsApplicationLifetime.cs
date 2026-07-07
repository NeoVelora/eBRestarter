using eBRestarter.Core.Application.Ports.Outbound.Interfaces.Application;

namespace eBRestarter.Infrastructure.Adapters.WindowsOS;

public sealed class AdapterWindowsApplicationLifetime : IOutboundPortApplicationLifetime
{
    public AdapterWindowsApplicationLifetime()
    {
    }

    public void ExitApplication(int exitCode)
    {
        Environment.Exit(exitCode);
    }
}
