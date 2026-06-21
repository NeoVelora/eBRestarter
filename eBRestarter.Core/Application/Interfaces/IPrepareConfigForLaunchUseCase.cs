using eBRestarter.Core.Application.Models;

namespace eBRestarter.Core.Application.Interfaces;

public interface IPrepareConfigForLaunchUseCase
{
    StartupDisplayPreferences PrepareConfigForLaunch();
}
