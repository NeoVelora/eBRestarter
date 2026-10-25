using eBRestarter.Core.Application.Ports.Outbound.OperatingSystem;

namespace eBRestarter.Infrastructure.Adapters.Outbound.BehavioralComponents.Providers.WindowsOS;

public sealed class AdapterProcessInfoProvider : IOutboundPortProcessInfoProvider
{
    public string GetCurrentExecutablePath() => Environment.ProcessPath!;
}
