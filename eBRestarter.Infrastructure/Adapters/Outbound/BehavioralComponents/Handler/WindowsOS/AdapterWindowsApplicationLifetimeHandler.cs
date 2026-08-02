using eBRestarter.Core.Application.Ports.Outbound.Interfaces.Application;

namespace eBRestarter.Infrastructure.Adapters.Outbound.BehavioralComponents.Handler.WindowsOS;

public sealed class AdapterWindowsApplicationLifetimeHandler : IOutboundPortApplicationLifetime
{
    public void ExitApplication(int exitCode)
    {
        Environment.Exit(exitCode);
    }
}
