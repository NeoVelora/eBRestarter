using eBRestarter.Desktop.WinUI3.Services;
using eBRestarter.Desktop.WinUI3.Services.Interfaces;
using Microsoft.Extensions.DependencyInjection;

namespace eBRestarter.Desktop.WinUI3.DependencyInjections;

public static class ThemeHandlerExtension
{
    public static IServiceCollection AddThemeHandler(this IServiceCollection services)
    {
        services.AddSingleton<IThemeHandler, ThemeHandler>();
        return services;
    }
}
