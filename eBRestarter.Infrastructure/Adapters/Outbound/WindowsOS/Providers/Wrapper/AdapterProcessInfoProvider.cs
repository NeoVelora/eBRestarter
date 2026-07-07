using eBRestarter.Core.Application.Ports.Outbound.Interfaces.OperatingSystem;

namespace eBRestarter.Infrastructure.Adapters.Outbound.WindowsOS.Providers.Wrapper;

public sealed class AdapterProcessInfoProvider : IOutboundPortProcessInfoProvider
{
    public AdapterProcessInfoProvider()
    {
    }

    public string GetCurrentExecutablePath()
    {
        return Environment.ProcessPath!;
    }
}


