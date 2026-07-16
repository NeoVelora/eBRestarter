using eBRestarter.Desktop.WinUI3.BehavioralComponents.Handler;
using eBRestarter.Desktop.WinUI3.BehavioralComponents.Handler.Interfaces;
using Microsoft.Extensions.DependencyInjection;

namespace eBRestarter.Desktop.WinUI3.BehavioralComponents.Extensions.DependencyInjections;

public static class ThemeServiceExtension
{
    public static IServiceCollection AddThemeHandler(this IServiceCollection services)
    {
        services.AddSingleton<IThemeHandler, ThemeHandler>();
        return services;
    }
}
