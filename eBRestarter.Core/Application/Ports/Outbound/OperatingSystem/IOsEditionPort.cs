namespace eBRestarter.Core.Application.Ports.Outbound.OperatingSystem;

public interface IOsEditionPort
{
    Task<string> RetrieveOsEditionAsync();
}


