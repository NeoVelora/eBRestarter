using eBRestarter.Desktop.WinUI3.ViewModels;
using Microsoft.Extensions.DependencyInjection;

namespace eBRestarter.Desktop.WinUI3.DependencyInjections;

public static class ViewModelServiceExtensions
{
    public static IServiceCollection AddViewModels(this IServiceCollection services)
    {
        services.AddSingleton<EBRestarter>();
        services.AddSingleton<ViewModelRestartTask>();
        services.AddSingleton<ViewModelRestarterProperties>();
        services.AddSingleton<ViewModelGeneralOverview>();
        services.AddSingleton<ViewModelInstalledBrowsers>();
        services.AddSingleton<ViewModelOptions>();
        services.AddSingleton<ViewModelInfocenter>();
        services.AddTransient<ViewModelAbout>();

        services.AddTransient<ViewModelNetworkTraffic>();
        services.AddTransient<ViewModelDeleteBrowserContent>();
        services.AddTransient<ViewModelInstallAddOn>();
        services.AddTransient<ViewModelActivateApi>();
        services.AddTransient<ViewModelTurnOffEdgeStartupBoost>();

        return services;
    }
}
