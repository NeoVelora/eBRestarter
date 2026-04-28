using eBRestarter.Core.Application.Models;

namespace eBRestarter.Core.Application.Interfaces;

public interface IApplicationLaunchConfigService
{
    StartupDisplayPreferences PrepareConfigForLaunch();
}
