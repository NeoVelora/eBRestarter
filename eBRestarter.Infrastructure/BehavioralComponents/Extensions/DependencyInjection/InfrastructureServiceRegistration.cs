using System;
using eBRestarter.Core.Application.ObjectArchetypes.Enums;
using eBRestarter.Core.Application.Ports.Inbound.Providers;
using eBRestarter.Core.Application.Ports.Inbound.Validators;
using eBRestarter.Core.Application.Ports.Outbound.Application;
using eBRestarter.Core.Application.Ports.Outbound.Authentication;
using eBRestarter.Core.Application.Ports.Outbound.Browser;
using eBRestarter.Core.Application.Ports.Outbound.Config;
using eBRestarter.Core.Application.Ports.Outbound.Logging;
using eBRestarter.Core.Application.Ports.Outbound.Network;
using eBRestarter.Core.Application.Ports.Outbound.OperatingSystem;
using eBRestarter.Core.Application.Ports.Outbound.Security;
using eBRestarter.Core.Application.Ports.Outbound.Update;
using eBRestarter.Infrastructure.Adapters.Outbound.BehavioralComponents.Factories;
using eBRestarter.Infrastructure.Adapters.Outbound.BehavioralComponents.Handlers.Http;
using eBRestarter.Infrastructure.Adapters.Outbound.BehavioralComponents.Handlers.WindowsOS;
using eBRestarter.Infrastructure.Adapters.Outbound.BehavioralComponents.Providers.API;
using eBRestarter.Infrastructure.Adapters.Outbound.BehavioralComponents.Providers.API.Authentication;
using eBRestarter.Infrastructure.Adapters.Outbound.BehavioralComponents.Providers.WindowsOS;
using eBRestarter.Infrastructure.Adapters.Outbound.BehavioralComponents.Providers.WindowsOS.Authentication;
using eBRestarter.Infrastructure.Adapters.Outbound.BehavioralComponents.Repositories.Authentication;
using eBRestarter.Infrastructure.Adapters.Outbound.BehavioralComponents.Repositories.Config;
using eBRestarter.Infrastructure.Adapters.Outbound.BehavioralComponents.Repositories.WindowsOS;
using eBRestarter.Infrastructure.Adapters.Outbound.BehavioralComponents.Services.Update;
using eBRestarter.Infrastructure.Adapters.Outbound.BehavioralComponents.Services.WindowsOS;
using eBRestarter.Infrastructure.Adapters.Outbound.BehavioralComponents.Utilities;
using eBRestarter.Infrastructure.Adapters.Outbound.BehavioralComponents.Wrappers.Browsers;
using eBRestarter.Infrastructure.Adapters.Outbound.BehavioralComponents.Wrappers.Logging;
using eBRestarter.Infrastructure.Adapters.Outbound.BehavioralComponents.Wrappers.WindowsOS;
using eBRestarter.Infrastructure.BehavioralComponents.Clients.Http;
using eBRestarter.Infrastructure.BehavioralComponents.Providers;
using eBRestarter.Infrastructure.BehavioralComponents.Validators;
using eBRestarter.Infrastructure.BehavioralComponents.Wrappers.WindowsOS;
using FluentValidation;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace eBRestarter.Infrastructure.BehavioralComponents.Extensions.DependencyInjection;

