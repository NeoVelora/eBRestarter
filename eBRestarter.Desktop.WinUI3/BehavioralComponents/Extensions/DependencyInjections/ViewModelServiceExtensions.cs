using System;

using eBRestarter.Desktop.WinUI3.ViewModels;
using Microsoft.Extensions.DependencyInjection;

namespace eBRestarter.Desktop.WinUI3.BehavioralComponents.Extensions.DependencyInjections;

public static class ViewModelServiceExtensions
{
    public static IServiceCollection AddViewModels(this IServiceCollection services)
    {
        services.AddSingleton<EBRestarter>();
        services.AddSingleton<ViewModelRestartTask>();
        services.AddSingleton<ViewModelRestarterProperties>();
        services.AddSingleton<ViewModelGeneralOverview>();
        services.AddSingleton<ViewModelInstalledBrowsers>();
        services.AddSingleton<ViewModelOptionsGeneral>();
        services.AddSingleton<ViewModelOptionsApi>();
        services.AddSingleton<ViewModelOptionsExtension>();
        services.AddSingleton<ViewModelInfocenter>();

        services.AddTransient<ViewModelAbout>();
        services.AddTransient<ViewModelDeleteBrowserContent>();
        services.AddTransient<ViewModelActivateApi>();
        services.AddTransient<ViewModelTurnOffEdgeStartupBoost>();

        services.AddSingleton<Func<ViewModelNetworkTraffic>>(
            static serviceProvider => () => ActivatorUtilities.CreateInstance<ViewModelNetworkTraffic>(serviceProvider));

        services.AddSingleton<Func<ViewModelInstallAddOn>>(
            static serviceProvider => () => ActivatorUtilities.CreateInstance<ViewModelInstallAddOn>(serviceProvider));

        return services;
    }
}
