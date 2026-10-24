namespace eBRestarter.Core.Application.Ports.Outbound.OperatingSystem;

public interface IOutboundPortOsEditionProvider
{
    Task<string> RetrieveOsEditionAsync();
}
