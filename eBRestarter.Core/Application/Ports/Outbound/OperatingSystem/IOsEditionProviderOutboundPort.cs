namespace eBRestarter.Core.Application.Ports.Outbound.OperatingSystem;

public interface IOsEditionProviderOutboundPort
{
    Task<string> RetrieveOsEditionAsync();
}
