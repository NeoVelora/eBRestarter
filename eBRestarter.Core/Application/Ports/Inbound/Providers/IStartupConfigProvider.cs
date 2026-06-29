using eBRestarter.Core.Application.Models;

namespace eBRestarter.Core.Application.Ports.Inbound.Providers;

public interface IStartupConfigProvider
{
    StartupDisplayPreferences PrepareConfigForLaunch();
}

