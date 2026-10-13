using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.UI.Xaml;
using Microsoft.Windows.Globalization;
using System;
using System.Globalization;
using System.Threading;
using System.Threading.Tasks;

using eBRestarter.Core.Application.Common.Attributes;
using eBRestarter.Core.Application.ObjectArchetypes.Constants;
using eBRestarter.Core.Application.ObjectArchetypes.DTOs.Records;
using eBRestarter.Core.Application.Ports.Inbound.Interfaces.Providers;
using eBRestarter.Core.Application.Ports.Inbound.Interfaces.Services;
using eBRestarter.Core.Application.Ports.Inbound.Interfaces.UseCases;
using eBRestarter.Desktop.WinUI3.BehavioralComponents.Extensions.DependencyInjections;
using eBRestarter.Desktop.WinUI3.BehavioralComponents.Handler;
using eBRestarter.Desktop.WinUI3.BehavioralComponents.Handler.Interfaces;
using eBRestarter.Desktop.WinUI3.BehavioralComponents.Providers;
using eBRestarter.Desktop.WinUI3.BehavioralComponents.Providers.Interfaces;
using eBRestarter.Desktop.WinUI3.BehavioralComponents.Services.Interfaces;
using eBRestarter.Desktop.WinUI3.BehavioralComponents.Utilities;
using eBRestarter.Desktop.WinUI3.BehavioralComponents.Utilities.Interfaces;
using eBRestarter.Desktop.WinUI3.BehavioralUIComponents.Helpers;
using eBRestarter.Desktop.WinUI3.BehavioralUIComponents.Helpers.Interfaces;
using eBRestarter.Desktop.WinUI3.Extensions.DependencyInjections;
using eBRestarter.Desktop.WinUI3.ObjectArchetypes.Enums;
using eBRestarter.Infrastructure.BehavioralComponents.Extensions.DependencyInjection;

namespace eBRestarter.Desktop.WinUI3;

public sealed partial class App : Application
{
    private const string AppSettingsFileName = "appsettings.json";
    private const string DefaultLanguageCode = "en-US";
    private const string DefaultThemeName = "Light";

    public static IHost? AppHost { get; private set; }

    public static string? StartupErrorMessage { get; private set; }

    public static Microsoft.UI.Dispatching.DispatcherQueue? AppDispatcherQueue { get; private set; }

    public static Window? MainWindoweBRestarter { get; private set; }


    public App()
    {
        InitializeComponent();

        AppHost = CreateHostBuilder().Build();

        RegisterGlobalExceptionHandlers();
    }


    [ExceptionBoundary(ExceptionBoundaryKind.ProcessEntry, Justification = "Process entry point / launch lifecycle boundary")]
    protected override void OnLaunched(LaunchActivatedEventArgs args)
    {
        var logger = AppHost!.Services.GetRequiredService<ILogger<App>>();

        StartupDisplayPreferences? launchConfig = null;

        try
        {
            launchConfig = AppHost!.Services.GetRequiredService<IInboundPortStartupConfigProvider>().RetrieveStartupPreferences();
            _ = AppHost!.Services.GetRequiredService<IUseCaseInitializeBrowserCleanup>().ExecuteAsync();
            string languageCode = launchConfig.LanguageCode;

            ApplicationLanguages.PrimaryLanguageOverride = languageCode;

            CultureInfo culture = new(languageCode);
            Thread.CurrentThread.CurrentCulture = culture;
            Thread.CurrentThread.CurrentUICulture = culture;
            CultureInfo.DefaultThreadCurrentCulture = culture;
            CultureInfo.DefaultThreadCurrentUICulture = culture;
        }
        catch (Exception ex)
        {
            logger.LogWarning(
                LogEventIds.UserInterface.StartupConfigurationFailed,
                ex,
                "Loading the startup configuration failed; falling back to the default language.");

            ApplicationLanguages.PrimaryLanguageOverride = DefaultLanguageCode;
        }

        string? schedulerStartError = null;

        try
        {
            var restartScheduler = AppHost!.Services.GetRequiredService<IInboundPortComputerRestartService>();
            restartScheduler.StartScheduler();
        }
        catch (Exception ex)
        {
            logger.LogError(
                LogEventIds.UserInterface.SchedulerStartFailed,
                ex,
                "Starting the computer restart scheduler failed; scheduled restarts will not run.");

            StartupErrorMessage = ex.Message;
            schedulerStartError = ex.Message;
        }

        MainWindoweBRestarter = AppHost!.Services.GetRequiredService<EBRestarter>();
        AppDispatcherQueue = MainWindoweBRestarter.DispatcherQueue;

        MainWindoweBRestarter.Closed += OnMainWindowClosed;

        try
        {
            var themeService = AppHost!.Services.GetRequiredService<IThemeHandler>();
            string themeToSet = launchConfig != null && !string.IsNullOrEmpty(launchConfig.ThemeName)
                ? launchConfig.ThemeName
                : DefaultThemeName;

            themeService.SetTheme(themeToSet);
        }
        catch (Exception ex)
        {
            logger.LogWarning(
                LogEventIds.UserInterface.ThemeLoadFailed,
                ex,
                "Applying the configured theme failed; the default theme stays active.");
        }

        MainWindoweBRestarter.Activate();

        if (schedulerStartError is not null)
        {
            AppDispatcherQueue?.TryEnqueue(async () =>
            {
                try
                {
                    var dialogService = AppHost!.Services.GetRequiredService<IDialogService>();
                    var localizationService = AppHost!.Services.GetRequiredService<IInboundPortLocalizationProvider>();
                    string title = localizationService.RetrieveString("Startup_SchedulerError_Title") ?? "Initialisierungsfehler";
                    string message = localizationService.RetrieveString("Startup_SchedulerError_Message") ?? "Der automatische Neustart-Planer konnte nicht gestartet werden. Geplante Neustarts werden nicht ausgeführt.";
                    await dialogService.ShowMessageAsync(title, message, DialogIcon.Error);
                }
                catch (Exception dialogEx)
                {
                    logger.LogError(
                        LogEventIds.UserInterface.ViewModelOperationFailed,
                        dialogEx,
                        "Displaying the startup scheduler failure dialog failed.");
                }
            });
        }
    }

