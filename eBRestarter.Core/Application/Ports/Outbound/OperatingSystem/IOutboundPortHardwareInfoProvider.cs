using eBRestarter.Core.Application.ObjectArchetypes.DTOs.System;

namespace eBRestarter.Core.Application.Ports.Outbound.OperatingSystem;

public interface IOutboundPortHardwareInfoProvider
{
    Task<HardwareInfo> RetrieveHardwareInfoAsync();
}


