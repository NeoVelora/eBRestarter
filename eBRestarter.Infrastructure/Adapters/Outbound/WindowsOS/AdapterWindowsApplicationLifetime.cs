using eBRestarter.Core.Application.Ports.Outbound.Interfaces.Application;

namespace eBRestarter.Infrastructure.Adapters.Outbound.WindowsOS;

public sealed class AdapterWindowsApplicationLifetime : IOutboundPortApplicationLifetime
{
    public void ExitApplication(int exitCode)
    {
        Environment.Exit(exitCode);
    }
}
