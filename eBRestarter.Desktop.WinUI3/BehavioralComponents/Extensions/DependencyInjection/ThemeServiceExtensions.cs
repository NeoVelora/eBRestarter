using eBRestarter.Desktop.WinUI3.BehavioralComponents.Handlers;
using eBRestarter.Desktop.WinUI3.BehavioralComponents.Handlers.Interfaces;
using Microsoft.Extensions.DependencyInjection;

namespace eBRestarter.Desktop.WinUI3.BehavioralComponents.Extensions.DependencyInjection;

public static class ThemeServiceExtensions
{
    public static IServiceCollection AddThemeHandler(this IServiceCollection services)
    {
        services.AddSingleton<IThemeHandler, ThemeHandler>();

        return services;
    }
}
