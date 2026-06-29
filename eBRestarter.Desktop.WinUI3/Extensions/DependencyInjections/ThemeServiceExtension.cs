using eBRestarter.Desktop.WinUI3.Services;
using eBRestarter.Desktop.WinUI3.Services.Interfaces;
using Microsoft.Extensions.DependencyInjection;

namespace eBRestarter.Desktop.WinUI3.Extensions.DependencyInjections;

public static class ThemeServiceExtension
{
    public static IServiceCollection AddThemeHandler(this IServiceCollection services)
    {
        services.AddSingleton<IThemeService, ThemeService>();
        return services;
    }
}
