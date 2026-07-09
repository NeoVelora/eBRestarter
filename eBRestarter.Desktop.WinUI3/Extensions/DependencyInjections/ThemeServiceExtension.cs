using eBRestarter.Desktop.WinUI3.Handler;
using eBRestarter.Desktop.WinUI3.Handler.Interfaces;
using Microsoft.Extensions.DependencyInjection;

namespace eBRestarter.Desktop.WinUI3.Extensions.DependencyInjections;

public static class ThemeServiceExtension
{
    public static IServiceCollection AddThemeHandler(this IServiceCollection services)
    {
        services.AddSingleton<IThemeHandler, ThemeHandler>();
        return services;
    }
}
