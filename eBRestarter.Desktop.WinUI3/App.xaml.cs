using eBRestarter.Core.Application.DependencyInjections;
using eBRestarter.Core.Application.Interfaces.Config;
using eBRestarter.Desktop.WinUI3.DependencyInjections;
using eBRestarter.Desktop.WinUI3.Services;
using eBRestarter.Desktop.WinUI3.Services.Interfaces;
using eBRestarter.Desktop.WinUI3.Views;
using eBRestarter.Infrastructure.DependencyInjection;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.UI.Xaml;
using System;

namespace eBRestarter.Desktop.WinUI3
{
    public partial class App : Application
    {
        public static Window? MainWindoweBRestarter { get; private set; }
        public static IHost? AppHost { get; private set; }
        public static Microsoft.UI.Dispatching.DispatcherQueue? AppDispatcherQueue { get; private set; }

        public App()
        {
            InitializeComponent();

            AppHost = CreateHostBuilder().Build();

        }

        private static IHostBuilder CreateHostBuilder() => Host.CreateDefaultBuilder()
             .ConfigureAppConfiguration((ctx, cfg) =>
             {
                 cfg.Sources.Clear();
                 cfg.SetBasePath(AppContext.BaseDirectory);
                 cfg.AddJsonFile("appsettings.json", optional: false, reloadOnChange: true);
             })
             .ConfigureServices((ctx, services) =>
             {
                 services.AddInfrastructureServices();
                 services.AddApplicationServices();
                 services.AddNavigationService();
                 services.AddDialoglServiceExtensions();
                 services.AddViewModels();

                 services.AddSingleton<IThemeService, ThemeService>();
             });

        protected override void OnLaunched(LaunchActivatedEventArgs args)
        {
            MainWindoweBRestarter = AppHost!.Services.GetRequiredService<EBRestarter>();

            AppDispatcherQueue = MainWindoweBRestarter.DispatcherQueue;

            try
            {
                var configService = AppHost.Services.GetRequiredService<IEVisitorConfigService>();
                var themeService = AppHost.Services.GetRequiredService<IThemeService>();

                var config = configService.LoadConfig();

                string themeToSet = string.IsNullOrEmpty(config.Settings.Theme) ? "Light" : config.Settings.Theme;

                themeService.SetTheme(themeToSet);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Fehler beim Laden des Themes: {ex.Message}");
            }

            MainWindoweBRestarter.Activate();
        }
    }
}