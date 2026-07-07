using eBRestarter.Core.Application.Models.Records;

namespace eBRestarter.Core.Application.Ports.Inbound.Providers;

public interface IInboundPortStartupConfigProvider
{
    StartupDisplayPreferences RetrieveStartupPreferences();
}