    private static void RegisterGlobalExceptionHandlers()
    {
        Current.UnhandledException += OnUnhandledException;
        TaskScheduler.UnobservedTaskException += OnUnobservedTaskException;
    }

    private static void OnUnhandledException(object sender, Microsoft.UI.Xaml.UnhandledExceptionEventArgs args)
    {
        var logger = AppHost?.Services.GetService<ILogger<App>>();

        logger?.LogCritical(
            LogEventIds.UserInterface.UnhandledUiException,
            args.Exception,
            "Unhandled exception reached the UI thread; the application is terminating.");
    }

    private static void OnUnobservedTaskException(object? sender, UnobservedTaskExceptionEventArgs args)
    {
        var logger = AppHost?.Services.GetService<ILogger<App>>();

        foreach (var innerException in args.Exception.Flatten().InnerExceptions)
        {
            logger?.LogError(
                LogEventIds.RestarterCycle.FireAndForgetTaskFaulted,
                innerException,
                "A fire-and-forget task faulted and its result was never observed.");
        }

        args.SetObserved();
    }

    private static void OnMainWindowClosed(object sender, WindowEventArgs args)
    {
        var logger = AppHost?.Services.GetService<ILogger<App>>();

        try
        {
            if (sender is Window closedWindow)
            {
                closedWindow.Closed -= OnMainWindowClosed;
            }

            AppHost?.Dispose();
        }
        catch (Exception ex)
        {
            logger?.LogError(
                LogEventIds.UserInterface.HostDisposeFailed,
                ex,
                "Disposing the application host during shutdown failed.");
        }
    }

    private static IHostBuilder CreateHostBuilder()
    {
        return Host.CreateDefaultBuilder()
            .ConfigureAppConfiguration((_, cfg) =>
            {
                cfg.Sources.Clear();
                cfg.SetBasePath(AppContext.BaseDirectory);
                cfg.AddJsonFile(AppSettingsFileName, optional: false, reloadOnChange: true);
            })
            .ConfigureServices((context, services) =>
            {
                services.AddApplicationLogging(context.Configuration);

                services.AddInfrastructureServices();
                services.AddApplicationServices();
                services.AddNavigationService();
                services.AddDialogServiceExtensions();
                services.AddThemeHandler();

                services.AddViewModels();

                services.AddSingleton<ILanguageHandler, LanguageHandler>();
                services.AddSingleton<IInboundPortLocalizationProvider, LocalizationProvider>();
                services.AddSingleton<IUIOptionsProvider>(sp => (LocalizationProvider)sp.GetRequiredService<IInboundPortLocalizationProvider>());
                services.AddSingleton<IIconCreditProvider, IconCreditProvider>();
                services.AddSingleton<IBrowserDisplayNameResolverUtility, BrowserDisplayNameResolverUtility>();

                services.AddSingleton<IMainWindowProvider, MainWindowProvider>();
                services.AddSingleton<IAppWindowHelper, AppWindowHelper>();
            });
    }
}