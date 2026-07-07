namespace eBRestarter.Core.Application.Ports.Outbound.Interfaces.OperatingSystem;

public interface IOutboundPortOsEditionProvider
{
    Task<string> RetrieveOsEditionAsync();
}