public static class InfrastructureServiceRegistration
{
    public static IServiceCollection AddInfrastructureServices(this IServiceCollection services)
    {
        services.AddSingleton<IProcessWrapper, ProcessWrapper>();
        services.AddSingleton<IOutboundPortOsProcessControl, AdapterWindowsProcessControlWrapper>();
        services.AddSingleton<IOutboundPortFileSystem, AdapterWindowsFileSystem>();
        services.AddSingleton<IOutboundPortApplicationLifetime, AdapterWindowsApplicationLifetimeHandler>();
        services.AddSingleton<AdapterWindowsBrowserExtensionDeploymentProvider>();
        services.AddSingleton<IOutboundPortBrowserExtensionDeployment>(sp => sp.GetRequiredService<AdapterWindowsBrowserExtensionDeploymentProvider>());
        services.AddSingleton<IOutboundPortBrowserExtensionPathProvider>(sp => sp.GetRequiredService<AdapterWindowsBrowserExtensionDeploymentProvider>());
        services.AddSingleton<IOutboundPortUpdate, AdapterGitHubUpdateService>();
        services.AddSingleton<IOutboundPortFileDeletion, AdapterWindowsFileDeletionService>();
        services.AddSingleton<IOutboundPortEncryption, AdapterWindowsEncryptionUtility>();

        services.AddSingleton<IOutboundPortExecutableSignatureVerifier, AdapterWindowsAuthenticodeVerifier>();
        services.AddSingleton<IOutboundPortBrowserFactory, AdapterBrowserFactory>();
        services.AddSingleton<IOutboundPortBrowserDiscoveryProvider, AdapterWindowsBrowserDiscoveryProvider>();
        services.AddSingleton<IOutboundPortSystemInfoProvider, AdapterWindowsSystemInfoProvider>();
        services.AddSingleton<IOutboundPortEVisitorApiProvider, AdapterEVisitorApiProvider>();

        services.AddSingleton<IOutboundPortProcessInfoProvider, AdapterProcessInfoProvider>();
        services.AddSingleton<IOutboundPortAppPathProvider, AdapterWindowsAppPathProvider>();
        services.AddSingleton<IOutboundPortHardwareInfoProvider>(provider => provider.GetRequiredService<AdapterWmiHardwareProvider>());
        services.AddSingleton<IOutboundPortOsEditionProvider>(provider => provider.GetRequiredService<AdapterWmiHardwareProvider>());
        services.AddSingleton<IOutboundPortNetworkProvider, AdapterWindowsNetworkProvider>();
        services.AddSingleton<IInboundPortNetworkInfoProvider, NetworkInfoProvider>();
        services.AddSingleton<IInboundPortOsAppPathProvider, WindowsAppPathProvider>();
        services.AddSingleton<AdapterWmiHardwareProvider>();

        services.AddTransient<AdapterChromeBrowserWrapper>();
        services.AddTransient<AdapterFirefoxBrowserWrapper>();
        services.AddTransient<AdapterEdgeBrowserWrapper>();
        services.AddTransient<AdapterBraveBrowserWrapper>();
        services.AddTransient<AdapterVivaldiBrowserWrapper>();

        services.AddKeyedTransient<IOutboundPortBrowser, AdapterChromeBrowserWrapper>(BrowserType.Chrome);
        services.AddKeyedTransient<IOutboundPortBrowser, AdapterFirefoxBrowserWrapper>(BrowserType.Firefox);
        services.AddKeyedTransient<IOutboundPortBrowser, AdapterEdgeBrowserWrapper>(BrowserType.Edge);
        services.AddKeyedTransient<IOutboundPortBrowser, AdapterBraveBrowserWrapper>(BrowserType.Brave);
        services.AddKeyedTransient<IOutboundPortBrowser, AdapterVivaldiBrowserWrapper>(BrowserType.Vivaldi);

        services.AddHttpClient(RestSharpClient.HttpClientName, client =>
                {
                    client.MaxResponseContentBufferSize = RestSharpClient.MaxAllowedResponseSizeBytes;
                })
                .SetHandlerLifetime(TimeSpan.FromMinutes(5))
                .AddStandardResilienceHandler();

        services.AddHttpClient(AdapterHttpClientDownloadHandler.HttpClientName)
                .SetHandlerLifetime(TimeSpan.FromMinutes(5));

        services.AddSingleton<System.Threading.RateLimiting.RateLimiter>(_ => new System.Threading.RateLimiting.TokenBucketRateLimiter(new System.Threading.RateLimiting.TokenBucketRateLimiterOptions
        {
            TokenLimit = 30,
            TokensPerPeriod = 30,
            ReplenishmentPeriod = TimeSpan.FromMinutes(1),
            QueueLimit = 100,
            QueueProcessingOrder = System.Threading.RateLimiting.QueueProcessingOrder.OldestFirst,
            AutoReplenishment = true
        }));

        services.AddSingleton<IOutboundPortHttpDownload>(static serviceProvider =>
            new AdapterHttpClientDownloadHandler(serviceProvider.GetRequiredService<System.Net.Http.IHttpClientFactory>()));

        services.AddSingleton<IRestClient, RestSharpClient>();
        services.AddSingleton<IOutboundPortApiAuthenticationProvider, AdapterEVisitorApiAuthenticationProvider>();
        services.AddSingleton<IOutboundPortActiveDirectoryProvider, AdapterWindowsActiveDirectoryProvider>();
        services.AddSingleton<IOutboundPortCredentialValidationProvider, AdapterWindowsCredentialValidationProvider>();
        services.AddSingleton<IOutboundPortAppVersionInfoProvider, AdapterWindowsAppVersionInfoProvider>();

        services.AddSingleton(TimeProvider.System);
        services.AddSingleton<IOutboundPortSystemConfigurationRepository, AdapterWindowsRegistryRepository>();
        services.AddSingleton<IOutboundPortAutoStartRepository, AdapterWindowsStartupRepository>();
        services.AddSingleton<IOutboundPortBrowserConfigRepository, AdapterWindowsStartupRepository>();
        services.AddSingleton<IOutboundPortOsAutoLogonRepository, AdapterWindowsAutoLogonRepository>();
        services.AddSingleton<AdapterEVRestarterConfigRepository>();

        services.AddSingleton<IOutboundPortEVisitorConfigRepository>(provider =>
            new AdapterEncryptedEVisitorConfigRepositoryDecorator(
                provider.GetRequiredService<AdapterEVRestarterConfigRepository>(),
                provider.GetRequiredService<IOutboundPortEncryption>(),
                provider.GetRequiredService<ILogger<AdapterEncryptedEVisitorConfigRepositoryDecorator>>()
            ));

        services.AddSingleton<IOutboundPortCredentialStoreRepository, AdapterJsonCredentialStoreRepository>();

        services.AddValidatorsFromAssemblyContaining<ConfigureAutoLogonValidator>();
        services.AddTransient(typeof(IInboundPortApplicationValidator<>), typeof(AdapterFluentValidationWrapper<>));
        services.AddSingleton(typeof(IOutboundPortApplicationLogger<>), typeof(AdapterMicrosoftExtensionsLoggerWrapper<>));

        return services;
    }
}
