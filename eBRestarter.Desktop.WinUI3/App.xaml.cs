using eBRestarter.Core.Application.DependencyInjections;
using eBRestarter.Core.Application.Interfaces;
using eBRestarter.Core.Application.Interfaces.Config;
using eBRestarter.Desktop.WinUI3.DependencyInjections;
using eBRestarter.Desktop.WinUI3.Services;
using eBRestarter.Desktop.WinUI3.Services.Interfaces;
using eBRestarter.Infrastructure.DependencyInjection;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.UI.Xaml;
using Microsoft.Windows.Globalization;
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
             .ConfigureServices((_, services) =>
             {
                 services.AddInfrastructureServices();
                 services.AddApplicationServices();
                 services.AddNavigationService();
                 services.AddDialoglServiceExtensions();
                 services.AddThemeService();
                 services.AddViewModels();

                 services.AddSingleton<ILanguageService, LanguageService>();
                 services.AddSingleton<ILocalizationService, LocalizationService>();
             });

        protected override async void OnLaunched(LaunchActivatedEventArgs args)
        {
            if (await WinUI3XamlPreview.Preview.IsXamlPreviewLaunched())
            {
                return;
            }

            try
            {
                var configService = AppHost!.Services.GetRequiredService<IEVisitorConfigService>();
                var config = configService.LoadConfig();

                int intervalDays = config.Browser?.DeleteBrowserCacheIntervalDays ?? 0;
                DateTime nextDate = config.Browser?.NextBrowserDeleteCacheDate ?? DateTime.MinValue;

                if (nextDate == DateTime.Today && intervalDays > 0)
                    nextDate = DateTime.Today.AddDays(intervalDays);

                config.Browser?.NextBrowserDeleteCacheDate = nextDate;
                configService.SaveConfig(config);

                string languageCode = config.Settings.Language == 0 ? "de-DE" : "en-US";

                ApplicationLanguages.PrimaryLanguageOverride = languageCode;

                System.Globalization.CultureInfo culture = new System.Globalization.CultureInfo(languageCode);
                System.Threading.Thread.CurrentThread.CurrentCulture = culture;
                System.Threading.Thread.CurrentThread.CurrentUICulture = culture;
                System.Globalization.CultureInfo.DefaultThreadCurrentCulture = culture;
                System.Globalization.CultureInfo.DefaultThreadCurrentUICulture = culture;
            }
            catch (Exception)
            {
                ApplicationLanguages.PrimaryLanguageOverride = "en-US";
            }

            try
            {
                var configService = AppHost!.Services.GetRequiredService<IEVisitorConfigService>();
                var themeService = AppHost.Services.GetRequiredService<IThemeService>();
                var config = configService.LoadConfig();

                string themeToSet = string.IsNullOrEmpty(config.Settings.Theme) ? "Light" : config.Settings.Theme;
                themeService.SetTheme(themeToSet);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Fehler beim Laden des Themes: {ex.Message}");
            }

            try
            {
                var restartScheduler = AppHost!.Services.GetRequiredService<IComputerRestartScheduler>();
                restartScheduler.StartScheduler();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Fehler beim Starten des ComputerRestartSchedulers: {ex.Message}");
            }

            MainWindoweBRestarter = AppHost!.Services.GetRequiredService<EBRestarter>();
            AppDispatcherQueue = MainWindoweBRestarter.DispatcherQueue;

            MainWindoweBRestarter.Activate();
        }
    }
}