using eBRestarter.Core.Application.ObjectArchetypes.DTOs.Records;

namespace eBRestarter.Core.Application.Ports.Inbound.Interfaces.Providers;

public interface IInboundPortStartupConfigProvider
{
    StartupDisplayPreferences RetrieveStartupPreferences();
}

