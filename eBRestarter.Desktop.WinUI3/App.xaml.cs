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
using eBRestarter.Core.Application.ObjectArchetypes.DTOs.System;
using eBRestarter.Core.Application.Ports.Inbound.Providers;
using eBRestarter.Core.Application.Ports.Inbound.Services;
using eBRestarter.Core.Application.Ports.Inbound.UseCases.Browser;
using eBRestarter.Desktop.WinUI3.BehavioralComponents.Extensions;
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
            AppHost!.Services.GetRequiredService<InitializeBrowserCleanupUseCase>()
                .ExecuteAsync()
                .Forget(logger, nameof(InitializeBrowserCleanupUseCase));
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
            LogStartupConfigurationFailed(logger, ex);

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
            LogSchedulerStartFailed(logger, ex);

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
            LogThemeLoadFailed(logger, ex);
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
                    LogStartupSchedulerDialogFailed(logger, dialogEx);
                }
            });
        }
    }

    private static void RegisterGlobalExceptionHandlers()
    {
        Current.UnhandledException += OnUnhandledException;
        TaskScheduler.UnobservedTaskException += OnUnobservedTaskException;
        AppDomain.CurrentDomain.UnhandledException += OnAppDomainUnhandledException;
    }

    private static void OnUnhandledException(object sender, Microsoft.UI.Xaml.UnhandledExceptionEventArgs args)
    {
        var logger = AppHost?.Services.GetService<ILogger<App>>();
        if (logger is not null)
        {
            LogUnhandledUiException(logger, args.Exception);
        }
    }

    private static void OnUnobservedTaskException(object? sender, UnobservedTaskExceptionEventArgs args)
    {
        var logger = AppHost?.Services.GetService<ILogger<App>>();

        foreach (var innerException in args.Exception.Flatten().InnerExceptions)
        {
            if (logger is not null)
            {
                LogUnobservedTaskException(logger, innerException);
            }
        }

        args.SetObserved();
    }

    private static void OnAppDomainUnhandledException(object sender, System.UnhandledExceptionEventArgs args)
    {
        var logger = AppHost?.Services.GetService<ILogger<App>>();

        if (logger is not null)
        {
            if (args.ExceptionObject is Exception ex)
            {
                LogUnhandledAppDomainException(logger, ex, args.IsTerminating);
            }
            else
            {
                LogUnhandledAppDomainObject(logger, args.ExceptionObject?.ToString(), args.IsTerminating);
            }
        }

        try
        {
            AppHost?.Dispose();
        }
        catch
        {
        }
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
            if (logger is not null)
            {
                LogHostDisposeFailed(logger, ex);
            }
        }
    }


    [LoggerMessage(
        EventId = LogEventIds.UserInterface.StartupConfigurationFailed,
        Level = LogLevel.Warning,
        Message = "Loading the startup configuration failed; falling back to the default language.")]
    private static partial void LogStartupConfigurationFailed(ILogger logger, Exception exception);

    [LoggerMessage(
        EventId = LogEventIds.UserInterface.SchedulerStartFailed,
        Level = LogLevel.Error,
        Message = "Starting the computer restart scheduler failed; scheduled restarts will not run.")]
    private static partial void LogSchedulerStartFailed(ILogger logger, Exception exception);

    [LoggerMessage(
        EventId = LogEventIds.UserInterface.ThemeLoadFailed,
        Level = LogLevel.Warning,
        Message = "Applying the configured theme failed; the default theme stays active.")]
    private static partial void LogThemeLoadFailed(ILogger logger, Exception exception);

    [LoggerMessage(
        EventId = LogEventIds.UserInterface.ViewModelOperationFailed,
        Level = LogLevel.Error,
        Message = "Displaying the startup scheduler failure dialog failed.")]
    private static partial void LogStartupSchedulerDialogFailed(ILogger logger, Exception exception);

    [LoggerMessage(
        EventId = LogEventIds.UserInterface.UnhandledUiException,
        Level = LogLevel.Critical,
        Message = "Unhandled exception reached the UI thread; the application is terminating.")]
    private static partial void LogUnhandledUiException(ILogger logger, Exception? exception);

    [LoggerMessage(
        EventId = LogEventIds.RestarterCycle.FireAndForgetTaskFaulted,
        Level = LogLevel.Error,
        Message = "A fire-and-forget task faulted and its result was never observed.")]
    private static partial void LogUnobservedTaskException(ILogger logger, Exception exception);

    [LoggerMessage(
        EventId = LogEventIds.UserInterface.UnhandledAppDomainException,
        Level = LogLevel.Critical,
        Message = "Unhandled exception occurred in AppDomain (IsTerminating: {IsTerminating}); the application is terminating.")]
    private static partial void LogUnhandledAppDomainException(ILogger logger, Exception exception, bool isTerminating);

    [LoggerMessage(
        EventId = LogEventIds.UserInterface.UnhandledAppDomainObject,
        Level = LogLevel.Critical,
        Message = "Unhandled non-Exception object occurred in AppDomain: {ExceptionObject} (IsTerminating: {IsTerminating}); the application is terminating.")]
    private static partial void LogUnhandledAppDomainObject(ILogger logger, string? exceptionObject, bool isTerminating);

    [LoggerMessage(
        EventId = LogEventIds.UserInterface.HostDisposeFailed,
        Level = LogLevel.Error,
        Message = "Disposing the application host during shutdown failed.")]
    private static partial void LogHostDisposeFailed(ILogger logger, Exception exception);

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