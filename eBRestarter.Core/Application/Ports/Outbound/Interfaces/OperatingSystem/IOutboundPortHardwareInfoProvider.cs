using eBRestarter.Core.Application.Models.Records;

namespace eBRestarter.Core.Application.Ports.Outbound.Interfaces.OperatingSystem;

public interface IOutboundPortHardwareInfoProvider
{
    Task<HardwareInfo> RetrieveHardwareInfoAsync();
}


