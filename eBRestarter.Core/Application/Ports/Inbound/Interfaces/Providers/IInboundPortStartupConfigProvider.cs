using eBRestarter.Core.Application.Models.Records;

namespace eBRestarter.Core.Application.Ports.Inbound.Interfaces.Providers;

public interface IInboundPortStartupConfigProvider
{
    StartupDisplayPreferences RetrieveStartupPreferences();
}

