using eBRestarter.Core.Application.ObjectArchetypes.DTOs.Records;

namespace eBRestarter.Core.Application.Ports.Outbound.Interfaces.OperatingSystem;

public interface IOutboundPortHardwareInfoProvider
{
    Task<HardwareInfo> RetrieveHardwareInfoAsync();
}


