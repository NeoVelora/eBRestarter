using eBRestarter.Core.Application.BehavioralComponents.Handlers;
using eBRestarter.Core.Application.BehavioralComponents.Providers;
using eBRestarter.Core.Application.BehavioralComponents.Services;
using eBRestarter.Core.Application.Ports.Inbound.Handlers;
using eBRestarter.Core.Application.Ports.Inbound.Providers;
using eBRestarter.Core.Application.Ports.Inbound.Services;
using eBRestarter.Core.Application.Ports.Inbound.UseCases.AutoLogon;
using eBRestarter.Core.Application.Ports.Inbound.UseCases.Browser;
using eBRestarter.Core.Application.Ports.Inbound.UseCases.Credentials;
using eBRestarter.Core.Application.Ports.Inbound.UseCases.System;
using eBRestarter.Core.Application.Ports.Inbound.UseCases.Update;
using eBRestarter.Core.Domain.Services;
using eBRestarter.Core.Domain.Validators;
using Microsoft.Extensions.DependencyInjection;

namespace eBRestarter.Infrastructure.BehavioralComponents.Extensions.DependencyInjection;

public static class ApplicationServiceRegistration
{
    public static IServiceCollection AddApplicationServices(this IServiceCollection services)
    {
        services.AddSingleton<IDelayPhaseHandler, DelayPhaseHandler>();
        services.AddSingleton<IRunBrowserPhaseHandler, RunBrowserPhaseHandler>();

        services.AddSingleton<IRestartCalculationService, RestartCalculationService>();
        services.AddSingleton<IBrowserCleanupScheduleService, BrowserCleanupScheduleService>();

        services.AddSingleton<IInboundPortNextRestartDateHandler, NextRestartDateHandler>();
        services.AddSingleton<IInboundPortStartupConfigProvider, StartupConfigProvider>();
        services.AddSingleton<IInboundPortRestartTaskDisplayStateHandler, RestartTaskDisplayStateHandler>();
        services.AddSingleton<IInboundPortSystemInformationProvider, SystemInformationProvider>();

        services.AddTransient<DeleteBrowserContentUseCase>();
        services.AddTransient<InitializeBrowserCleanupUseCase>();
        services.AddSingleton<IInboundPortRestarterCycleService, RestarterCycleService>();
        services.AddSingleton<IInboundPortComputerRestartService, ComputerRestartService>();
        services.AddSingleton<ConfigureAutoLogonUseCase>();
        services.AddSingleton<ToggleAppAutoStartUseCase>();
        services.AddTransient<ToggleEdgeStartupBoostUseCase>();
        services.AddSingleton<ManageApplicationUpdatesUseCase>();
        services.AddSingleton<DownloadBrowserUseCase>();
        services.AddSingleton<ScheduleBrowserCleanupUseCase>();
        services.AddSingleton<RemoveApiCredentialsUseCase>();

        services.AddSingleton<ICacheDeletionIntervalValidator, CacheDeletionIntervalValidator>();

        return services;
    }
}
